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
  viewChild,
} from '@angular/core';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LinkingQuickAddComponent } from '../../../linking/components/linking-quick-add/linking-quick-add.component';
import { QdDataTableComponent } from '../../../../shared/ui/data-table/data-table.component';
import { QdDataTableState } from '../../../../shared/ui/data-table/data-table.models';
import { QdSortableHeaderComponent } from '../../../../shared/ui/data-table/sortable-header.component';
import { syncTableScrollbarGutter } from '../../../../shared/ui/data-table/table-scrollbar-gutter-sync';
import { WORD_COUNT_DISABLED_REASON, WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  ROOTS_COLUMN_COUNT_LABELS,
  ROOTS_COLUMN_HEADERS,
  ROOTS_NO_RESULTS_LABEL,
  ROOTS_TABLE_BODY_LABEL,
  ROOTS_TABLE_LABEL,
} from '../../models/roots.labels';
import {
  DEFAULT_ROOT_SORT,
  LoadStatus,
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
import { ExplorerRowNavDirection } from '../../utils/explorer-table-scroll';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import {
  linkingSourcesByRow,
  rootLinkingSource,
} from '../../utils/words-linking-source-descriptor';
import { ExplorerTableColumnSettingsComponent } from '../explorer-table-column-settings/explorer-table-column-settings.component';
import {
  ExplorerTableColumnDefinition,
  ExplorerTableColumnsController,
} from '../../state/explorer-table-columns.controller';
const ROW_HEIGHT_DESKTOP = 40;
const ROW_HEIGHT_COMPACT = 127;
let nextDisabledReasonId = 0;
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

const ROOT_TABLE_COLUMNS: readonly ExplorerTableColumnDefinition[] = [
  { key: 'rowNumber', label: ROOTS_COLUMN_HEADERS.rowNumber, track: 'minmax(2.5rem, 0.3fr)', reorderLocked: true },
  { key: 'root', label: ROOTS_COLUMN_HEADERS.root, track: 'minmax(0, 1.6fr)', locked: true, reorderLocked: true },
  { key: 'occurrences', label: ROOTS_COLUMN_HEADERS.occurrences, track: 'minmax(4.5rem, 1fr)' },
  { key: 'ayahs', label: ROOTS_COLUMN_HEADERS.ayahs, track: 'minmax(4.5rem, 1fr)' },
  { key: 'surahs', label: ROOTS_COLUMN_HEADERS.surahs, track: 'minmax(4.5rem, 1fr)' },
  { key: 'simple', label: ROOTS_COLUMN_HEADERS.simpleWords, track: 'minmax(4.5rem, 1fr)' },
  { key: 'tashkeel', label: ROOTS_COLUMN_HEADERS.tashkeelWords, track: 'minmax(4.5rem, 1fr)' },
  { key: 'lemmas', label: ROOTS_COLUMN_HEADERS.lemmas, track: 'minmax(4.5rem, 1fr)' },
  { key: 'stems', label: ROOTS_COLUMN_HEADERS.stems, track: 'minmax(4.5rem, 1fr)' },
];

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
  imports: [
    NgTemplateOutlet,
    ExplorerTableColumnSettingsComponent,
    LinkingQuickAddComponent,
    QdActionDirective,
    QdDataTableComponent,
    QdSortableHeaderComponent,
    WordCountChipComponent,
  ],
  templateUrl: './roots-table.component.html',
  styleUrl: './roots-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootsTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly RootListItemViewModel[]>();
  readonly totalCount = input<number | null>(null);
  readonly loading = input(false);
  readonly status = input<LoadStatus>('idle');
  readonly errorMessage = input('');
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
  protected get noResultsLabel(): string {
    return ROOTS_NO_RESULTS_LABEL;
  }
  protected readonly loadingRowPlaceholders = Array.from({ length: 10 });
  protected readonly rowHeight = ROW_HEIGHT_DESKTOP;
  protected readonly compactRowHeight = ROW_HEIGHT_COMPACT;
  protected readonly columnSettings = new ExplorerTableColumnsController('roots', ROOT_TABLE_COLUMNS);
  protected readonly columnCount = this.columnSettings.visibleColumnCount;
  protected readonly visibleColumns = this.columnSettings.visibleColumns;
  protected readonly mobileColumns = computed(() => this.visibleColumns().filter((column) =>
    column.key === 'occurrences' || column.key === 'ayahs' || column.key === 'surahs',
  ));
  protected readonly keyboardColumnOrder = computed(() => this.visibleColumns()
    .map((column) => column.key)
    .filter((key): key is RootTableColumnKey => ROOT_TABLE_COLUMN_ORDER.includes(key as RootTableColumnKey)));
  protected readonly tableState = computed<QdDataTableState>(() => {
    if (this.loading()) return 'loading';
    if (this.status() === 'error') return 'error';
    if (this.status() === 'empty') return 'empty';
    return 'ready';
  });
  protected readonly selectedRow = computed(() => this.rows().find((row) => row.id === this.selectedRootId()) ?? null);
  protected readonly linkingSources = computed(() =>
    linkingSourcesByRow(this.rows(), rootLinkingSource),
  );
  protected readonly rowIdentity = (row: RootListItemViewModel): number => row.id;
  protected readonly sameRow = (row: RootListItemViewModel, selected: RootListItemViewModel | null): boolean => row.id === selected?.id;
  protected get disabledReason(): string { return WORD_COUNT_DISABLED_REASON; }
  protected readonly disabledReasonId = `roots-table-disabled-reason-${nextDisabledReasonId++}`;
  protected readonly hasDisabledCounts = computed(() => this.rows().some((row) =>
    row.occurrencesCount === 0 || row.ayahsCount === 0 || row.surahsCount === 0 ||
    row.simpleWordsCount === 0 || row.tashkeelWordsCount === 0 || row.lemmasCount === 0 || row.stemsCount === 0,
  ));

  private readonly table = viewChild(QdDataTableComponent<RootListItemViewModel>);

  constructor() {
    afterNextRender(() => {
      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--roots-table-scrollbar-gutter',
        '.qd-data-table__body',
        '.roots-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected selectRow(root: RootListItemViewModel): void {
    if (root.simpleWordsCount === 0) {
      return;
    }
    this.rowSelected.emit(root);
  }

  protected linkingSource(row: RootListItemViewModel) {
    return this.linkingSources().get(row) ?? rootLinkingSource(row);
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
    this.table()?.scrollRowIntoView(index, direction);
  }
}
