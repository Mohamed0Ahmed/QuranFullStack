import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, Subscription, debounceTime } from 'rxjs';

import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { ExplorerCountRangeFilterComponent } from '../../components/explorer-count-range-filter/explorer-count-range-filter.component';
import { ExplorerResultCountComponent } from '../../components/explorer-result-count/explorer-result-count.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { StemAyahTypeFiltersComponent } from '../../components/stem-ayah-type-filters/stem-ayah-type-filters.component';
import { StemDetailsPanelComponent } from '../../components/stem-details-panel/stem-details-panel.component';
import { StemLemmasListComponent } from '../../components/stem-lemmas-list/stem-lemmas-list.component';
import { StemCountOpenedEvent, StemsTableComponent } from '../../components/stems-table/stems-table.component';
import { StemWordsListComponent } from '../../components/stem-words-list/stem-words-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { STEMS_EMPTY_SELECTION_LABEL, STEMS_EMPTY_VIEW_LABEL, STEMS_LIST_PAGINATION_LABEL, STEMS_LOADING_LABEL, STEMS_NO_RESULTS_LABEL, STEMS_NOT_FOUND_LABEL, STEMS_PAGE_TITLE, STEMS_PANEL_SURFACE_LABEL, STEMS_RESULT_COUNT_LABEL, STEMS_SEARCH_LABEL, STEMS_SEARCH_PLACEHOLDER, STEMS_SORT_LABELS, STEMS_SURAHS_TABLIST_LABEL, STEMS_SURAHS_VIEW_LABELS, STEMS_TABLE_LABEL, STEMS_WORDS_TABLIST_LABEL, STEMS_WORD_VIEW_LABELS } from '../../models/stems.labels';
import { DEFAULT_STEM_VIEW, PagedResultDto, STEMS_RANGE_METRICS, STEM_DETAIL_PAGE_SIZE, StemListItemViewModel, StemSort, StemSurahView, StemView, StemWordItemDto, StemWordView } from '../../models/stems.models';
import { AyahMatchDto, PagedResultDto as SharedPagedResultDto } from '../../models/unique-words.models';
import { StemsDetailFacade } from '../../state/stems-detail.facade';
import { StemsExplorerFacade } from '../../state/stems-explorer.facade';
import { buildClearSelectionQueryParams, buildStemsQueryParams, parseStemsQueryParams } from '../../state/stems-url-sync';
import { MorphologyColumnKey, parseMorphologyColumnKey, resolveMorphologyActiveColumn } from '../../utils/explorer-count-active';
import { ExplorerTableFocusController } from '../../utils/explorer-table-focus-controller';
import { mapStemAyahMatchToShared } from '../../utils/stem-ayah-match.mapper';
import { EMPTY_RANGE_FILTERS, RangeFilters, buildRangeQueryParams } from '../../state/words-range-filters';

type StemTableColumnKey = Exclude<MorphologyColumnKey, 'stems'>;
type StemPanelState = ReturnType<StemsDetailFacade['panelState']>;
type StemCountTarget = StemCountOpenedEvent & { column: StemTableColumnKey };

@Component({
  selector: 'qd-stems-explorer-page',
  standalone: true,
  imports: [AyahMatchesListComponent, ExplorerCountRangeFilterComponent, ExplorerResultCountComponent, MissingSurahsListComponent, NgTemplateOutlet, PaginationComponent, StemAyahTypeFiltersComponent, StemDetailsPanelComponent, StemLemmasListComponent, StemsTableComponent, StemWordsListComponent, SurahOccurrencesListComponent],
  templateUrl: './stems-explorer-page.component.html',
  styleUrl: './stems-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemsExplorerPageComponent implements OnInit, OnDestroy {
  private readonly listFacade = inject(StemsExplorerFacade);
  private readonly detailFacade = inject(StemsDetailFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly restoredColumn = signal<StemTableColumnKey | null>(null);
  private readonly searchInput = new Subject<string>();
  private searchSub?: Subscription;
  private searchSyncSub?: Subscription;
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void => this.isDesktop.set(event.matches);
  protected readonly listState = this.listFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;
  private readonly tableFocus = new ExplorerTableFocusController<StemPanelState, StemCountTarget, StemTableColumnKey, StemView, StemWordView, StemSurahView>({
    panelState: this.detailFacade.panelState,
    getSelectedRowId: (state) => state.selectedStemId,
    getView: (state) => state.view,
    getWordView: (state) => state.wordView,
    getSurahView: (state) => state.surahView,
    getFallbackColumn: (state) => resolveMorphologyActiveColumn({ view: state.view, wordView: state.wordView, surahView: state.surahView, activeColumn: this.restoredColumn() }) as StemTableColumnKey | null,
    eventToFocus: (event) => ({ rowId: event.stem.id, column: event.column, view: event.view, wordView: event.wordView, surahView: event.surahView }),
    commitDeferred: (event) => this.commitCountOpened(event),
  });

  protected readonly pageTitle = STEMS_PAGE_TITLE;
  protected readonly emptySelectionLabel = STEMS_EMPTY_SELECTION_LABEL;
  protected readonly emptyViewLabel = STEMS_EMPTY_VIEW_LABEL;
  protected readonly notFoundLabel = STEMS_NOT_FOUND_LABEL;
  protected readonly noResultsLabel = STEMS_NO_RESULTS_LABEL;
  protected readonly searchLabel = STEMS_SEARCH_LABEL;
  protected readonly searchPlaceholder = STEMS_SEARCH_PLACEHOLDER;
  protected get resultCountLabel(): string { return STEMS_RESULT_COUNT_LABEL; }
  protected readonly panelLoadingLabel = STEMS_LOADING_LABEL;
  protected readonly tableSectionLabel = STEMS_TABLE_LABEL;
  protected readonly listPaginationLabel = STEMS_LIST_PAGINATION_LABEL;
  protected readonly panelSurfaceLabel = STEMS_PANEL_SURFACE_LABEL;
  protected readonly wordsTablistLabel = STEMS_WORDS_TABLIST_LABEL;
  protected readonly surahsTablistLabel = STEMS_SURAHS_TABLIST_LABEL;
  protected readonly sortOptions: readonly StemSort[] = ['mushaf-order', 'occurrences', 'alpha'];
  protected readonly wordViewOptions: readonly StemWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly StemSurahView[] = ['mentioned', 'missing'];
  protected readonly emptyAyahsPage: SharedPagedResultDto<AyahMatchDto> = { page: 1, pageSize: STEM_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly emptyWordsPage: PagedResultDto<StemWordItemDto> = { page: 1, pageSize: STEM_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapStemAyahMatchToShared) } : this.emptyAyahsPage;
  });
  protected readonly searchDraft = signal('');
  protected readonly ranges = signal<RangeFilters>(EMPTY_RANGE_FILTERS);
  protected get rangeMetrics() { return STEMS_RANGE_METRICS; }
  protected readonly isDesktop = signal(true);
  protected readonly selectedStemId = this.tableFocus.selectedRowId;
  protected readonly activeView = computed(() => this.tableFocus.activeView() ?? DEFAULT_STEM_VIEW);
  protected readonly activeWordView = computed(() => this.tableFocus.activeWordView() ?? this.panelState().wordView);
  protected readonly activeSurahView = computed(() => this.tableFocus.activeSurahView() ?? this.panelState().surahView);
  protected readonly activeColumn = this.tableFocus.activeColumn;
  protected readonly emptySelection = computed(() => this.panelState().selectedStemId === null);
  protected readonly defaultView: StemView = DEFAULT_STEM_VIEW;

  protected get sortLabels() { return STEMS_SORT_LABELS; }
  protected get wordViewLabels() { return STEMS_WORD_VIEW_LABELS; }
  protected get surahViewLabels() { return STEMS_SURAHS_VIEW_LABELS; }

  constructor() {
    effect(() => {
      const state = this.panelState();
      if (state.selectedStemId === null || state.view !== 'ayahs' || state.ayahTypeCode === null || state.summary === null) return;
      if (state.summary.typeDistribution.some((item) => item.code === state.ayahTypeCode)) return;
      this.detailFacade.setAyahTypeCode(null);
      this.updateQueryParams(buildStemsQueryParams({ typeCode: null, detailPage: 1 }));
    });
  }

  ngOnInit(): void {
    this.listFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);
    this.searchSyncSub = this.route.queryParamMap.subscribe((params) => {
      const parsed = parseStemsQueryParams(params);
      this.searchDraft.set(parsed.search);
      this.ranges.set(parsed.ranges);
      this.restoredColumn.set(parseMorphologyColumnKey(parsed.column) as StemTableColumnKey | null);
    });
    this.searchSub = this.searchInput.pipe(debounceTime(300)).subscribe((value) => this.updateQueryParams({ search: value || null, page: null }));
    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
      this.desktopQuery = window.matchMedia(QD_BP_DESKTOP_MIN_QUERY);
      this.isDesktop.set(this.desktopQuery.matches);
      this.desktopQuery.addEventListener('change', this.onDesktopChange);
    }
  }

  ngOnDestroy(): void {
    this.listFacade.unbindFromRoute();
    this.detailFacade.unbindFromRoute();
    this.searchSub?.unsubscribe();
    this.searchSyncSub?.unsubscribe();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
    this.tableFocus.destroy();
  }

  protected onSearchInput(value: string): void { this.clearTableFocus(); this.searchDraft.set(value); this.searchInput.next(value); }
  protected onRangesChange(ranges: RangeFilters): void {
    this.clearTableFocus();
    this.updateQueryParams({ ...buildRangeQueryParams(ranges, STEMS_RANGE_METRICS), page: null });
  }
  protected onSortChange(sort: StemSort): void { this.clearTableFocus(); this.updateQueryParams({ sort, page: null }); }
  protected onPageChange(page: number): void { if (page !== this.listState().page) { this.clearTableFocus(); this.updateQueryParams(buildStemsQueryParams({ page })); } }
  protected onDetailPageChange(page: number): void { if (page !== this.panelState().detailPage) { this.tableFocus.cancel(); this.detailFacade.setDetailPage(page); this.updateQueryParams(buildStemsQueryParams({ detailPage: page })); } }

  protected onRowSelected(stem: StemListItemViewModel): void {
    this.tableFocus.setFocus({ rowId: stem.id, column: 'simple', view: DEFAULT_STEM_VIEW, wordView: 'simple' });
    this.updateQueryParams(buildStemsQueryParams({ stemId: stem.id, view: DEFAULT_STEM_VIEW, column: 'simple', wordView: 'simple', surahView: null, detailPage: 1, typeCode: null }));
  }

  protected onCountOpened(event: StemCountOpenedEvent): void {
    const target: StemCountTarget = { ...event, column: event.column ?? this.defaultColumnForEvent(event.view, event.wordView) };
    this.tableFocus.handleEvent(target, target.source ?? 'immediate');
  }

  protected onPanelViewChange(view: StemView): void {
    this.syncTableFocusToPanelView(view);
    this.detailFacade.setView(view);
    this.updateQueryParams(buildStemsQueryParams({ view, column: this.defaultColumnForView(view, 'simple'), detailPage: this.detailPageForView(view), wordView: view === 'words' ? 'simple' : null, surahView: view === 'surahs' ? 'mentioned' : null, typeCode: null }));
  }

  protected onAyahTypeChange(typeCode: string | null): void {
    const current = this.panelState();
    if (current.selectedStemId === null || current.view !== 'ayahs') return;
    if (current.ayahTypeCode === typeCode && current.detailPage === 1) return;
    this.tableFocus.cancel();
    this.detailFacade.setAyahTypeCode(typeCode);
    this.updateQueryParams(buildStemsQueryParams({ view: 'ayahs', column: this.activeColumn() ?? 'occurrences', detailPage: 1, typeCode }));
  }

  protected onWordViewChange(wordView: StemWordView): void {
    this.syncTableFocusToPanelView('words', wordView);
    this.detailFacade.setWordView(wordView);
    this.updateQueryParams(buildStemsQueryParams({ view: 'words', column: wordView === 'tashkeel' ? 'tashkeel' : 'simple', wordView, detailPage: 1, typeCode: null }));
  }

  protected onSurahViewChange(surahView: StemSurahView): void {
    this.syncTableFocusToPanelView('surahs', undefined, surahView);
    this.detailFacade.setSurahView(surahView);
    this.updateQueryParams(buildStemsQueryParams({ view: 'surahs', column: 'surahs', surahView, detailPage: null, typeCode: null }));
  }

  protected onClearSelection(): void { this.clearTableFocus(); this.detailFacade.clearSelection(); this.updateQueryParams(buildClearSelectionQueryParams()); }

  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: changes, queryParamsHandling: 'merge' });
  }

  private detailPageForView(view: StemView): number | null { return view === 'words' || view === 'ayahs' ? 1 : null; }

  private commitCountOpened(event: StemCountTarget): void {
    const { stem, view, column } = event;
    const wordView = view === 'words' ? (event.wordView ?? 'simple') : null;
    const surahView = view === 'surahs' ? (event.surahView ?? 'mentioned') : null;
    this.updateQueryParams(buildStemsQueryParams({ stemId: stem.id, view, column, detailPage: this.detailPageForView(view), wordView, surahView, typeCode: null }));
  }

  private clearTableFocus(): void { this.tableFocus.clear(); }

  private syncTableFocusToPanelView(view: StemView, wordView: StemWordView = this.panelState().wordView, surahView: StemSurahView = this.panelState().surahView): void {
    const selectedStemId = this.panelState().selectedStemId;
    this.tableFocus.setFocus(selectedStemId === null ? null : { rowId: selectedStemId, column: this.defaultColumnForView(view, wordView), view, wordView: view === 'words' ? wordView : undefined, surahView: view === 'surahs' ? surahView : undefined });
  }

  private defaultColumnForView(view: StemView, wordView: StemWordView = this.panelState().wordView): StemTableColumnKey {
    switch (view) {
      case 'words': return wordView === 'tashkeel' ? 'tashkeel' : 'simple';
      case 'surahs': return 'surahs';
      case 'lemmas': return 'lemmas';
      case 'ayahs':
      default: return 'occurrences';
    }
  }

  private defaultColumnForEvent(view: StemView, wordView: StemWordView | undefined): StemTableColumnKey {
    return this.defaultColumnForView(view, wordView ?? 'simple');
  }
}
