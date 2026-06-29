import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, Subject, debounceTime } from 'rxjs';

import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { StemDetailsPanelComponent } from '../../components/stem-details-panel/stem-details-panel.component';
import { StemAyahTypeFiltersComponent } from '../../components/stem-ayah-type-filters/stem-ayah-type-filters.component';
import { StemLemmasListComponent } from '../../components/stem-lemmas-list/stem-lemmas-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { StemWordsListComponent } from '../../components/stem-words-list/stem-words-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { StemCountOpenedEvent, StemsTableComponent } from '../../components/stems-table/stems-table.component';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  STEMS_EMPTY_SELECTION_LABEL,
  STEMS_EMPTY_VIEW_LABEL,
  STEMS_LOADING_LABEL,
  STEMS_NO_RESULTS_LABEL,
  STEMS_NOT_FOUND_LABEL,
  STEMS_PAGE_TITLE,
  STEMS_LIST_PAGINATION_LABEL,
  STEMS_PANEL_SURFACE_LABEL,
  STEMS_SEARCH_LABEL,
  STEMS_SEARCH_PLACEHOLDER,
  STEMS_SORT_LABELS,
  STEMS_SURAHS_TABLIST_LABEL,
  STEMS_SURAHS_VIEW_LABELS,
  STEMS_TABLE_LABEL,
  STEMS_WORDS_TABLIST_LABEL,
  STEMS_WORD_VIEW_LABELS,
} from '../../models/stems.labels';
import {
  DEFAULT_STEM_VIEW,
  PagedResultDto,
  STEM_DETAIL_PAGE_SIZE,
  StemListItemViewModel,
  StemWordItemDto,
  StemSort,
  StemSurahView,
  StemView,
  StemWordView,
  STEMS_QUERY_KEYS,
} from '../../models/stems.models';
import { AyahMatchDto, PagedResultDto as SharedPagedResultDto } from '../../models/unique-words.models';
import { StemsDetailFacade } from '../../state/stems-detail.facade';
import { StemsExplorerFacade } from '../../state/stems-explorer.facade';
import { buildClearSelectionQueryParams, buildStemsQueryParams } from '../../state/stems-url-sync';
import { mapStemAyahMatchToShared } from '../../utils/stem-ayah-match.mapper';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';

@Component({
  selector: 'qd-stems-explorer-page',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
    NgTemplateOutlet,
    PaginationComponent,
    StemDetailsPanelComponent,
    StemAyahTypeFiltersComponent,
    StemLemmasListComponent,
    StemWordsListComponent,
    StemsTableComponent,
    SurahOccurrencesListComponent,
  ],
  templateUrl: './stems-explorer-page.component.html',
  styleUrl: './stems-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemsExplorerPageComponent implements OnInit, OnDestroy {
  private readonly listFacade = inject(StemsExplorerFacade);
  private readonly detailFacade = inject(StemsDetailFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly pageTitle = STEMS_PAGE_TITLE;
  protected readonly emptySelectionLabel = STEMS_EMPTY_SELECTION_LABEL;
  protected readonly emptyViewLabel = STEMS_EMPTY_VIEW_LABEL;
  protected readonly notFoundLabel = STEMS_NOT_FOUND_LABEL;
  protected readonly noResultsLabel = STEMS_NO_RESULTS_LABEL;
  protected readonly searchLabel = STEMS_SEARCH_LABEL;
  protected readonly searchPlaceholder = STEMS_SEARCH_PLACEHOLDER;
  protected readonly panelLoadingLabel = STEMS_LOADING_LABEL;
  protected readonly tableSectionLabel = STEMS_TABLE_LABEL;
  protected readonly listPaginationLabel = STEMS_LIST_PAGINATION_LABEL;
  protected readonly panelSurfaceLabel = STEMS_PANEL_SURFACE_LABEL;
  protected readonly wordsTablistLabel = STEMS_WORDS_TABLIST_LABEL;
  protected readonly surahsTablistLabel = STEMS_SURAHS_TABLIST_LABEL;

  protected get sortLabels() {
    return STEMS_SORT_LABELS;
  }

  protected readonly listState = this.listFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;

  protected readonly sortOptions: readonly StemSort[] = ['mushaf-order', 'occurrences', 'alpha'];
  protected readonly wordViewOptions: readonly StemWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly StemSurahView[] = ['mentioned', 'missing'];

  protected get wordViewLabels() {
    return STEMS_WORD_VIEW_LABELS;
  }

  protected get surahViewLabels() {
    return STEMS_SURAHS_VIEW_LABELS;
  }

  protected readonly emptyAyahsPage: SharedPagedResultDto<AyahMatchDto> = {
    page: 1,
    pageSize: STEM_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected readonly emptyWordsPage: PagedResultDto<StemWordItemDto> = {
    page: 1,
    pageSize: STEM_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected readonly ayahsPageForView = computed((): SharedPagedResultDto<AyahMatchDto> => {
    const page = this.panelState().ayahs;

    if (!page) {
      return this.emptyAyahsPage;
    }

    return {
      ...page,
      items: page.items.map(mapStemAyahMatchToShared),
    };
  });

  protected readonly searchDraft = signal('');
  protected readonly isDesktop = signal(true);

  private readonly searchInput = new Subject<string>();
  private searchSub?: Subscription;
  private searchSyncSub?: Subscription;
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void => this.isDesktop.set(event.matches);

  protected readonly activeView = computed(() => this.panelState().view);
  protected readonly emptySelection = computed(() => this.panelState().selectedStemId === null);
  protected readonly defaultView: StemView = DEFAULT_STEM_VIEW;

  constructor() {
    effect(() => {
      const state = this.panelState();
      const typeCode = state.ayahTypeCode;
      const summary = state.summary;

      if (state.selectedStemId === null || state.view !== 'ayahs' || typeCode === null || summary === null) {
        return;
      }

      if (summary.typeDistribution.some((item) => item.code === typeCode)) {
        return;
      }

      this.detailFacade.setAyahTypeCode(null);
      this.updateQueryParams(buildStemsQueryParams({ typeCode: null, detailPage: 1 }));
    });
  }

  ngOnInit(): void {
    this.listFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);

    this.searchSyncSub = this.route.queryParamMap.subscribe((params) => {
      this.searchDraft.set(params.get(STEMS_QUERY_KEYS.search) ?? '');
    });

    this.searchSub = this.searchInput
      .pipe(debounceTime(300))
      .subscribe((value) => this.updateQueryParams({ search: value || null, page: null }));

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
  }

  protected onSearchInput(value: string): void {
    this.searchDraft.set(value);
    this.searchInput.next(value);
  }

  protected onSortChange(sort: StemSort): void {
    this.updateQueryParams({ sort, page: null });
  }

  protected onPageChange(page: number): void {
    if (page === this.listState().page) {
      return;
    }
    this.updateQueryParams(buildStemsQueryParams({ page }));
  }

  protected onDetailPageChange(page: number): void {
    if (page === this.panelState().detailPage) {
      return;
    }

    this.detailFacade.setDetailPage(page);
    this.updateQueryParams(buildStemsQueryParams({ detailPage: page }));
  }

  protected onRowSelected(stem: StemListItemViewModel): void {
    this.updateQueryParams(
      buildStemsQueryParams({
        stemId: stem.id,
        view: DEFAULT_STEM_VIEW,
        wordView: 'simple',
        surahView: null,
        detailPage: 1,
        typeCode: null,
      }),
    );
  }

  protected onCountOpened(event: StemCountOpenedEvent): void {
    const { stem, view } = event;
    const wordView = view === 'words' ? (event.wordView ?? 'simple') : undefined;
    const surahView = view === 'surahs' ? (event.surahView ?? 'mentioned') : undefined;

    this.updateQueryParams(
      buildStemsQueryParams({
        stemId: stem.id,
        view,
        detailPage: this.detailPageForView(view),
        wordView: wordView ?? null,
        surahView: surahView ?? null,
        typeCode: null,
      }),
    );
  }

  protected onPanelViewChange(view: StemView): void {
    this.detailFacade.setView(view);
    this.updateQueryParams(
      buildStemsQueryParams({
        view,
        detailPage: this.detailPageForView(view),
        wordView: view === 'words' ? 'simple' : null,
        surahView: view === 'surahs' ? 'mentioned' : null,
        typeCode: null,
      }),
    );
  }

  protected onAyahTypeChange(typeCode: string | null): void {
    const current = this.panelState();
    if (current.selectedStemId === null || current.view !== 'ayahs') {
      return;
    }

    if (current.ayahTypeCode === typeCode && current.detailPage === 1) {
      return;
    }

    this.detailFacade.setAyahTypeCode(typeCode);
    this.updateQueryParams(
      buildStemsQueryParams({
        view: 'ayahs',
        detailPage: 1,
        typeCode,
      }),
    );
  }

  protected onWordViewChange(wordView: StemWordView): void {
    this.detailFacade.setWordView(wordView);
    this.updateQueryParams(
      buildStemsQueryParams({ view: 'words', wordView, detailPage: 1, typeCode: null }),
    );
  }

  protected onSurahViewChange(surahView: StemSurahView): void {
    this.detailFacade.setSurahView(surahView);
    this.updateQueryParams(
      buildStemsQueryParams({
        view: 'surahs',
        surahView,
        detailPage: null,
        typeCode: null,
      }),
    );
  }

  protected onClearSelection(): void {
    this.detailFacade.clearSelection();
    this.updateQueryParams(buildClearSelectionQueryParams());
  }

  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: changes,
      queryParamsHandling: 'merge',
    });
  }

  private detailPageForView(view: StemView): number | null {
    return view === 'words' || view === 'ayahs' ? 1 : null;
  }
}
