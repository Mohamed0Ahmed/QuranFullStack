import { HttpParams } from '@angular/common/http';
import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import {
  RangeMetric,
  appendRangeApiParams,
  buildRangeQueryParams,
  hasActiveRanges,
  parseRangeFilters,
  serializeRangeFiltersKey,
} from './words-range-filters';

const METRICS: readonly RangeMetric[] = [
  { key: 'occurrences', urlKey: 'occ', apiKey: 'occ', family: 'occurrences', labelAr: 'المواضع' },
  { key: 'simpleWords', urlKey: 'simple', apiKey: 'simpleWords', family: 'subCount', labelAr: 'بدون تشكيل' },
];

function params(query: string) {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

describe('words-range-filters', () => {
  it('parses only active metric ranges from the URL', () => {
    expect(parseRangeFilters(params('occ=11..100&simple=..5'), METRICS)).toEqual({
      occurrences: { min: 11, max: 100 },
      simpleWords: { min: null, max: 5 },
    });
    expect(parseRangeFilters(params(''), METRICS)).toEqual({});
  });

  it('builds a full param set that clears absent metrics (null) and serializes active ones', () => {
    expect(buildRangeQueryParams({ occurrences: { min: 11, max: 100 } }, METRICS)).toEqual({
      occ: '11..100',
      simple: null,
    });
  });

  it('appends only active min/max api params, mapping url keys to api prefixes', () => {
    const result = appendRangeApiParams(
      new HttpParams(),
      { occurrences: { min: 11, max: 100 }, simpleWords: { min: 2, max: null } },
      METRICS,
    );
    expect(result.get('occMin')).toBe('11');
    expect(result.get('occMax')).toBe('100');
    expect(result.get('simpleWordsMin')).toBe('2');
    expect(result.has('simpleWordsMax')).toBe(false);
  });

  it('produces an empty cache key for an unfiltered read (pre-feature identity)', () => {
    expect(serializeRangeFiltersKey({}, METRICS)).toBe('');
  });

  it('produces a deterministic cache key for active ranges', () => {
    const key = serializeRangeFiltersKey(
      { simpleWords: { min: 2, max: null }, occurrences: { min: 11, max: 100 } },
      METRICS,
    );
    expect(key).toBe('occurrences=11..100,simpleWords=2..');
  });

  it('reports active ranges', () => {
    expect(hasActiveRanges({})).toBe(false);
    expect(hasActiveRanges({ occurrences: { min: 1, max: null } })).toBe(true);
  });
});
