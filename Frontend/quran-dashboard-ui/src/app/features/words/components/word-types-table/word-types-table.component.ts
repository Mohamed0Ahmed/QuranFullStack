import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, inject, input, output } from '@angular/core';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  WORD_TYPES_LOADING_LABEL,
  WORD_TYPES_NULL_PLACEHOLDER,
  WORD_TYPES_TABLE_HEADERS,
  WORD_TYPE_TABLE_VIEW_TABLE_LABELS,
} from '../../models/word-types.labels';
import {
  PagedResultDto,
  WordTableRowDto,
  WordTypeDetailView,
  WordTypeTableRowDto,
  WordTypeTableView,
  WordTypesLoadStatus,
  groupedTableRowId,
  normalizeWordTableRow,
} from '../../models/word-types.models';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';

export type WordTypeCountColumn = 'occurrences' | 'ayahs' | 'surahs';

export interface WordTypeCountOpenedEvent {
  row: WordTableRowDto;
  column: WordTypeCountColumn;
  view: WordTypeDetailView;
}

@Component({
  selector: 'qd-word-types-table',
  standalone: true,
  imports: [WordCountChipComponent],
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
  readonly rowSelected = output<WordTypeTableRowDto>();
  readonly countOpened = output<WordTypeCountOpenedEvent>();
  readonly retry = output<void>();

  protected readonly loadingRowPlaceholders = [0, 1, 2, 3, 4] as const;

  constructor() {
    afterNextRender(() => {
      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--word-types-table-scrollbar-gutter',
        '.word-types-table__body',
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

  // Defense-in-depth: a row renders only when its discriminant matches the active view exactly, so a
  // stale/mismatched response can never paint (e.g.) a root row under the stems tab.
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
  // error states render inside the same stable shell instead of replacing it (locked decision 4).
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

  protected selectRow(row: WordTypeTableRowDto): void {
    this.rowSelected.emit(row);
  }

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

  protected openCount(row: WordTableRowDto, column: WordTypeCountColumn): void {
    const view: WordTypeDetailView = column === 'surahs' ? 'surahs' : 'ayahs';
    this.countOpened.emit({ row, column, view });
  }

  focusRow(row: WordTypeTableRowDto | null): void {
    if (!row) {
      return;
    }

    const host = this.host.nativeElement as HTMLElement;
    const button = host.querySelector<HTMLButtonElement>(
      `[data-word-types-row="${this.rowDomId(row)}"]`,
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
