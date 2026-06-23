import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { HighlightedAyahComponent } from '../highlighted-ayah/highlighted-ayah.component';
import {
  AYAH_REF_LABEL,
  MUSHAF_PAGE_REF_LABEL,
} from '../../models/unique-words.labels';
import { PagedResultDto, UniqueWordAyahMatchDto } from '../../models/unique-words.models';
import {
  formatPageRowRangeLabel,
  pageRelativeRowNumber,
  pageRowRange,
} from '../../utils/unique-words-pagination-display';

@Component({
  selector: 'qd-ayah-matches-list',
  standalone: true,
  imports: [HighlightedAyahComponent],
  templateUrl: './ayah-matches-list.component.html',
  styleUrl: './ayah-matches-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AyahMatchesListComponent {
  readonly page = input.required<PagedResultDto<UniqueWordAyahMatchDto>>();
  readonly currentPage = input.required<number>();

  readonly pageChange = output<number>();

  protected readonly ayahRefLabel = AYAH_REF_LABEL;
  protected readonly mushafPageRefLabel = MUSHAF_PAGE_REF_LABEL;

  protected readonly lastPage = computed(() =>
    Math.max(1, Math.ceil(this.page().totalCount / this.page().pageSize)),
  );

  protected readonly pageRangeLabel = computed(() =>
    formatPageRowRangeLabel(
      pageRowRange(this.currentPage(), this.page().pageSize, this.page().totalCount),
      this.page().totalCount,
    ),
  );

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }
}
