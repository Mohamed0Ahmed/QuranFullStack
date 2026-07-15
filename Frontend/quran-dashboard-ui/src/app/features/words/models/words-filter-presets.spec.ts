import { describe, expect, it } from 'vitest';

import { countRangesEqual, isRangeActive, parseCountRange, serializeCountRange } from './words-filter-presets';

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
