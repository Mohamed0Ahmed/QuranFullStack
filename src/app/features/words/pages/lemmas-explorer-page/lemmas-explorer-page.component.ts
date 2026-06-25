import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, Subject, debounceTime } from 'rxjs';

import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { LemmaDetailsPanelComponent } from '../../components/lemma-details-panel/lemma-details-panel.component';
import { LemmaWordsListComponent } from '../../components/lemma-words-list/lemma-words-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import {
  LemmaCountOpenedEvent,
  LemmasTableComponent,
} from '../../components/lemmas-table/lemmas-table.component';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  LEMMAS_EMPTY_SELECTION_LABEL,
  LEMMAS_EMPTY_VIEW_LABEL,
  LEMMAS_LOADING_LABEL,
  LEMMAS_NO_RESULTS_LABEL,
  LEMMAS_NOT_FOUND_LABEL,
  LEMMAS_PAGE_TITLE,
  LEMMAS_SEARCH_LABEL,
  LEMMAS_SEARCH_PLACEHOLDER,
  LEMMAS_SORT_LABELS,
  LEMMAS_SURAHS_VIEW_LABELS,
  LEMMAS_WORD_VIEW_LABELS,
} from '../../models/lemmas.labels';
import {
  DEFAULT_LEMMA_VIEW,
  LEMMA_DETAIL_PAGE_SIZE,
  LemmaAyahMatchDto,
  LemmaListItemViewModel,
  LemmaWordItemDto,
  LemmaSort,
  LemmaSurahView,
  LemmaView,
  LemmaWordView,
  PagedResultDto,
  LEMMAS_QUERY_KEYS,
  toLemmaSummary,
} from '../../models/lemmas.models';
import { LemmasDetailFacade } from '../../state/lemmas-detail.facade';
import { LemmasExplorerFacade } from '../../state/lemmas-explorer.facade';
import {
  buildClearSelectionQueryParams,
  buildLemmasQueryParams,
} from '../../state/lemmas-url-sync';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';

@Component({
  selector: 'qd-lemmas-explorer-page',
  standalone: true,
  imports: [
    NgTemplateOutlet,
    AyahMatchesListComponent,
    LemmaDetailsPanelComponent,
    LemmaWordsListComponent,
    LemmasTableComponent,
    MissingSurahsListComponent,
    PaginationComponent,
    SurahOccurrencesListComponent,
  ],
  templateUrl: './lemmas-explorer-page.component.html',
  styleUrl: './lemmas-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmasExplorerPageComponent implements OnInit, OnDestroy {
  private readonly listFacade = inject(LemmasExplorerFacade);
  private readonly detailFacade = inject(LemmasDetailFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly pageTitle = LEMMAS_PAGE_TITLE;
  protected readonly emptySelectionLabel = LEMMAS_EMPTY_SELECTION_LABEL;
  protected readonly emptyViewLabel = LEMMAS_EMPTY_VIEW_LABEL;
  protected readonly notFoundLabel = LEMMAS_NOT_FOUND_LABEL;
  protected readonly noResultsLabel = LEMMAS_NO_RESULTS_LABEL;
  protected readonly searchLabel = LEMMAS_SEARCH_LABEL;
  protected readonly searchPlaceholder = LEMMAS_SEARCH_PLACEHOLDER;
  protected readonly panelLoadingLabel = LEMMAS_LOADING_LABEL;

  protected get sortLabels() {
    return LEMMAS_SORT_LABELS;
  }

  protected readonly listState = this.listFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;

  protected readonly sortOptions: readonly LemmaSort[] = ['mushaf-order', 'occurrences', 'alpha'];
  protected readonly wordViewOptions: readonly LemmaWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly LemmaSurahView[] = ['mentioned', 'missing'];

  protected get wordViewLabels() {
    return LEMMAS_WORD_VIEW_LABELS;
  }

  protected get surahViewLabels() {
    return LEMMAS_SURAHS_VIEW_LABELS;
  }

  protected readonly emptyAyahsPage: PagedResultDto<LemmaAyahMatchDto> = {
    page: 1,
    pageSize: LEMMA_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected readonly emptyWordsPage: PagedResultDto<LemmaWordItemDto> = {
    page: 1,
    pageSize: LEMMA_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected readonly searchDraft = signal('');
  protected readonly isDesktop = signal(true);

  private readonly searchInput = new Subject<string>();
  private searchSub?: Subscription;
  private searchSyncSub?: Subscription;
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void =>
    this.isDesktop.set(event.matches);

  protected readonly activeView = computed(() => this.panelState().view);
  protected readonly emptySelection = computed(
    () => this.panelState().selectedLemmaId === null,
  );
  protected readonly defaultView: LemmaView = DEFAULT_LEMMA_VIEW;

  ngOnInit(): void {
    this.listFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);

    this.searchSyncSub = this.route.queryParamMap.subscribe((params) => {
      this.searchDraft.set(params.get(LEMMAS_QUERY_KEYS.search) ?? '');
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

  protected onSortChange(sort: LemmaSort): void {
    this.updateQueryParams({ sort, page: null });
  }

  protected onPageChange(page: number): void {
    if (page === this.listState().page) {
      return;
    }
    this.updateQueryParams(buildLemmasQueryParams({ page }));
  }

  protected onDetailPageChange(page: number): void {
    if (page === this.panelState().detailPage) {
      return;
    }

    this.detailFacade.setDetailPage(page);
    this.updateQueryParams(buildLemmasQueryParams({ detailPage: page }));
  }

  protected onRowSelected(lemma: LemmaListItemViewModel): void {
    this.detailFacade.selectLemma(toLemmaSummary(lemma), DEFAULT_LEMMA_VIEW);
    this.updateQueryParams(
      buildLemmasQueryParams({
        lemmaId: lemma.id,
        view: DEFAULT_LEMMA_VIEW,
        wordView: 'simple',
        surahView: null,
        detailPage: null,
      }),
    );
  }

  protected onCountOpened(event: LemmaCountOpenedEvent): void {
    const { lemma, view } = event;
    const wordView = view === 'words' ? (event.wordView ?? 'simple') : undefined;
    const surahView = view === 'surahs' ? (event.surahView ?? 'mentioned') : undefined;

    this.detailFacade.selectLemmaWithPanel(
      toLemmaSummary(lemma),
      view,
      wordView,
      surahView,
    );

    this.updateQueryParams(
      buildLemmasQueryParams({
        lemmaId: lemma.id,
        view,
        detailPage: null,
        wordView: view === 'words' ? (event.wordView ?? 'simple') : null,
        surahView: view === 'surahs' ? (event.surahView ?? 'mentioned') : null,
      }),
    );
  }

  protected onPanelViewChange(view: LemmaView): void {
    this.detailFacade.setView(view);
    this.updateQueryParams(
      buildLemmasQueryParams({
        view,
        detailPage: null,
        wordView: view === 'words' ? 'simple' : null,
        surahView: view === 'surahs' ? 'mentioned' : null,
      }),
    );
  }

  protected onWordViewChange(wordView: LemmaWordView): void {
    this.detailFacade.setWordView(wordView);
    this.updateQueryParams(buildLemmasQueryParams({ view: 'words', wordView, detailPage: null }));
  }

  protected onSurahViewChange(surahView: LemmaSurahView): void {
    this.detailFacade.setSurahView(surahView);
    this.updateQueryParams(
      buildLemmasQueryParams({
        view: 'surahs',
        surahView,
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
}
