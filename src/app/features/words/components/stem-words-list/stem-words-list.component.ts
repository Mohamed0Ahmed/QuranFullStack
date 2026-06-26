import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  STEMS_LOADING_LABEL,
  STEMS_OPEN_UNIQUE_WORD_LABEL,
  STEMS_WORD_DISPLAY_HEADER,
  STEMS_WORD_OCCURRENCES_HEADER,
} from '../../models/stems.labels';
import { StemWordItemDto, PagedResultDto } from '../../models/stems.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';
import { buildUniqueWordsDeepLink } from '../../state/unique-words-url-sync';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

interface StemWordRowViewModel {
  item: StemWordItemDto;
  uniqueWordHref: string;
}

@Component({
  selector: 'qd-stem-words-list',
  standalone: true,
  imports: [PaginationComponent],
  templateUrl: './stem-words-list.component.html',
  styleUrl: './stem-words-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemWordsListComponent {
  readonly page = input.required<PagedResultDto<StemWordItemDto>>();
  readonly currentPage = input.required<number>();
  readonly loading = input(false);

  readonly pageChange = output<number>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly wordHeader = STEMS_WORD_DISPLAY_HEADER;
  protected readonly occurrencesHeader = STEMS_WORD_OCCURRENCES_HEADER;
  protected readonly loadingLabel = STEMS_LOADING_LABEL;
  protected readonly openUniqueWordLabel = STEMS_OPEN_UNIQUE_WORD_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  protected readonly rows = computed((): readonly StemWordRowViewModel[] =>
    this.page().items.map((item) => ({
      item,
      uniqueWordHref: deepLinkToHref(
        buildUniqueWordsDeepLink(item.kind, {
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
