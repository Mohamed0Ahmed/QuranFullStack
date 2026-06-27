import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  LEMMAS_LOADING_LABEL,
  LEMMAS_OPEN_UNIQUE_WORD_LABEL,
  LEMMAS_WORD_DISPLAY_HEADER,
  LEMMAS_WORD_OCCURRENCES_HEADER,
} from '../../models/lemmas.labels';
import { LemmaWordItemDto, LemmaWordView, PagedResultDto } from '../../models/lemmas.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';
import { buildUniqueWordsDeepLink } from '../../state/unique-words-url-sync';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

interface LemmaWordRowViewModel {
  item: LemmaWordItemDto;
  uniqueWordHref: string;
}

@Component({
  selector: 'qd-lemma-words-list',
  standalone: true,
  imports: [PaginationComponent],
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
  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  protected readonly rows = computed((): readonly LemmaWordRowViewModel[] =>
    this.page().items.map((item) => ({
      item,
      uniqueWordHref: deepLinkToHref(
        buildUniqueWordsDeepLink(this.kind(), {
          wordId: item.uniqueWordId,
          view: 'ayahs',
        }),
      ),
    })),
  );

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }

  protected uniqueWordLabel(word: string): string {
    return `${this.openUniqueWordLabel}: ${word}`;
  }
}
