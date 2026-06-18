import { describe, expect, it } from 'vitest';

import { mushafCommonLigature, MUSHAF_BASMALLAH_LIGATURE } from './mushaf-common-ligature';

describe('mushafCommonLigature', () => {
  it('maps surah_header to the header frame ligature', () => {
    expect(mushafCommonLigature('surah_header')).toBe('header');
  });

  it('exposes the bismillah ligature for basmallah lines', () => {
    expect(MUSHAF_BASMALLAH_LIGATURE.length).toBeGreaterThan(0);
  });
});
