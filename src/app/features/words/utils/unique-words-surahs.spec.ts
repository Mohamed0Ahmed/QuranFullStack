import { describe, expect, it } from 'vitest';

import { buildMissingSurahsPayload, computeMissingSurahs, getUniqueWordsSurahCatalog } from './unique-words-surahs';

describe('unique-words surah helpers', () => {
  it('exposes a deduped 114-surah catalog ordered by surah number', () => {
    const catalog = getUniqueWordsSurahCatalog();

    expect(catalog).toHaveLength(114);
    expect(catalog[0]?.surahNumber).toBe(1);
    expect(catalog[113]?.surahNumber).toBe(114);
  });

  it('computes the missing surah complement from mentioned surah numbers', () => {
    const missing = computeMissingSurahs([1, 2, 114]);

    expect(missing).toHaveLength(111);
    expect(missing.map((surah) => surah.surahNumber)).not.toContain(1);
    expect(missing.map((surah) => surah.surahNumber)).not.toContain(2);
    expect(missing.map((surah) => surah.surahNumber)).not.toContain(114);
  });

  it('builds a missing-surahs payload from a mentioned-surahs payload', () => {
    const payload = buildMissingSurahsPayload({
      id: 7,
      kind: 'tashkeel',
      displayTextUthmani: 'كلمة-تجريبية',
      surahsCount: 2,
      surahs: [
        { surahNumber: 1, nameArabic: 'الفاتحة', occurrencesInSurah: 1 },
        { surahNumber: 2, nameArabic: 'البقرة', occurrencesInSurah: 2 },
      ],
    });

    expect(payload.id).toBe(7);
    expect(payload.missingSurahsCount).toBe(112);
    expect(payload.surahs).toHaveLength(112);
  });
});
