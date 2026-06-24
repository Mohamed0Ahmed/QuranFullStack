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
import {
  RootCountOpenedEvent,
  RootsTableComponent,
} from '../../components/roots-table/roots-table.component';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  ROOTS_EMPTY_SELECTION_LABEL,
  ROOTS_EMPTY_VIEW_LABEL,
  ROOTS_LOADING_LABEL,
  ROOTS_NO_RESULTS_LABEL,
  ROOTS_NOT_FOUND_LABEL,
  ROOTS_PAGE_TITLE,
  ROOTS_SEARCH_LABEL,
  ROOTS_SEARCH_PLACEHOLDER,
  ROOTS_SORT_LABELS,
} from '../../models/roots.labels';
import {
  DEFAULT_ROOT_VIEW,
  RootListItemViewModel,
  RootSort,
  RootView,
} from '../../models/roots.models';
import { RootsDetailFacade } from '../../state/roots-detail.facade';
import { RootsExplorerFacade } from '../../state/roots-explorer.facade';
import {
  buildClearSelectionQueryParams,
  buildRootsQueryParams,
} from '../../state/roots-url-sync';

/**
 * Roots Explorer routeable smart page (Feature 015, US1 T030/T032). Route:
 * `/dashboard/words/roots`. Composes the roots table + search + sort + shared
 * pagination on the inline-start side and the persistent detail panel shell on
 * the inline-end side. Wires list state to the URL (search/sort/page) and
 * selection/panel state (root/view/...). Count-cell clicks and row-select drive
 * the URL; the panel renders the empty-selection state until a root is selected.
 */
@Component({
  selector: 'qd-roots-explorer-page',
  standalone: true,
  imports: [AyahMatchesListComponent, RootDetailsPanelComponent, RootsTableComponent, PaginationComponent],
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
  protected readonly loadingLabel = ROOTS_LOADING_LABEL;
  protected readonly notFoundLabel = ROOTS_NOT_FOUND_LABEL;
  protected readonly noResultsLabel = ROOTS_NO_RESULTS_LABEL;
  protected readonly searchLabel = ROOTS_SEARCH_LABEL;
  protected readonly searchPlaceholder = ROOTS_SEARCH_PLACEHOLDER;

  // Getter mirrors the UniqueWordsFacade workaround so the experimental unit-test
  // SSR runner resolves the cross-module const correctly.
  protected get sortLabels() {
    return ROOTS_SORT_LABELS;
  }

  protected readonly listState = this.listFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;

  protected readonly sortOptions: readonly RootSort[] = ['mushaf-order', 'occurrences', 'alpha'];

  // Local draft for the search input so typing does not reload on every
  // keystroke; the facade reloads after the debounced emission. Seeded from the
  // active (URL) search so a restored link shows its term in the box.
  protected readonly searchDraft = signal('');

  private readonly searchInput = new Subject<string>();
  private searchSub?: Subscription;

  protected readonly activeView = computed(() => this.panelState().view);
  protected readonly emptySelection = computed(
    () => this.panelState().selectedRootId === null,
  );
  protected readonly defaultView: RootView = DEFAULT_ROOT_VIEW;

  ngOnInit(): void {
    this.listFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);

    this.searchSub = this.searchInput
      .pipe(debounceTime(300))
      .subscribe((value) => this.updateQueryParams({ search: value || null, page: null }));
  }

  ngOnDestroy(): void {
    this.listFacade.unbindFromRoute();
    this.detailFacade.unbindFromRoute();
    this.searchSub?.unsubscribe();
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

  /** Row select → default view=ayahs + root selected. */
  protected onRowSelected(root: RootListItemViewModel): void {
    this.detailFacade.selectRoot(
      {
        id: root.id,
        rootText: root.rootText,
        occurrencesCount: root.occurrencesCount,
        ayahsCount: root.ayahsCount,
        surahsCount: root.surahsCount,
        simpleWordsCount: root.simpleWordsCount,
        tashkeelWordsCount: root.tashkeelWordsCount,
        lemmasCount: root.lemmasCount,
        stemsCount: root.stemsCount,
        firstVerseKey: root.firstVerseKey,
      },
      DEFAULT_ROOT_VIEW,
    );
    this.updateQueryParams(
      buildRootsQueryParams({ rootId: root.id, view: DEFAULT_ROOT_VIEW, detailPage: null }),
    );
  }

  /** Count-cell click → target view + sub-view (URL reflects the mapping). */
  protected onCountOpened(event: RootCountOpenedEvent): void {
    const { root, view } = event;
    const wordView = view === 'words' ? (event.wordView ?? 'simple') : undefined;
    const surahView = view === 'surahs' ? (event.surahView ?? 'mentioned') : undefined;

    this.detailFacade.selectRootWithPanel(
      {
        id: root.id,
        rootText: root.rootText,
        occurrencesCount: root.occurrencesCount,
        ayahsCount: root.ayahsCount,
        surahsCount: root.surahsCount,
        simpleWordsCount: root.simpleWordsCount,
        tashkeelWordsCount: root.tashkeelWordsCount,
        lemmasCount: root.lemmasCount,
        stemsCount: root.stemsCount,
        firstVerseKey: root.firstVerseKey,
      },
      view,
      wordView,
      surahView,
    );

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
