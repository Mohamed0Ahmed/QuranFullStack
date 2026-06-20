import { mushafCommonLigature, MushafCommonLigatureKey } from './mushaf-common-ligature';

export function mushafJuzNumberLigature(juzNumber: number): string | null {
  if (juzNumber < 1 || juzNumber > 30) {
    return null;
  }

  const key = `juz-${juzNumber}-number` as MushafCommonLigatureKey;
  return mushafCommonLigature(key);
}
