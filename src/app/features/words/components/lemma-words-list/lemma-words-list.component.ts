import { ChangeDetectionStrategy, Component, ElementRef, computed, input, output, viewChild } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  LEMMAS_LOADING_LABEL,
  LEMMAS_OPEN_UNIQUE_WORD_LABEL,
  LEMMAS_WORD_DISPLAY_HEADER,
  LEMMAS_WORD_OCCURRENCES_HEADER,
  LEMMAS_WORD_VIEW_LABELS,
} from '../../models/lemmas.labels';
import { LemmaWordItemDto, LemmaWordView, PagedResultDto, LEMMA_WORD_VIEW_KEYS } from '../../models/lemmas.models';
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
  readonly wordView = input.required<LemmaWordView>();
  readonly loading = input(false);

  readonly pageChange = output<number>();
  readonly wordViewChange = output<LemmaWordView>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly wordHeader = LEMMAS_WORD_DISPLAY_HEADER;
  protected readonly occurrencesHeader = LEMMAS_WORD_OCCURRENCES_HEADER;
  protected readonly loadingLabel = LEMMAS_LOADING_LABEL;
  protected readonly openUniqueWordLabel = LEMMAS_OPEN_UNIQUE_WORD_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });
  protected readonly tabs = LEMMA_WORD_VIEW_KEYS.map((key) => ({
    key,
    label: LEMMAS_WORD_VIEW_LABELS[key],
  }));

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

  protected readonly rows = computed((): readonly LemmaWordRowViewModel[] =>
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

  protected isActive(key: LemmaWordView): boolean {
    return this.wordView() === key;
  }

  protected selectWordView(key: LemmaWordView): void {
    if (this.loading() || key === this.wordView()) {
      return;
    }

    this.wordViewChange.emit(key);
  }

  protected onTabKeydown(event: KeyboardEvent, currentKey: LemmaWordView): void {
    const order = LEMMA_WORD_VIEW_KEYS;
    const index = order.indexOf(currentKey);
    let nextIndex: number | null = null;

    switch (event.key) {
      case 'ArrowLeft':
        nextIndex = (index + 1) % order.length;
        break;
      case 'ArrowRight':
        nextIndex = (index - 1 + order.length) % order.length;
        break;
      case 'Home':
        nextIndex = 0;
        break;
      case 'End':
        nextIndex = order.length - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    if (nextIndex === null) {
      return;
    }

    const nextKey = order[nextIndex];
    this.selectWordView(nextKey);
    this.focusTab(nextKey);
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.page().pageSize, index);
  }

  protected uniqueWordLabel(word: string): string {
    return `${this.openUniqueWordLabel}: ${word}`;
  }

  private focusTab(key: LemmaWordView): void {
    const list = this.tabList()?.nativeElement;
    const tab = list?.querySelector<HTMLElement>(`[data-lemma-word-tab="${key}"]`);
    tab?.focus();
  }
}
