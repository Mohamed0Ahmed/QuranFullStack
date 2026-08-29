import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { PhraseResolutionCandidateDto } from '../../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseResolutionViewState } from '../../models/phrase-query.models';
import { PhraseTextMode } from '../../models/phrase-repetitions.models';
import { PhraseTextModeToggleComponent } from '../phrase-text-mode-toggle/phrase-text-mode-toggle.component';

@Component({
  selector: 'qd-phrase-query-resolution',
  standalone: true,
  imports: [PhraseTextModeToggleComponent],
  templateUrl: './phrase-query-resolution.component.html',
  styleUrl: './phrase-query-resolution.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseQueryResolutionComponent {
  readonly state = input.required<PhraseResolutionViewState>();
  readonly availableModes = input.required<readonly PhraseTextMode[]>();
  readonly label = input('عبارة البحث');
  readonly help = input('تُرسل العبارة عند الضغط على زر البحث أو مفتاح Enter فقط.');
  readonly inlineControls = input(false);

  readonly draftChange = output<string>();
  readonly modeChange = output<PhraseTextMode>();
  readonly submitRequested = output<void>();
  readonly candidateSelected = output<PhraseResolutionCandidateDto>();

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.submitRequested.emit();
    }
  }
}
