import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, inject, input, output, signal, viewChild } from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import { LEMMAS_COLUMN_COUNT_LABELS, LEMMAS_COLUMN_HEADERS, LEMMAS_LOADING_LABEL, LEMMAS_NO_RESULTS_LABEL, LEMMAS_ROOT_MISSING_ARIA, LEMMAS_ROOT_MISSING_LABEL, LEMMAS_ROOT_LINK_PREFIX, LEMMAS_TABLE_BODY_LABEL, LEMMAS_TABLE_LABEL } from '../../models/lemmas.labels';
import { DEFAULT_LEMMA_SORT, LEMMAS_LIST_PAGE_SIZE, LEMMA_SORT_COLUMNS, LemmaListItemViewModel, LemmaSort, LemmaSurahView, LemmaView, LemmaWordView, LoadStatus, normalizeLemmaSort } from '../../models/lemmas.models';
import { isMorphologyCountActive, MorphologyColumnKey, resolveMorphologyActiveColumn } from '../../utils/explorer-count-active';
import { ExplorerInteractionSource, handleExplorerTableKeydown } from '../../utils/explorer-table-keydown';
import { ExplorerTableSortController } from '../../utils/explorer-table-sort.controller';
import { ExplorerRowNavDirection, scrollExplorerRowIntoView } from '../../utils/explorer-table-scroll';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';
import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { buildRootsDeepLink } from '../../state/roots-url-sync';

import { QD_BP_TABLET_MAX_QUERY } from '../../../../shared/layout/breakpoints';

const ROW_HEIGHT_DESKTOP = 40;
const ROW_HEIGHT_MOBILE = 104;
const HAS_RESIZE_OBSERVER = typeof ResizeObserver !== 'undefined';
type LemmaTableColumnKey = Exclude<MorphologyColumnKey, 'lemmas'>;

const LEMMA_TABLE_COLUMN_ORDER = [
  'stems',
  'tashkeel',
  'simple',
  'surahs',
  'ayahs',
  'occurrences',
] as const satisfies readonly LemmaTableColumnKey[];

export interface LemmaCountOpenedEvent {
  lemma: LemmaListItemViewModel;
  column?: LemmaTableColumnKey;
  view: LemmaView;
  wordView?: LemmaWordView;
  surahView?: LemmaSurahView;
  source?: ExplorerInteractionSource;
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
  /**
   * The list's own status, so the error / no-results states render INSIDE this mounted shell
   * instead of above the page grid (Feature 030, N3 row 5). `loading` still drives the skeleton
   * body — this input is only consulted for the states that replace the body.
   */
  readonly status = input<LoadStatus>('idle');
  readonly errorMessage = input('');
  readonly selectedLemmaId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(LEMMAS_LIST_PAGE_SIZE);
  readonly activeView = input<LemmaView | null>(null);
  readonly activeWordView = input<LemmaWordView | null>(null);
  readonly activeSurahView = input<LemmaSurahView | null>(null);
  readonly activeColumn = input<LemmaTableColumnKey | null>(null);
  readonly sort = input<LemmaSort>(DEFAULT_LEMMA_SORT);

  readonly rowSelected = output<LemmaListItemViewModel>();
  readonly countOpened = output<LemmaCountOpenedEvent>();
  /** null = release the sort param back to the default (ترتيب المصحف). */
  readonly sortChange = output<LemmaSort | null>();

  protected readonly sortControl = new ExplorerTableSortController<LemmaSort>(
    () => this.sort(),
    (value) => normalizeLemmaSort(value),
    (sort) => this.sortChange.emit(sort),
  );

  protected get sortColumns(): typeof LEMMA_SORT_COLUMNS { return LEMMA_SORT_COLUMNS; }
  protected get headers() { return LEMMAS_COLUMN_HEADERS; }
  protected get countLabels() { return LEMMAS_COLUMN_COUNT_LABELS; }
  protected readonly loadingLabel = LEMMAS_LOADING_LABEL;
  protected readonly tableLabel = LEMMAS_TABLE_LABEL;
  protected readonly tableBodyLabel = LEMMAS_TABLE_BODY_LABEL;
  protected get noResultsLabel(): string {
    return LEMMAS_NO_RESULTS_LABEL;
  }
  protected readonly rootLinkPrefix = LEMMAS_ROOT_LINK_PREFIX;
  protected readonly rootMissingAria = LEMMAS_ROOT_MISSING_ARIA;
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
    column: LemmaTableColumnKey,
    view: LemmaView,
    options: { wordView?: LemmaWordView; surahView?: LemmaSurahView } = {},
    source: ExplorerInteractionSource = 'immediate',
  ): void {
    this.countOpened.emit({ lemma, column, view, source, ...options });
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

  protected isCountActive(
    lemma: LemmaListItemViewModel,
    column: LemmaTableColumnKey,
    count: number,
  ): boolean {
    return isMorphologyCountActive({
      rowId: lemma.id,
      selectedRowId: this.selectedLemmaId(),
      column,
      activeColumn: resolveMorphologyActiveColumn({
        view: this.activeView(),
        wordView: this.activeWordView(),
        surahView: this.activeSurahView(),
        activeColumn: this.activeColumn(),
      }),
      disabled: count === 0,
    });
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

  protected onTableKeydown(event: KeyboardEvent): void {
    if (this.loading()) {
      return;
    }
    handleExplorerTableKeydown({
      event,
      rows: this.rows(),
      selectedRowId: this.selectedLemmaId(),
      currentColumn: this.currentColumn(),
      columnOrder: LEMMA_TABLE_COLUMN_ORDER,
      isColumnEnabled: (row, column) => this.isColumnEnabled(row, column),
      emitColumnTarget: (row, column, source) => this.emitColumnTarget(row, column, source),
      scrollToRow: (index, direction) => this.scrollToRow(index, direction),
    });
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

  private emitColumnTarget(
    lemma: LemmaListItemViewModel,
    column: LemmaTableColumnKey,
    source: ExplorerInteractionSource,
  ): void {
    switch (column) {
      case 'occurrences':
      case 'ayahs':
        this.openCount(lemma, column, 'ayahs', {}, source);
        return;
      case 'surahs':
        this.openCount(lemma, column, 'surahs', { surahView: 'mentioned' }, source);
        return;
      case 'simple':
        this.openCount(lemma, column, 'words', { wordView: 'simple' }, source);
        return;
      case 'tashkeel':
        this.openCount(lemma, column, 'words', { wordView: 'tashkeel' }, source);
        return;
      case 'stems':
        this.openCount(lemma, column, 'stems', {}, source);
        return;
    }
  }

  private isColumnEnabled(lemma: LemmaListItemViewModel, column: LemmaTableColumnKey): boolean {
    switch (column) {
      case 'occurrences':
        return lemma.occurrencesCount > 0;
      case 'ayahs':
        return lemma.ayahsCount > 0;
      case 'surahs':
        return lemma.surahsCount > 0;
      case 'simple':
        return lemma.simpleWordsCount > 0;
      case 'tashkeel':
        return lemma.tashkeelWordsCount > 0;
      case 'stems':
        return lemma.stemsCount > 0;
    }
  }

  private currentColumn(): LemmaTableColumnKey | null {
    const column = resolveMorphologyActiveColumn({
      view: this.activeView(),
      wordView: this.activeWordView(),
      surahView: this.activeSurahView(),
      activeColumn: this.activeColumn(),
    });

    return column === 'lemmas' ? null : column;
  }

  private scrollToRow(index: number, direction: ExplorerRowNavDirection): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll && viewport) {
      scrollExplorerRowIntoView({
        targetIndex: index,
        direction,
        itemSize: this.rowHeight(),
        viewport,
      });
      return;
    }

    const body = this.host.nativeElement.querySelector('.lemmas-table__body') as HTMLElement | null;
    scrollExplorerRowIntoView({
      targetIndex: index,
      direction,
      itemSize: this.rowHeight(),
      container: body,
    });
  }
}
