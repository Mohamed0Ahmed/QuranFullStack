import { describe, expect, it } from 'vitest';

import { resolveAdjacentColumn } from './explorer-table-column-nav';

describe('explorer-table-column-nav', () => {
  const order = [
    'stems',
    'lemmas',
    'tashkeel',
    'simple',
    'surahs',
    'ayahs',
    'occurrences',
  ] as const;

  it('moves right in RTL table order', () => {
    expect(
      resolveAdjacentColumn(order, 'surahs', 'right', () => true),
    ).toBe('ayahs');
  });

  it('skips disabled columns', () => {
    expect(
      resolveAdjacentColumn(order, 'surahs', 'right', (column) => column !== 'ayahs'),
    ).toBe('occurrences');
  });

  it('clamps at the ends', () => {
    expect(resolveAdjacentColumn(order, 'stems', 'left', () => true)).toBeNull();
    expect(resolveAdjacentColumn(order, 'occurrences', 'right', () => true)).toBeNull();
  });
});
