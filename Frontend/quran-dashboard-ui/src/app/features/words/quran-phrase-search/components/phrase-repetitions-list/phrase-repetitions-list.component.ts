import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { PhraseRepetitionListItemDto } from '../../../../../core/api/generated/models/phrase-repetition-list-item-dto';
import {
  QdResultItemDirective,
  QdResultListDirective,
} from '../../../../../shared/ui/result-list/result-list.directive';

@Component({
  selector: 'qd-phrase-repetitions-list',
  standalone: true,
  imports: [QdResultItemDirective, QdResultListDirective],
  templateUrl: './phrase-repetitions-list.component.html',
  styleUrl: './phrase-repetitions-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseRepetitionsListComponent {
  readonly items = input.required<readonly PhraseRepetitionListItemDto[]>();
  readonly totalCount = input.required<number>();
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly selectedVariantId = input<number | null>(null);
  readonly disabled = input(false);

  readonly phraseSelected = output<number>();

  protected position(index: number): number {
    return (this.page() - 1) * this.pageSize() + index + 1;
  }
}
