import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  DetailOverlayAyahLinkDirective,
  DetailOverlayBaseTarget,
} from '../../../../core/navigation/detail-overlay/detail-overlay-ayah-link.directive';
import { DetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { HighlightedAyahComponent } from '../highlighted-ayah/highlighted-ayah.component';
import { WORDS_AYAHS_PAGINATION_LABEL, WORDS_LOADING_LABEL } from '../../models/words.labels';
import {
  AYAH_REF_LABEL,
  MUSHAF_PAGE_REF_LABEL,
  OPEN_AYAH_IN_MUSHAF_LABEL,
} from '../../models/unique-words.labels';
import { AyahMatchDto, PagedResultDto } from '../../models/unique-words.models';
import { buildMushafDeepLink } from '../../../mushaf/state/mushaf-url-sync';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

interface AyahMatchRowViewModel {
  match: AyahMatchDto;
  mushafTarget: DetailOverlayBaseTarget;
}

@Component({
  selector: 'qd-ayah-matches-list',
  standalone: true,
  imports: [AyahCardComponent, DetailOverlayAyahLinkDirective, HighlightedAyahComponent, PaginationComponent],
  templateUrl: './ayah-matches-list.component.html',
  styleUrl: './ayah-matches-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AyahMatchesListComponent {
  readonly page = input.required<PagedResultDto<AyahMatchDto>>();
  readonly currentPage = input.required<number>();
  readonly loading = input(false);
  readonly showAnalysisAction = input(false);
  readonly analysisActionLabel = input('');
  readonly parentFrame = input<DetailFrame | null>(null);

  readonly pageChange = output<number>();
  readonly analysisRequested = output<string>();

  protected readonly loadingCardPlaceholders = Array.from({ length: 4 });

  protected readonly rows = computed((): readonly AyahMatchRowViewModel[] =>
    this.page().items.map((match) => {
      const deepLink = buildMushafDeepLink({
        pageNumber: match.pageNumber,
        ayah: match.verseKey,
        focusAyah: match.verseKey,
        panel: 'ayah',
      });
      return {
        match,
        mushafTarget: { basePath: deepLink.path, queryParams: deepLink.queryParams },
      };
    }),
  );

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
    return WORDS_LOADING_LABEL;
  }

  protected get paginationLabel() {
    return WORDS_AYAHS_PAGINATION_LABEL;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }

  protected requestAnalysis(location: string | null | undefined): void {
    if (location) {
      this.analysisRequested.emit(location);
    }
  }
}
