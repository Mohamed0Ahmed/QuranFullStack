export type ExplorerRowNavDirection = 'up' | 'down';

export interface VisibleRowRange {
  first: number;
  last: number;
}

export interface ExplorerVirtualScrollViewport {
  getViewportSize(): number;
  measureScrollOffset(from?: 'top'): number;
  scrollToOffset(offset: number, behavior?: ScrollBehavior): void;
}

export interface ScrollExplorerRowIntoViewOptions {
  targetIndex: number;
  direction: ExplorerRowNavDirection;
  itemSize: number;
  viewport?: ExplorerVirtualScrollViewport | null;
  container?: HTMLElement | null;
}

export function getVisibleRowRange(
  offset: number,
  viewportSize: number,
  itemSize: number,
): VisibleRowRange {
  if (itemSize <= 0 || viewportSize <= 0) {
    return { first: 0, last: -1 };
  }

  const first = Math.max(0, Math.floor(offset / itemSize));
  const last = Math.max(first, Math.floor((offset + viewportSize - 1) / itemSize));
  return { first, last };
}

export function shouldScrollForRowNav(
  targetIndex: number,
  range: VisibleRowRange,
  direction: ExplorerRowNavDirection,
): boolean {
  return direction === 'down' ? targetIndex > range.last : targetIndex < range.first;
}

export function scrollExplorerRowByOne(
  scrollOffset: number,
  itemSize: number,
  direction: ExplorerRowNavDirection,
): number {
  const nextOffset = direction === 'down' ? scrollOffset + itemSize : scrollOffset - itemSize;
  return Math.max(0, nextOffset);
}

export function scrollExplorerRowIntoView(options: ScrollExplorerRowIntoViewOptions): void {
  if (options.itemSize <= 0) {
    return;
  }

  if (options.viewport) {
    const offset = options.viewport.measureScrollOffset('top');
    const range = getVisibleRowRange(offset, options.viewport.getViewportSize(), options.itemSize);
    if (shouldScrollForRowNav(options.targetIndex, range, options.direction)) {
      options.viewport.scrollToOffset(
        scrollExplorerRowByOne(offset, options.itemSize, options.direction),
        'auto',
      );
    }
    return;
  }

  const container = options.container;
  if (!container) {
    return;
  }

  const range = getVisibleRowRange(container.scrollTop, container.clientHeight, options.itemSize);
  if (shouldScrollForRowNav(options.targetIndex, range, options.direction)) {
    container.scrollTop = scrollExplorerRowByOne(container.scrollTop, options.itemSize, options.direction);
  }
}
