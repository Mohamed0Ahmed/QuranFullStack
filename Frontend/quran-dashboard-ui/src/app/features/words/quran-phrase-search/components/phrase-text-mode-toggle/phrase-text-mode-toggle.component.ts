import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import {
  PHRASE_TEXT_MODE_LABELS,
  PhraseTextMode,
} from '../../models/phrase-repetitions.models';

@Component({
  selector: 'qd-phrase-text-mode-toggle',
  standalone: true,
  templateUrl: './phrase-text-mode-toggle.component.html',
  styleUrl: './phrase-text-mode-toggle.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseTextModeToggleComponent {
  readonly mode = input.required<PhraseTextMode>();
  readonly availableModes = input.required<readonly PhraseTextMode[]>();
  readonly disabled = input(false);

  readonly modeChange = output<PhraseTextMode>();

  protected readonly labels = PHRASE_TEXT_MODE_LABELS;

  protected select(mode: PhraseTextMode): void {
    if (!this.disabled() && mode !== this.mode()) {
      this.modeChange.emit(mode);
    }
  }
}
