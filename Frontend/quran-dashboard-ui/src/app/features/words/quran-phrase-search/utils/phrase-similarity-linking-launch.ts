import { PhraseSimilarityLinkingSelectionResponse } from '../../../../core/api/generated/models/phrase-similarity-linking-selection-response';
import { isCanonicalQuranWordId } from '../../../linking/models/linking-manual-mushaf.models';
import { LinkingSourceLaunch } from '../../../linking/models/linking-source-launch.models';
import { ManualLinkingSourceFactory } from '../../../linking/utils/manual-linking-source.factory';
import { PhraseSimilarityAyahSelectionSnapshot } from '../state/phrase-similarity-ayah-selection.store';
import {
  compareQuranVerseKeys,
  parseQuranVerseKey,
  type QuranVerseKey,
} from '../../../../shared/quran/quran-location';

type CanonicalSimilarityAyah = PhraseSimilarityLinkingSelectionResponse['ayahs'][number] & {
  readonly verseKey: QuranVerseKey;
};

export interface PhraseSimilarityLinkingPopulationSnapshot {
  readonly resultSetKey: string;
  readonly routeQuery: string;
  readonly activeBuildId: string;
  readonly resolutionRef: string;
  readonly minimumMatchedWords: number;
  readonly queryVariantId: number;
  readonly queryDisplayText: string;
  readonly queryWordCount: number;
  readonly selection: PhraseSimilarityAyahSelectionSnapshot;
}

export function createPhraseSimilarityLinkingLaunch(
  response: PhraseSimilarityLinkingSelectionResponse,
  snapshot: PhraseSimilarityLinkingPopulationSnapshot,
): LinkingSourceLaunch | null {
  if (!isCompleteResponse(response, snapshot)) {
    return null;
  }

  const canonicalAyahs = canonicalSimilarityAyahs(response.ayahs);
  if (canonicalAyahs === null) {
    return null;
  }
  const sortedAyahs = canonicalAyahs.sort(compareAyahs);
  return ManualLinkingSourceFactory.createLaunch({
    label: `متشابهات العبارة «${response.query.displayText}»`,
    contextKey: null,
    verseKeys: sortedAyahs.map((ayah) => ayah.verseKey),
    selectedWords: sortedAyahs.flatMap((ayah) =>
      [...ayah.selectedQuranWordIds]
        .sort((left, right) => left - right)
        .map((quranWordId) => ({ ayahId: ayah.ayahId, quranWordId })),
    ),
    configuration: 'explicit',
  });
}

function isCompleteResponse(
  response: PhraseSimilarityLinkingSelectionResponse,
  snapshot: PhraseSimilarityLinkingPopulationSnapshot,
): boolean {
  if (
    !sameBuild(response.activeBuildId, snapshot.activeBuildId) ||
    !isMatchingQuery(response, snapshot) ||
    !Number.isSafeInteger(response.selectedAyahCount) ||
    response.selectedAyahCount <= 0 ||
    response.selectedAyahCount !== snapshot.selection.selectedCount ||
    !Array.isArray(response.ayahs) ||
    response.ayahs.length !== response.selectedAyahCount
  ) {
    return false;
  }

  const ayahIds = new Set<number>();
  const verseKeys = new Set<string>();
  for (const ayah of response.ayahs) {
    if (ayah === null || typeof ayah !== 'object' || !Array.isArray(ayah.selectedQuranWordIds)) {
      return false;
    }
    const wordIds = new Set(ayah.selectedQuranWordIds);
    if (
      !Number.isSafeInteger(ayah.ayahId) ||
      ayah.ayahId <= 0 ||
      ayahIds.has(ayah.ayahId) ||
      !isCanonicalVerseKey(ayah.verseKey) ||
      verseKeys.has(ayah.verseKey) ||
      !Number.isSafeInteger(ayah.pageNumber) ||
      ayah.pageNumber < 1 ||
      ayah.pageNumber > 604 ||
      ayah.selectedQuranWordIds.length === 0 ||
      wordIds.size !== ayah.selectedQuranWordIds.length ||
      !ayah.selectedQuranWordIds.every(isCanonicalQuranWordId)
    ) {
      return false;
    }
    ayahIds.add(ayah.ayahId);
    verseKeys.add(ayah.verseKey);
  }

  const overrides = new Set(snapshot.selection.ayahIds);
  return snapshot.selection.mode === 'only'
    ? overrides.size === ayahIds.size && [...overrides].every((ayahId) => ayahIds.has(ayahId))
    : [...overrides].every((ayahId) => !ayahIds.has(ayahId));
}

function isMatchingQuery(
  response: PhraseSimilarityLinkingSelectionResponse,
  snapshot: PhraseSimilarityLinkingPopulationSnapshot,
): boolean {
  const query = response.query;
  return query !== null &&
    typeof query === 'object' &&
    Number.isSafeInteger(query.variantId) &&
    query.variantId > 0 &&
    query.variantId === snapshot.queryVariantId &&
    typeof query.displayText === 'string' &&
    query.displayText.trim().length > 0 &&
    query.displayText === query.displayText.trim() &&
    query.displayText === snapshot.queryDisplayText &&
    Number.isSafeInteger(query.wordCount) &&
    query.wordCount > 0 &&
    query.wordCount === snapshot.queryWordCount;
}

function compareAyahs(
  left: CanonicalSimilarityAyah,
  right: CanonicalSimilarityAyah,
): number {
  return compareQuranVerseKeys(left.verseKey, right.verseKey) || left.ayahId - right.ayahId;
}

function canonicalSimilarityAyahs(
  ayahs: PhraseSimilarityLinkingSelectionResponse['ayahs'],
): CanonicalSimilarityAyah[] | null {
  const parsed = ayahs.map((ayah) => {
    const verse = parseQuranVerseKey(ayah.verseKey);
    return verse && verse.key === ayah.verseKey ? { ...ayah, verseKey: verse.key } : null;
  });
  return parsed.some((ayah) => ayah === null)
    ? null
    : parsed.filter((ayah): ayah is CanonicalSimilarityAyah => ayah !== null);
}

function isCanonicalVerseKey(value: unknown): boolean {
  const parsed = parseQuranVerseKey(value);
  return parsed !== null && parsed.key === value;
}

function sameBuild(left: string, right: string): boolean {
  return typeof left === 'string' &&
    left.trim().length > 0 &&
    left.toLowerCase() === right.toLowerCase();
}
