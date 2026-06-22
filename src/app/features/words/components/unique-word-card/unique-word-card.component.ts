import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  OCCURRENCES_CHIP_LABEL,
  WORD_DRILLDOWN_VIEW_LABELS,
} from '../../models/unique-words.labels';
import { UniqueWordListItemDto } from '../../models/unique-words.models';

/**
 * One unique-word card. The primary label is the Uthmani display text; four
 * count chips show المواضع / الآيات / السور / لم يذكر في. Drill-down chips are
 * disabled in US2 (the modal is added in US3), so the chips render but do not
 * navigate. The occurrence chip is summary-only in v1.
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

  protected readonly occurrencesLabel = OCCURRENCES_CHIP_LABEL;
  protected readonly ayahsLabel = WORD_DRILLDOWN_VIEW_LABELS.ayahs;
  protected readonly surahsLabel = WORD_DRILLDOWN_VIEW_LABELS.surahs;
  protected readonly missingLabel = WORD_DRILLDOWN_VIEW_LABELS.missing;
}
