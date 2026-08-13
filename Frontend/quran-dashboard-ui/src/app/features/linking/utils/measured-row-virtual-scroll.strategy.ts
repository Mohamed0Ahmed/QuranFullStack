import { ListRange } from '@angular/cdk/collections';
import { CdkVirtualScrollViewport, VirtualScrollStrategy } from '@angular/cdk/scrolling';
import { Observable, Subject, distinctUntilChanged } from 'rxjs';

const CONTENT_WRAPPER_SELECTOR = '.cdk-virtual-scroll-content-wrapper';

export class MeasuredRowVirtualScrollStrategy implements VirtualScrollStrategy {
  private readonly scrolledIndex = new Subject<number>();
  private viewport: CdkVirtualScrollViewport | null = null;
  private contentWrapper: HTMLElement | null = null;
  private resizeObserver: ResizeObserver | null = null;
  private measuredSizes: (number | null)[] = [];
  private offsets: readonly number[] = [0];
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
    const bounded = Math.min(Math.max(index, 0), this.offsets.length - 1);
    this.viewport?.scrollToOffset(this.offsets[bounded], behavior);
  }

  private resetSizes(length: number): void {
    this.measuredSizes = new Array<number | null>(length).fill(null);
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
      this.recomputeOffsets();
      const length = this.measuredSizes.length;
      viewport.setTotalContentSize(this.offsets[length]);
      if (length === 0) {
        this.applyRange({ start: 0, end: 0 });
        return;
      }
      const scrollOffset = viewport.measureScrollOffset();
      const viewportSize = viewport.getViewportSize();
      const start = this.indexAt(scrollOffset - this.bufferSize);
      const end = Math.min(length, this.indexAt(scrollOffset + viewportSize + this.bufferSize) + 1);
      this.applyRange({ start, end });
      viewport.setRenderedContentOffset(this.offsets[start]);
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
      if (size > 0) {
        this.measuredSizes[index] = size;
      }
    }
  }

  private recomputeOffsets(): void {
    const estimate = this.estimatedSize();
    const offsets = new Array<number>(this.measuredSizes.length + 1);
    offsets[0] = 0;
    for (let index = 0; index < this.measuredSizes.length; index += 1) {
      offsets[index + 1] = offsets[index] + (this.measuredSizes[index] ?? estimate);
    }
    this.offsets = offsets;
  }

  private estimatedSize(): number {
    let total = 0;
    let count = 0;
    for (const size of this.measuredSizes) {
      if (size !== null) {
        total += size;
        count += 1;
      }
    }
    return count === 0 ? this.estimatedRowSize : total / count;
  }

  private indexAt(offset: number): number {
    const target = Math.max(offset, 0);
    let low = 0;
    let high = this.measuredSizes.length - 1;
    while (low < high) {
      const middle = Math.ceil((low + high) / 2);
      if (this.offsets[middle] <= target) {
        low = middle;
      } else {
        high = middle - 1;
      }
    }
    return Math.max(low, 0);
  }
}
