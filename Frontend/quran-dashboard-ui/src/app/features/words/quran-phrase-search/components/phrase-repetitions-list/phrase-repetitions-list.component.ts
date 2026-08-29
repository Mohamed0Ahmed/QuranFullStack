import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, output, signal } from '@angular/core';

import { PhraseRepetitionListItemDto } from '../../../../../core/api/generated/models/phrase-repetition-list-item-dto';
import { QdActionDirective } from '../../../../../shared/ui/action/action.directive';
import { QdDataTableComponent } from '../../../../../shared/ui/data-table/data-table.component';

const ROW_HEIGHT = 40;
const COMPACT_ROW_HEIGHT = 88;

@Component({
  selector: 'qd-phrase-repetitions-list',
  standalone: true,
  imports: [QdActionDirective, QdDataTableComponent],
  templateUrl: './phrase-repetitions-list.component.html',
  styleUrl: './phrase-repetitions-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseRepetitionsListComponent {
  private readonly destroyRef = inject(DestroyRef);
  private copiedTimer: ReturnType<typeof setTimeout> | undefined;

  readonly items = input.required<readonly PhraseRepetitionListItemDto[]>();
  readonly activeBuildId = input.required<string>();
  readonly mode = input.required<string>();
  readonly wordCount = input.required<number>();
  readonly query = input.required<string>();
  readonly sort = input.required<string>();
  readonly totalCount = input.required<number>();
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly selectedVariantId = input<number | null>(null);
  readonly disabled = input(false);

  readonly phraseSelected = output<number>();

  protected readonly copiedVariantId = signal<number | null>(null);
  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly compactRowHeight = COMPACT_ROW_HEIGHT;
  protected readonly selectedRow = computed(
    () => this.items().find((item) => item.variantId === this.selectedVariantId()) ?? null,
  );
  protected readonly scrollStateKey = computed(
    () => [
      'words.table.phrase-repetitions',
      this.activeBuildId().toLowerCase(),
      this.mode(),
      this.wordCount(),
      this.query(),
      this.sort(),
      this.pageSize(),
      this.page(),
    ].join('.'),
  );
  protected readonly rowIdentity = (item: PhraseRepetitionListItemDto): number => item.variantId;
  protected readonly sameRow = (
    item: PhraseRepetitionListItemDto,
    selected: PhraseRepetitionListItemDto | null,
  ): boolean => item.variantId === selected?.variantId;

  protected position(index: number): number {
    return (this.page() - 1) * this.pageSize() + index + 1;
  }

  constructor() {
    this.destroyRef.onDestroy(() => clearTimeout(this.copiedTimer));
  }

  protected copyPhrase(event: MouseEvent, item: PhraseRepetitionListItemDto): void {
    event.stopPropagation();
    if (!navigator.clipboard) {
      return;
    }

    void navigator.clipboard.writeText(item.displayText)
      .then(() => this.showCopied(item.variantId))
      .catch(() => undefined);
  }

  private showCopied(variantId: number): void {
    clearTimeout(this.copiedTimer);
    this.copiedVariantId.set(variantId);
    this.copiedTimer = setTimeout(() => this.copiedVariantId.set(null), 2000);
  }
}
