import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { UniqueDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  LEMMAS_LOADING_LABEL,
  LEMMAS_OPEN_UNIQUE_WORD_LABEL,
  LEMMAS_WORD_DISPLAY_HEADER,
  LEMMAS_WORD_OCCURRENCES_HEADER,
  LEMMAS_WORDS_PAGINATION_LABEL,
} from '../../models/lemmas.labels';
import { LemmaWordItemDto, LemmaWordView, PagedResultDto } from '../../models/lemmas.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

interface LemmaWordRowViewModel {
  item: LemmaWordItemDto;
  frame: UniqueDetailFrame;
}

@Component({
  selector: 'qd-lemma-words-list',
  standalone: true,
  imports: [DetailOverlayLinkDirective, PaginationComponent],
  templateUrl: './lemma-words-list.component.html',
  styleUrl: './lemma-words-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaWordsListComponent {
  readonly page = input.required<PagedResultDto<LemmaWordItemDto>>();
  readonly currentPage = input.required<number>();
  readonly kind = input.required<LemmaWordView>();
  readonly loading = input(false);

  readonly pageChange = output<number>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly wordHeader = LEMMAS_WORD_DISPLAY_HEADER;
  protected readonly occurrencesHeader = LEMMAS_WORD_OCCURRENCES_HEADER;
  protected readonly loadingLabel = LEMMAS_LOADING_LABEL;
  protected readonly openUniqueWordLabel = LEMMAS_OPEN_UNIQUE_WORD_LABEL;
  protected readonly paginationLabel = LEMMAS_WORDS_PAGINATION_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  // Mirrors the retired unique-words explorer deep link (same mode + ayahs
  // view); frame defaults are serialized explicitly per the URL contract.
  protected readonly rows = computed((): readonly LemmaWordRowViewModel[] =>
    this.page().items.map((item) => ({
      item,
      frame: {
        kind: 'unique',
        mode: this.kind(),
        id: item.uniqueWordId,
        view: 'ayahs',
        ayahPage: 1,
      },
    })),
  );

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }

  protected uniqueWordLabel(word: string): string {
    return `${this.openUniqueWordLabel}: ${word}`;
  }
}
