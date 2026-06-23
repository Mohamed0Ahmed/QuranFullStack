import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  OnDestroy,
  signal,
  ViewChild,
  output,
} from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  LOADING_LABEL,
  OCCURRENCES_CHIP_LABEL,
  ROW_NUMBER_HEADER,
  WORD_DRILLDOWN_VIEW_LABELS,
} from '../../models/unique-words.labels';
import {
  UNIQUE_WORDS_PAGE_SIZE,
  UniqueWordListItemViewModel,
  WordDrilldownView,
} from '../../models/unique-words.models';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';

const ROW_HEIGHT = 60;

@Component({
  selector: 'qd-unique-words-table',
  standalone: true,
  imports: [ScrollingModule, WordCountChipComponent],
  templateUrl: './unique-words-table.component.html',
  styleUrl: './unique-words-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsTableComponent implements OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly rows = input.required<readonly UniqueWordListItemViewModel[]>();
  readonly loading = input(false);
  readonly selectedWordId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(UNIQUE_WORDS_PAGE_SIZE);

  readonly rowSelected = output<UniqueWordListItemViewModel>();
  readonly drilldownOpen = output<{ word: UniqueWordListItemViewModel; view: WordDrilldownView }>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly occurrencesLabel = OCCURRENCES_CHIP_LABEL;
  protected readonly ayahsLabel = WORD_DRILLDOWN_VIEW_LABELS.ayahs;
  protected readonly surahsLabel = WORD_DRILLDOWN_VIEW_LABELS.surahs;
  protected readonly missingLabel = WORD_DRILLDOWN_VIEW_LABELS.missing;
  protected readonly loadingLabel = LOADING_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 12 });
  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly useVirtualScroll = signal(false);

  @ViewChild(CdkVirtualScrollViewport) set viewportRef(viewport: CdkVirtualScrollViewport | undefined) {
    if (this.viewport === viewport) {
      return;
    }

    this.resizeObserver?.disconnect();
    this.resizeObserver = undefined;
    this.viewport = viewport;

    if (!viewport) {
      return;
    }

    this.initializeVirtualScroll(viewport.elementRef.nativeElement);
  }

  private viewport?: CdkVirtualScrollViewport;

  private resizeObserver?: ResizeObserver;

  // The viewport can report 0 height while its container is collapsed,
  // behind a tab, or before layout settles. Without re-evaluation the flag
  // would latch `false` and the non-virtual fallback would render every
  // accumulated row. Re-check on resize so virtualization activates once the
  // viewport gains height. (jsdom has no ResizeObserver, so the unit-test path
  // stays on the deterministic non-virtual branch.)
  private initializeVirtualScroll(element: HTMLElement): void {
    if (this.activateVirtualScrollIfSized(element)) {
      return;
    }

    if (typeof ResizeObserver === 'undefined') {
      return;
    }

    this.resizeObserver = new ResizeObserver(() => {
      if (this.activateVirtualScrollIfSized(element)) {
        this.resizeObserver?.disconnect();
        this.resizeObserver = undefined;
      }
    });
    this.resizeObserver.observe(element);
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  private activateVirtualScrollIfSized(element: HTMLElement): boolean {
    const hasHeight = element.clientHeight > 0;
    if (hasHeight) {
      this.useVirtualScroll.set(true);
    }
    return hasHeight;
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

  scrollToTop(): void {
    if (this.useVirtualScroll() && this.viewport) {
      this.viewport.scrollToIndex(0, 'auto');
      return;
    }

    const body = this.host.nativeElement.querySelector('.unique-words-table__body') as HTMLElement | null;
    if (body) {
      body.scrollTop = 0;
    }
  }
}
