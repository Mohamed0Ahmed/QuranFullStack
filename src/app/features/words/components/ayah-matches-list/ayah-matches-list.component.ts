import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { HighlightedAyahComponent } from '../highlighted-ayah/highlighted-ayah.component';
import {
  AYAH_REF_LABEL,
  MUSHAF_PAGE_REF_LABEL,
  OPEN_AYAH_IN_MUSHAF_LABEL,
} from '../../models/unique-words.labels';
import { PagedResultDto, UniqueWordAyahMatchDto } from '../../models/unique-words.models';
import { buildMushafDeepLink } from '../../../mushaf/state/mushaf-url-sync';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

@Component({
  selector: 'qd-ayah-matches-list',
  standalone: true,
  imports: [HighlightedAyahComponent, PaginationComponent],
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
  protected readonly openAyahInMushafLabel = OPEN_AYAH_IN_MUSHAF_LABEL;

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }

  protected mushafHref(match: UniqueWordAyahMatchDto): string {
    return deepLinkToHref(
      buildMushafDeepLink({
        pageNumber: match.pageNumber,
        ayah: match.verseKey,
        focusAyah: match.verseKey,
        panel: 'ayah',
      }),
    );
  }
}
