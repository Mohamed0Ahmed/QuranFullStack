import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  OCCURRENCES_CHIP_LABEL,
  WORD_DRILLDOWN_VIEW_LABELS,
} from '../../models/unique-words.labels';
import { UniqueWordListItemDto, WordDrilldownView } from '../../models/unique-words.models';

/**
 * One unique-word card. Drill-down chips open the modal (US3).
 */
@Component({
  selector: 'qd-unique-word-card',
  standalone: true,
  imports: [WordCountChipComponent],
  templateUrl: './unique-word-card.component.html',
  styleUrl: './unique-word-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordCardComponent {
  readonly word = input.required<UniqueWordListItemDto>();

  readonly drilldownOpen = output<WordDrilldownView>();

  protected readonly occurrencesLabel = OCCURRENCES_CHIP_LABEL;
  protected readonly ayahsLabel = WORD_DRILLDOWN_VIEW_LABELS.ayahs;
  protected readonly surahsLabel = WORD_DRILLDOWN_VIEW_LABELS.surahs;
  protected readonly missingLabel = WORD_DRILLDOWN_VIEW_LABELS.missing;
}
