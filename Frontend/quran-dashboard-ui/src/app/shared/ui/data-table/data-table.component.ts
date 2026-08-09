import { isPlatformBrowser, NgTemplateOutlet } from '@angular/common';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  contentChild,
  DestroyRef,
  ElementRef,
  inject,
  input,
  output,
  PLATFORM_ID,
  signal,
  TemplateRef,
  viewChild,
} from '@angular/core';

import { QD_BP_COMPACT_QUERY } from '../../layout/breakpoints';
import { QdDataTableRenderer, QdDataTableRowContext, QdDataTableRowDirection, QdDataTableState } from './data-table.models';

@Component({
  selector: 'qd-data-table',
  standalone: true,
  imports: [NgTemplateOutlet, ScrollingModule],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'qd-data-table',
    '[attr.role]': "isCompact() ? 'list' : 'table'",
    '[attr.aria-label]': 'ariaLabel()',
    '[attr.aria-rowcount]': 'isCompact() ? null : totalRowCount()',
    '[attr.aria-colcount]': 'isCompact() ? null : columnCount()',
    '[attr.aria-busy]': "state() === 'loading' || state() === 'refreshing' ? 'true' : null",
    '[style.--qd-data-table-compact-row-height.px]': 'compactRowHeight()',
    '[attr.data-renderer]': 'renderer()',
    '[attr.data-state]': 'state()',
    '[class.qd-data-table--compact]': 'isCompact()',
    '[class.qd-data-table--standard]': "renderer() === 'standard'",
    '[class.qd-data-table--wide-columns]': "renderer() === 'wide-columns'",
    '[class.qd-data-table--grouped-rows]': "renderer() === 'grouped-rows'",
  },
})
export class QdDataTableComponent<T> {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isCompactLayout = signal(false);

  readonly renderer = input.required<QdDataTableRenderer>();
  readonly rows = input.required<readonly T[]>();
  readonly rowId = input.required<(row: T) => string | number>();
  readonly ariaLabel = input.required<string>();
  readonly bodyAriaLabel = input<string | null>(null);
  readonly columnCount = input.required<number>();
  readonly totalRowCount = input.required<number>();
  readonly state = input<QdDataTableState>('ready');
  readonly selectedRow = input<T | null>(null);
  readonly selected = input<(row: T, selectedRow: T | null) => boolean>((row, selectedRow) => row === selectedRow);
  readonly selectable = input(false);
  readonly virtual = input(true);
  readonly rowHeight = input(40);
  readonly compactRowHeight = input(88);

  readonly rowSelected = output<T>();

  protected readonly isCompact = this.isCompactLayout.asReadonly();
  protected readonly headerTemplate = contentChild<TemplateRef<unknown>>('headerTemplate');
  protected readonly rowTemplate = contentChild<TemplateRef<QdDataTableRowContext<T>>>('rowTemplate');
  protected readonly compactRowTemplate = contentChild<TemplateRef<QdDataTableRowContext<T>>>('compactRowTemplate');
  protected readonly loadingTemplate = contentChild<TemplateRef<unknown>>('loadingTemplate');
  protected readonly emptyTemplate = contentChild<TemplateRef<unknown>>('emptyTemplate');
  protected readonly errorTemplate = contentChild<TemplateRef<unknown>>('errorTemplate');
  protected readonly paginationTemplate = contentChild<TemplateRef<unknown>>('paginationTemplate');
  protected readonly trackRow = (_index: number, row: T): string | number => this.rowId()(row);
  private readonly viewport = viewChild(CdkVirtualScrollViewport);

  constructor() {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    afterNextRender(() => {
      if (typeof window.matchMedia !== 'function') {
        return;
      }

      const compactQuery = window.matchMedia(QD_BP_COMPACT_QUERY);
      const syncCompactLayout = () => this.isCompactLayout.set(compactQuery.matches);
      syncCompactLayout();
      compactQuery.addEventListener('change', syncCompactLayout);
      this.destroyRef.onDestroy(() => compactQuery.removeEventListener('change', syncCompactLayout));
    });
  }

  scrollToTop(): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll() && viewport) {
      viewport.scrollToIndex(0, 'auto');
      return;
    }

    const body = this.host.nativeElement.querySelector('[data-testid="qd-data-table-body"]') as HTMLElement | null;
    if (body) {
      body.scrollTop = 0;
    }
  }

  scrollRowIntoView(index: number, direction: QdDataTableRowDirection): void {
    const itemSize = this.isCompact() ? this.compactRowHeight() : this.rowHeight();
    if (itemSize <= 0) {
      return;
    }

    const viewport = this.viewport();
    if (this.useVirtualScroll() && viewport) {
      const offset = viewport.measureScrollOffset('top');
      const viewportSize = viewport.getViewportSize();
      if (this.shouldScroll(index, offset, viewportSize, itemSize, direction)) {
        viewport.scrollToOffset(this.nextOffset(offset, itemSize, direction), 'auto');
      }
      return;
    }

    const body = this.host.nativeElement.querySelector('[data-testid="qd-data-table-body"]') as HTMLElement | null;
    if (body && this.shouldScroll(index, body.scrollTop, body.clientHeight, itemSize, direction)) {
      body.scrollTop = this.nextOffset(body.scrollTop, itemSize, direction);
    }
  }

  protected useVirtualScroll(): boolean {
    return this.virtual() && !this.isCompact() && typeof ResizeObserver !== 'undefined';
  }

  protected isSelected(row: T): boolean {
    return this.selected()(row, this.selectedRow());
  }

  protected selectRow(row: T): void {
    if (this.canSelect()) {
      this.rowSelected.emit(row);
    }
  }

  protected onRowKeydown(event: KeyboardEvent, row: T): void {
    if (!this.canSelect() || (event.key !== 'Enter' && event.key !== ' ')) {
      return;
    }

    event.preventDefault();
    this.rowSelected.emit(row);
  }

  protected rowContext(row: T, index: number): QdDataTableRowContext<T> {
    return { $implicit: row, row, index };
  }

  protected canSelect(): boolean {
    return this.selectable() && this.renderer() !== 'grouped-rows';
  }

  private shouldScroll(
    index: number,
    offset: number,
    viewportSize: number,
    itemSize: number,
    direction: QdDataTableRowDirection,
  ): boolean {
    const first = Math.max(0, Math.floor(offset / itemSize));
    const last = Math.max(first, Math.floor((offset + viewportSize - 1) / itemSize));
    return direction === 'down' ? index > last : index < first;
  }

  private nextOffset(offset: number, itemSize: number, direction: QdDataTableRowDirection): number {
    return Math.max(0, direction === 'down' ? offset + itemSize : offset - itemSize);
  }
}
