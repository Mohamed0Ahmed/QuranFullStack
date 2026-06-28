import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import {
  STEMS_TYPE_DISTRIBUTION_EMPTY_LABEL,
  STEMS_TYPE_DISTRIBUTION_LABEL,
  STEMS_TYPE_DISTRIBUTION_LOADING_LABEL,
} from '../../models/stems.labels';
import { WORDS_SHARED_HEADERS, WORDS_SHARED_LIST_HEADERS } from '../../models/words-shared.labels';

export interface TypeDistributionItem {
  code: string;
  arabicLabel: string;
  englishLabel: string;
  occurrencesCount: number;
  firstSurahNumber: number;
  firstAyahNumber: number;
  firstWordNumber: number;
}

interface TypeDistributionRow extends TypeDistributionItem {
  dominant: boolean;
}

@Component({
  selector: 'qd-type-distribution-list',
  standalone: true,
  templateUrl: './type-distribution-list.component.html',
  styleUrl: './type-distribution-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TypeDistributionListComponent {
  readonly items = input.required<readonly TypeDistributionItem[]>();
  readonly loading = input(false);

  protected readonly sectionLabel = STEMS_TYPE_DISTRIBUTION_LABEL;
  protected readonly typeHeader = WORDS_SHARED_HEADERS.type;
  protected readonly countHeader = WORDS_SHARED_LIST_HEADERS.occurrences;
  protected readonly loadingLabel = STEMS_TYPE_DISTRIBUTION_LOADING_LABEL;
  protected readonly emptyLabel = STEMS_TYPE_DISTRIBUTION_EMPTY_LABEL;

  protected readonly rows = computed<readonly TypeDistributionRow[]>(() =>
    this.items().map((item, index) => ({
      ...item,
      dominant: index === 0,
    })),
  );

}
