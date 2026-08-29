import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { PhraseContextBranchesResponse } from '../../../../../core/api/generated/models/phrase-context-branches-response';
import { phraseOccurrenceLabel } from '../phrase-context-copy';

@Component({
  selector: 'qd-phrase-context-current',
  standalone: true,
  templateUrl: './phrase-context-current.component.html',
  styleUrl: './phrase-context-current.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextCurrentComponent {
  readonly branches = input.required<PhraseContextBranchesResponse>();

  protected readonly occurrenceLabel = phraseOccurrenceLabel;
  protected readonly previousDisplayTokens = computed(() =>
    [...this.branches().previousSelection.tokens].reverse(),
  );
}
