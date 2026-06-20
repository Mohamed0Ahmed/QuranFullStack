import { describe, expect, it } from 'vitest';

import { mushafJuzNumberLigature } from './mushaf-juz-number-ligature';

describe('mushafJuzNumberLigature', () => {
  it('maps juz numbers to juz-number font ligature keys', () => {
    expect(mushafJuzNumberLigature(1)).toBe('juz001');
    expect(mushafJuzNumberLigature(30)).toBe('juz030');
  });

  it('returns null for out-of-range juz numbers', () => {
    expect(mushafJuzNumberLigature(0)).toBeNull();
    expect(mushafJuzNumberLigature(31)).toBeNull();
  });
});
