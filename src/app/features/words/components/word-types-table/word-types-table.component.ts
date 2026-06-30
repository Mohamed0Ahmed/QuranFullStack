import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { WORD_TYPES_TABLE_LABEL } from '../../models/word-types.labels';
import { PagedResultDto, WordTypeRowDto } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-types-table',
  standalone: true,
  templateUrl: './word-types-table.component.html',
  styleUrl: './word-types-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypesTableComponent {
  readonly rows = input<PagedResultDto<WordTypeRowDto> | null>(null);
  protected readonly tableLabel = WORD_TYPES_TABLE_LABEL;
}
