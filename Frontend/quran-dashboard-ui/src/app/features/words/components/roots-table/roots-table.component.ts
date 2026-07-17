import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  ROOTS_COLUMN_COUNT_LABELS,
  ROOTS_COLUMN_HEADERS,
  ROOTS_TABLE_BODY_LABEL,
  ROOTS_TABLE_LABEL,
} from '../../models/roots.labels';
import {
  DEFAULT_ROOT_SORT,
  ROOTS_LIST_PAGE_SIZE,
  ROOT_SORT_COLUMNS,
  RootListItemViewModel,
  RootSort,
  RootSurahView,
  RootView,
  RootWordView,
  normalizeRootSort,
} from '../../models/roots.models';
import { WORDS_LOADING_LABEL } from '../../models/words.labels';
import {
  isMorphologyCountActive,
  MorphologyColumnKey,
  resolveMorphologyActiveColumn,
} from '../../utils/explorer-count-active';
import {
  ExplorerInteractionSource,
  handleExplorerTableKeydown,
} from '../../utils/explorer-table-keydown';
import { ExplorerTableSortController } from '../../utils/explorer-table-sort.controller';
import {
  ExplorerRowNavDirection,
  scrollExplorerRowIntoView,
} from '../../utils/explorer-table-scroll';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';

import { QD_BP_TABLET_MAX_QUERY } from '../../../../shared/layout/breakpoints';

const ROW_HEIGHT_DESKTOP = 40;
const ROW_HEIGHT_MOBILE = 88;
const HAS_RESIZE_OBSERVER = typeof ResizeObserver !== 'undefined';
type RootTableColumnKey = MorphologyColumnKey;

const ROOT_TABLE_COLUMN_ORDER = [
  'stems',
  'lemmas',
  'tashkeel',
  'simple',
  'surahs',
  'ayahs',
  'occurrences',
] as const satisfies readonly RootTableColumnKey[];

export interface RootCountOpenedEvent {
  root: RootListItemViewModel;
  column?: RootTableColumnKey;
  view: RootView;
  wordView?: RootWordView;
  surahView?: RootSurahView;
  source?: ExplorerInteractionSource;
}
@Component({
  selector: 'qd-roots-table',
  standalone: true,
  imports: [NgTemplateOutlet, ScrollingModule, WordCountChipComponent],
  templateUrl: './roots-table.component.html',
  styleUrl: './roots-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootsTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly RootListItemViewModel[]>();
  readonly loading = input(false);
  readonly selectedRootId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(ROOTS_LIST_PAGE_SIZE);
  readonly activeView = input<RootView | null>(null);
  readonly activeWordView = input<RootWordView | null>(null);
  readonly activeSurahView = input<RootSurahView | null>(null);
  readonly activeColumn = input<RootTableColumnKey | null>(null);
  readonly sort = input<RootSort>(DEFAULT_ROOT_SORT);

  readonly rowSelected = output<RootListItemViewModel>();
  readonly countOpened = output<RootCountOpenedEvent>();
  /** null = release the sort param back to the default (ترتيب المصحف). */
  readonly sortChange = output<RootSort | null>();

  protected readonly sortControl = new ExplorerTableSortController<RootSort>(
    () => this.sort(),
    (value) => normalizeRootSort(value),
    (sort) => this.sortChange.emit(sort),
  );

  protected get sortColumns(): typeof ROOT_SORT_COLUMNS {
    return ROOT_SORT_COLUMNS;
  }

  protected get headers() {
    return ROOTS_COLUMN_HEADERS;
  }
  protected get countLabels() {
    return ROOTS_COLUMN_COUNT_LABELS;
  }
  protected readonly tableLabel = ROOTS_TABLE_LABEL;
  protected readonly tableBodyLabel = ROOTS_TABLE_BODY_LABEL;
  protected readonly loadingLabel = WORDS_LOADING_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 12 });
  protected readonly rowHeight = signal(ROW_HEIGHT_DESKTOP);
  protected readonly useVirtualScroll = HAS_RESIZE_OBSERVER;

  private readonly viewport = viewChild(CdkVirtualScrollViewport);

  constructor() {
    afterNextRender(() => {
      if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
        const mobileMq = window.matchMedia(QD_BP_TABLET_MAX_QUERY);
        const syncRowHeight = () => {
          this.rowHeight.set(mobileMq.matches ? ROW_HEIGHT_MOBILE : ROW_HEIGHT_DESKTOP);
        };
        syncRowHeight();
        if (typeof mobileMq.addEventListener === 'function') {
          mobileMq.addEventListener('change', syncRowHeight);
          this.destroyRef.onDestroy(() => mobileMq.removeEventListener('change', syncRowHeight));
        }
      }

      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--roots-table-scrollbar-gutter',
        '.roots-table__body',
        '.roots-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected selectRow(root: RootListItemViewModel): void {
    this.rowSelected.emit(root);
  }

  protected openCount(
    root: RootListItemViewModel,
    column: RootTableColumnKey,
    view: RootView,
    options: { wordView?: RootWordView; surahView?: RootSurahView } = {},
    source: ExplorerInteractionSource = 'immediate',
  ): void {
    this.countOpened.emit({ root, column, view, source, ...options });
  }

  protected isSelected(root: RootListItemViewModel): boolean {
    return this.selectedRootId() === root.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected trackRowById(_index: number, root: RootListItemViewModel): number {
    return root.id;
  }

  protected isCountActive(
    root: RootListItemViewModel,
    column: RootTableColumnKey,
    count: number,
  ): boolean {
    return isMorphologyCountActive({
      rowId: root.id,
      selectedRowId: this.selectedRootId(),
      column,
      activeColumn: resolveMorphologyActiveColumn({
        view: this.activeView(),
        wordView: this.activeWordView(),
        surahView: this.activeSurahView(),
        activeColumn: this.activeColumn(),
      }),
      disabled: count === 0,
    });
  }

  protected onTableKeydown(event: KeyboardEvent): void {
    if (this.loading()) {
      return;
    }
    handleExplorerTableKeydown({
      event,
      rows: this.rows(),
      selectedRowId: this.selectedRootId(),
      currentColumn: this.currentColumn(),
      columnOrder: ROOT_TABLE_COLUMN_ORDER,
      isColumnEnabled: (row, column) => this.isColumnEnabled(row, column),
      emitColumnTarget: (row, column, source) => this.emitColumnTarget(row, column, source),
      scrollToRow: (index, direction) => this.scrollToRow(index, direction),
    });
  }

  scrollToTop(): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll && viewport) {
      viewport.scrollToIndex(0, 'auto');
      return;
    }

    const body = this.host.nativeElement.querySelector('.roots-table__body') as HTMLElement | null;
    if (body) {
      body.scrollTop = 0;
    }
  }

  private emitColumnTarget(
    root: RootListItemViewModel,
    column: RootTableColumnKey,
    source: ExplorerInteractionSource,
  ): void {
    switch (column) {
      case 'occurrences':
      case 'ayahs':
        this.openCount(root, column, 'ayahs', {}, source);
        return;
      case 'surahs':
        this.openCount(root, column, 'surahs', { surahView: 'mentioned' }, source);
        return;
      case 'simple':
        this.openCount(root, column, 'words', { wordView: 'simple' }, source);
        return;
      case 'tashkeel':
        this.openCount(root, column, 'words', { wordView: 'tashkeel' }, source);
        return;
      case 'lemmas':
        this.openCount(root, column, 'lemmas', {}, source);
        return;
      case 'stems':
        this.openCount(root, column, 'stems', {}, source);
        return;
    }
  }

  private isColumnEnabled(root: RootListItemViewModel, column: RootTableColumnKey): boolean {
    switch (column) {
      case 'occurrences':
        return root.occurrencesCount > 0;
      case 'ayahs':
        return root.ayahsCount > 0;
      case 'surahs':
        return root.surahsCount > 0;
      case 'simple':
        return root.simpleWordsCount > 0;
      case 'tashkeel':
        return root.tashkeelWordsCount > 0;
      case 'lemmas':
        return root.lemmasCount > 0;
      case 'stems':
        return root.stemsCount > 0;
    }
  }

  private currentColumn(): RootTableColumnKey | null {
    return resolveMorphologyActiveColumn({
      view: this.activeView(),
      wordView: this.activeWordView(),
      surahView: this.activeSurahView(),
      activeColumn: this.activeColumn(),
    });
  }

  private scrollToRow(index: number, direction: ExplorerRowNavDirection): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll && viewport) {
      scrollExplorerRowIntoView({
        targetIndex: index,
        direction,
        itemSize: this.rowHeight(),
        viewport,
      });
      return;
    }

    const body = this.host.nativeElement.querySelector('.roots-table__body') as HTMLElement | null;
    scrollExplorerRowIntoView({
      targetIndex: index,
      direction,
      itemSize: this.rowHeight(),
      container: body,
    });
  }
}
