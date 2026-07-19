import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, computed, inject, input, output, signal } from '@angular/core';
import { ScrollingModule } from '@angular/cdk/scrolling';

import { QD_BP_TABLET_MAX_QUERY } from '../../../../shared/layout/breakpoints';
import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
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
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';

export type WordTypeCountColumn = 'occurrences' | 'ayahs' | 'surahs';

export interface WordTypeCountOpenedEvent {
  row: WordTypeTableRowDto;
  column: WordTypeCountColumn;
  view: WordTypeDetailView;
}

const ROW_HEIGHT_DESKTOP = 40;
const ROW_HEIGHT_MOBILE = 88;
const HAS_RESIZE_OBSERVER = typeof ResizeObserver !== 'undefined';

@Component({
  selector: 'qd-word-types-table',
  standalone: true,
  imports: [NgTemplateOutlet, ScrollingModule, WordCountChipComponent],
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
  // null = release the sort param — unlike the other four explorers, this lands on المواضع desc (the
  // Word Types default), not Mushaf order.
  readonly sortChange = output<WordTypeSort | null>();

  protected readonly sortControl = new ExplorerTableSortController<WordTypeSort>(
    () => this.sort(),
    (value) => normalizeWordTypeSort(value),
    (sort) => this.sortChange.emit(sort),
  );

  // 12 rows, matching the four sibling explorer tables (Feature 030, N3-d) — this table showed 5.
  protected readonly loadingRowPlaceholders = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] as const;

  // Virtual scrolling keeps the 1000-row list bounded (mirrors the other explorer tables). It is guarded
  // on ResizeObserver so the jsdom test builder falls back to plain rendering.
  protected readonly useVirtualScroll = HAS_RESIZE_OBSERVER;
  protected readonly rowHeight = signal(ROW_HEIGHT_DESKTOP);

  // Only rows whose discriminant matches the active view render; a stale/mismatched response is dropped
  // here (defense-in-depth) so it can never paint a root row under the stems tab, in either branch.
  protected readonly visibleRows = computed<readonly WordTypeTableRowDto[]>(() => {
    const page = this.rows();
    if (!page) {
      return [];
    }
    return page.items.filter((row) => this.matchesActiveView(row));
  });

  constructor() {
    afterNextRender(() => {
      if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
        const mobileMq = window.matchMedia(QD_BP_TABLET_MAX_QUERY);
        const syncRowHeight = () => this.rowHeight.set(mobileMq.matches ? ROW_HEIGHT_MOBILE : ROW_HEIGHT_DESKTOP);
        syncRowHeight();
        if (typeof mobileMq.addEventListener === 'function') {
          mobileMq.addEventListener('change', syncRowHeight);
          this.destroyRef.onDestroy(() => mobileMq.removeEventListener('change', syncRowHeight));
        }
      }

      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--word-types-table-scrollbar-gutter',
        '.word-types-table__body',
        '.word-types-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  // Arrow-function field, not a method: CDK's DefaultIterableDiffer invokes the virtual-scroll
  // `trackBy` callback unbound (no `this`). A prototype method would dereference `this` as
  // undefined and throw every change-detection cycle, rendering zero rows in the browser (the
  // jsdom specs use the non-virtual `@for` fallback, so they never caught it). The arrow binds
  // `this` lexically.
  protected readonly trackRowDomId = (_index: number, row: WordTypeTableRowDto): string =>
    this.rowDomId(row);

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

  // Page-relative sequence shared across all four views; the persisted database ID is never shown.
  protected rowNumber(index: number): number {
    const page = this.rows();
    if (!page) {
      return index + 1;
    }
    return pageRelativeRowNumber(page.page, page.pageSize, index);
  }

  // The header and rows render only when there is real data or a load in flight. Prompt, empty, and
  // error states render inside the same stable shell instead of replacing it.
  protected hasRows(): boolean {
    const page = this.rows();
    return page !== null && page.items.length > 0;
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

  // The dimension text column IS the `alpha` sort column (N8-f): one sort token across all four views,
  // but the header label (and aria-label) follows the view so it names the column the user is viewing.
  protected readonly alphaSortColumn = computed<ExplorerSortColumn<WordTypeSortColumnKey>>(() => ({
    ...WORD_TYPE_SORT_COLUMNS.alpha,
    label: this.dimensionHeader(),
  }));

  protected isSelected(row: WordTypeTableRowDto): boolean {
    const selected = this.selectedRow();
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
      `[data-word-types-row="${this.rowDomId(row)}"] [data-word-count-column="${resolvedColumn}"] [data-testid="word-count-chip"]`,
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
}
