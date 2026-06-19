import { describe, expect, it } from 'vitest';

import { arabicSearchIncludes, normalizeArabicForSearch } from './arabic-search-normalize';

describe('normalizeArabicForSearch', () => {
  it('unifies hamza variants', () => {
    expect(normalizeArabicForSearch('الإنجليزية')).toBe(normalizeArabicForSearch('الانجليزية'));
  });

  it('removes diacritics', () => {
    expect(normalizeArabicForSearch('العَرَبِيَّة')).toBe(normalizeArabicForSearch('العربية'));
  });
});

describe('arabicSearchIncludes', () => {
  it('matches language names without hamzas in the query', () => {
    expect(arabicSearchIncludes('الإنجليزية', 'انجل')).toBe(true);
    expect(arabicSearchIncludes('العربية', 'عرب')).toBe(true);
    expect(arabicSearchIncludes('العربية', 'فرنس')).toBe(false);
  });
});
