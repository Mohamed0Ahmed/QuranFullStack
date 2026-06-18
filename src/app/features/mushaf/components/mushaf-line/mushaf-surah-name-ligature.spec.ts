import { describe, expect, it } from 'vitest';

import { mushafSurahNameLigature } from './mushaf-surah-name-ligature';

describe('mushafSurahNameLigature', () => {
  it('maps surah numbers to surah-name font ligature keys', () => {
    expect(mushafSurahNameLigature(2)).toBe('surah002');
    expect(mushafSurahNameLigature(114)).toBe('surah114');
  });

  it('returns null for unknown surah numbers', () => {
    expect(mushafSurahNameLigature(0)).toBeNull();
  });
});
