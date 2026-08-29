import { PhraseSimilarityLinkingSelectionResponse } from '../../../../core/api/generated/models/phrase-similarity-linking-selection-response';
import { isCanonicalQuranWordId } from '../../../linking/models/linking-manual-mushaf.models';
import { LinkingSourceLaunch } from '../../../linking/models/linking-source-launch.models';
import { isVerseKey } from '../../../linking/models/linking-source.models';
import { PhraseSimilarityAyahSelectionSnapshot } from '../state/phrase-similarity-ayah-selection.store';

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

  const sortedAyahs = [...response.ayahs].sort(compareAyahs);
  return {
    source: {
      kind: 'manual-mushaf-ayahs',
      label: `متشابهات العبارة «${response.query.displayText}»`,
      contextKey: null,
      manualAyahs: sortedAyahs.map((ayah) => ({
        verseKey: ayah.verseKey,
        pageNumber: ayah.pageNumber,
        displayHint: ayah.verseKey,
      })),
    },
    initialConfiguration: {
      inclusionMode: 'all-except',
      ayahOverrideIds: [],
      selectedWords: sortedAyahs.flatMap((ayah) =>
        [...ayah.selectedQuranWordIds]
          .sort((left, right) => left - right)
          .map((quranWordId) => ({ ayahId: ayah.ayahId, quranWordId })),
      ),
      automaticWordMatchesEnabled: null,
      manualLinkShape: 'independent',
      descriptions: [],
    },
  };
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
      !isVerseKey(ayah.verseKey) ||
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
  left: PhraseSimilarityLinkingSelectionResponse['ayahs'][number],
  right: PhraseSimilarityLinkingSelectionResponse['ayahs'][number],
): number {
  const [leftSurah, leftAyah] = left.verseKey.split(':').map(Number);
  const [rightSurah, rightAyah] = right.verseKey.split(':').map(Number);
  return leftSurah - rightSurah || leftAyah - rightAyah || left.ayahId - right.ayahId;
}

function sameBuild(left: string, right: string): boolean {
  return typeof left === 'string' &&
    left.trim().length > 0 &&
    left.toLowerCase() === right.toLowerCase();
}
