import { describe, expect, it } from 'vitest';

import { buildPaginationWindow } from './pagination-window';

describe('buildPaginationWindow', () => {
  it('returns an empty window when lastPage is zero', () => {
    expect(buildPaginationWindow(1, 0)).toEqual([]);
  });

  it('returns all pages when lastPage fits in the window', () => {
    expect(buildPaginationWindow(2, 4)).toEqual([1, 2, 3, 4]);
  });

  it('centers the current page in a five-page window', () => {
    expect(buildPaginationWindow(5, 22)).toEqual([3, 4, 5, 6, 7]);
  });

  it('pins the window to the first pages near the start', () => {
    expect(buildPaginationWindow(2, 22)).toEqual([1, 2, 3, 4, 5]);
  });

  it('pins the window to the last pages near the end', () => {
    expect(buildPaginationWindow(21, 22)).toEqual([18, 19, 20, 21, 22]);
  });
});
