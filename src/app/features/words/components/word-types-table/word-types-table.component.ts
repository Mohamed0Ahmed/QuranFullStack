import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import { WORD_TYPES_NULL_PLACEHOLDER, WORD_TYPES_TABLE_HEADERS, WORD_TYPES_TABLE_LABEL } from '../../models/word-types.labels';
import { PagedResultDto, WordTypeRowDto } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-types-table',
  standalone: true,
  imports: [WordCountChipComponent],
  templateUrl: './word-types-table.component.html',
  styleUrl: './word-types-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypesTableComponent {
  readonly rows = input<PagedResultDto<WordTypeRowDto> | null>(null);
  readonly loading = input(false);
  readonly selectedRow = input<WordTypeRowDto | null>(null);
  readonly rowSelected = output<WordTypeRowDto>();

  protected readonly tableLabel = WORD_TYPES_TABLE_LABEL;

  protected get headers() { return WORD_TYPES_TABLE_HEADERS; }
  protected get placeholder() { return WORD_TYPES_NULL_PLACEHOLDER; }

  protected selectRow(row: WordTypeRowDto): void {
    this.rowSelected.emit(row);
  }

  protected isSelected(row: WordTypeRowDto): boolean {
    const selected = this.selectedRow();
    return selected?.tashkeelWordId === row.tashkeelWordId && selected.contextCode === row.contextCode;
  }
}
