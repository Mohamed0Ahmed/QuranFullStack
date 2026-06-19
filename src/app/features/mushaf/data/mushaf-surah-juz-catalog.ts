import catalogJson from './mushaf-surah-juz-catalog.json';

import { MushafSurahJuzGroupDto } from '../models/mushaf.models';

interface MushafSurahJuzCatalogFile {
  juzGroups: MushafSurahJuzGroupDto[];
}

const catalog = catalogJson as MushafSurahJuzCatalogFile;

export const MUSHAF_SURAH_JUZ_GROUPS: readonly MushafSurahJuzGroupDto[] = catalog.juzGroups;

export const MUSHAF_SURAH_START_PAGES: ReadonlyMap<number, number> = buildStartPageMap(
  MUSHAF_SURAH_JUZ_GROUPS,
);

function buildStartPageMap(groups: readonly MushafSurahJuzGroupDto[]): Map<number, number> {
  const startPages = new Map<number, number>();

  for (const group of groups) {
    for (const surah of group.surahs) {
      const existing = startPages.get(surah.surahNumber);
      if (existing === undefined) {
        startPages.set(surah.surahNumber, surah.startPageNumber);
        continue;
      }

      if (existing !== surah.startPageNumber) {
        throw new Error(
          `Inconsistent startPageNumber for surah ${surah.surahNumber}: ${existing} vs ${surah.startPageNumber}`,
        );
      }
    }
  }

  return startPages;
}

export function resolveMushafSurahStartPage(surahNumber: number): number | null {
  return MUSHAF_SURAH_START_PAGES.get(surahNumber) ?? null;
}
