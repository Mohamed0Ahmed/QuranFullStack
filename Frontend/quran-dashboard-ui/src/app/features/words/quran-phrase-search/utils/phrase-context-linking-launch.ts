import { PhraseContextLinkingSelectionResponse } from '../../../../core/api/generated/models/phrase-context-linking-selection-response';
import { isCanonicalQuranWordId } from '../../../linking/models/linking-manual-mushaf.models';
import { LinkingSourceLaunch } from '../../../linking/models/linking-source-launch.models';
import { ManualLinkingSourceFactory } from '../../../linking/utils/manual-linking-source.factory';
import { PhraseContextAyahSelectionSnapshot } from '../state/phrase-context-ayah-selection.store';
import {
  compareQuranVerseKeys,
  parseQuranVerseKey,
  type QuranVerseKey,
} from '../../../../shared/quran/quran-location';

type CanonicalContextAyah = PhraseContextLinkingSelectionResponse['ayahs'][number] & {
  readonly verseKey: QuranVerseKey;
};

export function createPhraseContextLinkingLaunch(
  response: PhraseContextLinkingSelectionResponse,
  query: string,
  selection: PhraseContextAyahSelectionSnapshot,
): LinkingSourceLaunch | null {
  const normalizedQuery = query.trim();
  if (
    normalizedQuery.length === 0 ||
    !isCompleteResponse(response, selection)
  ) {
    return null;
  }

  const canonicalAyahs = canonicalContextAyahs(response.ayahs);
  if (canonicalAyahs === null) {
    return null;
  }
  const sortedAyahs = canonicalAyahs.sort(compareAyahs);
  const selectedWords = sortedAyahs.flatMap((ayah) =>
    [...new Set(ayah.selectedQuranWordIds)]
      .sort((left, right) => left - right)
      .map((quranWordId) => ({ ayahId: ayah.ayahId, quranWordId })),
  );

  if (selectedWords.length === 0) {
    return null;
  }

  return ManualLinkingSourceFactory.createLaunch({
    label: `البحث عن «${normalizedQuery}»`,
    contextKey: null,
    verseKeys: sortedAyahs.map((ayah) => ayah.verseKey),
    selectedWords,
    configuration: 'explicit',
  });
}

function isCompleteResponse(
  response: PhraseContextLinkingSelectionResponse,
  selection: PhraseContextAyahSelectionSnapshot,
): boolean {
  if (
    response.activeBuildId.trim().length === 0 ||
    !Number.isSafeInteger(response.selectedAyahCount) ||
    response.selectedAyahCount <= 0 ||
    response.selectedAyahCount !== selection.selectedCount ||
    response.ayahs.length !== response.selectedAyahCount
  ) {
    return false;
  }

  const ayahIds = new Set<number>();
  const verseKeys = new Set<string>();
  for (const ayah of response.ayahs) {
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
      !ayah.selectedQuranWordIds.every(isCanonicalQuranWordId)
    ) {
      return false;
    }
    ayahIds.add(ayah.ayahId);
    verseKeys.add(ayah.verseKey);
  }

  const overrides = new Set(selection.ayahIds);
  return selection.mode === 'only'
    ? overrides.size === ayahIds.size && [...overrides].every((ayahId) => ayahIds.has(ayahId))
    : [...overrides].every((ayahId) => !ayahIds.has(ayahId));
}

function compareAyahs(
  left: CanonicalContextAyah,
  right: CanonicalContextAyah,
): number {
  return compareQuranVerseKeys(left.verseKey, right.verseKey) || left.ayahId - right.ayahId;
}

function canonicalContextAyahs(
  ayahs: PhraseContextLinkingSelectionResponse['ayahs'],
): CanonicalContextAyah[] | null {
  const parsed = ayahs.map((ayah) => {
    const verse = parseQuranVerseKey(ayah.verseKey);
    return verse && verse.key === ayah.verseKey ? { ...ayah, verseKey: verse.key } : null;
  });
  return parsed.some((ayah) => ayah === null)
    ? null
    : parsed.filter((ayah): ayah is CanonicalContextAyah => ayah !== null);
}

function isCanonicalVerseKey(value: unknown): boolean {
  const parsed = parseQuranVerseKey(value);
  return parsed !== null && parsed.key === value;
}
