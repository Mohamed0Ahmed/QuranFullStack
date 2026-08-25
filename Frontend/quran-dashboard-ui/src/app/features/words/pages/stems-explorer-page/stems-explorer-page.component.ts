import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, Subscription, debounceTime, switchMap } from 'rxjs';

import { StemDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { ExplorerCountRangeFilterComponent } from '../../components/explorer-count-range-filter/explorer-count-range-filter.component';
import { ExplorerToolbarComponent } from '../../components/explorer-toolbar/explorer-toolbar.component';
import {
  AssociationOption,
  ExplorerAssociationFilterComponent,
} from '../../components/explorer-association-filter/explorer-association-filter.component';
import { WordsAssociationOptionsService } from '../../data-access/words-association-options.service';
import { ExplorerResultCountComponent } from '../../../../shared/ui/result-count/explorer-result-count.component';
import { ExplorerSearchRowComponent } from '../../components/explorer-search-row/explorer-search-row.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { StemAyahTypeFiltersComponent } from '../../components/stem-ayah-type-filters/stem-ayah-type-filters.component';
import { StemDetailsPanelComponent } from '../../components/stem-details-panel/stem-details-panel.component';
import { StemLemmasListComponent } from '../../components/stem-lemmas-list/stem-lemmas-list.component';
import { StemCountOpenedEvent, StemsTableComponent } from '../../components/stems-table/stems-table.component';
import { StemWordsListComponent } from '../../components/stem-words-list/stem-words-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { WordsExplainerComponent } from '../../components/words-explainer/words-explainer.component';
import { WordsLocalNavComponent } from '../../components/words-local-nav/words-local-nav.component';
import { sortQueryValue } from '../../models/explorer-sort';
import { WORDS_EXPLAINER_CONTENT } from '../../models/words-explainer.content';
import { WordsExplainerPreference } from '../../state/words-explainer-preference';
import { STEMS_EMPTY_SELECTION_LABEL, STEMS_EMPTY_VIEW_LABEL, STEMS_LIST_PAGINATION_LABEL, STEMS_LOADING_LABEL, STEMS_PAGE_TITLE, STEMS_PANEL_SURFACE_LABEL, STEMS_PRIMARY_LEMMA_FILTER_LABEL, STEMS_PRIMARY_LEMMA_FILTER_PLACEHOLDER, STEMS_PRIMARY_ROOT_FILTER_LABEL, STEMS_PRIMARY_ROOT_FILTER_PLACEHOLDER, STEMS_RESULT_COUNT_LABEL, STEMS_SEARCH_LABEL, STEMS_SEARCH_PLACEHOLDER, STEMS_SORT_LABELS, STEMS_SORT_OPTIONS, STEMS_SURAHS_TABLIST_LABEL, STEMS_SURAHS_VIEW_LABELS, STEMS_TABLE_LABEL, STEMS_WORDS_TABLIST_LABEL, STEMS_WORD_VIEW_LABELS } from '../../models/stems.labels';
import { DEFAULT_STEM_SORT, DEFAULT_STEM_VIEW, PagedResultDto, STEMS_RANGE_METRICS, STEM_DETAIL_PAGE_SIZE, StemListItemViewModel, StemSort, StemSurahView, StemView, StemWordItemDto, StemWordView, normalizeStemSort } from '../../models/stems.models';
import { AyahMatchDto, PagedResultDto as SharedPagedResultDto } from '../../models/unique-words.models';
import { StemsDetailFacade } from '../../state/stems-detail.facade';
import { StemsExplorerFacade } from '../../state/stems-explorer.facade';
import { buildClearSelectionQueryParams, buildStemsQueryParams, parseStemsQueryParams } from '../../state/stems-url-sync';
import { MorphologyColumnKey, parseMorphologyColumnKey, resolveMorphologyActiveColumn } from '../../utils/explorer-count-active';
import { ExplorerTableFocusController } from '../../utils/explorer-table-focus-controller';
import { mapStemAyahMatchToShared } from '../../utils/stem-ayah-match.mapper';
import { EMPTY_RANGE_FILTERS, RangeFilters, buildRangeQueryParams } from '../../state/words-range-filters';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';
import { WordsDebouncedSearchDraftController } from '../../state/words-debounced-search-draft.controller';

type StemTableColumnKey = Exclude<MorphologyColumnKey, 'stems'>;
type StemPanelState = ReturnType<StemsDetailFacade['panelState']>;
type StemCountTarget = StemCountOpenedEvent & { column: StemTableColumnKey };

let nextSubViewInstance = 0;

@Component({
  selector: 'qd-stems-explorer-page',
  standalone: true,
  imports: [AyahMatchesListComponent, ExplorerCountRangeFilterComponent, ExplorerAssociationFilterComponent, ExplorerResultCountComponent, ExplorerSearchRowComponent, ExplorerToolbarComponent, MissingSurahsListComponent, NgTemplateOutlet, PaginationComponent, QdTabDirective, QdTabsComponent, StemAyahTypeFiltersComponent, StemDetailsPanelComponent, StemLemmasListComponent, StemsTableComponent, StemWordsListComponent, SurahOccurrencesListComponent, WordsExplainerComponent, WordsLocalNavComponent],
  templateUrl: './stems-explorer-page.component.html',
  styleUrl: './stems-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemsExplorerPageComponent implements OnInit, OnDestroy {
  private readonly listFacade = inject(StemsExplorerFacade);
  private readonly detailFacade = inject(StemsDetailFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly explainerPreference = inject(WordsExplainerPreference);
  private readonly associationOptions = inject(WordsAssociationOptionsService);
  private readonly searchDraftController = WordsDebouncedSearchDraftController.create('stems', (value) => this.updateQueryParams({ search: value || null, page: null }));
  private readonly restoredColumn = signal<StemTableColumnKey | null>(null);
  private readonly rootSearchInput = new Subject<string>();
  private readonly lemmaSearchInput = new Subject<string>();
  private searchSyncSub?: Subscription;
  private rootSearchSub?: Subscription;
  private lemmaSearchSub?: Subscription;
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
  protected get explainer() { return WORDS_EXPLAINER_CONTENT.stems; }
  protected readonly explainerExpanded = signal(this.explainerPreference.isExpanded('stems'));
  protected readonly emptySelectionLabel = STEMS_EMPTY_SELECTION_LABEL;
  protected readonly emptyViewLabel = STEMS_EMPTY_VIEW_LABEL;
  protected readonly searchLabel = STEMS_SEARCH_LABEL;
  protected readonly searchPlaceholder = STEMS_SEARCH_PLACEHOLDER;
  protected get resultCountLabel(): string { return STEMS_RESULT_COUNT_LABEL; }
  protected readonly panelLoadingLabel = STEMS_LOADING_LABEL;
  protected readonly tableSectionLabel = STEMS_TABLE_LABEL;
  protected readonly listPaginationLabel = STEMS_LIST_PAGINATION_LABEL;
  protected readonly panelSurfaceLabel = STEMS_PANEL_SURFACE_LABEL;
  protected readonly wordsTablistLabel = STEMS_WORDS_TABLIST_LABEL;
  protected readonly surahsTablistLabel = STEMS_SURAHS_TABLIST_LABEL;
  protected get sortOptions() { return STEMS_SORT_OPTIONS; }
  protected readonly wordViewOptions: readonly StemWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly StemSurahView[] = ['mentioned', 'missing'];
  protected readonly emptyAyahsPage: SharedPagedResultDto<AyahMatchDto> = { page: 1, pageSize: STEM_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly emptyWordsPage: PagedResultDto<StemWordItemDto> = { page: 1, pageSize: STEM_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapStemAyahMatchToShared) } : this.emptyAyahsPage;
  });
  protected readonly lemmasCount = computed(() => {
    const lemmas = this.panelState().lemmas;
    return lemmas === null ? null : lemmas.lemmas.length;
  });
  protected readonly linkingSource = computed<LinkingSourceDescriptor | null>(() => {
    const state = this.panelState();
    if (state.selectedStemId === null || state.summary === null || state.summary.id !== state.selectedStemId) {
      return null;
    }
    return {
      kind: 'stem',
      stemId: state.selectedStemId,
      typeCodes: state.ayahTypeCode === null ? [] : [state.ayahTypeCode],
      label: state.summary.stemText,
    };
  });
  protected readonly ayahParentFrame = computed<StemDetailFrame | null>(() => {
    const state = this.panelState();
    if (state.selectedStemId === null) {
      return null;
    }
    return {
      kind: 'stem',
      id: state.selectedStemId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      detailPage: state.detailPage,
      typeCode: state.view === 'ayahs' ? state.ayahTypeCode : null,
    };
  });
  protected readonly searchDraft = this.searchDraftController.draft;
  protected readonly ranges = signal<RangeFilters>(EMPTY_RANGE_FILTERS);
  protected get rangeMetrics() { return STEMS_RANGE_METRICS; }
  protected readonly association = this.listFacade.association;
  protected readonly rootOptions = signal<readonly AssociationOption[]>([]);
  protected readonly rootOptionsLoading = signal(false);
  protected readonly rootOptionsError = signal(false);
  protected readonly selectedRootLabel = signal<string | null>(null);
  protected readonly lemmaOptions = signal<readonly AssociationOption[]>([]);
  protected readonly lemmaOptionsLoading = signal(false);
  protected readonly lemmaOptionsError = signal(false);
  protected readonly selectedLemmaLabel = signal<string | null>(null);
  protected get primaryRootLabel(): string { return STEMS_PRIMARY_ROOT_FILTER_LABEL; }
  protected get primaryRootPlaceholder(): string { return STEMS_PRIMARY_ROOT_FILTER_PLACEHOLDER; }
  protected get primaryLemmaLabel(): string { return STEMS_PRIMARY_LEMMA_FILTER_LABEL; }
  protected get primaryLemmaPlaceholder(): string { return STEMS_PRIMARY_LEMMA_FILTER_PLACEHOLDER; }
  protected readonly isDesktop = signal(true);
  protected readonly selectedStemId = this.tableFocus.selectedRowId;
  protected readonly activeView = computed(() => this.tableFocus.activeView() ?? DEFAULT_STEM_VIEW);
  protected readonly activeWordView = computed(() => this.tableFocus.activeWordView() ?? this.panelState().wordView);
  protected readonly activeSurahView = computed(() => this.tableFocus.activeSurahView() ?? this.panelState().surahView);
  protected readonly activeColumn = this.tableFocus.activeColumn;
  private readonly subViewId = `stems-explorer-subview-${nextSubViewInstance++}`;
  protected readonly subViewPanelId = `${this.subViewId}-panel`;
  protected readonly activeSubViewTabId = computed(() => {
    const view = this.activeView();
    if (view === 'words') {
      return this.subViewTabId(this.panelState().wordView);
    }
    return view === 'surahs' ? this.subViewTabId(this.panelState().surahView) : null;
  });
  protected readonly emptySelection = computed(() => this.panelState().selectedStemId === null);
  protected readonly defaultView: StemView = DEFAULT_STEM_VIEW;

  protected get sortLabels() { return STEMS_SORT_LABELS; }
  protected get wordViewLabels() { return STEMS_WORD_VIEW_LABELS; }
  protected get surahViewLabels() { return STEMS_SURAHS_VIEW_LABELS; }

  constructor() {
    effect(() => {
      const state = this.panelState();
      if (state.selectedStemId === null || (state.view !== 'ayahs' && state.view !== 'words') || state.ayahTypeCode === null || state.summary === null) return;
      if (state.summary.typeDistribution.some((item) => item.code === state.ayahTypeCode)) return;
      this.detailFacade.setAyahTypeCode(null);
      this.updateQueryParams(buildStemsQueryParams({ typeCode: null, detailPage: 1 }));
    });
  }

  ngOnInit(): void {
    this.searchDraftController.start();
    this.listFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);
    this.searchSyncSub = this.route.queryParamMap.subscribe((params) => {
      const parsed = parseStemsQueryParams(params);
      this.searchDraftController.syncCommitted(parsed.search);
      this.ranges.set(parsed.ranges);
      this.restoredColumn.set(parseMorphologyColumnKey(parsed.column) as StemTableColumnKey | null);
    });
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
    this.lemmaSearchSub = this.lemmaSearchInput
      .pipe(
        debounceTime(300),
        switchMap((term) => this.associationOptions.searchLemmas(term)),
      )
      .subscribe((result) => {
        this.lemmaOptions.set(result.status === 'success' ? result.options : []);
        this.lemmaOptionsError.set(result.status === 'error');
        this.lemmaOptionsLoading.set(false);
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
    this.searchDraftController.destroy();
    this.searchSyncSub?.unsubscribe();
    this.rootSearchSub?.unsubscribe();
    this.lemmaSearchSub?.unsubscribe();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
    this.tableFocus.destroy();
  }

  protected onSearchInput(value: string): void { this.clearTableFocus(); this.searchDraftController.update(value); }
  protected onRangesChange(ranges: RangeFilters): void {
    this.clearTableFocus();
    this.updateQueryParams({ ...buildRangeQueryParams(ranges, STEMS_RANGE_METRICS), page: null });
  }
  protected onRootSearch(term: string): void { this.rootOptionsLoading.set(true); this.rootSearchInput.next(term); }
  protected onLemmaSearch(term: string): void { this.lemmaOptionsLoading.set(true); this.lemmaSearchInput.next(term); }
  protected onPrimaryRootChange(option: AssociationOption | null): void {
    this.clearTableFocus();
    this.selectedRootLabel.set(option?.label ?? null);
    this.updateQueryParams({ rootId: option === null ? null : String(option.id), page: null });
  }
  protected onPrimaryLemmaChange(option: AssociationOption | null): void {
    this.clearTableFocus();
    this.selectedLemmaLabel.set(option?.label ?? null);
    this.updateQueryParams({ lemmaId: option === null ? null : String(option.id), page: null });
  }
  protected onSortChange(sort: StemSort | null): void {
    this.clearTableFocus();
    this.updateQueryParams(buildStemsQueryParams({ sort, page: null }));
  }

  protected onSortSelect(value: string): void {
    this.onSortChange(sortQueryValue(normalizeStemSort(value), DEFAULT_STEM_SORT));
  }
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
    const typeCode = view === 'ayahs' || view === 'words' ? this.panelState().ayahTypeCode : null;
    this.syncTableFocusToPanelView(view);
    this.detailFacade.setView(view);
    this.updateQueryParams(buildStemsQueryParams({ view, column: this.defaultColumnForView(view, 'simple'), detailPage: this.detailPageForView(view), wordView: view === 'words' ? 'simple' : null, surahView: view === 'surahs' ? 'mentioned' : null, typeCode }));
  }

  protected onAyahTypeChange(typeCode: string | null): void {
    const current = this.panelState();
    if (current.selectedStemId === null || (current.view !== 'ayahs' && current.view !== 'words')) return;
    if (current.ayahTypeCode === typeCode && current.detailPage === 1) return;
    this.tableFocus.cancel();
    this.detailFacade.setAyahTypeCode(typeCode);
    this.updateQueryParams(buildStemsQueryParams({ view: current.view, column: this.activeColumn() ?? 'occurrences', detailPage: 1, typeCode }));
  }

  protected subViewTabId(option: string): string {
    return `${this.subViewId}-tab-${option}`;
  }

  protected onWordViewChange(wordView: StemWordView): void {
    this.syncTableFocusToPanelView('words', wordView);
    this.detailFacade.setWordView(wordView);
    this.updateQueryParams(buildStemsQueryParams({ view: 'words', column: wordView === 'tashkeel' ? 'tashkeel' : 'simple', wordView, detailPage: 1, typeCode: this.panelState().ayahTypeCode }));
  }

  protected onSurahViewChange(surahView: StemSurahView): void {
    this.syncTableFocusToPanelView('surahs', undefined, surahView);
    this.detailFacade.setSurahView(surahView);
    this.updateQueryParams(buildStemsQueryParams({ view: 'surahs', column: 'surahs', surahView, detailPage: null, typeCode: null }));
  }

  protected onClearSelection(): void { this.clearTableFocus(); this.detailFacade.clearSelection(); this.updateQueryParams(buildClearSelectionQueryParams()); }

  protected onExplainerToggled(expanded: boolean): void {
    this.explainerExpanded.set(expanded);
    this.explainerPreference.setExpanded('stems', expanded);
  }

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
