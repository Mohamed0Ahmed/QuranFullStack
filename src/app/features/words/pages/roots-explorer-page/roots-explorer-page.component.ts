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

import { RootDetailsPanelComponent } from '../../components/root-details-panel/root-details-panel.component';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { RootLemmasListComponent } from '../../components/root-lemmas-list/root-lemmas-list.component';
import { RootStemsListComponent } from '../../components/root-stems-list/root-stems-list.component';
import { RootWordsListComponent } from '../../components/root-words-list/root-words-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import {
  RootCountOpenedEvent,
  RootsTableComponent,
} from '../../components/roots-table/roots-table.component';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  ROOTS_EMPTY_SELECTION_LABEL,
  ROOTS_EMPTY_VIEW_LABEL,
  ROOTS_NO_RESULTS_LABEL,
  ROOTS_NOT_FOUND_LABEL,
  ROOTS_PANEL_LABEL,
  ROOTS_LIST_PAGINATION_LABEL,
  ROOTS_PAGE_TITLE,
  ROOTS_SEARCH_LABEL,
  ROOTS_SEARCH_PLACEHOLDER,
  ROOTS_SORT_LABELS,
  ROOTS_SURAHS_VIEW_LABELS,
  ROOTS_SURAHS_TABLIST_LABEL,
  ROOTS_TABLE_LABEL,
  ROOTS_WORDS_TABLIST_LABEL,
  ROOTS_WORD_VIEW_LABELS,
} from '../../models/roots.labels';
import {
  DEFAULT_ROOT_VIEW,
  PagedResultDto,
  RootListItemViewModel,
  RootWordItemDto,
  ROOT_DETAIL_PAGE_SIZE,
  RootSort,
  RootSurahView,
  RootView,
  RootWordView,
  toRootSummary,
} from '../../models/roots.models';
import { RootsDetailFacade } from '../../state/roots-detail.facade';
import { RootsExplorerFacade } from '../../state/roots-explorer.facade';
import {
  buildClearSelectionQueryParams,
  buildRootsQueryParams,
} from '../../state/roots-url-sync';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { AyahMatchDto } from '../../models/unique-words.models';
import { mapRootAyahMatchToShared } from '../../utils/root-ayah-match.mapper';

@Component({
  selector: 'qd-roots-explorer-page',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
    NgTemplateOutlet,
    RootDetailsPanelComponent,
    RootLemmasListComponent,
    RootStemsListComponent,
    RootWordsListComponent,
    RootsTableComponent,
    SurahOccurrencesListComponent,
    PaginationComponent,
  ],
  templateUrl: './roots-explorer-page.component.html',
  styleUrl: './roots-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootsExplorerPageComponent implements OnInit, OnDestroy {
  private readonly listFacade = inject(RootsExplorerFacade);
  private readonly detailFacade = inject(RootsDetailFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly pageTitle = ROOTS_PAGE_TITLE;
  protected readonly emptySelectionLabel = ROOTS_EMPTY_SELECTION_LABEL;
  protected readonly emptyViewLabel = ROOTS_EMPTY_VIEW_LABEL;
  protected readonly notFoundLabel = ROOTS_NOT_FOUND_LABEL;
  protected readonly noResultsLabel = ROOTS_NO_RESULTS_LABEL;
  protected readonly searchLabel = ROOTS_SEARCH_LABEL;
  protected readonly searchPlaceholder = ROOTS_SEARCH_PLACEHOLDER;

  protected get sortLabels() {
    return ROOTS_SORT_LABELS;
  }

  protected readonly listState = this.listFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;

  protected readonly sortOptions: readonly RootSort[] = ['mushaf-order', 'occurrences', 'alpha'];
  protected readonly wordViewOptions: readonly RootWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly RootSurahView[] = ['mentioned', 'missing'];

  protected readonly emptyAyahsPage: PagedResultDto<AyahMatchDto> = {
    page: 1,
    pageSize: ROOT_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected readonly emptyWordsPage: PagedResultDto<RootWordItemDto> = {
    page: 1,
    pageSize: ROOT_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected get wordViewLabels() {
    return ROOTS_WORD_VIEW_LABELS;
  }

  protected get surahViewLabels() {
    return ROOTS_SURAHS_VIEW_LABELS;
  }

  protected readonly listPaginationLabel = ROOTS_LIST_PAGINATION_LABEL;
  protected readonly panelLabel = ROOTS_PANEL_LABEL;
  protected readonly rootsTableLabel = ROOTS_TABLE_LABEL;
  protected readonly wordsTablistLabel = ROOTS_WORDS_TABLIST_LABEL;
  protected readonly surahsTablistLabel = ROOTS_SURAHS_TABLIST_LABEL;

  protected readonly searchDraft = signal('');
  protected readonly isDesktop = signal(true);

  private readonly searchInput = new Subject<string>();
  private searchSub?: Subscription;
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void =>
    this.isDesktop.set(event.matches);

  protected readonly activeView = computed(() => this.panelState().view);
  protected readonly emptySelection = computed(
    () => this.panelState().selectedRootId === null,
  );
  protected readonly defaultView: RootView = DEFAULT_ROOT_VIEW;
  protected readonly ayahsPageForView = computed((): PagedResultDto<AyahMatchDto> => {
    const page = this.panelState().ayahs;

    if (!page) {
      return this.emptyAyahsPage;
    }

    return {
      ...page,
      items: page.items.map(mapRootAyahMatchToShared),
    };
  });

  ngOnInit(): void {
    this.listFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);

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
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
  }

  protected onSearchInput(value: string): void {
    this.searchDraft.set(value);
    this.searchInput.next(value);
  }

  protected onSortChange(sort: RootSort): void {
    this.updateQueryParams({ sort, page: null });
  }

  protected onPageChange(page: number): void {
    if (page === this.listState().page) {
      return;
    }
    this.updateQueryParams(buildRootsQueryParams({ page }));
  }

  protected onRowSelected(root: RootListItemViewModel): void {
    this.detailFacade.selectRoot(toRootSummary(root), DEFAULT_ROOT_VIEW);
    this.updateQueryParams(
      buildRootsQueryParams({
        rootId: root.id,
        view: DEFAULT_ROOT_VIEW,
        wordView: 'simple',
        surahView: null,
        detailPage: null,
      }),
    );
  }

  protected onCountOpened(event: RootCountOpenedEvent): void {
    const { root, view } = event;
    const wordView = view === 'words' ? (event.wordView ?? 'simple') : undefined;
    const surahView = view === 'surahs' ? (event.surahView ?? 'mentioned') : undefined;

    this.detailFacade.selectRootWithPanel(toRootSummary(root), view, wordView, surahView);

    const params = buildRootsQueryParams({
      rootId: root.id,
      view,
      detailPage: null,
      wordView: view === 'words' ? (event.wordView ?? 'simple') : null,
      surahView: view === 'surahs' ? (event.surahView ?? 'mentioned') : null,
    });

    this.updateQueryParams(params);
  }

  protected onDetailPageChange(page: number): void {
    this.detailFacade.setDetailPage(page);
    this.updateQueryParams(buildRootsQueryParams({ detailPage: page }));
  }

  protected onPanelViewChange(view: RootView): void {
    this.detailFacade.setView(view);
    this.updateQueryParams(
      buildRootsQueryParams({
        view,
        detailPage: null,
        wordView: view === 'words' ? 'simple' : null,
        surahView: view === 'surahs' ? 'mentioned' : null,
      }),
    );
  }

  protected onWordViewChange(wordView: RootWordView): void {
    this.detailFacade.setWordView(wordView);
    this.updateQueryParams(
      buildRootsQueryParams({
        view: 'words',
        wordView,
        detailPage: null,
      }),
    );
  }

  protected onSurahViewChange(surahView: RootSurahView): void {
    this.detailFacade.setSurahView(surahView);
    this.updateQueryParams(
      buildRootsQueryParams({
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
