import { LinkingAyah, LinkingAyahWord } from '../models/linking-ayah.models';

export function completeManualMushafAyah(
  ayah: Omit<LinkingAyah, 'words'>,
  wordsCount: number,
  words: readonly LinkingAyahWord[],
): LinkingAyah {
  const nonMarkerWords = words.filter((word) => !word.isAyahMarker);
  if (
    !Number.isSafeInteger(wordsCount) ||
    wordsCount < 1 ||
    nonMarkerWords.length !== wordsCount ||
    new Set(nonMarkerWords.map((word) => word.wordLocation)).size !== nonMarkerWords.length ||
    !nonMarkerWords.every((word, index) => word.wordLocation !== null && word.renderPosition === index + 1)
  ) {
    throw new Error('تعذر التحقق من اكتمال كلمات آية المصحف.');
  }

  return { ...ayah, words };
}

export function validateManualWordLocations(
  ayahs: readonly LinkingAyah[],
  locationsByVerseKey: Readonly<Record<string, readonly string[]>>,
): void {
  const wordsByVerseKey = new Map(
    ayahs.map((ayah) => [
      ayah.verseKey,
      new Set(ayah.words.filter((word) => !word.isAyahMarker).map((word) => word.wordLocation)),
    ]),
  );
  for (const [verseKey, locations] of Object.entries(locationsByVerseKey)) {
    const knownLocations = wordsByVerseKey.get(verseKey);
    if (!knownLocations || locations.some((location) => !knownLocations.has(location))) {
      throw new Error('إحداثيات كلمات المصحف المحفوظة لم تعد صالحة.');
    }
  }
}
