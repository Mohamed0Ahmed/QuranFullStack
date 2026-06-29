import { describe, expect, it, vi } from 'vitest';

import {
  ExplorerVirtualScrollViewport,
  getVisibleRowRange,
  scrollExplorerRowIntoView,
  shouldScrollForRowNav,
} from './explorer-table-scroll';

describe('explorer-table-scroll', () => {
  it('keeps scroll fixed when target row is already visible', () => {
    const range = getVisibleRowRange(0, 240, 48);

    expect(range).toEqual({ first: 0, last: 4 });
    expect(shouldScrollForRowNav(3, range, 'down')).toBe(false);
  });

  it('scrolls down by one row when target passes the last visible row', () => {
    const viewport = createViewport({ offset: 0, viewportSize: 240 });

    scrollExplorerRowIntoView({
      targetIndex: 5,
      direction: 'down',
      itemSize: 48,
      viewport,
    });

    expect(viewport.scrollToOffset).toHaveBeenCalledOnce();
    expect(viewport.scrollToOffset).toHaveBeenCalledWith(48, 'auto');
  });

  it('scrolls up by one row when target passes the first visible row', () => {
    const viewport = createViewport({ offset: 96, viewportSize: 240 });

    scrollExplorerRowIntoView({
      targetIndex: 1,
      direction: 'up',
      itemSize: 48,
      viewport,
    });

    expect(viewport.scrollToOffset).toHaveBeenCalledOnce();
    expect(viewport.scrollToOffset).toHaveBeenCalledWith(48, 'auto');
  });
});

function createViewport(options: {
  offset: number;
  viewportSize: number;
}): ExplorerVirtualScrollViewport {
  return {
    getViewportSize: vi.fn(() => options.viewportSize),
    measureScrollOffset: vi.fn(() => options.offset),
    scrollToOffset: vi.fn(),
  };
}
