import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, computed, inject, input, output, signal } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdDataTableComponent } from '../../../../shared/ui/data-table/data-table.component';
import { QdDataTableRenderer, QdDataTableState } from '../../../../shared/ui/data-table/data-table.models';
import { QdSortableHeaderComponent } from '../../../../shared/ui/data-table/sortable-header.component';
import { syncTableScrollbarGutter } from '../../../../shared/ui/data-table/table-scrollbar-gutter-sync';
import { WORD_COUNT_DISABLED_REASON, WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  WORD_TYPES_LOADING_LABEL,
  WORD_TYPES_NULL_PLACEHOLDER,
  WORD_TYPES_TABLE_HEADERS,
  WORD_TYPE_TABLE_VIEW_TABLE_LABELS,
} from '../../models/word-types.labels';
import { ExplorerSortColumn } from '../../models/explorer-sort';
import {
  DEFAULT_WORD_TYPE_SORT,
  PagedResultDto,
  WORD_TYPE_SORT_COLUMNS,
  WordTypeDetailView,
  WordTypeSort,
  WordTypeSortColumnKey,
  WordTypeTableRowDto,
  WordTypeTableView,
  WordTypesLoadStatus,
  groupedTableRowId,
  normalizeWordTableRow,
  normalizeWordTypeSort,
} from '../../models/word-types.models';
import { ExplorerTableSortController } from '../../utils/explorer-table-sort.controller';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

import { QD_BP_MEDIUM_QUERY } from '../../../../shared/layout/breakpoints';

export type WordTypeCountColumn = 'occurrences' | 'ayahs' | 'surahs';

export interface WordTypeCountOpenedEvent {
  row: WordTypeTableRowDto;
  column: WordTypeCountColumn;
  view: WordTypeDetailView;
}

const ROW_HEIGHT_DESKTOP = 40;
const ROW_HEIGHT_COMPACT = 127;
const WORD_TYPES_WIDE_COLUMN_COUNT = 9;
const WORD_TYPES_MEDIUM_COLUMN_COUNT = 6;
const WORD_TYPES_GROUPED_COLUMN_COUNT = 5;
let nextDisabledReasonId = 0;

@Component({
  selector: 'qd-word-types-table',
  standalone: true,
  imports: [NgTemplateOutlet, QdActionDirective, QdDataTableComponent, QdSortableHeaderComponent, WordCountChipComponent],
  templateUrl: './word-types-table.component.html',
  styleUrl: './word-types-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypesTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input<PagedResultDto<WordTypeTableRowDto> | null>(null);
  readonly tableView = input<WordTypeTableView>('words');
  readonly loading = input(false);
  readonly status = input<WordTypesLoadStatus>('idle');
  readonly errorMessage = input('');
  readonly selectPromptLabel = input('');
  readonly emptyLabel = input('');
  readonly errorLabel = input('');
  readonly retryLabel = input('');
  readonly selectedRow = input<WordTypeTableRowDto | null>(null);
  readonly sort = input<WordTypeSort>(DEFAULT_WORD_TYPE_SORT);
  readonly countOpened = output<WordTypeCountOpenedEvent>();
  readonly retry = output<void>();
  readonly sortChange = output<WordTypeSort | null>();

  protected readonly sortControl = new ExplorerTableSortController<WordTypeSort>(
    () => this.sort(),
    (value) => normalizeWordTypeSort(value),
    (sort) => this.sortChange.emit(sort),
  );

  protected readonly loadingRowPlaceholders = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] as const;

  protected readonly rowHeight = ROW_HEIGHT_DESKTOP;
  protected readonly compactRowHeight = ROW_HEIGHT_COMPACT;

  protected readonly visibleRows = computed<readonly WordTypeTableRowDto[]>(() => {
    const page = this.rows();
    if (!page) {
      return [];
    }
    return page.items.filter((row) => this.matchesActiveView(row));
  });

  protected readonly renderer = computed<QdDataTableRenderer>(() =>
    this.isWordView() ? 'wide-columns' : 'grouped-rows',
  );

  protected readonly isMedium = signal(false);

  protected readonly columnCount = computed(() => {
    if (!this.isWordView()) {
      return WORD_TYPES_GROUPED_COLUMN_COUNT;
    }
    return this.isMedium() ? WORD_TYPES_MEDIUM_COLUMN_COUNT : WORD_TYPES_WIDE_COLUMN_COUNT;
  });

  protected readonly totalRowCount = computed(() => this.rows()?.totalCount ?? this.visibleRows().length);

  protected readonly tableState = computed<QdDataTableState>(() => {
    if (this.loading()) return 'loading';
    if (this.hasRows()) return 'ready';
    if (this.status() === 'error') return 'error';
    return 'empty';
  });

  protected readonly rowIdentity = (row: WordTypeTableRowDto): string => this.rowDomId(row);
  protected readonly sameRow = (row: WordTypeTableRowDto, selected: WordTypeTableRowDto | null): boolean =>
    this.matchesSelection(row, selected);

  protected get disabledReason(): string {
    return WORD_COUNT_DISABLED_REASON;
  }
  protected readonly disabledReasonId = `word-types-table-disabled-reason-${nextDisabledReasonId++}`;
  protected readonly hasDisabledCounts = computed(() =>
    this.visibleRows().some(
      (row) => row.occurrencesCount === 0 || row.ayahsCount === 0 || row.surahsCount === 0,
    ),
  );

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
        '--word-types-table-scrollbar-gutter',
        '.qd-data-table__body',
        '.word-types-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected get headers() { return WORD_TYPES_TABLE_HEADERS; }
  protected get loadingLabel() { return WORD_TYPES_LOADING_LABEL; }
  protected get placeholder() { return WORD_TYPES_NULL_PLACEHOLDER; }
  protected get tableLabel() { return WORD_TYPE_TABLE_VIEW_TABLE_LABELS[this.tableView()]; }

  protected isWordView(): boolean {
    return this.tableView() === 'words';
  }

  protected matchesActiveView(row: WordTypeTableRowDto): boolean {
    switch (this.tableView()) {
      case 'words': return row.kind === 'word';
      case 'roots': return row.kind === 'root';
      case 'stems': return row.kind === 'stem';
      case 'lemmas': return row.kind === 'lemma';
    }
  }

  protected rowNumber(index: number): number {
    const page = this.rows();
    if (!page) {
      return index + 1;
    }
    return pageRelativeRowNumber(page.page, page.pageSize, index);
  }

  protected hasRows(): boolean {
    const page = this.rows();
    return page !== null && page.items.length > 0;
  }

  protected hasHeader(): boolean {
    return this.loading() || this.hasRows();
  }

  protected dimensionHeader(): string {
    switch (this.tableView()) {
      case 'roots': return this.headers.root;
      case 'stems': return this.headers.stem;
      case 'lemmas': return this.headers.lemma;
      case 'words': return this.headers.word;
    }
  }

  protected get sortColumns(): typeof WORD_TYPE_SORT_COLUMNS { return WORD_TYPE_SORT_COLUMNS; }

  protected readonly alphaSortColumn = computed<ExplorerSortColumn<WordTypeSortColumnKey>>(() => ({
    ...WORD_TYPE_SORT_COLUMNS.alpha,
    label: this.dimensionHeader(),
  }));

  protected isSelected(row: WordTypeTableRowDto): boolean {
    return this.matchesSelection(row, this.selectedRow());
  }

  protected openCount(row: WordTypeTableRowDto, column: WordTypeCountColumn): void {
    const view: WordTypeDetailView = column === 'surahs'
      ? 'surahs'
      : row.kind !== 'word' && column === 'occurrences'
        ? 'words'
        : 'ayahs';
    this.countOpened.emit({ row, column, view });
  }

  focusStatistic(
    row: WordTypeTableRowDto | null,
    view: WordTypeDetailView,
    column: WordTypeCountColumn | null = null,
  ): void {
    if (!row) {
      return;
    }

    const resolvedColumn = column ?? (view === 'words' ? 'occurrences' : view);
    const host = this.host.nativeElement as HTMLElement;
    const button = host.querySelector<HTMLButtonElement>(
      `[data-row-id="${this.rowDomId(row)}"] [data-word-count-column="${resolvedColumn}"] [data-testid="word-count-chip"]`,
    );
    button?.focus();
  }

  protected rowDomId(row: WordTypeTableRowDto): string {
    switch (row.kind) {
      case 'word': {
        const identity = normalizeWordTableRow(row);
        return [identity.tashkeelWordId, identity.contextCode, identity.case, identity.tense, identity.voice].join(':');
      }
      case 'root':
        return `root:${row.rootId}`;
      case 'stem':
        return `stem:${row.stemId}`;
      case 'lemma':
        return `lemma:${row.lemmaId}`;
    }
  }

  private matchesSelection(row: WordTypeTableRowDto, selected: WordTypeTableRowDto | null): boolean {
    if (!selected || selected.kind !== row.kind) {
      return false;
    }

    if (selected.kind === 'word' && row.kind === 'word') {
      const selectedIdentity = normalizeWordTableRow(selected);
      const rowIdentity = normalizeWordTableRow(row);
      return selectedIdentity.tashkeelWordId === rowIdentity.tashkeelWordId
        && selectedIdentity.contextCode === rowIdentity.contextCode
        && selectedIdentity.case === rowIdentity.case
        && selectedIdentity.tense === rowIdentity.tense
        && selectedIdentity.voice === rowIdentity.voice;
    }

    if (selected.kind !== 'word' && row.kind !== 'word') {
      return groupedTableRowId(selected) === groupedTableRowId(row);
    }

    return false;
  }
}
