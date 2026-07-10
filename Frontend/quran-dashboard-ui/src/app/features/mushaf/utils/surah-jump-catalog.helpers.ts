import { MushafSurahCatalogItemDto, MushafSurahJuzGroupDto } from '../models/mushaf.models';
import { arabicSearchIncludes } from './arabic-search-normalize';

export function dedupeSurahCatalogItems(
  groups: readonly MushafSurahJuzGroupDto[],
): MushafSurahCatalogItemDto[] {
  const seen = new Map<number, MushafSurahCatalogItemDto>();

  for (const group of groups) {
    for (const surah of group.surahs) {
      if (!seen.has(surah.surahNumber)) {
        seen.set(surah.surahNumber, surah);
      }
    }
  }

  return [...seen.values()].sort((left, right) => left.surahNumber - right.surahNumber);
}

function surahMatchesQuery(surah: MushafSurahCatalogItemDto, query: string): boolean {
  const trimmed = query.trim();
  if (!trimmed) {
    return true;
  }

  return (
    arabicSearchIncludes(surah.nameArabic, trimmed) ||
    arabicSearchIncludes(String(surah.surahNumber), trimmed)
  );
}

export function filterUniqueSurahs(
  surahs: readonly MushafSurahCatalogItemDto[],
  query: string,
): MushafSurahCatalogItemDto[] {
  const trimmed = query.trim();
  if (!trimmed) {
    return [...surahs];
  }

  return surahs.filter((surah) => surahMatchesQuery(surah, trimmed));
}
