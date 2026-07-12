import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { WORD_TYPES_DETAIL_SUMMARY_LABEL, WORD_TYPES_TABLE_HEADERS } from '../../models/word-types.labels';

@Component({
  selector: 'qd-word-type-detail-summary',
  standalone: true,
  templateUrl: './word-type-detail-summary.component.html',
  styleUrl: './word-type-detail-summary.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeDetailSummaryComponent {
  readonly label = input.required<string>();
  readonly occurrences = input.required<number>();
  readonly ayahs = input.required<number>();
  readonly surahs = input.required<number>();

  protected get headers() { return WORD_TYPES_TABLE_HEADERS; }
  protected get summaryLabel() { return WORD_TYPES_DETAIL_SUMMARY_LABEL; }
}
