import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  OnDestroy,
  signal,
  ViewChild,
  output,
} from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import { LOADING_LABEL, OCCURRENCES_CHIP_LABEL, WORD_DRILLDOWN_VIEW_LABELS } from '../../models/unique-words.labels';
import {
  UniqueWordListItemViewModel,
  WordDrilldownView,
} from '../../models/unique-words.models';

const ROW_HEIGHT = 76;
const LOAD_MORE_THRESHOLD_ROWS = 3;

@Component({
  selector: 'qd-unique-words-table',
  standalone: true,
  imports: [ScrollingModule, WordCountChipComponent],
  templateUrl: './unique-words-table.component.html',
  styleUrl: './unique-words-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsTableComponent implements AfterViewInit, OnDestroy {
  readonly rows = input.required<readonly UniqueWordListItemViewModel[]>();
  readonly selectedWordId = input<number | null>(null);
  readonly loadingMore = input(false);
  readonly hasMore = input(false);

  readonly rowSelected = output<UniqueWordListItemViewModel>();
  readonly drilldownOpen = output<{ word: UniqueWordListItemViewModel; view: WordDrilldownView }>();
  readonly loadMoreRequested = output<void>();

  protected readonly loadingLabel = LOADING_LABEL;
  protected readonly occurrencesLabel = OCCURRENCES_CHIP_LABEL;
  protected readonly ayahsLabel = WORD_DRILLDOWN_VIEW_LABELS.ayahs;
  protected readonly surahsLabel = WORD_DRILLDOWN_VIEW_LABELS.surahs;
  protected readonly missingLabel = WORD_DRILLDOWN_VIEW_LABELS.missing;
  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly useVirtualScroll = signal(false);

  @ViewChild(CdkVirtualScrollViewport) private viewport?: CdkVirtualScrollViewport;

  private lastObservedRowsLength = 0;
  private loadMoreEmittedForLength = -1;
  private resizeObserver?: ResizeObserver;

  constructor() {
    effect(() => {
      const length = this.rows().length;
      if (length !== this.lastObservedRowsLength) {
        this.lastObservedRowsLength = length;
        this.loadMoreEmittedForLength = -1;
      }
    });
  }

  ngAfterViewInit(): void {
    const element = this.viewport?.elementRef.nativeElement;
    if (!element || this.activateVirtualScrollIfSized(element)) {
      return;
    }

    // The viewport can report 0 height at init — mounted while its container is
    // collapsed/`display:none`, behind a tab, or before layout settles. Without
    // re-evaluation the flag would latch `false` and the non-virtual fallback
    // would render every accumulated row. Re-check on resize so virtualization
    // activates once the viewport gains height. (jsdom has no ResizeObserver, so
    // the unit-test path stays on the deterministic non-virtual branch.)
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

  /** Turns on virtual scroll once the viewport has a measurable height. */
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

  protected trackById(_: number, row: UniqueWordListItemViewModel): number {
    return row.id;
  }

  onScrolledIndexChange(index: number): void {
    if (!this.hasMore() || this.loadingMore()) {
      return;
    }

    if (this.rows().length === 0) {
      return;
    }

    const thresholdIndex = Math.max(0, this.rows().length - LOAD_MORE_THRESHOLD_ROWS);

    if (index < thresholdIndex) {
      return;
    }

    if (this.loadMoreEmittedForLength === this.rows().length) {
      return;
    }

    this.loadMoreEmittedForLength = this.rows().length;
    this.loadMoreRequested.emit();
  }
}
