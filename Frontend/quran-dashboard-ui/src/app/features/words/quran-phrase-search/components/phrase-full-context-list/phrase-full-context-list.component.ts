import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { PhraseExactTokenDto } from '../../../../../core/api/generated/models/phrase-exact-token-dto';
import { PhraseFullContextGroupDto } from '../../../../../core/api/generated/models/phrase-full-context-group-dto';
import { QdDataTableComponent } from '../../../../../shared/ui/data-table/data-table.component';

const ROW_HEIGHT = 64;
const COMPACT_ROW_HEIGHT = 96;

@Component({
  selector: 'qd-phrase-full-context-list',
  standalone: true,
  imports: [QdDataTableComponent],
  templateUrl: './phrase-full-context-list.component.html',
  styleUrl: './phrase-full-context-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseFullContextListComponent {
  readonly items = input.required<readonly PhraseFullContextGroupDto[]>();
  readonly totalCount = input.required<number>();
  readonly selectedContextRef = input<string | null>(null);
  readonly busy = input(false);
  readonly selectedPreviousWordCount = input(0);
  readonly selectedFollowingWordCount = input(0);

  readonly contextSelected = output<string>();

  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly compactRowHeight = COMPACT_ROW_HEIGHT;
  protected readonly rowIdentity = (item: PhraseFullContextGroupDto): string => item.contextRef;
  protected readonly canOpenOccurrences = (item: PhraseFullContextGroupDto): boolean =>
    item.exactFullContextCount > 1;
  protected readonly sameRow = (
    item: PhraseFullContextGroupDto,
    selected: PhraseFullContextGroupDto | null,
  ): boolean => item.contextRef === selected?.contextRef;
  protected readonly selectedRow = computed(
    () => this.items().find((item) => item.contextRef === this.selectedContextRef()) ?? null,
  );

  protected previousForDisplay(tokens: readonly PhraseExactTokenDto[]): PhraseExactTokenDto[] {
    return [...tokens].reverse();
  }

  protected isSelectedPrevious(index: number, total: number): boolean {
    return index >= total - this.selectedPreviousWordCount();
  }

  protected isSelectedFollowing(index: number): boolean {
    return index < this.selectedFollowingWordCount();
  }
}
