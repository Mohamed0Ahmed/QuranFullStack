import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { PhraseContextBranchOptionDto } from '../../../../../core/api/generated/models/phrase-context-branch-option-dto';
import { PhraseContextSidePageDto } from '../../../../../core/api/generated/models/phrase-context-side-page-dto';
import { PhraseSelectedPathDto } from '../../../../../core/api/generated/models/phrase-selected-path-dto';
import { phraseOccurrenceLabel, phraseOptionLabel } from '../phrase-context-copy';

export type PhraseContextWebSide = 'previous' | 'following';

@Component({
  selector: 'qd-phrase-context-web',
  standalone: true,
  templateUrl: './phrase-context-web.component.html',
  styleUrl: './phrase-context-web.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextWebComponent {
  readonly side = input.required<PhraseContextWebSide>();
  readonly page = input.required<PhraseContextSidePageDto>();
  readonly selection = input.required<PhraseSelectedPathDto>();
  readonly options = input.required<readonly PhraseContextBranchOptionDto[]>();
  readonly busy = input(false);
  readonly focused = input(false);

  readonly optionSelected = output<string>();
  readonly reversed = output<void>();
  readonly moreRequested = output<void>();

  protected readonly occurrenceLabel = phraseOccurrenceLabel;
  protected readonly optionLabel = phraseOptionLabel;

  protected boundaryLabel(option: PhraseContextBranchOptionDto): string {
    if (option.boundaryKind === 'start') {
      return 'بداية الآية';
    }
    if (option.boundaryKind === 'end') {
      return 'نهاية الآية';
    }
    return option.displayText;
  }
}
