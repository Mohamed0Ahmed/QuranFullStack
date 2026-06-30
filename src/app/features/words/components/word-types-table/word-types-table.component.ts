import { ChangeDetectionStrategy, Component, ElementRef, inject, input, output } from '@angular/core';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import { WORD_TYPES_NULL_PLACEHOLDER, WORD_TYPES_TABLE_HEADERS, WORD_TYPES_TABLE_LABEL } from '../../models/word-types.labels';
import { PagedResultDto, WordTypeDetailView, WordTypeRowDto } from '../../models/word-types.models';

export type WordTypeCountColumn = 'occurrences' | 'ayahs' | 'surahs';

export interface WordTypeCountOpenedEvent {
  row: WordTypeRowDto;
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

  readonly rows = input<PagedResultDto<WordTypeRowDto> | null>(null);
  readonly loading = input(false);
  readonly selectedRow = input<WordTypeRowDto | null>(null);
  readonly rowSelected = output<WordTypeRowDto>();
  readonly countOpened = output<WordTypeCountOpenedEvent>();

  protected readonly tableLabel = WORD_TYPES_TABLE_LABEL;

  protected get headers() { return WORD_TYPES_TABLE_HEADERS; }
  protected get placeholder() { return WORD_TYPES_NULL_PLACEHOLDER; }

  protected selectRow(row: WordTypeRowDto): void {
    this.rowSelected.emit(row);
  }

  protected isSelected(row: WordTypeRowDto): boolean {
    const selected = this.selectedRow();
    return selected?.tashkeelWordId === row.tashkeelWordId
      && selected.contextCode === row.contextCode
      && selected.case === row.case
      && selected.tense === row.tense
      && selected.voice === row.voice;
  }

  protected openCount(row: WordTypeRowDto, column: WordTypeCountColumn): void {
    const view: WordTypeDetailView = column === 'surahs' ? 'surahs' : 'ayahs';
    this.countOpened.emit({ row, column, view });
  }

  focusRow(row: WordTypeRowDto | null): void {
    if (!row) {
      return;
    }

    const host = this.host.nativeElement as HTMLElement;
    const button = host.querySelector<HTMLButtonElement>(
      `[data-word-types-row="${this.rowDomId(row)}"]`,
    );
    button?.focus();
  }

  private rowDomId(row: WordTypeRowDto): string {
    return [row.tashkeelWordId, row.contextCode, row.case, row.tense, row.voice].join(':');
  }
}
