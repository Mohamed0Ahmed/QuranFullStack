import { describe, expect, it } from 'vitest';

import { MUSHAF_BASMALLAH_DISPLAY_TEXT } from './mushaf-basmallah-display-text';

describe('MUSHAF_BASMALLAH_DISPLAY_TEXT', () => {
  it('is a non-empty presentation constant', () => {
    expect(MUSHAF_BASMALLAH_DISPLAY_TEXT.trim().length).toBeGreaterThan(0);
  });
});
