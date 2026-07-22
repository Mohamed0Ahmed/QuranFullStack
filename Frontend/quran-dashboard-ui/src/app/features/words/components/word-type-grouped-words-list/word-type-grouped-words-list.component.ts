import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { WordTypeGroupedMemberWordDto } from '../../models/word-types-detail.models';
import {
  WORD_TYPES_LOADING_LABEL,
  WORD_TYPES_MEMBER_WORDS_EMPTY_LABEL,
  WORD_TYPES_MEMBER_WORDS_PAGINATION_LABEL,
  WORD_TYPES_TABLE_HEADERS,
} from '../../models/word-types.labels';
import { PagedResultDto } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-type-grouped-words-list',
  standalone: true,
  imports: [PaginationComponent],
  templateUrl: './word-type-grouped-words-list.component.html',
  styleUrl: './word-type-grouped-words-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeGroupedWordsListComponent {
  readonly page = input.required<PagedResultDto<WordTypeGroupedMemberWordDto>>();
  readonly currentPage = input.required<number>();
  readonly loading = input(false);

  readonly pageChange = output<number>();

  protected readonly loadingRowPlaceholders = [0, 1, 2, 3, 4, 5] as const;

  protected get headers() { return WORD_TYPES_TABLE_HEADERS; }
  protected get loadingLabel() { return WORD_TYPES_LOADING_LABEL; }
  protected get emptyLabel() { return WORD_TYPES_MEMBER_WORDS_EMPTY_LABEL; }
  protected get paginationLabel() { return WORD_TYPES_MEMBER_WORDS_PAGINATION_LABEL; }

  protected rowKey(item: WordTypeGroupedMemberWordDto): string {
    return `${item.tashkeelWordId}:${item.contextCode}`;
  }
}
