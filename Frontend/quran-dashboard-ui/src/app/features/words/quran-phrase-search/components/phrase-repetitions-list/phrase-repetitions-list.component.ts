import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { PhraseRepetitionListItemDto } from '../../../../../core/api/generated/models/phrase-repetition-list-item-dto';
import { QdDataTableComponent } from '../../../../../shared/ui/data-table/data-table.component';

const ROW_HEIGHT = 40;
const COMPACT_ROW_HEIGHT = 88;

@Component({
  selector: 'qd-phrase-repetitions-list',
  standalone: true,
  imports: [QdDataTableComponent],
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

  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly compactRowHeight = COMPACT_ROW_HEIGHT;
  protected readonly selectedRow = computed(
    () => this.items().find((item) => item.variantId === this.selectedVariantId()) ?? null,
  );
  protected readonly scrollStateKey = computed(
    () => `words.table.phrase-repetitions.${this.page()}`,
  );
  protected readonly rowIdentity = (item: PhraseRepetitionListItemDto): number => item.variantId;
  protected readonly sameRow = (
    item: PhraseRepetitionListItemDto,
    selected: PhraseRepetitionListItemDto | null,
  ): boolean => item.variantId === selected?.variantId;

  protected position(index: number): number {
    return (this.page() - 1) * this.pageSize() + index + 1;
  }
}
