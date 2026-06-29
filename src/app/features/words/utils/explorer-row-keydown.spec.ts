import { describe, expect, it } from 'vitest';

import { resolveAdjacentRow } from './explorer-row-keydown';

describe('explorer-row-keydown', () => {
  const rows = [{ id: 11 }, { id: 12 }, { id: 13 }];

  it('moves between rows on the same page', () => {
    expect(resolveAdjacentRow(rows, 12, 'up')).toEqual({ index: 0, row: rows[0] });
    expect(resolveAdjacentRow(rows, 12, 'down')).toEqual({ index: 2, row: rows[2] });
  });

  it('clamps at page boundaries', () => {
    expect(resolveAdjacentRow(rows, 11, 'up')).toBeNull();
    expect(resolveAdjacentRow(rows, 13, 'down')).toBeNull();
  });
});
