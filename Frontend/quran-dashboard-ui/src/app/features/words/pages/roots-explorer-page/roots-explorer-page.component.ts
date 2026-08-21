import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { RootDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { AyahTypeFiltersComponent } from '../../components/ayah-type-filters/ayah-type-filters.component';
import { ExplorerCountRangeFilterComponent } from '../../components/explorer-count-range-filter/explorer-count-range-filter.component';
import { ExplorerToolbarComponent } from '../../components/explorer-toolbar/explorer-toolbar.component';
import { ExplorerResultCountComponent } from '../../../../shared/ui/result-count/explorer-result-count.component';
import { ExplorerSearchRowComponent } from '../../components/explorer-search-row/explorer-search-row.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { RootDetailsPanelComponent } from '../../components/root-details-panel/root-details-panel.component';
import { RootLemmasListComponent } from '../../components/root-lemmas-list/root-lemmas-list.component';
import { RootStemsListComponent } from '../../components/root-stems-list/root-stems-list.component';
import { RootWordsListComponent } from '../../components/root-words-list/root-words-list.component';
import { RootCountOpenedEvent, RootsTableComponent } from '../../components/roots-table/roots-table.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { WordsExplainerComponent } from '../../components/words-explainer/words-explainer.component';
import { sortQueryValue } from '../../models/explorer-sort';
import { WORDS_EXPLAINER_CONTENT } from '../../models/words-explainer.content';
import { WordsExplainerPreference } from '../../state/words-explainer-preference';
import { ROOTS_EMPTY_SELECTION_LABEL, ROOTS_EMPTY_VIEW_LABEL, ROOTS_LIST_PAGINATION_LABEL, ROOTS_LOADING_LABEL, ROOTS_PAGE_TITLE, ROOTS_PANEL_LABEL, ROOTS_RESULT_COUNT_LABEL, ROOTS_SEARCH_LABEL, ROOTS_SEARCH_PLACEHOLDER, ROOTS_SORT_LABELS, ROOTS_SORT_OPTIONS, ROOTS_SURAHS_TABLIST_LABEL, ROOTS_SURAHS_VIEW_LABELS, ROOTS_TABLE_LABEL, ROOTS_WORDS_TABLIST_LABEL, ROOTS_WORD_VIEW_LABELS } from '../../models/roots.labels';
import { DEFAULT_ROOT_SORT, DEFAULT_ROOT_VIEW, PagedResultDto, ROOTS_RANGE_METRICS, ROOT_DETAIL_PAGE_SIZE, RootListItemViewModel, RootSort, RootSurahView, RootView, RootWordItemDto, RootWordView, normalizeRootSort } from '../../models/roots.models';
import { AyahMatchDto } from '../../models/unique-words.models';
import { RootsDetailFacade } from '../../state/roots-detail.facade';
import { RootsExplorerFacade } from '../../state/roots-explorer.facade';
import { buildClearSelectionQueryParams, buildRootsQueryParams, parseRootsQueryParams } from '../../state/roots-url-sync';
import { MorphologyColumnKey, parseMorphologyColumnKey, resolveMorphologyActiveColumn } from '../../utils/explorer-count-active';
import { ExplorerTableFocusController } from '../../utils/explorer-table-focus-controller';
import { mapRootAyahMatchToShared } from '../../utils/root-ayah-match.mapper';
import { EMPTY_RANGE_FILTERS, RangeFilters, buildRangeQueryParams } from '../../state/words-range-filters';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';
import { WordsDebouncedSearchDraftController } from '../../state/words-debounced-search-draft.controller';

type RootTableColumnKey = MorphologyColumnKey;
type RootPanelState = ReturnType<RootsDetailFacade['panelState']>;
type RootCountTarget = RootCountOpenedEvent & { column: RootTableColumnKey };

let nextSubViewInstance = 0;

@Component({
  selector: 'qd-roots-explorer-page',
  standalone: true,
  imports: [AyahMatchesListComponent, AyahTypeFiltersComponent, ExplorerCountRangeFilterComponent, ExplorerResultCountComponent, ExplorerSearchRowComponent, ExplorerToolbarComponent, MissingSurahsListComponent, NgTemplateOutlet, PaginationComponent, QdTabDirective, QdTabsComponent, RootDetailsPanelComponent, RootLemmasListComponent, RootStemsListComponent, RootWordsListComponent, RootsTableComponent, SurahOccurrencesListComponent, WordsExplainerComponent],
  templateUrl: './roots-explorer-page.component.html',
  styleUrl: './roots-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootsExplorerPageComponent implements OnInit, OnDestroy {
  private readonly listFacade = inject(RootsExplorerFacade);
  private readonly detailFacade = inject(RootsDetailFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly explainerPreference = inject(WordsExplainerPreference);
  private readonly searchDraftController = WordsDebouncedSearchDraftController.create('roots', (value) => this.updateQueryParams({ search: value || null, page: null }));
  private readonly restoredColumn = signal<RootTableColumnKey | null>(null);
  private querySyncSub?: Subscription;
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void => this.isDesktop.set(event.matches);
  protected readonly listState = this.listFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;
  private readonly tableFocus = new ExplorerTableFocusController<RootPanelState, RootCountTarget, RootTableColumnKey, RootView, RootWordView, RootSurahView>({
    panelState: this.detailFacade.panelState,
    getSelectedRowId: (state) => state.selectedRootId,
    getView: (state) => state.view,
    getWordView: (state) => state.wordView,
    getSurahView: (state) => state.surahView,
    getFallbackColumn: (state) => resolveMorphologyActiveColumn({ view: state.view, wordView: state.wordView, surahView: state.surahView, activeColumn: this.restoredColumn() }) as RootTableColumnKey | null,
    eventToFocus: (event) => ({ rowId: event.root.id, column: event.column, view: event.view, wordView: event.wordView, surahView: event.surahView }),
    commitDeferred: (event) => this.commitCountOpened(event),
  });

  protected readonly pageTitle = ROOTS_PAGE_TITLE;
  protected get explainer() { return WORDS_EXPLAINER_CONTENT.roots; }
  protected readonly explainerExpanded = signal(this.explainerPreference.isExpanded('roots'));
  protected readonly emptySelectionLabel = ROOTS_EMPTY_SELECTION_LABEL;
  protected readonly emptyViewLabel = ROOTS_EMPTY_VIEW_LABEL;
  protected readonly panelLoadingLabel = ROOTS_LOADING_LABEL;
  protected readonly searchLabel = ROOTS_SEARCH_LABEL;
  protected readonly searchPlaceholder = ROOTS_SEARCH_PLACEHOLDER;
  protected get resultCountLabel(): string { return ROOTS_RESULT_COUNT_LABEL; }
  protected get sortOptions() { return ROOTS_SORT_OPTIONS; }
  protected readonly wordViewOptions: readonly RootWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly RootSurahView[] = ['mentioned', 'missing'];
  protected readonly emptyAyahsPage: PagedResultDto<AyahMatchDto> = { page: 1, pageSize: ROOT_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly emptyWordsPage: PagedResultDto<RootWordItemDto> = { page: 1, pageSize: ROOT_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly listPaginationLabel = ROOTS_LIST_PAGINATION_LABEL;
  protected readonly panelLabel = ROOTS_PANEL_LABEL;
  protected readonly rootsTableLabel = ROOTS_TABLE_LABEL;
  protected readonly wordsTablistLabel = ROOTS_WORDS_TABLIST_LABEL;
  protected readonly surahsTablistLabel = ROOTS_SURAHS_TABLIST_LABEL;
  protected readonly searchDraft = this.searchDraftController.draft;
  protected readonly ranges = signal<RangeFilters>(EMPTY_RANGE_FILTERS);
  protected get rangeMetrics() { return ROOTS_RANGE_METRICS; }
  protected readonly isDesktop = signal(true);
  protected readonly selectedRootId = this.tableFocus.selectedRowId;
  protected readonly activeView = computed(() => this.tableFocus.activeView() ?? DEFAULT_ROOT_VIEW);
  protected readonly activeWordView = computed(() => this.tableFocus.activeWordView() ?? this.panelState().wordView);
  protected readonly activeSurahView = computed(() => this.tableFocus.activeSurahView() ?? this.panelState().surahView);
  protected readonly activeColumn = this.tableFocus.activeColumn;
  private readonly subViewId = `roots-explorer-subview-${nextSubViewInstance++}`;
  protected readonly subViewPanelId = `${this.subViewId}-panel`;
  protected readonly activeSubViewTabId = computed(() => {
    const view = this.activeView();
    if (view === 'words') {
      return this.subViewTabId(this.panelState().wordView);
    }
    return view === 'surahs' ? this.subViewTabId(this.panelState().surahView) : null;
  });
  protected readonly emptySelection = computed(() => this.panelState().selectedRootId === null);
  protected readonly defaultView: RootView = DEFAULT_ROOT_VIEW;
  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapRootAyahMatchToShared) } : this.emptyAyahsPage;
  });
  protected readonly linkingSource = computed<LinkingSourceDescriptor | null>(() => {
    const state = this.panelState();
    if (state.selectedRootId === null || state.summary === null || state.summary.id !== state.selectedRootId) {
      return null;
    }
    return {
      kind: 'root',
      rootId: state.selectedRootId,
      typeCodes: state.ayahTypeCode === null ? [] : [state.ayahTypeCode],
      label: state.summary.rootText,
    };
  });
  protected readonly ayahParentFrame = computed<RootDetailFrame | null>(() => {
    const state = this.panelState();
    if (state.selectedRootId === null) {
      return null;
    }
    return {
      kind: 'root',
      id: state.selectedRootId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      detailPage: state.detailPage,
      typeCode: state.view === 'ayahs' ? state.ayahTypeCode : null,
    };
  });

  protected get sortLabels() { return ROOTS_SORT_LABELS; }
  protected get wordViewLabels() { return ROOTS_WORD_VIEW_LABELS; }
  protected get surahViewLabels() { return ROOTS_SURAHS_VIEW_LABELS; }

  constructor() {
    effect(() => {
      const state = this.panelState();
      if (state.selectedRootId === null || state.view !== 'ayahs' || state.ayahTypeCode === null || state.summary === null) return;
      if (state.summary.typeDistribution.some((item) => item.code === state.ayahTypeCode)) return;
      this.detailFacade.setAyahTypeCode(null);
      this.updateQueryParams(buildRootsQueryParams({ typeCode: null, detailPage: 1 }));
    });
  }

  ngOnInit(): void {
    this.searchDraftController.start();
    this.listFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);
    this.querySyncSub = this.route.queryParamMap.subscribe((params) => {
      const parsed = parseRootsQueryParams(params);
      this.searchDraftController.syncCommitted(parsed.search);
      this.restoredColumn.set(parseMorphologyColumnKey(parsed.column));
      this.ranges.set(parsed.ranges);
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
    this.querySyncSub?.unsubscribe();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
    this.tableFocus.destroy();
  }

  protected onSearchInput(value: string): void { this.clearTableFocus(); this.searchDraftController.update(value); }
  protected onRangesChange(ranges: RangeFilters): void {
    this.clearTableFocus();
    this.updateQueryParams({ ...buildRangeQueryParams(ranges, ROOTS_RANGE_METRICS), page: null });
  }
  protected onSortChange(sort: RootSort | null): void {
    this.clearTableFocus();
    this.updateQueryParams(buildRootsQueryParams({ sort, page: null }));
  }

  protected onSortSelect(value: string): void {
    this.onSortChange(sortQueryValue(normalizeRootSort(value), DEFAULT_ROOT_SORT));
  }
  protected onPageChange(page: number): void { if (page !== this.listState().page) { this.clearTableFocus(); this.updateQueryParams(buildRootsQueryParams({ page })); } }

  protected onRowSelected(root: RootListItemViewModel): void {
    this.tableFocus.setFocus({ rowId: root.id, column: 'simple', view: DEFAULT_ROOT_VIEW, wordView: 'simple' });
    this.updateQueryParams(buildRootsQueryParams({ rootId: root.id, view: DEFAULT_ROOT_VIEW, column: 'simple', wordView: 'simple', surahView: null, detailPage: null, typeCode: null }));
  }

  protected onCountOpened(event: RootCountOpenedEvent): void {
    const target: RootCountTarget = { ...event, column: event.column ?? this.defaultColumnForEvent(event.view, event.wordView) };
    this.tableFocus.handleEvent(target, target.source ?? 'immediate');
  }

  protected onDetailPageChange(page: number): void { this.tableFocus.cancel(); this.detailFacade.setDetailPage(page); this.updateQueryParams(buildRootsQueryParams({ detailPage: page })); }

  protected onAyahTypeChange(typeCode: string | null): void {
    const current = this.panelState();
    if (current.selectedRootId === null || current.view !== 'ayahs') return;
    if (current.ayahTypeCode === typeCode && current.detailPage === 1) return;
    this.tableFocus.cancel();
    this.detailFacade.setAyahTypeCode(typeCode);
    this.updateQueryParams(buildRootsQueryParams({ view: 'ayahs', column: this.activeColumn() ?? 'occurrences', detailPage: 1, typeCode }));
  }

  protected onPanelViewChange(view: RootView): void {
    this.syncTableFocusToPanelView(view);
    this.detailFacade.setView(view);
    this.updateQueryParams(buildRootsQueryParams({ view, column: this.defaultColumnForView(view, 'simple'), detailPage: null, wordView: view === 'words' ? 'simple' : null, surahView: view === 'surahs' ? 'mentioned' : null, typeCode: null }));
  }

  protected subViewTabId(option: string): string {
    return `${this.subViewId}-tab-${option}`;
  }

  protected onWordViewChange(wordView: RootWordView): void {
    this.syncTableFocusToPanelView('words', wordView);
    this.detailFacade.setWordView(wordView);
    this.updateQueryParams(buildRootsQueryParams({ view: 'words', column: wordView === 'tashkeel' ? 'tashkeel' : 'simple', wordView, detailPage: null, typeCode: null }));
  }

  protected onSurahViewChange(surahView: RootSurahView): void {
    this.syncTableFocusToPanelView('surahs', undefined, surahView);
    this.detailFacade.setSurahView(surahView);
    this.updateQueryParams(buildRootsQueryParams({ view: 'surahs', column: 'surahs', surahView, typeCode: null }));
  }

  protected onClearSelection(): void { this.clearTableFocus(); this.detailFacade.clearSelection(); this.updateQueryParams(buildClearSelectionQueryParams()); }

  protected onExplainerToggled(expanded: boolean): void {
    this.explainerExpanded.set(expanded);
    this.explainerPreference.setExpanded('roots', expanded);
  }

  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: changes, queryParamsHandling: 'merge' });
  }

  private commitCountOpened(event: RootCountTarget): void {
    const { root, view, column } = event;
    const wordView = view === 'words' ? (event.wordView ?? 'simple') : undefined;
    const surahView = view === 'surahs' ? (event.surahView ?? 'mentioned') : undefined;
    this.updateQueryParams(buildRootsQueryParams({ rootId: root.id, view, column, detailPage: null, wordView: view === 'words' ? (event.wordView ?? 'simple') : null, surahView: view === 'surahs' ? (event.surahView ?? 'mentioned') : null, typeCode: null }));
  }

  private clearTableFocus(): void { this.tableFocus.clear(); }

  private syncTableFocusToPanelView(view: RootView, wordView: RootWordView = this.panelState().wordView, surahView: RootSurahView = this.panelState().surahView): void {
    const selectedRootId = this.panelState().selectedRootId;
    this.tableFocus.setFocus(selectedRootId === null ? null : { rowId: selectedRootId, column: this.defaultColumnForView(view, wordView), view, wordView: view === 'words' ? wordView : undefined, surahView: view === 'surahs' ? surahView : undefined });
  }

  private defaultColumnForView(view: RootView, wordView: RootWordView = this.panelState().wordView): RootTableColumnKey {
    switch (view) {
      case 'words': return wordView === 'tashkeel' ? 'tashkeel' : 'simple';
      case 'surahs': return 'surahs';
      case 'lemmas': return 'lemmas';
      case 'stems': return 'stems';
      case 'ayahs':
      default: return 'occurrences';
    }
  }

  private defaultColumnForEvent(view: RootView, wordView: RootWordView | undefined): RootTableColumnKey {
    return this.defaultColumnForView(view, wordView ?? 'simple');
  }
}
