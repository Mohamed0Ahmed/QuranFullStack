import { describe, expect, it } from 'vitest';

import { mapUniqueWordListItem, mapUniqueWordListItems } from './unique-words-display.mapper';
import { UniqueWordListItemDto } from '../models/unique-words.models';

function word(overrides: Partial<UniqueWordListItemDto> = {}): UniqueWordListItemDto {
  return {
    id: 1,
    kind: 'tashkeel',
    displayTextUthmani: 'كلمة-مشكولة',
    textUthmani: 'كلمة-مشكولة',
    textUthmaniSimple: 'كلمة-بسيطة',
    textImlaeiSimple: 'كلمة-إملائية',
    occurrencesCount: 1,
    ayahsCount: 1,
    surahsCount: 1,
    missingSurahsCount: 113,
    firstVerseKey: '1:1',
    firstLocation: '1:1:1',
    ...overrides,
  };
}

describe('unique-words display mapper', () => {
  it('maps tashkeel mode to the Uthmani display text', () => {
    expect(mapUniqueWordListItem(word(), 'tashkeel').displayText).toBe('كلمة-مشكولة');
  });

  it('maps simple mode to the simple Uthmani text when available', () => {
    expect(mapUniqueWordListItem(word(), 'simple').displayText).toBe('كلمة-بسيطة');
  });

  it('falls back to the canonical display text when simple text is missing', () => {
    expect(mapUniqueWordListItem(word({ textUthmaniSimple: undefined, textImlaeiSimple: undefined }), 'simple').displayText).toBe('كلمة-مشكولة');
  });

  it('maps a list of items', () => {
    const rows = mapUniqueWordListItems(
      [word(), word({ id: 2, displayTextUthmani: 'كلمة-2', textUthmani: undefined })],
      'tashkeel',
    );
    expect(rows).toHaveLength(2);
    expect(rows[1]?.displayText).toBe('كلمة-2');
  });
});
