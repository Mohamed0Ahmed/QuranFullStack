import { PhraseContextLinkingSelectionResponse } from '../../../../core/api/generated/models/phrase-context-linking-selection-response';
import { isCanonicalQuranWordId } from '../../../linking/models/linking-manual-mushaf.models';
import { LinkingSourceLaunch } from '../../../linking/models/linking-source-launch.models';
import { isVerseKey } from '../../../linking/models/linking-source.models';
import { PhraseContextAyahSelectionSnapshot } from '../state/phrase-context-ayah-selection.store';

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

  const sortedAyahs = [...response.ayahs].sort(compareAyahs);
  const manualAyahs = sortedAyahs.map((ayah) => ({
    verseKey: ayah.verseKey,
    pageNumber: ayah.pageNumber,
    displayHint: ayah.verseKey,
  }));
  const selectedWords = sortedAyahs.flatMap((ayah) =>
    [...new Set(ayah.selectedQuranWordIds)]
      .sort((left, right) => left - right)
      .map((quranWordId) => ({ ayahId: ayah.ayahId, quranWordId })),
  );

  if (selectedWords.length === 0) {
    return null;
  }

  return {
    source: {
      kind: 'manual-mushaf-ayahs',
      label: `البحث عن «${normalizedQuery}»`,
      contextKey: null,
      manualAyahs,
    },
    initialConfiguration: {
      inclusionMode: 'all-except',
      ayahOverrideIds: [],
      selectedWords,
      automaticWordMatchesEnabled: null,
      manualLinkShape: 'independent',
      descriptions: [],
    },
  };
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
      !isVerseKey(ayah.verseKey) ||
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
  left: PhraseContextLinkingSelectionResponse['ayahs'][number],
  right: PhraseContextLinkingSelectionResponse['ayahs'][number],
): number {
  const [leftSurah, leftAyah] = left.verseKey.split(':').map(Number);
  const [rightSurah, rightAyah] = right.verseKey.split(':').map(Number);
  return leftSurah - rightSurah || leftAyah - rightAyah || left.ayahId - right.ayahId;
}
