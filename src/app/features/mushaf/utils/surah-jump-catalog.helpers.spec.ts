import { describe, expect, it } from 'vitest';

import { MushafSurahJuzGroupDto } from '../models/mushaf.models';
import {
  dedupeSurahCatalogItems,
  filterUniqueSurahs,
} from './surah-jump-catalog.helpers';

const catalogFixture: readonly MushafSurahJuzGroupDto[] = [
  {
    juzNumber: 1,
    surahs: [
      { surahNumber: 1, nameArabic: 'سورة-تجريبية-١', startPageNumber: 1 },
      { surahNumber: 2, nameArabic: 'سورة-تجريبية-٢', startPageNumber: 2 },
    ],
  },
  {
    juzNumber: 2,
    surahs: [
      { surahNumber: 2, nameArabic: 'سورة-تجريبية-٢', startPageNumber: 2 },
      { surahNumber: 3, nameArabic: 'سورة-تجريبية-٣', startPageNumber: 50 },
    ],
  },
];

describe('surah-jump-catalog.helpers', () => {
  it('dedupes surahs across juz groups ordered by surah number', () => {
    const unique = dedupeSurahCatalogItems(catalogFixture);

    expect(unique).toHaveLength(3);
    expect(unique.map((surah) => surah.surahNumber)).toEqual([1, 2, 3]);
  });

  it('filters unique surahs by Arabic name or surah number', () => {
    const unique = dedupeSurahCatalogItems(catalogFixture);

    expect(filterUniqueSurahs(unique, '2')).toHaveLength(1);
    expect(filterUniqueSurahs(unique, '2')[0].surahNumber).toBe(2);
    expect(filterUniqueSurahs(unique, 'تجريبية-١')).toHaveLength(1);
    expect(filterUniqueSurahs(unique, 'missing')).toHaveLength(0);
  });
});
