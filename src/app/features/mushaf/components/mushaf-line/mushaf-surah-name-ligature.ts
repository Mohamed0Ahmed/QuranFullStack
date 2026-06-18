import suraNames from '../../assets/sura-names.ligatures.json';

export function mushafSurahNameLigature(surahNumber: number): string | null {
  const key = `surah-${surahNumber}` as keyof typeof suraNames;
  return suraNames[key] ?? null;
}
