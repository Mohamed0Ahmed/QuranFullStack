import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { WORD_TYPES_DETAILS_PANEL_LABEL } from '../../models/word-types.labels';
import { WordTypesDetailState } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-type-details-panel',
  standalone: true,
  templateUrl: './word-type-details-panel.component.html',
  styleUrl: './word-type-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeDetailsPanelComponent {
  readonly state = input<WordTypesDetailState | null>(null);
  protected readonly panelLabel = WORD_TYPES_DETAILS_PANEL_LABEL;
}
