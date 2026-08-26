import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { PhraseExactTokenDto } from '../../../../../core/api/generated/models/phrase-exact-token-dto';
import { PhraseFullContextGroupDto } from '../../../../../core/api/generated/models/phrase-full-context-group-dto';
import {
  QdResultItemDirective,
  QdResultListDirective,
} from '../../../../../shared/ui/result-list/result-list.directive';

@Component({
  selector: 'qd-phrase-full-context-list',
  standalone: true,
  imports: [QdResultItemDirective, QdResultListDirective],
  templateUrl: './phrase-full-context-list.component.html',
  styleUrl: './phrase-full-context-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseFullContextListComponent {
  readonly items = input.required<readonly PhraseFullContextGroupDto[]>();
  readonly totalCount = input.required<number>();
  readonly selectedContextRef = input<string | null>(null);
  readonly nextCursor = input<string | null>(null);
  readonly busy = input(false);

  readonly contextSelected = output<string>();
  readonly moreRequested = output<void>();

  protected previousForDisplay(tokens: readonly PhraseExactTokenDto[]): PhraseExactTokenDto[] {
    return [...tokens].reverse();
  }
}
