import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { UniqueDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  STEMS_LOADING_LABEL,
  STEMS_OPEN_UNIQUE_WORD_LABEL,
  STEMS_WORD_DISPLAY_HEADER,
  STEMS_WORD_OCCURRENCES_HEADER,
  STEMS_WORDS_PAGINATION_LABEL,
} from '../../models/stems.labels';
import { StemWordItemDto, PagedResultDto } from '../../models/stems.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

interface StemWordRowViewModel {
  item: StemWordItemDto;
  frame: UniqueDetailFrame;
}

@Component({
  selector: 'qd-stem-words-list',
  standalone: true,
  imports: [DetailOverlayLinkDirective, PaginationComponent],
  templateUrl: './stem-words-list.component.html',
  styleUrl: './stem-words-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemWordsListComponent {
  readonly page = input.required<PagedResultDto<StemWordItemDto>>();
  readonly currentPage = input.required<number>();
  readonly wordView = input<'simple' | 'tashkeel'>('simple');
  readonly loading = input(false);

  readonly pageChange = output<number>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly wordHeader = STEMS_WORD_DISPLAY_HEADER;
  protected readonly occurrencesHeader = STEMS_WORD_OCCURRENCES_HEADER;
  protected readonly loadingLabel = STEMS_LOADING_LABEL;
  protected readonly openUniqueWordLabel = STEMS_OPEN_UNIQUE_WORD_LABEL;
  protected readonly paginationLabel = STEMS_WORDS_PAGINATION_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  protected readonly rows = computed((): readonly StemWordRowViewModel[] =>
    this.page().items.map((item) => ({
      item,
      frame: {
        kind: 'unique',
        mode: this.wordView(),
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
