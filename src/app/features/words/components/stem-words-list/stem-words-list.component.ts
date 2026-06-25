import { ChangeDetectionStrategy, Component, ElementRef, computed, input, output, viewChild } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  STEMS_LOADING_LABEL,
  STEMS_OPEN_UNIQUE_WORD_LABEL,
  STEMS_WORD_DISPLAY_HEADER,
  STEMS_WORD_OCCURRENCES_HEADER,
  STEMS_WORD_VIEW_LABELS,
} from '../../models/stems.labels';
import { StemWordItemDto, StemWordView, PagedResultDto, STEM_WORD_VIEW_KEYS } from '../../models/stems.models';
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
  readonly wordView = input.required<StemWordView>();
  readonly loading = input(false);

  readonly pageChange = output<number>();
  readonly wordViewChange = output<StemWordView>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly wordHeader = STEMS_WORD_DISPLAY_HEADER;
  protected readonly occurrencesHeader = STEMS_WORD_OCCURRENCES_HEADER;
  protected readonly loadingLabel = STEMS_LOADING_LABEL;
  protected readonly openUniqueWordLabel = STEMS_OPEN_UNIQUE_WORD_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });
  protected readonly tabs = STEM_WORD_VIEW_KEYS.map((key) => ({
    key,
    label: STEMS_WORD_VIEW_LABELS[key],
  }));

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

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

  protected isActive(key: StemWordView): boolean {
    return this.wordView() === key;
  }

  protected selectWordView(key: StemWordView): void {
    if (this.loading() || key === this.wordView()) {
      return;
    }

    this.wordViewChange.emit(key);
  }

  protected onTabKeydown(event: KeyboardEvent, currentKey: StemWordView): void {
    const order = STEM_WORD_VIEW_KEYS;
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

  private focusTab(key: StemWordView): void {
    const list = this.tabList()?.nativeElement;
    const tab = list?.querySelector<HTMLElement>(`[data-stem-word-tab="${key}"]`);
    tab?.focus();
  }
}
