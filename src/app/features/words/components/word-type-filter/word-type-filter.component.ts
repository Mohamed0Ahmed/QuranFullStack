import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { WORD_TYPES_FILTER_LABEL } from '../../models/word-types.labels';
import { WordTypeTreeDto } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-type-filter',
  standalone: true,
  templateUrl: './word-type-filter.component.html',
  styleUrl: './word-type-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeFilterComponent {
  readonly tree = input<WordTypeTreeDto | null>(null);
  protected readonly filterLabel = WORD_TYPES_FILTER_LABEL;
}
