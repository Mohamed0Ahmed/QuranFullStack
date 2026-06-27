import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  LEMMAS_COLUMN_COUNT_LABELS,
  LEMMAS_COLUMN_HEADERS,
  LEMMAS_LOADING_LABEL,
  LEMMAS_ROOT_MISSING_LABEL,
} from '../../models/lemmas.labels';
import {
  LEMMAS_LIST_PAGE_SIZE,
  LemmaListItemViewModel,
  LemmaSurahView,
  LemmaView,
  LemmaWordView,
} from '../../models/lemmas.models';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';
import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { buildRootsDeepLink } from '../../state/roots-url-sync';

import { QD_BP_TABLET_MAX_QUERY } from '../../../../shared/layout/breakpoints';

const ROW_HEIGHT_DESKTOP = 48;
const ROW_HEIGHT_MOBILE = 88;
const HAS_RESIZE_OBSERVER = typeof ResizeObserver !== 'undefined';

export interface LemmaCountOpenedEvent {
  lemma: LemmaListItemViewModel;
  view: LemmaView;
  wordView?: LemmaWordView;
  surahView?: LemmaSurahView;
}

/**
 * Lemmas Explorer nine-column catalogue grid (Feature 016, US1). Sibling of
 * `RootsTableComponent`. Columns: row number, lemma text, owned root (safe
 * new-tab anchor or a non-interactive dash when `rootId` is null), and six count
 * chips.
 *
 * Technical lemma/root IDs are navigation fields and are never rendered as
 * visible labels. The owned-root anchor opens the Roots Explorer in a new tab
 * with `rel="noopener noreferrer"`. Zero-count count controls remain enabled and
 * emit the same mapped detail event as non-zero counts.
 */
@Component({
  selector: 'qd-lemmas-table',
  standalone: true,
  imports: [NgTemplateOutlet, ScrollingModule, WordCountChipComponent],
  templateUrl: './lemmas-table.component.html',
  styleUrl: './lemmas-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmasTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly LemmaListItemViewModel[]>();
  readonly loading = input(false);
  readonly selectedLemmaId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(LEMMAS_LIST_PAGE_SIZE);

  readonly rowSelected = output<LemmaListItemViewModel>();
  readonly countOpened = output<LemmaCountOpenedEvent>();

  protected get headers() {
    return LEMMAS_COLUMN_HEADERS;
  }
  protected get countLabels() {
    return LEMMAS_COLUMN_COUNT_LABELS;
  }
  protected readonly loadingLabel = LEMMAS_LOADING_LABEL;
  protected readonly rootMissingLabel = LEMMAS_ROOT_MISSING_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 12 });
  protected readonly rowHeight = signal(ROW_HEIGHT_DESKTOP);
  protected readonly useVirtualScroll = HAS_RESIZE_OBSERVER;

  private readonly viewport = viewChild(CdkVirtualScrollViewport);

  constructor() {
    afterNextRender(() => {
      if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
        const mobileMq = window.matchMedia(QD_BP_TABLET_MAX_QUERY);
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
        '--lemmas-table-scrollbar-gutter',
        '.lemmas-table__body',
        '.lemmas-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected selectRow(lemma: LemmaListItemViewModel): void {
    this.rowSelected.emit(lemma);
  }

  protected openCount(
    lemma: LemmaListItemViewModel,
    view: LemmaView,
    options: { wordView?: LemmaWordView; surahView?: LemmaSurahView } = {},
  ): void {
    this.countOpened.emit({ lemma, view, ...options });
  }

  protected isSelected(lemma: LemmaListItemViewModel): boolean {
    return this.selectedLemmaId() === lemma.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected trackRowById(_index: number, lemma: LemmaListItemViewModel): number {
    return lemma.id;
  }

  /**
   * Owned-root deep link into the Roots Explorer. Only rendered when the lemma
   * has a non-null owned `rootId`; uses numeric identity, never text lookup.
   */
  protected rootHref(rootId: number): string {
    return deepLinkToHref(
      buildRootsDeepLink({ rootId, view: 'words', wordView: 'simple' }),
    );
  }

  scrollToTop(): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll && viewport) {
      viewport.scrollToIndex(0, 'auto');
      return;
    }

    const body = this.host.nativeElement.querySelector('.lemmas-table__body') as HTMLElement | null;
    if (body) {
      body.scrollTop = 0;
    }
  }
}
