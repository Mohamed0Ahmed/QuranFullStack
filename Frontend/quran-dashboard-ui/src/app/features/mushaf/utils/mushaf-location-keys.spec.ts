import { describe, expect, it } from 'vitest';

import { verseKeyFromWordLocation } from './mushaf-location-keys';

describe('verseKeyFromWordLocation', () => {
  it('derives surah:ayah from a readable word location', () => {
    expect(verseKeyFromWordLocation('2:25:3')).toBe('2:25');
    expect(verseKeyFromWordLocation('114:6:1')).toBe('114:6');
  });

  it('returns null for malformed locations', () => {
    expect(verseKeyFromWordLocation('2:25')).toBeNull();
    expect(verseKeyFromWordLocation('')).toBeNull();
    expect(verseKeyFromWordLocation('bad')).toBeNull();
  });
});
