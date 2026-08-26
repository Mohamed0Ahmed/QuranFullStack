import { isPlatformBrowser, NgTemplateOutlet } from '@angular/common';
import {
  CdkVirtualScrollViewport,
  ScrollingModule,
  VIRTUAL_SCROLL_STRATEGY,
} from '@angular/cdk/scrolling';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  contentChild,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  input,
  output,
  PLATFORM_ID,
  signal,
  TemplateRef,
  viewChild,
} from '@angular/core';

import { QD_BP_COMPACT_QUERY } from '../../layout/breakpoints';
import { SessionScrollStateDirective } from '../../navigation/session-scroll-state/session-scroll-state.directive';
import { QdRefreshingIndicatorComponent } from '../refreshing-indicator/refreshing-indicator.component';
import { MeasuredRowVirtualScrollStrategy } from '../virtual-scroll/measured-row-virtual-scroll.strategy';
import { QdDataTableRenderer, QdDataTableRowContext, QdDataTableRowDirection, QdDataTableState } from './data-table.models';

const DEFAULT_ROW_HEIGHT = 40;
const VIRTUAL_ROW_BUFFER = 640;

@Component({
  selector: 'qd-data-table',
  standalone: true,
  imports: [NgTemplateOutlet, QdRefreshingIndicatorComponent, ScrollingModule, SessionScrollStateDirective],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: VIRTUAL_SCROLL_STRATEGY,
      useFactory: (): MeasuredRowVirtualScrollStrategy =>
        new MeasuredRowVirtualScrollStrategy(DEFAULT_ROW_HEIGHT, VIRTUAL_ROW_BUFFER),
    },
  ],
  host: {
    class: 'qd-data-table',
    '[attr.role]': "isCompact() ? 'list' : 'table'",
    '[attr.aria-label]': 'ariaLabel()',
    '[attr.aria-rowcount]': 'isCompact() ? null : totalRowCount()',
    '[attr.aria-colcount]': 'isCompact() ? null : columnCount()',
    '[attr.aria-busy]': "state() === 'loading' || state() === 'refreshing' ? 'true' : null",
    '[class.qd-refreshing-region]': "state() === 'refreshing'",
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
  private readonly virtualScrollStrategy = inject(VIRTUAL_SCROLL_STRATEGY) as MeasuredRowVirtualScrollStrategy;
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
  readonly rowSelectable = input<(row: T) => boolean>(() => true);
  readonly virtual = input(true);
  readonly rowHeight = input(40);
  readonly compactRowHeight = input(88);
  readonly scrollStateKey = input('');

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
    effect(() =>
      this.virtualScrollStrategy.resetMeasurements(this.rows().length, this.rowHeightEstimate()),
    );

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
    const viewport = this.viewport();
    if (this.useVirtualScroll() && viewport) {
      this.scrollVirtualRowIntoView(viewport, index, direction);
      return;
    }

    const body = this.host.nativeElement.querySelector('[data-testid="qd-data-table-body"]') as HTMLElement | null;
    const row = body?.querySelectorAll<HTMLElement>('.qd-data-table__row').item(index);
    if (!body || !row) {
      return;
    }

    const bodyRect = body.getBoundingClientRect();
    const rowRect = row.getBoundingClientRect();
    if (direction === 'down' && rowRect.bottom > bodyRect.bottom) {
      body.scrollTop += rowRect.bottom - bodyRect.bottom;
    }
    if (direction === 'up' && rowRect.top < bodyRect.top) {
      body.scrollTop += rowRect.top - bodyRect.top;
    }
  }

  protected useVirtualScroll(): boolean {
    return this.virtual() && typeof ResizeObserver !== 'undefined';
  }

  protected isSelected(row: T): boolean {
    return this.selected()(row, this.selectedRow());
  }

  protected selectRow(row: T): void {
    if (this.canSelect(row)) {
      this.rowSelected.emit(row);
    }
  }

  protected onRowKeydown(event: KeyboardEvent, row: T): void {
    if (!this.canSelect(row) || (event.key !== 'Enter' && event.key !== ' ')) {
      return;
    }

    event.preventDefault();
    this.rowSelected.emit(row);
  }

  protected rowContext(row: T, index: number): QdDataTableRowContext<T> {
    return { $implicit: row, row, index };
  }

  protected canSelect(row: T): boolean {
    return this.selectable() && this.rowSelectable()(row) && this.renderer() !== 'grouped-rows';
  }

  private scrollVirtualRowIntoView(
    viewport: CdkVirtualScrollViewport,
    index: number,
    direction: QdDataTableRowDirection,
  ): void {
    const body = viewport.elementRef.nativeElement;
    const row = body.querySelector<HTMLElement>(`[data-row-index="${index}"]`);
    if (row === null) {
      viewport.scrollToIndex(index, 'auto');
      return;
    }
    const bodyRect = body.getBoundingClientRect();
    const rowRect = row.getBoundingClientRect();
    const offset = viewport.measureScrollOffset('top');
    if (direction === 'down' && rowRect.bottom > bodyRect.bottom) {
      viewport.scrollToOffset(offset + rowRect.bottom - bodyRect.bottom, 'auto');
    }
    if (direction === 'up' && rowRect.top < bodyRect.top) {
      viewport.scrollToOffset(offset + rowRect.top - bodyRect.top, 'auto');
    }
  }

  private rowHeightEstimate(): number {
    return this.isCompact() ? this.compactRowHeight() : this.rowHeight();
  }
}
