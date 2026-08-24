import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, computed, inject, input, output, viewChild } from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { LemmaDetailFrame, RootDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { LinkingQuickAddComponent } from '../../../linking/components/linking-quick-add/linking-quick-add.component';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdDataTableComponent } from '../../../../shared/ui/data-table/data-table.component';
import { QdDataTableState } from '../../../../shared/ui/data-table/data-table.models';
import { QdSortableHeaderComponent } from '../../../../shared/ui/data-table/sortable-header.component';
import { syncTableScrollbarGutter } from '../../../../shared/ui/data-table/table-scrollbar-gutter-sync';
import { WORD_COUNT_DISABLED_REASON, WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import { STEMS_COLUMN_COUNT_LABELS, STEMS_COLUMN_HEADERS, STEMS_LEMMA_LINK_PREFIX, STEMS_LEMMA_MISSING_ARIA, STEMS_LEMMA_MISSING_LABEL, STEMS_LOADING_LABEL, STEMS_NO_RESULTS_LABEL, STEMS_ROOT_LINK_PREFIX, STEMS_ROOT_MISSING_ARIA, STEMS_ROOT_MISSING_LABEL, STEMS_TABLE_BODY_LABEL, STEMS_TABLE_LABEL } from '../../models/stems.labels';
import { DEFAULT_STEM_SORT, LoadStatus, STEMS_LIST_PAGE_SIZE, STEM_SORT_COLUMNS, StemListItemViewModel, StemSort, StemSurahView, StemView, StemWordView, normalizeStemSort } from '../../models/stems.models';
import { isMorphologyCountActive, MorphologyColumnKey, resolveMorphologyActiveColumn } from '../../utils/explorer-count-active';
import { ExplorerInteractionSource, handleExplorerTableKeydown } from '../../utils/explorer-table-keydown';
import { ExplorerTableSortController } from '../../utils/explorer-table-sort.controller';
import { ExplorerRowNavDirection } from '../../utils/explorer-table-scroll';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import {
  linkingSourcesByRow,
  stemLinkingSource,
} from '../../utils/words-linking-source-descriptor';
import { ExplorerTableColumnSettingsComponent } from '../explorer-table-column-settings/explorer-table-column-settings.component';
import { ExplorerTableColumnDefinition, ExplorerTableColumnsController } from '../../state/explorer-table-columns.controller';

const ROW_HEIGHT_DESKTOP = 40;
const ROW_HEIGHT_COMPACT = 127;
let nextDisabledReasonId = 0;
type StemTableColumnKey = Exclude<MorphologyColumnKey, 'stems'>;

const STEM_TABLE_COLUMN_ORDER = [
  'tashkeel',
  'simple',
  'surahs',
  'ayahs',
  'occurrences',
  'lemmas',
] as const satisfies readonly StemTableColumnKey[];

const STEM_TABLE_COLUMNS: readonly ExplorerTableColumnDefinition[] = [
  { key: 'rowNumber', label: STEMS_COLUMN_HEADERS.rowNumber, track: 'minmax(2.5rem, 0.28fr)', reorderLocked: true },
  { key: 'stem', label: STEMS_COLUMN_HEADERS.stem, track: 'minmax(0, 1.35fr)', locked: true, reorderLocked: true },
  { key: 'lemma', label: STEMS_COLUMN_HEADERS.lemma, track: 'minmax(0, 1.1fr)' },
  { key: 'root', label: STEMS_COLUMN_HEADERS.root, track: 'minmax(0, 1fr)' },
  { key: 'occurrences', label: STEMS_COLUMN_HEADERS.occurrences, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'ayahs', label: STEMS_COLUMN_HEADERS.ayahs, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'surahs', label: STEMS_COLUMN_HEADERS.surahs, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'simple', label: STEMS_COLUMN_HEADERS.simpleWords, track: 'minmax(4.25rem, 0.9fr)' },
  { key: 'tashkeel', label: STEMS_COLUMN_HEADERS.tashkeelWords, track: 'minmax(4.25rem, 0.9fr)' },
];

export interface StemCountOpenedEvent {
  stem: StemListItemViewModel;
  column?: StemTableColumnKey;
  view: StemView;
  wordView?: StemWordView;
  surahView?: StemSurahView;
  source?: ExplorerInteractionSource;
}

@Component({
  selector: 'qd-stems-table',
  standalone: true,
  imports: [DetailOverlayLinkDirective, ExplorerTableColumnSettingsComponent, LinkingQuickAddComponent, NgTemplateOutlet, QdActionDirective, QdDataTableComponent, QdSortableHeaderComponent, WordCountChipComponent],
  templateUrl: './stems-table.component.html',
  styleUrls: ['./stems-table.component.scss', './stems-table.component.responsive.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemsTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly StemListItemViewModel[]>();
  readonly totalCount = input<number | null>(null);
  readonly loading = input(false);
  readonly status = input<LoadStatus>('idle');
  readonly errorMessage = input('');
  readonly selectedStemId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(STEMS_LIST_PAGE_SIZE);
  readonly activeView = input<StemView | null>(null);
  readonly activeWordView = input<StemWordView | null>(null);
  readonly activeSurahView = input<StemSurahView | null>(null);
  readonly activeColumn = input<StemTableColumnKey | null>(null);
  readonly sort = input<StemSort>(DEFAULT_STEM_SORT);

  readonly rowSelected = output<StemListItemViewModel>();
  readonly countOpened = output<StemCountOpenedEvent>();
  readonly sortChange = output<StemSort | null>();

  protected readonly sortControl = new ExplorerTableSortController<StemSort>(
    () => this.sort(),
    (value) => normalizeStemSort(value),
    (sort) => this.sortChange.emit(sort),
  );

  protected get sortColumns(): typeof STEM_SORT_COLUMNS { return STEM_SORT_COLUMNS; }
  protected get headers() { return STEMS_COLUMN_HEADERS; }
  protected get countLabels() { return STEMS_COLUMN_COUNT_LABELS; }

  protected readonly loadingLabel = STEMS_LOADING_LABEL;
  protected readonly tableLabel = STEMS_TABLE_LABEL;
  protected readonly tableBodyLabel = STEMS_TABLE_BODY_LABEL;
  protected get noResultsLabel(): string {
    return STEMS_NO_RESULTS_LABEL;
  }
  protected get lemmaLinkPrefix(): string { return STEMS_LEMMA_LINK_PREFIX; }
  protected get rootLinkPrefix(): string { return STEMS_ROOT_LINK_PREFIX; }
  protected get lemmaMissingLabel(): string { return STEMS_LEMMA_MISSING_LABEL; }
  protected get lemmaMissingAria(): string { return STEMS_LEMMA_MISSING_ARIA; }
  protected get rootMissingLabel(): string { return STEMS_ROOT_MISSING_LABEL; }
  protected get rootMissingAria(): string { return STEMS_ROOT_MISSING_ARIA; }
  protected readonly loadingRowPlaceholders = Array.from({ length: 12 });
  protected readonly rowHeight = ROW_HEIGHT_DESKTOP;
  protected readonly compactRowHeight = ROW_HEIGHT_COMPACT;
  protected readonly columnSettings = new ExplorerTableColumnsController('stems', STEM_TABLE_COLUMNS);
  protected readonly columnCount = this.columnSettings.visibleColumnCount;
  protected readonly visibleColumns = this.columnSettings.visibleColumns;
  protected readonly mobileRelationColumns = computed(() => this.visibleColumns().filter((column) =>
    column.key === 'lemma' || column.key === 'root',
  ));
  protected readonly mobileColumns = computed(() => this.visibleColumns().filter((column) =>
    column.key === 'occurrences' || column.key === 'ayahs' || column.key === 'surahs',
  ));
  protected readonly keyboardColumnOrder = computed(() => this.visibleColumns()
    .map((column) => column.key)
    .filter((key): key is StemTableColumnKey => STEM_TABLE_COLUMN_ORDER.includes(key as StemTableColumnKey)));
  protected readonly tableState = computed<QdDataTableState>(() => {
    if (this.loading()) return 'loading';
    if (this.status() === 'error') return 'error';
    if (this.status() === 'empty') return 'empty';
    return 'ready';
  });
  protected readonly selectedRow = computed(() => this.rows().find((row) => row.id === this.selectedStemId()) ?? null);
  protected readonly linkingSources = computed(() =>
    linkingSourcesByRow(this.rows(), stemLinkingSource),
  );
  protected readonly rowIdentity = (row: StemListItemViewModel): number => row.id;
  protected readonly sameRow = (row: StemListItemViewModel, selected: StemListItemViewModel | null): boolean => row.id === selected?.id;
  protected get disabledReason(): string { return WORD_COUNT_DISABLED_REASON; }
  protected readonly disabledReasonId = `stems-table-disabled-reason-${nextDisabledReasonId++}`;
  protected readonly hasDisabledCounts = computed(() => this.rows().some((row) =>
    row.occurrencesCount === 0 || row.ayahsCount === 0 || row.surahsCount === 0 ||
    row.simpleWordsCount === 0 || row.tashkeelWordsCount === 0,
  ));

  private readonly table = viewChild(QdDataTableComponent<StemListItemViewModel>);

  constructor() {
    afterNextRender(() => {
      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--stems-table-scrollbar-gutter',
        '.qd-data-table__body',
        '.stems-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected selectRow(stem: StemListItemViewModel): void {
    if (stem.simpleWordsCount === 0) {
      return;
    }
    this.rowSelected.emit(stem);
  }

  protected linkingSource(row: StemListItemViewModel) {
    return this.linkingSources().get(row) ?? stemLinkingSource(row);
  }

  protected openCount(
    stem: StemListItemViewModel,
    column: StemTableColumnKey,
    view: StemView,
    options: { wordView?: StemWordView; surahView?: StemSurahView } = {},
    source: ExplorerInteractionSource = 'immediate',
  ): void {
    this.countOpened.emit({ stem, column, view, source, ...options });
  }

  protected isSelected(stem: StemListItemViewModel): boolean {
    return this.selectedStemId() === stem.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected isCountActive(
    stem: StemListItemViewModel,
    column: StemTableColumnKey,
    count: number,
  ): boolean {
    return isMorphologyCountActive({
      rowId: stem.id,
      selectedRowId: this.selectedStemId(),
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

  protected lemmaDetailFrame(lemmaId: number): LemmaDetailFrame {
    return {
      kind: 'lemma',
      id: lemmaId,
      view: 'words',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: null,
    };
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
      selectedRowId: this.selectedStemId(),
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
    stem: StemListItemViewModel,
    column: StemTableColumnKey,
    source: ExplorerInteractionSource,
  ): void {
    switch (column) {
      case 'occurrences':
      case 'ayahs':
        this.openCount(stem, column, 'ayahs', {}, source);
        return;
      case 'surahs':
        this.openCount(stem, column, 'surahs', { surahView: 'mentioned' }, source);
        return;
      case 'simple':
        this.openCount(stem, column, 'words', { wordView: 'simple' }, source);
        return;
      case 'tashkeel':
        this.openCount(stem, column, 'words', { wordView: 'tashkeel' }, source);
        return;
      case 'lemmas':
        this.openCount(stem, column, 'lemmas', {}, source);
        return;
    }
  }

  private isColumnEnabled(stem: StemListItemViewModel, column: StemTableColumnKey): boolean {
    switch (column) {
      case 'occurrences':
        return stem.occurrencesCount > 0;
      case 'ayahs':
        return stem.ayahsCount > 0;
      case 'surahs':
        return stem.surahsCount > 0;
      case 'simple':
        return stem.simpleWordsCount > 0;
      case 'tashkeel':
        return stem.tashkeelWordsCount > 0;
      case 'lemmas':
        return stem.lemmaId !== null;
    }
  }

  private currentColumn(): StemTableColumnKey | null {
    const column = resolveMorphologyActiveColumn({
      view: this.activeView(),
      wordView: this.activeWordView(),
      surahView: this.activeSurahView(),
      activeColumn: this.activeColumn(),
    });

    return column === 'stems' ? null : column;
  }

  private scrollToRow(index: number, direction: ExplorerRowNavDirection): void {
    this.table()?.scrollRowIntoView(index, direction);
  }
}
