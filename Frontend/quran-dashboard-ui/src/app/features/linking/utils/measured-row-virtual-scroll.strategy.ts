import { ListRange } from '@angular/cdk/collections';
import { CdkVirtualScrollViewport, VirtualScrollStrategy } from '@angular/cdk/scrolling';
import { Observable, Subject, distinctUntilChanged } from 'rxjs';

const CONTENT_WRAPPER_SELECTOR = '.cdk-virtual-scroll-content-wrapper';

class FenwickDeltaTree {
  private values: number[] = [0];

  reset(length: number): void {
    this.values = new Array(length + 1).fill(0);
  }

  add(index: number, delta: number): void {
    for (let position = index + 1; position < this.values.length; position += position & -position) {
      this.values[position] += delta;
    }
  }

  prefix(length: number): number {
    let total = 0;
    for (let position = length; position > 0; position -= position & -position) {
      total += this.values[position];
    }
    return total;
  }
}

export class MeasuredRowVirtualScrollStrategy implements VirtualScrollStrategy {
  private readonly scrolledIndex = new Subject<number>();
  private readonly deltas = new FenwickDeltaTree();
  private viewport: CdkVirtualScrollViewport | null = null;
  private contentWrapper: HTMLElement | null = null;
  private resizeObserver: ResizeObserver | null = null;
  private measuredSizes: number[] = [];
  private renderedRange: ListRange = { start: 0, end: 0 };
  private updating = false;
  private skipNextMeasure = false;

  readonly scrolledIndexChange: Observable<number> = this.scrolledIndex.pipe(distinctUntilChanged());

  constructor(
    private readonly estimatedRowSize: number,
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
    this.resizeObserver = null;
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
    this.resizeObserver = new ResizeObserver(() => this.update());
    this.resizeObserver.observe(wrapper);
    this.resizeObserver.observe(host);
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
      const size = (rows[position] as HTMLElement).offsetHeight;
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
