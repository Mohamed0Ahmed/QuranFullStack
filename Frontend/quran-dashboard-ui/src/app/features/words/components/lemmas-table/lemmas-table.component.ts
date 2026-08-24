import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, computed, inject, input, output, viewChild } from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { RootDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { LinkingQuickAddComponent } from '../../../linking/components/linking-quick-add/linking-quick-add.component';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdDataTableComponent } from '../../../../shared/ui/data-table/data-table.component';
import { QdDataTableState } from '../../../../shared/ui/data-table/data-table.models';
import { QdSortableHeaderComponent } from '../../../../shared/ui/data-table/sortable-header.component';
import { syncTableScrollbarGutter } from '../../../../shared/ui/data-table/table-scrollbar-gutter-sync';
import { WORD_COUNT_DISABLED_REASON, WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import { LEMMAS_COLUMN_COUNT_LABELS, LEMMAS_COLUMN_HEADERS, LEMMAS_LOADING_LABEL, LEMMAS_NO_RESULTS_LABEL, LEMMAS_ROOT_MISSING_ARIA, LEMMAS_ROOT_MISSING_LABEL, LEMMAS_ROOT_LINK_PREFIX, LEMMAS_TABLE_BODY_LABEL, LEMMAS_TABLE_LABEL } from '../../models/lemmas.labels';
import { DEFAULT_LEMMA_SORT, LEMMAS_LIST_PAGE_SIZE, LEMMA_SORT_COLUMNS, LemmaListItemViewModel, LemmaSort, LemmaSurahView, LemmaView, LemmaWordView, LoadStatus, normalizeLemmaSort } from '../../models/lemmas.models';
import { isMorphologyCountActive, MorphologyColumnKey, resolveMorphologyActiveColumn } from '../../utils/explorer-count-active';
import { ExplorerInteractionSource, handleExplorerTableKeydown } from '../../utils/explorer-table-keydown';
import { ExplorerTableSortController } from '../../utils/explorer-table-sort.controller';
import { ExplorerRowNavDirection } from '../../utils/explorer-table-scroll';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import {
  lemmaLinkingSource,
  linkingSourcesByRow,
} from '../../utils/words-linking-source-descriptor';
import { ExplorerTableColumnSettingsComponent } from '../explorer-table-column-settings/explorer-table-column-settings.component';
import { ExplorerTableColumnDefinition, ExplorerTableColumnsController } from '../../state/explorer-table-columns.controller';

const ROW_HEIGHT_DESKTOP = 40;
const ROW_HEIGHT_COMPACT = 207;
let nextDisabledReasonId = 0;
type LemmaTableColumnKey = Exclude<MorphologyColumnKey, 'lemmas'>;

const LEMMA_TABLE_COLUMN_ORDER = [
  'stems',
  'tashkeel',
  'simple',
  'surahs',
  'ayahs',
  'occurrences',
] as const satisfies readonly LemmaTableColumnKey[];

const LEMMA_TABLE_COLUMNS: readonly ExplorerTableColumnDefinition[] = [
  { key: 'rowNumber', label: LEMMAS_COLUMN_HEADERS.rowNumber, track: 'minmax(2.5rem, 0.28fr)', reorderLocked: true },
  { key: 'lemma', label: LEMMAS_COLUMN_HEADERS.lemma, track: 'minmax(0, 1.35fr)', locked: true, reorderLocked: true },
  { key: 'root', label: LEMMAS_COLUMN_HEADERS.root, track: 'minmax(0, 0.9fr)' },
  { key: 'occurrences', label: LEMMAS_COLUMN_HEADERS.occurrences, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'ayahs', label: LEMMAS_COLUMN_HEADERS.ayahs, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'surahs', label: LEMMAS_COLUMN_HEADERS.surahs, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'simple', label: LEMMAS_COLUMN_HEADERS.simpleWords, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'tashkeel', label: LEMMAS_COLUMN_HEADERS.tashkeelWords, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'stems', label: LEMMAS_COLUMN_HEADERS.stems, track: 'minmax(4.25rem, 0.9fr)' },
];

export interface LemmaCountOpenedEvent {
  lemma: LemmaListItemViewModel;
  column?: LemmaTableColumnKey;
  view: LemmaView;
  wordView?: LemmaWordView;
  surahView?: LemmaSurahView;
  source?: ExplorerInteractionSource;
}

@Component({
  selector: 'qd-lemmas-table',
  standalone: true,
  imports: [DetailOverlayLinkDirective, ExplorerTableColumnSettingsComponent, LinkingQuickAddComponent, NgTemplateOutlet, QdActionDirective, QdDataTableComponent, QdSortableHeaderComponent, WordCountChipComponent],
  templateUrl: './lemmas-table.component.html',
  styleUrl: './lemmas-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmasTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly LemmaListItemViewModel[]>();
  readonly totalCount = input<number | null>(null);
  readonly loading = input(false);
  readonly status = input<LoadStatus>('idle');
  readonly errorMessage = input('');
  readonly selectedLemmaId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(LEMMAS_LIST_PAGE_SIZE);
  readonly activeView = input<LemmaView | null>(null);
  readonly activeWordView = input<LemmaWordView | null>(null);
  readonly activeSurahView = input<LemmaSurahView | null>(null);
  readonly activeColumn = input<LemmaTableColumnKey | null>(null);
  readonly sort = input<LemmaSort>(DEFAULT_LEMMA_SORT);

  readonly rowSelected = output<LemmaListItemViewModel>();
  readonly countOpened = output<LemmaCountOpenedEvent>();
  readonly sortChange = output<LemmaSort | null>();

  protected readonly sortControl = new ExplorerTableSortController<LemmaSort>(
    () => this.sort(),
    (value) => normalizeLemmaSort(value),
    (sort) => this.sortChange.emit(sort),
  );

  protected get sortColumns(): typeof LEMMA_SORT_COLUMNS { return LEMMA_SORT_COLUMNS; }
  protected get headers() { return LEMMAS_COLUMN_HEADERS; }
  protected get countLabels() { return LEMMAS_COLUMN_COUNT_LABELS; }
  protected readonly loadingLabel = LEMMAS_LOADING_LABEL;
  protected readonly tableLabel = LEMMAS_TABLE_LABEL;
  protected readonly tableBodyLabel = LEMMAS_TABLE_BODY_LABEL;
  protected get noResultsLabel(): string {
    return LEMMAS_NO_RESULTS_LABEL;
  }
  protected readonly rootLinkPrefix = LEMMAS_ROOT_LINK_PREFIX;
  protected readonly rootMissingAria = LEMMAS_ROOT_MISSING_ARIA;
  protected readonly rootMissingLabel = LEMMAS_ROOT_MISSING_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 10 });
  protected readonly rowHeight = ROW_HEIGHT_DESKTOP;
  protected readonly compactRowHeight = ROW_HEIGHT_COMPACT;
  protected readonly columnSettings = new ExplorerTableColumnsController('lemmas', LEMMA_TABLE_COLUMNS);
  protected readonly columnCount = this.columnSettings.visibleColumnCount;
  protected readonly visibleColumns = this.columnSettings.visibleColumns;
  protected readonly mobileColumns = computed(() => this.visibleColumns().filter((column) =>
    !['rowNumber', 'lemma', 'root'].includes(column.key),
  ));
  protected readonly keyboardColumnOrder = computed(() => this.visibleColumns()
    .map((column) => column.key)
    .filter((key): key is LemmaTableColumnKey => LEMMA_TABLE_COLUMN_ORDER.includes(key as LemmaTableColumnKey)));
  protected readonly tableState = computed<QdDataTableState>(() => {
    if (this.loading()) return 'loading';
    if (this.status() === 'error') return 'error';
    if (this.status() === 'empty') return 'empty';
    return 'ready';
  });
  protected readonly selectedRow = computed(() => this.rows().find((row) => row.id === this.selectedLemmaId()) ?? null);
  protected readonly linkingSources = computed(() =>
    linkingSourcesByRow(this.rows(), lemmaLinkingSource),
  );
  protected readonly rowIdentity = (row: LemmaListItemViewModel): number => row.id;
  protected readonly sameRow = (row: LemmaListItemViewModel, selected: LemmaListItemViewModel | null): boolean => row.id === selected?.id;
  protected get disabledReason(): string { return WORD_COUNT_DISABLED_REASON; }
  protected readonly disabledReasonId = `lemmas-table-disabled-reason-${nextDisabledReasonId++}`;
  protected readonly hasDisabledCounts = computed(() => this.rows().some((row) =>
    row.occurrencesCount === 0 || row.ayahsCount === 0 || row.surahsCount === 0 ||
    row.simpleWordsCount === 0 || row.tashkeelWordsCount === 0 || row.stemsCount === 0,
  ));

  private readonly table = viewChild(QdDataTableComponent<LemmaListItemViewModel>);

  constructor() {
    afterNextRender(() => {
      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--lemmas-table-scrollbar-gutter',
        '.qd-data-table__body',
        '.lemmas-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected selectRow(lemma: LemmaListItemViewModel): void {
    if (lemma.simpleWordsCount === 0) {
      return;
    }
    this.rowSelected.emit(lemma);
  }

  protected linkingSource(row: LemmaListItemViewModel) {
    return this.linkingSources().get(row) ?? lemmaLinkingSource(row);
  }

  protected openCount(
    lemma: LemmaListItemViewModel,
    column: LemmaTableColumnKey,
    view: LemmaView,
    options: { wordView?: LemmaWordView; surahView?: LemmaSurahView } = {},
    source: ExplorerInteractionSource = 'immediate',
  ): void {
    this.countOpened.emit({ lemma, column, view, source, ...options });
  }

  protected isSelected(lemma: LemmaListItemViewModel): boolean {
    return this.selectedLemmaId() === lemma.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected isCountActive(
    lemma: LemmaListItemViewModel,
    column: LemmaTableColumnKey,
    count: number,
  ): boolean {
    return isMorphologyCountActive({
      rowId: lemma.id,
      selectedRowId: this.selectedLemmaId(),
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

  protected onTableKeydown(event: KeyboardEvent): void {
    if (this.loading()) {
      return;
    }
    handleExplorerTableKeydown({
      event,
      rows: this.rows(),
      selectedRowId: this.selectedLemmaId(),
      currentColumn: this.currentColumn(),
      columnOrder: this.keyboardColumnOrder(),
      isColumnEnabled: (row, column) => this.isColumnEnabled(row, column),
      emitColumnTarget: (row, column, source) => this.emitColumnTarget(row, column, source),
      scrollToRow: (index, direction) => this.scrollToRow(index, direction),
    });
  }

  scrollToTop(): void {
    this.table()?.scrollToTop();
  }

  private emitColumnTarget(
    lemma: LemmaListItemViewModel,
    column: LemmaTableColumnKey,
    source: ExplorerInteractionSource,
  ): void {
    switch (column) {
      case 'occurrences':
      case 'ayahs':
        this.openCount(lemma, column, 'ayahs', {}, source);
        return;
      case 'surahs':
        this.openCount(lemma, column, 'surahs', { surahView: 'mentioned' }, source);
        return;
      case 'simple':
        this.openCount(lemma, column, 'words', { wordView: 'simple' }, source);
        return;
      case 'tashkeel':
        this.openCount(lemma, column, 'words', { wordView: 'tashkeel' }, source);
        return;
      case 'stems':
        this.openCount(lemma, column, 'stems', {}, source);
        return;
    }
  }

  private isColumnEnabled(lemma: LemmaListItemViewModel, column: LemmaTableColumnKey): boolean {
    switch (column) {
      case 'occurrences':
        return lemma.occurrencesCount > 0;
      case 'ayahs':
        return lemma.ayahsCount > 0;
      case 'surahs':
        return lemma.surahsCount > 0;
      case 'simple':
        return lemma.simpleWordsCount > 0;
      case 'tashkeel':
        return lemma.tashkeelWordsCount > 0;
      case 'stems':
        return lemma.stemsCount > 0;
    }
  }

  private currentColumn(): LemmaTableColumnKey | null {
    const column = resolveMorphologyActiveColumn({
      view: this.activeView(),
      wordView: this.activeWordView(),
      surahView: this.activeSurahView(),
      activeColumn: this.activeColumn(),
    });

    return column === 'lemmas' ? null : column;
  }

  private scrollToRow(index: number, direction: ExplorerRowNavDirection): void {
    this.table()?.scrollRowIntoView(index, direction);
  }
}
