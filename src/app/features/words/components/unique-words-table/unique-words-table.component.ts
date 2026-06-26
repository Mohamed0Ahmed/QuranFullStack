import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  afterNextRender,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  LOADING_LABEL,
  OCCURRENCES_CHIP_LABEL,
  ROW_NUMBER_HEADER,
  UNIQUE_WORD_NULL_PLACEHOLDER,
  UNIQUE_WORD_ROOT_HEADER,
  UNIQUE_WORD_TYPE_HEADER,
  UNIQUE_WORD_WORD_HEADER,
  WORD_DRILLDOWN_VIEW_LABELS,
} from '../../models/unique-words.labels';
import {
  UNIQUE_WORDS_PAGE_SIZE,
  UniqueWordListItemViewModel,
  WordDrilldownView,
} from '../../models/unique-words.models';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';
import { buildRootsDeepLink } from '../../state/roots-url-sync';
import { deepLinkToHref } from '../../../../shared/url/deep-link-href';

import { QD_BP_PHONE_MAX_QUERY } from '../../../../shared/layout/breakpoints';

const ROW_HEIGHT_DESKTOP = 48;
const ROW_HEIGHT_MOBILE = 72;
const HAS_RESIZE_OBSERVER = typeof ResizeObserver !== 'undefined';

@Component({
  selector: 'qd-unique-words-table',
  standalone: true,
  imports: [NgTemplateOutlet, ScrollingModule, WordCountChipComponent],
  templateUrl: './unique-words-table.component.html',
  styleUrl: './unique-words-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly UniqueWordListItemViewModel[]>();
  readonly loading = input(false);
  readonly selectedWordId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(UNIQUE_WORDS_PAGE_SIZE);

  readonly rowSelected = output<UniqueWordListItemViewModel>();
  readonly drilldownOpen = output<{ word: UniqueWordListItemViewModel; view: WordDrilldownView }>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly wordHeader = UNIQUE_WORD_WORD_HEADER;
  protected readonly typeHeader = UNIQUE_WORD_TYPE_HEADER;
  protected readonly rootHeader = UNIQUE_WORD_ROOT_HEADER;
  protected readonly nullPlaceholder = UNIQUE_WORD_NULL_PLACEHOLDER;
  protected readonly occurrencesLabel = OCCURRENCES_CHIP_LABEL;
  protected readonly ayahsLabel = WORD_DRILLDOWN_VIEW_LABELS.ayahs;
  protected readonly surahsLabel = WORD_DRILLDOWN_VIEW_LABELS.surahs;
  protected readonly missingLabel = WORD_DRILLDOWN_VIEW_LABELS.missing;
  protected readonly loadingLabel = LOADING_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 12 });
  protected readonly rowHeight = signal(ROW_HEIGHT_DESKTOP);
  protected readonly useVirtualScroll = HAS_RESIZE_OBSERVER;

  @ViewChild(CdkVirtualScrollViewport) private viewport?: CdkVirtualScrollViewport;

  constructor() {
    afterNextRender(() => {
      if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
        const mobileMq = window.matchMedia(QD_BP_PHONE_MAX_QUERY);
        const syncRowHeight = () => {
          this.rowHeight.set(mobileMq.matches ? ROW_HEIGHT_MOBILE : ROW_HEIGHT_DESKTOP);
        };
        syncRowHeight();
        if (typeof mobileMq.addEventListener === 'function') {
          mobileMq.addEventListener('change', syncRowHeight);
          this.destroyRef.onDestroy(() => mobileMq.removeEventListener('change', syncRowHeight));
        }
      }

      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--unique-words-table-scrollbar-gutter',
        '.unique-words-table__body',
        '.unique-words-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected wordTypeLabel(row: UniqueWordListItemViewModel): string {
    return row.primaryWordTypeBroadArabicLabel ?? this.nullPlaceholder;
  }

  protected hasRoot(row: UniqueWordListItemViewModel): boolean {
    return row.rootId !== null && Boolean(row.rootText);
  }

  protected rootHref(row: UniqueWordListItemViewModel): string {
    return deepLinkToHref(
      buildRootsDeepLink({
        rootId: row.rootId ?? undefined,
        view: 'words',
        wordView: 'simple',
      }),
    );
  }

  protected selectRow(row: UniqueWordListItemViewModel): void {
    this.rowSelected.emit(row);
  }

  protected openDrilldown(row: UniqueWordListItemViewModel, view: WordDrilldownView): void {
    this.drilldownOpen.emit({ word: row, view });
  }

  protected isSelected(row: UniqueWordListItemViewModel): boolean {
    return this.selectedWordId() === row.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected trackRowById(_index: number, row: UniqueWordListItemViewModel): number {
    return row.id;
  }

  scrollToTop(): void {
    if (this.useVirtualScroll && this.viewport) {
      this.viewport.scrollToIndex(0, 'auto');
      return;
    }

    const body = this.host.nativeElement.querySelector('.unique-words-table__body') as HTMLElement | null;
    if (body) {
      body.scrollTop = 0;
    }
  }
}
