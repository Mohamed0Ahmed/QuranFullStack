export function parseVerseKey(verseKey: string): { surahNumber: number; ayahNumber: number } {
  const [surahNumber, ayahNumber] = verseKey.split(':');

  return {
    surahNumber: Number(surahNumber),
    ayahNumber: Number(ayahNumber),
  };
}
