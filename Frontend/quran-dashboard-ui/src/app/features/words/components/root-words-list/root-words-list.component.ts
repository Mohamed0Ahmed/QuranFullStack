import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { UniqueDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  ROOTS_OPEN_UNIQUE_WORD_LABEL,
  ROOTS_WORD_DISPLAY_HEADER,
  ROOTS_WORD_OCCURRENCES_HEADER,
  ROOTS_WORDS_PAGINATION_LABEL,
} from '../../models/roots.labels';
import { PagedResultDto, RootWordItemDto, RootWordView } from '../../models/roots.models';
import { WORDS_LOADING_LABEL } from '../../models/words.labels';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

interface RootWordRowViewModel {
  item: RootWordItemDto;
  frame: UniqueDetailFrame;
}

@Component({
  selector: 'qd-root-words-list',
  standalone: true,
  imports: [DetailOverlayLinkDirective, PaginationComponent],
  templateUrl: './root-words-list.component.html',
  styleUrl: './root-words-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootWordsListComponent {
  readonly page = input.required<PagedResultDto<RootWordItemDto>>();
  readonly currentPage = input.required<number>();
  readonly wordView = input.required<RootWordView>();
  readonly loading = input(false);

  readonly pageChange = output<number>();

  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly displayHeader = ROOTS_WORD_DISPLAY_HEADER;
  protected readonly occurrencesHeader = ROOTS_WORD_OCCURRENCES_HEADER;
  protected readonly openUniqueWordLabel = ROOTS_OPEN_UNIQUE_WORD_LABEL;
  protected readonly loadingLabel = WORDS_LOADING_LABEL;
  protected readonly paginationLabel = ROOTS_WORDS_PAGINATION_LABEL;

  protected readonly rows = computed((): readonly RootWordRowViewModel[] => {
    const wordView = this.wordView();
    return this.page().items.map((item) => ({
      item,
      frame: {
        kind: 'unique',
        mode: wordView,
        id: item.uniqueWordId,
        view: 'ayahs',
        ayahPage: 1,
      },
    }));
  });

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }
}
