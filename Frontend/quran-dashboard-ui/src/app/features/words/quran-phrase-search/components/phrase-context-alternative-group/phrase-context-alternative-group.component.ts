import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { PhraseContextBranchOptionDto } from '../../../../../core/api/generated/models/phrase-context-branch-option-dto';
import type { PhraseContextWebSide } from '../phrase-context-web/phrase-context-web.component';

@Component({
  selector: 'qd-phrase-context-alternative-group',
  standalone: true,
  templateUrl: './phrase-context-alternative-group.component.html',
  styleUrl: './phrase-context-alternative-group.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextAlternativeGroupComponent {
  readonly side = input.required<PhraseContextWebSide>();
  readonly options = input.required<readonly PhraseContextBranchOptionDto[]>();
  readonly disabled = input(false);
  readonly cleared = output<void>();
}
