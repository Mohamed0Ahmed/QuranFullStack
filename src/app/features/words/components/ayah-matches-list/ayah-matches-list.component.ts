import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { HighlightedAyahComponent } from '../highlighted-ayah/highlighted-ayah.component';
import { ROOTS_LOADING_LABEL } from '../../models/roots.labels';
import {
  AYAH_REF_LABEL,
  MUSHAF_PAGE_REF_LABEL,
  OPEN_AYAH_IN_MUSHAF_LABEL,
} from '../../models/unique-words.labels';
import { AyahMatchDto, PagedResultDto } from '../../models/unique-words.models';
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
  readonly page = input.required<PagedResultDto<AyahMatchDto>>();
  readonly currentPage = input.required<number>();
  readonly loading = input(false);

  readonly pageChange = output<number>();

  protected readonly loadingCardPlaceholders = Array.from({ length: 4 });

  // Getters defer label resolution past module init — these cross-module consts
  // can be in the TDZ at field-init time in the Vitest SSR bundle (the labels
  // module is still wiring its routing dependency), which would leave the
  // template bindings undefined.
  protected get ayahRefLabel() {
    return AYAH_REF_LABEL;
  }

  protected get mushafPageRefLabel() {
    return MUSHAF_PAGE_REF_LABEL;
  }

  protected get openAyahInMushafLabel() {
    return OPEN_AYAH_IN_MUSHAF_LABEL;
  }

  protected get loadingLabel() {
    return ROOTS_LOADING_LABEL;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }

  protected mushafHref(match: AyahMatchDto): string {
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
