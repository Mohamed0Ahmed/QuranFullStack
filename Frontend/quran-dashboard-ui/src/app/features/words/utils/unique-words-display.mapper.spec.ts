import { describe, expect, it } from 'vitest';

import { mapUniqueWordListItem, mapUniqueWordListItems } from './unique-words-display.mapper';
import { UniqueWordListItemDto } from '../models/unique-words.models';

function word(overrides: Partial<UniqueWordListItemDto> = {}): UniqueWordListItemDto {
  return {
    id: 1,
    kind: 'tashkeel',
    displayText: 'كلمة-تجريبية',
    occurrencesCount: 1,
    ayahsCount: 1,
    surahsCount: 1,
    missingSurahsCount: 113,
    primaryWordTypeCode: null,
    primaryWordTypeBroadArabicLabel: null,
    rootId: null,
    rootText: null,
    ...overrides,
  };
}

describe('unique-words display mapper', () => {
  it('passes the backend displayText through unchanged', () => {
    expect(mapUniqueWordListItem(word()).displayText).toBe('كلمة-تجريبية');
  });

  it('preserves the morphology enrichment fields on the view model', () => {
    const mapped = mapUniqueWordListItem(
      word({
        primaryWordTypeCode: 'PN',
        primaryWordTypeBroadArabicLabel: 'اسم',
        rootId: 5001,
        rootText: 'أ ل ه',
      }),
    );

    expect(mapped.primaryWordTypeCode).toBe('PN');
    expect(mapped.primaryWordTypeBroadArabicLabel).toBe('اسم');
    expect(mapped.rootId).toBe(5001);
    expect(mapped.rootText).toBe('أ ل ه');
  });

  it('maps a list of items', () => {
    const rows = mapUniqueWordListItems([word(), word({ id: 2, displayText: 'كلمة-2' })]);
    expect(rows).toHaveLength(2);
    expect(rows[1]?.displayText).toBe('كلمة-2');
  });
});
