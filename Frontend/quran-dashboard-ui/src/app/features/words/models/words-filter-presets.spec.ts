import { describe, expect, it } from 'vitest';

import { LEMMAS_RANGE_METRICS } from './lemmas.models';
import { ROOTS_RANGE_METRICS } from './roots.models';
import { STEMS_RANGE_METRICS } from './stems.models';
import { UNIQUE_WORDS_RANGE_METRICS } from './unique-words.models';
import {
  buildRangeChips,
  countRangesEqual,
  isRangeActive,
  parseCountRange,
  resolveRangeThreshold,
  serializeCountRange,
} from './words-filter-presets';
import { RangeMetric } from '../state/words-range-filters';

const EXPLORER_METRICS: Readonly<Record<string, readonly RangeMetric[]>> = {
  roots: ROOTS_RANGE_METRICS,
  lemmas: LEMMAS_RANGE_METRICS,
  stems: STEMS_RANGE_METRICS,
  'unique-words': UNIQUE_WORDS_RANGE_METRICS,
};

function thresholdOf(metrics: readonly RangeMetric[], key: string): number {
  const metric = metrics.find((candidate) => candidate.key === key)!;
  return resolveRangeThreshold(metric.family, metric.threshold);
}

describe('words-filter-presets range grammar', () => {
  describe('parseCountRange (fail-closed)', () => {
    it('parses a closed range', () => {
      expect(parseCountRange('11..100')).toEqual({ min: 11, max: 100 });
    });

    it('parses open-ended bounds on either side', () => {
      expect(parseCountRange('5..')).toEqual({ min: 5, max: null });
      expect(parseCountRange('..10')).toEqual({ min: null, max: 10 });
    });

    it('parses an equal-bounds range', () => {
      expect(parseCountRange('5..5')).toEqual({ min: 5, max: 5 });
    });

    it('treats a missing separator as absent', () => {
      expect(parseCountRange('42')).toBeNull();
    });

    it('treats an empty range as absent', () => {
      expect(parseCountRange('..')).toBeNull();
      expect(parseCountRange('')).toBeNull();
      expect(parseCountRange(null)).toBeNull();
    });

    it('fails closed on min greater than max', () => {
      expect(parseCountRange('9..2')).toBeNull();
    });

    it('fails closed on non-numeric or negative bounds', () => {
      expect(parseCountRange('a..10')).toBeNull();
      expect(parseCountRange('-1..10')).toBeNull();
      expect(parseCountRange('1..x')).toBeNull();
    });
  });

  describe('serializeCountRange', () => {
    it('round-trips a closed range through the grammar', () => {
      expect(serializeCountRange({ min: 2, max: 10 })).toBe('2..10');
      expect(parseCountRange(serializeCountRange({ min: 2, max: 10 }))).toEqual({ min: 2, max: 10 });
    });

    it('serializes open-ended bounds', () => {
      expect(serializeCountRange({ min: 1001, max: null })).toBe('1001..');
      expect(serializeCountRange({ min: null, max: 50 })).toBe('..50');
    });

    it('returns null for an absent/empty range', () => {
      expect(serializeCountRange(null)).toBeNull();
      expect(serializeCountRange({ min: null, max: null })).toBeNull();
    });
  });

  it('isRangeActive reflects at least one bound', () => {
    expect(isRangeActive({ min: 1, max: null })).toBe(true);
    expect(isRangeActive({ min: null, max: null })).toBe(false);
    expect(isRangeActive(null)).toBe(false);
  });

  it('countRangesEqual compares canonical ranges', () => {
    expect(countRangesEqual({ min: 1, max: 1 }, { min: 1, max: 1 })).toBe(true);
    expect(countRangesEqual({ min: 1, max: 1 }, { min: 1, max: 2 })).toBe(false);
    expect(countRangesEqual(null, { min: null, max: null })).toBe(true);
  });
});

describe('words-filter-presets chip thresholds', () => {
  describe('resolveRangeThreshold', () => {
    it.each([
      { family: 'occurrences', expected: 100 },
      { family: 'ayahsSurahs', expected: 100 },
      { family: 'subCount', expected: 10 },
    ] as const)('falls back to the $family family default', ({ family, expected }) => {
      expect(resolveRangeThreshold(family)).toBe(expected);
    });

    it('lets a per-metric override win over the family default', () => {
      expect(resolveRangeThreshold('ayahsSurahs', 50)).toBe(50);
    });
  });

  describe('buildRangeChips', () => {
    it('builds strictly-bounded gt/lt chips around the threshold', () => {
      expect(buildRangeChips('occurrences')).toEqual([
        { kind: 'gt', threshold: 100, min: 101, max: null },
        { kind: 'lt', threshold: 100, min: null, max: 99 },
      ]);
    });

    it('bounds the chips on the override when a metric carries one', () => {
      expect(buildRangeChips('ayahsSurahs', 50)).toEqual([
        { kind: 'gt', threshold: 50, min: 51, max: null },
        { kind: 'lt', threshold: 50, min: null, max: 49 },
      ]);
    });
  });

  describe('per-metric derivation (locked table N4-a)', () => {
    it.each([
      { key: 'occurrences', expected: 100 },
      { key: 'ayahs', expected: 100 },
      { key: 'surahs', expected: 50 },
      { key: 'simpleWords', expected: 10 },
      { key: 'tashkeelWords', expected: 10 },
      { key: 'lemmas', expected: 10 },
      { key: 'stems', expected: 10 },
    ])('resolves $key to $expected', ({ key, expected }) => {
      expect(thresholdOf(ROOTS_RANGE_METRICS, key)).toBe(expected);
    });

    it('keeps ayahs and surahs apart despite their shared family', () => {
      const ayahs = ROOTS_RANGE_METRICS.find((metric) => metric.key === 'ayahs')!;
      const surahs = ROOTS_RANGE_METRICS.find((metric) => metric.key === 'surahs')!;

      expect(surahs.family).toBe(ayahs.family);
      expect(thresholdOf(ROOTS_RANGE_METRICS, 'surahs')).not.toBe(thresholdOf(ROOTS_RANGE_METRICS, 'ayahs'));
    });

    it.each(Object.keys(EXPLORER_METRICS))('resolves surahs to 50 on the %s explorer', (explorer) => {
      expect(thresholdOf(EXPLORER_METRICS[explorer], 'surahs')).toBe(50);
    });
  });
});
