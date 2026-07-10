import { describe, expect, it } from 'vitest';

import {
  MUSHAF_SURAH_JUZ_GROUPS,
  MUSHAF_SURAH_START_PAGES,
  resolveMushafSurahStartPage,
} from './mushaf-surah-juz-catalog';

describe('mushaf-surah-juz-catalog', () => {
  it('defines 30 ordered juz groups with 114 unique surahs', () => {
    expect(MUSHAF_SURAH_JUZ_GROUPS).toHaveLength(30);
    expect(MUSHAF_SURAH_JUZ_GROUPS.map((group) => group.juzNumber)).toEqual(
      Array.from({ length: 30 }, (_, index) => index + 1),
    );
    expect(MUSHAF_SURAH_START_PAGES.size).toBe(114);
  });

  it('keeps start pages within the Mushaf bounds', () => {
    for (const startPage of MUSHAF_SURAH_START_PAGES.values()) {
      expect(startPage).toBeGreaterThanOrEqual(1);
      expect(startPage).toBeLessThanOrEqual(604);
    }
  });

  it('uses the same start page for repeated surah entries across juz groups', () => {
    const seen = new Map<number, number>();

    for (const group of MUSHAF_SURAH_JUZ_GROUPS) {
      for (const surah of group.surahs) {
        const prior = seen.get(surah.surahNumber);
        if (prior === undefined) {
          seen.set(surah.surahNumber, surah.startPageNumber);
          continue;
        }

        expect(prior).toBe(surah.startPageNumber);
      }
    }
  });

  it('includes every surah number from 1 through 114 exactly once', () => {
    const surahNumbers = [...MUSHAF_SURAH_START_PAGES.keys()].sort((a, b) => a - b);
    expect(surahNumbers).toEqual(Array.from({ length: 114 }, (_, index) => index + 1));
  });

  it('keeps start pages in non-decreasing surah order', () => {
    let priorPage = 0;

    for (let surahNumber = 1; surahNumber <= 114; surahNumber++) {
      const startPage = MUSHAF_SURAH_START_PAGES.get(surahNumber);
      expect(startPage).toBeDefined();
      expect(startPage!).toBeGreaterThanOrEqual(priorPage);
      priorPage = startPage!;
    }
  });

  it('repeats long surahs across adjacent juz groups', () => {
    const juzOneSurahs = MUSHAF_SURAH_JUZ_GROUPS[0].surahs.map((surah) => surah.surahNumber);
    const juzTwoSurahs = MUSHAF_SURAH_JUZ_GROUPS[1].surahs.map((surah) => surah.surahNumber);

    expect(juzOneSurahs).toContain(2);
    expect(juzTwoSurahs).toContain(2);
  });

  it('resolves known surah start pages from the static map', () => {
    expect(resolveMushafSurahStartPage(1)).toBe(1);
    expect(resolveMushafSurahStartPage(2)).toBe(2);
    expect(resolveMushafSurahStartPage(114)).toBe(604);
    expect(resolveMushafSurahStartPage(999)).toBeNull();
  });
});
