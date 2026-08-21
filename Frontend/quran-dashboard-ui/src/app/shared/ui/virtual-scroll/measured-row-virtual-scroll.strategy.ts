import { ListRange } from '@angular/cdk/collections';
import { CdkVirtualScrollViewport, VirtualScrollStrategy } from '@angular/cdk/scrolling';
import { Observable, Subject, distinctUntilChanged } from 'rxjs';

import { FenwickDeltaTree } from './fenwick-delta-tree';

const CONTENT_WRAPPER_SELECTOR = '.cdk-virtual-scroll-content-wrapper';

export class MeasuredRowVirtualScrollStrategy implements VirtualScrollStrategy {
  private readonly scrolledIndex = new Subject<number>();
  private readonly deltas = new FenwickDeltaTree();
  private viewport: CdkVirtualScrollViewport | null = null;
  private contentWrapper: HTMLElement | null = null;
  private resizeObserver: ResizeObserver | null = null;
  private resizeFrame: number | null = null;
  private measuredSizes: number[] = [];
  private renderedRange: ListRange = { start: 0, end: 0 };
  private updating = false;
  private skipNextMeasure = false;

  readonly scrolledIndexChange: Observable<number> = this.scrolledIndex.pipe(distinctUntilChanged());

  constructor(
    private estimatedRowSize: number,
    private readonly bufferSize: number,
  ) {}

  attach(viewport: CdkVirtualScrollViewport): void {
    this.viewport = viewport;
    this.renderedRange = { start: 0, end: 0 };
    this.resetSizes(viewport.getDataLength());
    this.update();
  }

  detach(): void {
    this.resizeObserver?.disconnect();
    if (this.resizeFrame !== null) {
      cancelAnimationFrame(this.resizeFrame);
    }
    this.resizeObserver = null;
    this.resizeFrame = null;
    this.contentWrapper = null;
    this.viewport = null;
  }

  onContentScrolled(): void {
    this.update();
  }

  onDataLengthChanged(): void {
    this.resetSizes(this.viewport?.getDataLength() ?? 0);
    this.update();
  }

  onContentRendered(): void {
    this.update();
  }

  onRenderedOffsetChanged(): void {}

  scrollToIndex(index: number, behavior: ScrollBehavior): void {
    const bounded = Math.min(Math.max(index, 0), this.measuredSizes.length);
    this.viewport?.scrollToOffset(this.offsetFor(bounded), behavior);
  }

  resetMeasurements(length: number, estimatedRowSize: number): void {
    this.estimatedRowSize = Math.max(1, estimatedRowSize);
    this.resetSizes(length);
    this.update();
  }

  private resetSizes(length: number): void {
    this.measuredSizes = new Array(length).fill(0);
    this.deltas.reset(length);
    this.skipNextMeasure = true;
  }

  private update(): void {
    const viewport = this.viewport;
    if (viewport === null || this.updating) {
      return;
    }
    this.updating = true;
    try {
      this.observeContent(viewport);
      this.measureRenderedRows();
      const length = this.measuredSizes.length;
      viewport.setTotalContentSize(this.offsetFor(length));
      if (length === 0) {
        this.applyRange({ start: 0, end: 0 });
        return;
      }
      const scrollOffset = viewport.measureScrollOffset();
      const viewportSize = viewport.getViewportSize();
      const start = this.indexAt(scrollOffset - this.bufferSize);
      const end = Math.min(length, this.indexAt(scrollOffset + viewportSize + this.bufferSize) + 1);
      this.applyRange({ start, end });
      viewport.setRenderedContentOffset(this.offsetFor(start));
      this.scrolledIndex.next(this.indexAt(scrollOffset));
    } finally {
      this.updating = false;
    }
  }

  private applyRange(range: ListRange): void {
    if (range.start !== this.renderedRange.start || range.end !== this.renderedRange.end) {
      this.renderedRange = range;
      this.viewport?.setRenderedRange(range);
    }
  }

  private observeContent(viewport: CdkVirtualScrollViewport): void {
    if (this.contentWrapper !== null || typeof ResizeObserver === 'undefined') {
      return;
    }
    const host = viewport.elementRef.nativeElement;
    const wrapper = host.querySelector<HTMLElement>(CONTENT_WRAPPER_SELECTOR);
    if (wrapper === null) {
      return;
    }
    this.contentWrapper = wrapper;
    this.resizeObserver = new ResizeObserver(() => this.scheduleResizeUpdate());
    this.resizeObserver.observe(wrapper);
    this.resizeObserver.observe(host);
  }

  private scheduleResizeUpdate(): void {
    if (this.resizeFrame !== null) {
      return;
    }
    this.resizeFrame = requestAnimationFrame(() => {
      this.resizeFrame = null;
      this.update();
    });
  }

  private measureRenderedRows(): void {
    if (this.skipNextMeasure) {
      this.skipNextMeasure = false;
      return;
    }
    const rows = this.contentWrapper?.children;
    if (rows === undefined) {
      return;
    }
    for (let position = 0; position < rows.length; position += 1) {
      const index = this.renderedRange.start + position;
      if (index >= this.measuredSizes.length) {
        return;
      }
      const size = this.rowBlockSize(rows[position] as HTMLElement);
      if (size <= 0 || size === this.measuredSizes[index]) {
        continue;
      }
      const previousDelta = this.measuredSizes[index] === 0
        ? 0
        : this.measuredSizes[index] - this.estimatedRowSize;
      this.measuredSizes[index] = size;
      this.deltas.add(index, size - this.estimatedRowSize - previousDelta);
    }
  }

  private rowBlockSize(row: HTMLElement): number {
    const style = getComputedStyle(row);
    return row.offsetHeight + this.pixelValue(style.marginBlockStart) + this.pixelValue(style.marginBlockEnd);
  }

  private pixelValue(value: string): number {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  private offsetFor(index: number): number {
    return index * this.estimatedRowSize + this.deltas.prefix(index);
  }

  private indexAt(offset: number): number {
    const target = Math.max(offset, 0);
    let low = 0;
    let high = Math.max(this.measuredSizes.length - 1, 0);
    while (low < high) {
      const middle = Math.ceil((low + high) / 2);
      if (this.offsetFor(middle) <= target) {
        low = middle;
      } else {
        high = middle - 1;
      }
    }
    return low;
  }
}
