import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { RootDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdDataTableComponent } from '../../../../shared/ui/data-table/data-table.component';
import { QdDataTableState } from '../../../../shared/ui/data-table/data-table.models';
import { QdSortableHeaderComponent } from '../../../../shared/ui/data-table/sortable-header.component';
import { syncTableScrollbarGutter } from '../../../../shared/ui/data-table/table-scrollbar-gutter-sync';
import { WORD_COUNT_DISABLED_REASON, WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  EMPTY_LIST_LABEL,
  LOADING_LABEL,
  OCCURRENCES_CHIP_LABEL,
  ROW_NUMBER_HEADER,
  UNIQUE_WORD_NULL_PLACEHOLDER,
  UNIQUE_WORD_ROOT_HEADER,
  UNIQUE_WORD_TABLE_BODY_LABEL,
  UNIQUE_WORD_TABLE_LABEL,
  UNIQUE_WORD_TYPE_HEADER,
  UNIQUE_WORD_WORD_HEADER,
  WORD_DRILLDOWN_VIEW_LABELS,
} from '../../models/unique-words.labels';
import {
  DEFAULT_UNIQUE_WORD_SORT,
  LoadStatus,
  UNIQUE_WORDS_PAGE_SIZE,
  UNIQUE_WORD_SORT_COLUMNS,
  UniqueWordListItemViewModel,
  UniqueWordSort,
  WordDrilldownView,
  normalizeUniqueWordSort,
} from '../../models/unique-words.models';
import {
  isUniqueWordCountActive,
  UniqueWordsColumnKey,
} from '../../utils/explorer-count-active';
import {
  ExplorerInteractionSource,
  handleExplorerTableKeydown,
} from '../../utils/explorer-table-keydown';
import { ExplorerRowNavDirection } from '../../utils/explorer-table-scroll';
import { ExplorerTableSortController } from '../../utils/explorer-table-sort.controller';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

import { QD_BP_MEDIUM_QUERY } from '../../../../shared/layout/breakpoints';

const ROW_HEIGHT_DESKTOP = 40;
const ROW_HEIGHT_COMPACT = 68;
const UNIQUE_TABLE_WIDE_COLUMN_COUNT = 8;
const UNIQUE_TABLE_MEDIUM_COLUMN_COUNT = 6;
let nextDisabledReasonId = 0;

const UNIQUE_WORDS_COLUMN_ORDER = [
  'missing',
  'surahs',
  'ayahs',
] as const satisfies readonly UniqueWordsColumnKey[];

export interface UniqueWordsDrilldownOpenEvent {
  word: UniqueWordListItemViewModel;
  column: UniqueWordsColumnKey;
  view: WordDrilldownView;
  source?: ExplorerInteractionSource;
}

@Component({
  selector: 'qd-unique-words-table',
  standalone: true,
  imports: [DetailOverlayLinkDirective, NgTemplateOutlet, QdActionDirective, QdDataTableComponent, QdSortableHeaderComponent, WordCountChipComponent],
  templateUrl: './unique-words-table.component.html',
  styleUrl: './unique-words-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly UniqueWordListItemViewModel[]>();
  readonly totalCount = input<number | null>(null);
  readonly loading = input(false);
  readonly status = input<LoadStatus>('idle');
  readonly errorMessage = input('');
  readonly selectedWordId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(UNIQUE_WORDS_PAGE_SIZE);
  readonly drilldownIsOpen = input(false);
  readonly activeColumn = input<UniqueWordsColumnKey | null>(null);
  readonly sort = input<UniqueWordSort>(DEFAULT_UNIQUE_WORD_SORT);

  readonly rowSelected = output<UniqueWordListItemViewModel>();
  readonly drilldownOpen = output<UniqueWordsDrilldownOpenEvent>();
  readonly sortChange = output<UniqueWordSort | null>();

  protected readonly sortControl = new ExplorerTableSortController<UniqueWordSort>(
    () => this.sort(),
    (value) => normalizeUniqueWordSort(value),
    (sort) => this.sortChange.emit(sort),
  );

  protected get sortColumns(): typeof UNIQUE_WORD_SORT_COLUMNS {
    return UNIQUE_WORD_SORT_COLUMNS;
  }

  protected readonly loadingRowPlaceholders = Array.from({ length: 12 });
  protected readonly rowHeight = ROW_HEIGHT_DESKTOP;
  protected readonly compactRowHeight = ROW_HEIGHT_COMPACT;
  protected readonly isMedium = signal(false);
  protected readonly columnCount = computed(() =>
    this.isMedium() ? UNIQUE_TABLE_MEDIUM_COLUMN_COUNT : UNIQUE_TABLE_WIDE_COLUMN_COUNT,
  );
  protected readonly tableState = computed<QdDataTableState>(() => {
    if (this.loading()) return 'loading';
    if (this.status() === 'error') return 'error';
    if (this.status() === 'empty') return 'empty';
    return 'ready';
  });
  protected readonly selectedRow = computed(
    () => this.rows().find((row) => row.id === this.selectedWordId()) ?? null,
  );
  protected readonly rowIdentity = (row: UniqueWordListItemViewModel): number => row.id;
  protected readonly sameRow = (
    row: UniqueWordListItemViewModel,
    selected: UniqueWordListItemViewModel | null,
  ): boolean => row.id === selected?.id;
  protected get disabledReason(): string {
    return WORD_COUNT_DISABLED_REASON;
  }
  protected readonly disabledReasonId = `unique-words-table-disabled-reason-${nextDisabledReasonId++}`;
  protected readonly hasDisabledCounts = computed(() =>
    this.rows().some(
      (row) =>
        row.occurrencesCount === 0 ||
        row.ayahsCount === 0 ||
        row.surahsCount === 0 ||
        row.missingSurahsCount === 0,
    ),
  );

  private readonly table = viewChild(QdDataTableComponent<UniqueWordListItemViewModel>);

  constructor() {
    afterNextRender(() => {
      if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
        const mediumQuery = window.matchMedia(QD_BP_MEDIUM_QUERY);
        const syncMedium = () => this.isMedium.set(mediumQuery.matches);
        syncMedium();
        if (typeof mediumQuery.addEventListener === 'function') {
          mediumQuery.addEventListener('change', syncMedium);
          this.destroyRef.onDestroy(() => mediumQuery.removeEventListener('change', syncMedium));
        }
      }

      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--unique-words-table-scrollbar-gutter',
        '.qd-data-table__body',
        '.unique-words-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected get rowNumberHeader(): string {
    return ROW_NUMBER_HEADER;
  }

  protected get wordHeader(): string {
    return UNIQUE_WORD_WORD_HEADER;
  }

  protected get typeHeader(): string {
    return UNIQUE_WORD_TYPE_HEADER;
  }

  protected get rootHeader(): string {
    return UNIQUE_WORD_ROOT_HEADER;
  }

  protected get nullPlaceholder(): string {
    return UNIQUE_WORD_NULL_PLACEHOLDER;
  }

  protected get occurrencesLabel(): string {
    return OCCURRENCES_CHIP_LABEL;
  }

  protected get ayahsLabel(): string {
    return WORD_DRILLDOWN_VIEW_LABELS.ayahs;
  }

  protected get surahsLabel(): string {
    return WORD_DRILLDOWN_VIEW_LABELS.surahs;
  }

  protected get missingLabel(): string {
    return WORD_DRILLDOWN_VIEW_LABELS.missing;
  }

  protected get loadingLabel(): string {
    return LOADING_LABEL;
  }

  protected get tableLabel(): string {
    return UNIQUE_WORD_TABLE_LABEL;
  }

  protected get noResultsLabel(): string {
    return EMPTY_LIST_LABEL;
  }

  protected get tableBodyLabel(): string {
    return UNIQUE_WORD_TABLE_BODY_LABEL;
  }

  protected wordTypeLabel(row: UniqueWordListItemViewModel): string {
    return row.primaryWordTypeBroadArabicLabel ?? this.nullPlaceholder;
  }

  protected hasRoot(
    row: UniqueWordListItemViewModel,
  ): row is UniqueWordListItemViewModel & { rootId: number; rootText: string } {
    return row.rootId !== null && Boolean(row.rootText);
  }

  protected rootDetailFrame(rootId: number): RootDetailFrame {
    return {
      kind: 'root',
      id: rootId,
      view: 'words',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: null,
    };
  }

  protected selectRow(row: UniqueWordListItemViewModel): void {
    if (row.surahsCount === 0) {
      return;
    }
    this.rowSelected.emit(row);
  }

  protected openDrilldown(
    row: UniqueWordListItemViewModel,
    column: UniqueWordsColumnKey,
    view: WordDrilldownView,
    source: ExplorerInteractionSource = 'immediate',
  ): void {
    this.drilldownOpen.emit({ word: row, column, view, source });
  }

  protected isSelected(row: UniqueWordListItemViewModel): boolean {
    return this.selectedWordId() === row.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected isCountActive(
    row: UniqueWordListItemViewModel,
    column: UniqueWordsColumnKey,
    count: number,
  ): boolean {
    return isUniqueWordCountActive({
      rowId: row.id,
      selectedWordId: this.selectedWordId(),
      column,
      activeColumn: this.activeColumn(),
      drilldownOpen: this.drilldownIsOpen(),
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
      selectedRowId: this.selectedWordId(),
      currentColumn: this.activeColumn(),
      columnOrder: UNIQUE_WORDS_COLUMN_ORDER,
      isColumnEnabled: (row, column) => this.isColumnEnabled(row, column),
      emitColumnTarget: (row, column, source) => this.emitColumnTarget(row, column, source),
      scrollToRow: (index, direction) => this.scrollToRow(index, direction),
    });
  }

  scrollToTop(): void {
    this.table()?.scrollToTop();
  }

  private emitColumnTarget(
    row: UniqueWordListItemViewModel,
    column: UniqueWordsColumnKey,
    source: ExplorerInteractionSource,
  ): void {
    this.openDrilldown(row, column, column, source);
  }

  private isColumnEnabled(row: UniqueWordListItemViewModel, column: UniqueWordsColumnKey): boolean {
    switch (column) {
      case 'ayahs':
        return row.ayahsCount > 0;
      case 'surahs':
        return row.surahsCount > 0;
      case 'missing':
        return row.missingSurahsCount > 0;
    }
  }

  private scrollToRow(index: number, direction: ExplorerRowNavDirection): void {
    this.table()?.scrollRowIntoView(index, direction);
  }
}
