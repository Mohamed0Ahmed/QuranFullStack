import { describe, expect, it } from 'vitest';

import { parseVerseKey } from './verse-key';

describe('parseVerseKey', () => {
  it('splits verse key into surah and ayah numbers', () => {
    expect(parseVerseKey('2:255')).toEqual({ surahNumber: 2, ayahNumber: 255 });
  });
});
