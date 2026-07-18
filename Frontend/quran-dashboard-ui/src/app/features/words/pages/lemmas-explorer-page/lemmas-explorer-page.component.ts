import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, Subscription, debounceTime, switchMap } from 'rxjs';

import { LemmaDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { ExplorerCountRangeFilterComponent } from '../../components/explorer-count-range-filter/explorer-count-range-filter.component';
import {
  AssociationOption,
  ExplorerAssociationFilterComponent,
} from '../../components/explorer-association-filter/explorer-association-filter.component';
import { WordsAssociationOptionsService } from '../../data-access/words-association-options.service';
import { ExplorerResultCountComponent } from '../../components/explorer-result-count/explorer-result-count.component';
import { ExplorerSearchRowComponent } from '../../components/explorer-search-row/explorer-search-row.component';
import { LemmaAyahTypeFiltersComponent } from '../../components/lemma-ayah-type-filters/lemma-ayah-type-filters.component';
import { LemmaDetailsPanelComponent } from '../../components/lemma-details-panel/lemma-details-panel.component';
import { LemmaStemsListComponent } from '../../components/lemma-stems-list/lemma-stems-list.component';
import { LemmaWordsListComponent } from '../../components/lemma-words-list/lemma-words-list.component';
import { LemmaCountOpenedEvent, LemmasTableComponent } from '../../components/lemmas-table/lemmas-table.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { WordsExplainerComponent } from '../../components/words-explainer/words-explainer.component';
import { sortQueryValue } from '../../models/explorer-sort';
import { WORDS_EXPLAINER_CONTENT } from '../../models/words-explainer.content';
import { WordsExplainerPreference } from '../../state/words-explainer-preference';
import { LEMMAS_EMPTY_SELECTION_LABEL, LEMMAS_EMPTY_VIEW_LABEL, LEMMAS_LIST_PAGINATION_LABEL, LEMMAS_LOADING_LABEL, LEMMAS_PAGE_TITLE, LEMMAS_PANEL_SURFACE_LABEL, LEMMAS_RESULT_COUNT_LABEL, LEMMAS_ROOT_FILTER_LABEL, LEMMAS_ROOT_FILTER_PLACEHOLDER, LEMMAS_SEARCH_LABEL, LEMMAS_SEARCH_PLACEHOLDER, LEMMAS_SORT_LABELS, LEMMAS_SORT_OPTIONS, LEMMAS_SURAHS_TABLIST_LABEL, LEMMAS_SURAHS_VIEW_LABELS, LEMMAS_TABLE_LABEL, LEMMAS_WORDS_TABLIST_LABEL, LEMMAS_WORD_VIEW_LABELS } from '../../models/lemmas.labels';
import { DEFAULT_LEMMA_SORT, DEFAULT_LEMMA_VIEW, LEMMAS_RANGE_METRICS, LEMMA_DETAIL_PAGE_SIZE, LemmaListItemViewModel, LemmaSort, LemmaSurahView, LemmaView, LemmaWordItemDto, LemmaWordView, PagedResultDto, normalizeLemmaSort } from '../../models/lemmas.models';
import { AyahMatchDto, PagedResultDto as SharedPagedResultDto } from '../../models/unique-words.models';
import { LemmasDetailFacade } from '../../state/lemmas-detail.facade';
import { LemmasExplorerFacade } from '../../state/lemmas-explorer.facade';
import { buildClearSelectionQueryParams, buildLemmasQueryParams, parseLemmasQueryParams } from '../../state/lemmas-url-sync';
import { MorphologyColumnKey, parseMorphologyColumnKey, resolveMorphologyActiveColumn } from '../../utils/explorer-count-active';
import { ExplorerTableFocusController } from '../../utils/explorer-table-focus-controller';
import { mapLemmaAyahMatchToShared } from '../../utils/lemma-ayah-match.mapper';
import { EMPTY_RANGE_FILTERS, RangeFilters, buildRangeQueryParams } from '../../state/words-range-filters';

type LemmaTableColumnKey = Exclude<MorphologyColumnKey, 'lemmas'>;
type LemmaPanelState = ReturnType<LemmasDetailFacade['panelState']>;
type LemmaCountTarget = LemmaCountOpenedEvent & { column: LemmaTableColumnKey };

@Component({
  selector: 'qd-lemmas-explorer-page',
  standalone: true,
  imports: [NgTemplateOutlet, AyahMatchesListComponent, ExplorerCountRangeFilterComponent, ExplorerAssociationFilterComponent, ExplorerResultCountComponent, ExplorerSearchRowComponent, LemmaAyahTypeFiltersComponent, LemmaDetailsPanelComponent, LemmaStemsListComponent, LemmaWordsListComponent, LemmasTableComponent, MissingSurahsListComponent, PaginationComponent, SurahOccurrencesListComponent, WordsExplainerComponent],
  templateUrl: './lemmas-explorer-page.component.html',
  styleUrl: './lemmas-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmasExplorerPageComponent implements OnInit, OnDestroy {
  private readonly listFacade = inject(LemmasExplorerFacade);
  private readonly detailFacade = inject(LemmasDetailFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly explainerPreference = inject(WordsExplainerPreference);
  private readonly associationOptions = inject(WordsAssociationOptionsService);
  private readonly restoredColumn = signal<LemmaTableColumnKey | null>(null);
  private readonly searchInput = new Subject<string>();
  private readonly rootSearchInput = new Subject<string>();
  private searchSub?: Subscription;
  private searchSyncSub?: Subscription;
  private rootSearchSub?: Subscription;
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void => this.isDesktop.set(event.matches);
  protected readonly listState = this.listFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;
  private readonly tableFocus = new ExplorerTableFocusController<LemmaPanelState, LemmaCountTarget, LemmaTableColumnKey, LemmaView, LemmaWordView, LemmaSurahView>({
    panelState: this.detailFacade.panelState,
    getSelectedRowId: (state) => state.selectedLemmaId,
    getView: (state) => state.view,
    getWordView: (state) => state.wordView,
    getSurahView: (state) => state.surahView,
    getFallbackColumn: (state) => resolveMorphologyActiveColumn({ view: state.view, wordView: state.wordView, surahView: state.surahView, activeColumn: this.restoredColumn() }) as LemmaTableColumnKey | null,
    eventToFocus: (event) => ({ rowId: event.lemma.id, column: event.column, view: event.view, wordView: event.wordView, surahView: event.surahView }),
    commitDeferred: (event) => this.commitCountOpened(event),
  });

  protected readonly pageTitle = LEMMAS_PAGE_TITLE;
  // TDZ-safe content getter + synchronous collapse restore (no first-paint shift).
  protected get explainer() { return WORDS_EXPLAINER_CONTENT.lemmas; }
  protected readonly explainerExpanded = signal(this.explainerPreference.isExpanded('lemmas'));
  protected readonly emptySelectionLabel = LEMMAS_EMPTY_SELECTION_LABEL;
  protected readonly emptyViewLabel = LEMMAS_EMPTY_VIEW_LABEL;
  protected readonly searchLabel = LEMMAS_SEARCH_LABEL;
  protected readonly searchPlaceholder = LEMMAS_SEARCH_PLACEHOLDER;
  protected get resultCountLabel(): string { return LEMMAS_RESULT_COUNT_LABEL; }
  protected readonly panelLoadingLabel = LEMMAS_LOADING_LABEL;
  protected readonly tableSectionLabel = LEMMAS_TABLE_LABEL;
  protected readonly listPaginationLabel = LEMMAS_LIST_PAGINATION_LABEL;
  protected readonly panelSurfaceLabel = LEMMAS_PANEL_SURFACE_LABEL;
  protected readonly wordsTablistLabel = LEMMAS_WORDS_TABLIST_LABEL;
  protected readonly surahsTablistLabel = LEMMAS_SURAHS_TABLIST_LABEL;
  protected get sortOptions() { return LEMMAS_SORT_OPTIONS; }
  protected readonly wordViewOptions: readonly LemmaWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly LemmaSurahView[] = ['mentioned', 'missing'];
  protected readonly emptyAyahsPage: SharedPagedResultDto<AyahMatchDto> = { page: 1, pageSize: LEMMA_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly emptyWordsPage: PagedResultDto<LemmaWordItemDto> = { page: 1, pageSize: LEMMA_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly searchDraft = signal('');
  protected readonly ranges = signal<RangeFilters>(EMPTY_RANGE_FILTERS);
  protected get rangeMetrics() { return LEMMAS_RANGE_METRICS; }
  protected readonly association = this.listFacade.association;
  protected readonly rootOptions = signal<readonly AssociationOption[]>([]);
  protected readonly rootOptionsLoading = signal(false);
  // M32/M43 + M74: a picker load failure must be distinguishable from a genuine empty result.
  protected readonly rootOptionsError = signal(false);
  protected readonly selectedRootLabel = signal<string | null>(null);
  protected get rootFilterLabel(): string { return LEMMAS_ROOT_FILTER_LABEL; }
  protected get rootFilterPlaceholder(): string { return LEMMAS_ROOT_FILTER_PLACEHOLDER; }
  protected readonly isDesktop = signal(true);
  protected readonly selectedLemmaId = this.tableFocus.selectedRowId;
  protected readonly activeView = computed(() => this.tableFocus.activeView() ?? DEFAULT_LEMMA_VIEW);
  protected readonly activeWordView = computed(() => this.tableFocus.activeWordView() ?? this.panelState().wordView);
  protected readonly activeSurahView = computed(() => this.tableFocus.activeSurahView() ?? this.panelState().surahView);
  protected readonly activeColumn = this.tableFocus.activeColumn;
  protected readonly emptySelection = computed(() => this.panelState().selectedLemmaId === null);
  protected readonly defaultView: LemmaView = DEFAULT_LEMMA_VIEW;
  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapLemmaAyahMatchToShared) } : this.emptyAyahsPage;
  });
  /** This panel's own typed frame (Feature 029 B7): an ayah click promotes it over the Mushaf. */
  protected readonly ayahParentFrame = computed<LemmaDetailFrame | null>(() => {
    const state = this.panelState();
    if (state.selectedLemmaId === null) {
      return null;
    }
    return {
      kind: 'lemma',
      id: state.selectedLemmaId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      detailPage: state.detailPage,
      typeCode: state.view === 'ayahs' ? state.ayahTypeCode : null,
    };
  });

  protected get sortLabels() { return LEMMAS_SORT_LABELS; }
  protected get wordViewLabels() { return LEMMAS_WORD_VIEW_LABELS; }
  protected get surahViewLabels() { return LEMMAS_SURAHS_VIEW_LABELS; }

  constructor() {
    effect(() => {
      const state = this.panelState();
      if (state.selectedLemmaId === null || state.view !== 'ayahs' || state.ayahTypeCode === null || state.summary === null) return;
      if (state.summary.typeDistribution.some((item) => item.code === state.ayahTypeCode)) return;
      this.detailFacade.setAyahTypeCode(null);
      this.updateQueryParams(buildLemmasQueryParams({ typeCode: null, detailPage: 1 }));
    });
  }

  ngOnInit(): void {
    this.listFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);
    this.searchSyncSub = this.route.queryParamMap.subscribe((params) => {
      const parsed = parseLemmasQueryParams(params);
      this.searchDraft.set(parsed.search);
      this.ranges.set(parsed.ranges);
      this.restoredColumn.set(parseMorphologyColumnKey(parsed.column) as LemmaTableColumnKey | null);
    });
    this.searchSub = this.searchInput.pipe(debounceTime(300)).subscribe((value) => this.updateQueryParams({ search: value || null, page: null }));
    this.rootSearchSub = this.rootSearchInput
      .pipe(
        debounceTime(300),
        switchMap((term) => this.associationOptions.searchRoots(term)),
      )
      .subscribe((result) => {
        this.rootOptions.set(result.status === 'success' ? result.options : []);
        this.rootOptionsError.set(result.status === 'error');
        this.rootOptionsLoading.set(false);
      });
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
    this.rootSearchSub?.unsubscribe();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
    this.tableFocus.destroy();
  }

  protected onSearchInput(value: string): void { this.clearTableFocus(); this.searchDraft.set(value); this.searchInput.next(value); }
  protected onRangesChange(ranges: RangeFilters): void {
    this.clearTableFocus();
    this.updateQueryParams({ ...buildRangeQueryParams(ranges, LEMMAS_RANGE_METRICS), page: null });
  }
  protected onRootSearch(term: string): void { this.rootOptionsLoading.set(true); this.rootSearchInput.next(term); }
  protected onRootFilterChange(option: AssociationOption | null): void {
    this.clearTableFocus();
    this.selectedRootLabel.set(option?.label ?? null);
    this.updateQueryParams({ rootId: option === null ? null : String(option.id), page: null });
  }
  /** A header cycle step (token) or its release (null). Changing the ordering always resets page. */
  protected onSortChange(sort: LemmaSort | null): void {
    this.clearTableFocus();
    this.updateQueryParams(buildLemmasQueryParams({ sort, page: null }));
  }

  /** The ≤1023px fallback select drives the same contract; the default order stays param-absent. */
  protected onSortSelect(value: string): void {
    this.onSortChange(sortQueryValue(normalizeLemmaSort(value), DEFAULT_LEMMA_SORT));
  }
  protected onPageChange(page: number): void { if (page !== this.listState().page) { this.clearTableFocus(); this.updateQueryParams(buildLemmasQueryParams({ page })); } }
  protected onDetailPageChange(page: number): void { if (page !== this.panelState().detailPage) { this.tableFocus.cancel(); this.detailFacade.setDetailPage(page); this.updateQueryParams(buildLemmasQueryParams({ detailPage: page })); } }

  protected onRowSelected(lemma: LemmaListItemViewModel): void {
    this.tableFocus.setFocus({ rowId: lemma.id, column: 'simple', view: DEFAULT_LEMMA_VIEW, wordView: 'simple' });
    this.updateQueryParams(buildLemmasQueryParams({ lemmaId: lemma.id, view: DEFAULT_LEMMA_VIEW, column: 'simple', wordView: 'simple', surahView: null, detailPage: 1, typeCode: null }));
  }

  protected onCountOpened(event: LemmaCountOpenedEvent): void {
    const target: LemmaCountTarget = { ...event, column: event.column ?? this.defaultColumnForEvent(event.view, event.wordView) };
    this.tableFocus.handleEvent(target, target.source ?? 'immediate');
  }

  protected onPanelViewChange(view: LemmaView): void {
    this.syncTableFocusToPanelView(view);
    this.detailFacade.setView(view);
    this.updateQueryParams(buildLemmasQueryParams({ view, column: this.defaultColumnForView(view, 'simple'), detailPage: this.detailPageForView(view), wordView: view === 'words' ? 'simple' : null, surahView: view === 'surahs' ? 'mentioned' : null, typeCode: null }));
  }

  protected onAyahTypeChange(typeCode: string | null): void {
    const current = this.panelState();
    if (current.selectedLemmaId === null || current.view !== 'ayahs') return;
    if (current.ayahTypeCode === typeCode && current.detailPage === 1) return;
    this.tableFocus.cancel();
    this.detailFacade.setAyahTypeCode(typeCode);
    this.updateQueryParams(buildLemmasQueryParams({ view: 'ayahs', column: this.activeColumn() ?? 'occurrences', detailPage: 1, typeCode }));
  }

  protected onWordViewChange(wordView: LemmaWordView): void {
    this.syncTableFocusToPanelView('words', wordView);
    this.detailFacade.setWordView(wordView);
    this.updateQueryParams(buildLemmasQueryParams({ view: 'words', column: wordView === 'tashkeel' ? 'tashkeel' : 'simple', wordView, detailPage: 1, typeCode: null }));
  }

  protected onSurahViewChange(surahView: LemmaSurahView): void {
    this.syncTableFocusToPanelView('surahs', undefined, surahView);
    this.detailFacade.setSurahView(surahView);
    this.updateQueryParams(buildLemmasQueryParams({ view: 'surahs', column: 'surahs', surahView, detailPage: null, typeCode: null }));
  }

  protected onClearSelection(): void { this.clearTableFocus(); this.detailFacade.clearSelection(); this.updateQueryParams(buildClearSelectionQueryParams()); }

  protected onExplainerToggled(expanded: boolean): void {
    this.explainerExpanded.set(expanded);
    this.explainerPreference.setExpanded('lemmas', expanded);
  }

  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: changes, queryParamsHandling: 'merge' });
  }

  private commitCountOpened(event: LemmaCountTarget): void {
    const { lemma, view, column } = event;
    const wordView = view === 'words' ? (event.wordView ?? 'simple') : null;
    const surahView = view === 'surahs' ? (event.surahView ?? 'mentioned') : null;
    this.updateQueryParams(buildLemmasQueryParams({ lemmaId: lemma.id, view, column, detailPage: this.detailPageForView(view), wordView, surahView, typeCode: null }));
  }

  private detailPageForView(view: LemmaView): number | null { return view === 'words' || view === 'ayahs' ? 1 : null; }
  private clearTableFocus(): void { this.tableFocus.clear(); }

  private syncTableFocusToPanelView(view: LemmaView, wordView: LemmaWordView = this.panelState().wordView, surahView: LemmaSurahView = this.panelState().surahView): void {
    const selectedLemmaId = this.panelState().selectedLemmaId;
    this.tableFocus.setFocus(selectedLemmaId === null ? null : { rowId: selectedLemmaId, column: this.defaultColumnForView(view, wordView), view, wordView: view === 'words' ? wordView : undefined, surahView: view === 'surahs' ? surahView : undefined });
  }

  private defaultColumnForView(view: LemmaView, wordView: LemmaWordView = this.panelState().wordView): LemmaTableColumnKey {
    switch (view) {
      case 'words': return wordView === 'tashkeel' ? 'tashkeel' : 'simple';
      case 'surahs': return 'surahs';
      case 'stems': return 'stems';
      case 'ayahs':
      default: return 'occurrences';
    }
  }

  private defaultColumnForEvent(view: LemmaView, wordView: LemmaWordView | undefined): LemmaTableColumnKey {
    return this.defaultColumnForView(view, wordView ?? 'simple');
  }
}
