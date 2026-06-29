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
  STEMS_COLUMN_COUNT_LABELS,
  STEMS_COLUMN_HEADERS,
  STEMS_LEMMA_LINK_PREFIX,
  STEMS_LEMMA_MISSING_ARIA,
  STEMS_LEMMA_MISSING_LABEL,
  STEMS_LOADING_LABEL,
  STEMS_ROOT_LINK_PREFIX,
  STEMS_ROOT_MISSING_ARIA,
  STEMS_ROOT_MISSING_LABEL,
  STEMS_TABLE_BODY_LABEL,
  STEMS_TABLE_LABEL,
} from '../../models/stems.labels';
import {
  STEMS_LIST_PAGE_SIZE,
  StemListItemViewModel,
  StemSurahView,
  StemView,
  StemWordView,
} from '../../models/stems.models';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';
import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { buildLemmasDeepLink } from '../../state/lemmas-url-sync';
import { buildRootsDeepLink } from '../../state/roots-url-sync';

import { QD_BP_TABLET_MAX_QUERY } from '../../../../shared/layout/breakpoints';

const ROW_HEIGHT_DESKTOP = 48;
const ROW_HEIGHT_MOBILE = 92;
const HAS_RESIZE_OBSERVER = typeof ResizeObserver !== 'undefined';

export interface StemCountOpenedEvent {
  stem: StemListItemViewModel;
  view: StemView;
  wordView?: StemWordView;
  surahView?: StemSurahView;
}

@Component({
  selector: 'qd-stems-table',
  standalone: true,
  imports: [NgTemplateOutlet, ScrollingModule, WordCountChipComponent],
  templateUrl: './stems-table.component.html',
  styleUrls: ['./stems-table.component.scss', './stems-table.component.mobile.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemsTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly StemListItemViewModel[]>();
  readonly loading = input(false);
  readonly selectedStemId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(STEMS_LIST_PAGE_SIZE);

  readonly rowSelected = output<StemListItemViewModel>();
  readonly countOpened = output<StemCountOpenedEvent>();

  protected get headers() {
    return STEMS_COLUMN_HEADERS;
  }

  protected get countLabels() {
    return STEMS_COLUMN_COUNT_LABELS;
  }

  protected readonly loadingLabel = STEMS_LOADING_LABEL;
  protected readonly tableLabel = STEMS_TABLE_LABEL;
  protected readonly tableBodyLabel = STEMS_TABLE_BODY_LABEL;
  protected readonly lemmaLinkPrefix = STEMS_LEMMA_LINK_PREFIX;
  protected readonly rootLinkPrefix = STEMS_ROOT_LINK_PREFIX;
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
        '--stems-table-scrollbar-gutter',
        '.stems-table__body',
        '.stems-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected selectRow(stem: StemListItemViewModel): void {
    this.rowSelected.emit(stem);
  }

  protected openCount(
    stem: StemListItemViewModel,
    view: StemView,
    options: { wordView?: StemWordView; surahView?: StemSurahView } = {},
  ): void {
    this.countOpened.emit({ stem, view, ...options });
  }

  protected isSelected(stem: StemListItemViewModel): boolean {
    return this.selectedStemId() === stem.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected trackRowById(_index: number, stem: StemListItemViewModel): number {
    return stem.id;
  }

  protected lemmaHref(lemmaId: number): string {
    return deepLinkToHref(buildLemmasDeepLink({ lemmaId, view: 'words', wordView: 'simple' }));
  }

  protected rootHref(rootId: number): string {
    return deepLinkToHref(buildRootsDeepLink({ rootId, view: 'words', wordView: 'simple' }));
  }

  scrollToTop(): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll && viewport) {
      viewport.scrollToIndex(0, 'auto');
      return;
    }

    const body = this.host.nativeElement.querySelector('.stems-table__body') as HTMLElement | null;
    if (body) {
      body.scrollTop = 0;
    }
  }

  protected lemmaMissingLabel(): string {
    return STEMS_LEMMA_MISSING_LABEL;
  }

  protected lemmaMissingAria(): string {
    return STEMS_LEMMA_MISSING_ARIA;
  }

  protected rootMissingLabel(): string {
    return STEMS_ROOT_MISSING_LABEL;
  }

  protected rootMissingAria(): string {
    return STEMS_ROOT_MISSING_ARIA;
  }
}
