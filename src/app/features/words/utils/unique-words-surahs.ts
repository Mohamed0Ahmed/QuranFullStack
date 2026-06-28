import { MUSHAF_SURAH_JUZ_GROUPS } from '../../mushaf/data/mushaf-surah-juz-catalog';
import { dedupeSurahCatalogItems } from '../../mushaf/utils/surah-jump-catalog.helpers';
import {
  MissingSurahItemDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSurahsDto,
} from '../models/unique-words.models';

export interface UniqueWordSurahCatalogItem {
  surahNumber: number;
  nameArabic: string;
  startPageNumber: number;
}

const ALL_SURAHS: readonly UniqueWordSurahCatalogItem[] = dedupeSurahCatalogItems(
  MUSHAF_SURAH_JUZ_GROUPS,
);

export function computeMissingSurahs(
  mentionedSurahNumbers: readonly number[],
): MissingSurahItemDto[] {
  const mentioned = new Set(mentionedSurahNumbers);
  return ALL_SURAHS.filter((surah) => !mentioned.has(surah.surahNumber)).map((surah) => ({
    surahNumber: surah.surahNumber,
    nameArabic: surah.nameArabic,
  }));
}

export function buildMissingSurahsPayload(
  mentionedSurahs: UniqueWordSurahsDto,
): UniqueWordMissingSurahsDto {
  return {
    surahs: computeMissingSurahs(mentionedSurahs.surahs.map((surah) => surah.surahNumber)),
  };
}

export function getUniqueWordsSurahCatalog(): readonly UniqueWordSurahCatalogItem[] {
  return ALL_SURAHS;
}
