import { compareQuranVerseKeys, type QuranVerseKey } from '../../../shared/quran/quran-location';

export function orderedUniqueLinkingVerseKeys(
  verseKeys: readonly QuranVerseKey[],
): readonly QuranVerseKey[] {
  return [...new Set(verseKeys)].sort(compareQuranVerseKeys);
}
