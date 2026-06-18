import commonLigatures from '../../assets/mushaf-common.ligatures.json';

export type MushafCommonLigatureKey = keyof typeof commonLigatures;

export function mushafCommonLigature(key: MushafCommonLigatureKey): string {
  return commonLigatures[key];
}

export const MUSHAF_BASMALLAH_LIGATURE = commonLigatures.bismillah;
