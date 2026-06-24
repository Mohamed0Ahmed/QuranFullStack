import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  ROOTS_OPEN_UNIQUE_WORD_LABEL,
  ROOTS_WORD_DISPLAY_HEADER,
  ROOTS_WORD_OCCURRENCES_HEADER,
} from '../../models/roots.labels';
import { PagedResultDto, RootWordItemDto, RootWordView } from '../../models/roots.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';
import { buildUniqueWordsDeepLink } from '../../state/unique-words-url-sync';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

@Component({
  selector: 'qd-root-words-list',
  standalone: true,
  imports: [PaginationComponent],
  templateUrl: './root-words-list.component.html',
  styleUrl: './root-words-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootWordsListComponent {
  readonly page = input.required<PagedResultDto<RootWordItemDto>>();
  readonly currentPage = input.required<number>();
  readonly wordView = input.required<RootWordView>();

  readonly pageChange = output<number>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly displayHeader = ROOTS_WORD_DISPLAY_HEADER;
  protected readonly occurrencesHeader = ROOTS_WORD_OCCURRENCES_HEADER;
  protected readonly openUniqueWordLabel = ROOTS_OPEN_UNIQUE_WORD_LABEL;

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }

  protected uniqueWordHref(item: RootWordItemDto): string {
    return deepLinkToHref(
      buildUniqueWordsDeepLink(this.wordView(), {
        wordId: item.uniqueWordId,
        view: 'ayahs',
      }),
    );
  }
}
