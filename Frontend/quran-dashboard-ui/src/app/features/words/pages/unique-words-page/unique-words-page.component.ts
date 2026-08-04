import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, debounceTime, Subject, switchMap } from 'rxjs';

import { UniqueWordsFacade } from '../../state/unique-words.facade';
import {
  buildModalCloseQueryParams,
  buildUniqueWordsQueryParams,
  parseUniqueWordsQueryParams,
} from '../../state/unique-words-url-sync';
import { EMPTY_RANGE_FILTERS, RangeFilters, buildRangeQueryParams } from '../../state/words-range-filters';
import { ExplorerCountRangeFilterComponent } from '../../components/explorer-count-range-filter/explorer-count-range-filter.component';
import {
  AssociationOption,
  ExplorerAssociationFilterComponent,
} from '../../components/explorer-association-filter/explorer-association-filter.component';
import { WordsAssociationOptionsService } from '../../data-access/words-association-options.service';
import { ExplorerResultCountComponent } from '../../../../shared/ui/result-count/explorer-result-count.component';
import { UniqueWordsTabsComponent } from '../../components/unique-words-tabs/unique-words-tabs.component';
import { ExplorerSearchRowComponent } from '../../components/explorer-search-row/explorer-search-row.component';
import { UniqueWordsTableComponent } from '../../components/unique-words-table/unique-words-table.component';
import { WordDrilldownModalComponent } from '../../components/word-drilldown-modal/word-drilldown-modal.component';
import { WordsExplainerComponent } from '../../components/words-explainer/words-explainer.component';
import { WORDS_EXPLAINER_CONTENT } from '../../models/words-explainer.content';
import { WordsExplainerPreference } from '../../state/words-explainer-preference';
import { UniqueDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { sortQueryValue } from '../../models/explorer-sort';
import {
  ACTIVE_HUB_SECTION,
  RESTORED_WORD_NOT_FOUND_LABEL,
  SEARCH_LABEL,
  SEARCH_PLACEHOLDER,
  SORT_LABEL,
  UNIQUE_WORDS_PRIMARY_ROOT_FILTER_LABEL,
  UNIQUE_WORDS_PRIMARY_ROOT_FILTER_PLACEHOLDER,
  UNIQUE_WORDS_PRIMARY_TYPE_FILTER_LABEL,
  UNIQUE_WORDS_PRIMARY_TYPE_FILTER_PLACEHOLDER,
  UNIQUE_WORDS_RESULT_COUNT_LABEL,
  UNIQUE_WORD_KIND_LABELS,
  UNIQUE_WORD_LIST_PAGINATION_LABEL,
  UNIQUE_WORD_PANEL_SURFACE_LABEL,
  UNIQUE_WORD_SORT_OPTIONS,
} from '../../models/unique-words.labels';
import {
  UNIQUE_WORDS_RANGE_METRICS,
  DEFAULT_UNIQUE_WORD_SORT,
  normalizeUniqueWordSort,
  UniqueWordKind,
  UniqueWordSort,
  WordDrilldownView,
  UniqueWordListItemViewModel,
} from '../../models/unique-words.models';
import { UniqueWordsColumnKey } from '../../utils/explorer-count-active';
import { UniqueWordsDrilldownOpenEvent } from '../../components/unique-words-table/unique-words-table.component';
import { ExplorerTableFocusController } from '../../utils/explorer-table-focus-controller';

type UniqueWordsDrilldownState = ReturnType<UniqueWordsFacade['drilldownState']>;

@Component({
  selector: 'qd-unique-words-page',
  standalone: true,
  imports: [
    ExplorerCountRangeFilterComponent,
    ExplorerAssociationFilterComponent,
    ExplorerResultCountComponent,
    UniqueWordsTabsComponent,
    ExplorerSearchRowComponent,
    UniqueWordsTableComponent,
    PaginationComponent,
    WordDrilldownModalComponent,
    WordsExplainerComponent,
  ],
  templateUrl: './unique-words-page.component.html',
  styleUrl: './unique-words-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsPageComponent implements OnInit, OnDestroy {
  private readonly facade = inject(UniqueWordsFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly associationOptions = inject(WordsAssociationOptionsService);
  private readonly explainerPreference = inject(WordsExplainerPreference);

  private readonly searchInput = new Subject<string>();
  private readonly rootSearchInput = new Subject<string>();
  private searchSub?: Subscription;
  private rangesSyncSub?: Subscription;
  private rootSearchSub?: Subscription;
  private typeOptionsSub?: Subscription;

  protected readonly ranges = signal<RangeFilters>(EMPTY_RANGE_FILTERS);
  protected get rangeMetrics() { return UNIQUE_WORDS_RANGE_METRICS; }

  protected readonly association = this.facade.association;
  protected readonly typeOptions = signal<readonly AssociationOption[]>([]);
  protected readonly rootOptions = signal<readonly AssociationOption[]>([]);
  protected readonly rootOptionsLoading = signal(false);
  // M32/M43 + M74: a picker load failure must be distinguishable from a genuine empty result.
  protected readonly rootOptionsError = signal(false);
  protected readonly selectedRootLabel = signal<string | null>(null);
  protected get primaryTypeLabel(): string { return UNIQUE_WORDS_PRIMARY_TYPE_FILTER_LABEL; }
  protected get primaryTypePlaceholder(): string { return UNIQUE_WORDS_PRIMARY_TYPE_FILTER_PLACEHOLDER; }
  protected get primaryRootLabel(): string { return UNIQUE_WORDS_PRIMARY_ROOT_FILTER_LABEL; }
  protected get primaryRootPlaceholder(): string { return UNIQUE_WORDS_PRIMARY_ROOT_FILTER_PLACEHOLDER; }

  protected get searchLabel(): string { return SEARCH_LABEL; }
  protected get searchPlaceholder(): string { return SEARCH_PLACEHOLDER; }
  protected get sortLabel(): string { return SORT_LABEL; }
  protected get sortOptions() { return UNIQUE_WORD_SORT_OPTIONS; }

  protected readonly listState = this.facade.listState;
  protected readonly drilldownState = this.facade.drilldownState;
  protected readonly restoredNotFoundLabel = RESTORED_WORD_NOT_FOUND_LABEL;
  protected readonly pageTitle = ACTIVE_HUB_SECTION.labelAr;
  // TDZ-safe content getter + synchronous collapse restore (no first-paint shift).
  protected get explainer() { return WORDS_EXPLAINER_CONTENT.unique; }
  protected readonly explainerExpanded = signal(this.explainerPreference.isExpanded('unique'));
  protected readonly listPaginationLabel = UNIQUE_WORD_LIST_PAGINATION_LABEL;
  protected readonly panelSurfaceLabel = UNIQUE_WORD_PANEL_SURFACE_LABEL;
  protected get resultCountLabel(): string { return UNIQUE_WORDS_RESULT_COUNT_LABEL; }

  protected readonly isDesktop = signal(true);
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void =>
    this.isDesktop.set(event.matches);

  protected readonly searchDraft = signal('');
  private readonly tableFocus = new ExplorerTableFocusController<
    UniqueWordsDrilldownState,
    UniqueWordsDrilldownOpenEvent,
    UniqueWordsColumnKey,
    WordDrilldownView,
    never,
    never
  >({
    panelState: this.drilldownState,
    getSelectedRowId: (state) => state.selectedWordId,
    getView: (state) => state.view,
    getWordView: () => null,
    getSurahView: () => null,
    getFallbackColumn: (state) => state.view,
    eventToFocus: (event) => ({
      rowId: event.word.id,
      column: event.column,
      view: event.view,
    }),
    commitDeferred: (event) => this.commitDrilldownOpen(event),
  });

  private readonly table = viewChild(UniqueWordsTableComponent);
  private lastScrolledListPage = 0;

  protected readonly modeLabel = computed(() => UNIQUE_WORD_KIND_LABELS[this.facade.mode()]);
  protected readonly selectedWordId = this.tableFocus.selectedRowId;
  protected readonly activeColumn = this.tableFocus.activeColumn;
  protected readonly drilldownIsOpen = computed(
    () => this.tableFocus.focus() !== null || this.drilldownState().isOpen,
  );

  protected readonly ayahParentFrame = computed<UniqueDetailFrame | null>(() => {
    const state = this.drilldownState();
    if (!state.isOpen || state.selectedWordId === null) {
      return null;
    }
    return {
      kind: 'unique',
      mode: this.facade.mode(),
      id: state.selectedWordId,
      view: state.view,
      ayahPage: state.ayahPage,
    };
  });

  constructor() {
    effect(() => {
      this.facade.mode();
      this.facade.search();
      this.searchDraft.set(untracked(() => this.facade.search()));
    });

    effect(() => {
      const state = this.listState();
      const table = this.table();
      if (
        !table ||
        state.status === 'loading' ||
        state.page === this.lastScrolledListPage ||
        (state.status !== 'success' && state.status !== 'empty')
      ) {
        return;
      }

      untracked(() => {
        table.scrollToTop();
        this.lastScrolledListPage = state.page;
      });
    });
  }

  ngOnInit(): void {
    this.facade.bindToRoute(this.route);

    this.searchSub = this.searchInput
      .pipe(debounceTime(300))
      .subscribe((value) => this.updateQueryParams({ search: value || null, page: null }));

    this.rangesSyncSub = this.route.queryParamMap.subscribe((params) =>
      this.ranges.set(parseUniqueWordsQueryParams(params).ranges),
    );

    this.typeOptionsSub = this.associationOptions
      .wordTypeOptions()
      .subscribe((result) => this.typeOptions.set(result.status === 'success' ? result.options : []));

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
    this.facade.unbindFromRoute();
    this.searchSub?.unsubscribe();
    this.rangesSyncSub?.unsubscribe();
    this.rootSearchSub?.unsubscribe();
    this.typeOptionsSub?.unsubscribe();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
    this.tableFocus.destroy();
  }

  protected onSearchChange(value: string): void {
    this.clearTableFocus();
    this.searchDraft.set(value);
    this.searchInput.next(value);
  }

  protected onSortChange(sort: UniqueWordSort | null): void {
    this.clearTableFocus();
    this.updateQueryParams(buildUniqueWordsQueryParams({ sort, page: null }));
  }

  protected onSortSelect(value: string): void {
    this.onSortChange(sortQueryValue(normalizeUniqueWordSort(value), DEFAULT_UNIQUE_WORD_SORT));
  }

  protected onRangesChange(ranges: RangeFilters): void {
    this.clearTableFocus();
    this.updateQueryParams({ ...buildRangeQueryParams(ranges, UNIQUE_WORDS_RANGE_METRICS), page: null });
  }

  protected onRootSearch(term: string): void {
    this.rootOptionsLoading.set(true);
    this.rootSearchInput.next(term);
  }

  protected onPrimaryTypeChange(option: AssociationOption | null): void {
    this.clearTableFocus();
    this.updateQueryParams({ primaryType: option === null ? null : String(option.id), page: null });
  }

  protected onPrimaryRootChange(option: AssociationOption | null): void {
    this.clearTableFocus();
    this.selectedRootLabel.set(option?.label ?? null);
    this.updateQueryParams({ rootId: option === null ? null : String(option.id), page: null });
  }

  protected onTabActivated(mode: UniqueWordKind): void {
    this.clearTableFocus();

    void this.router.navigate([`/dashboard/words/unique/${mode}`], {
      queryParams: { search: null, sort: null, page: null, word: null, view: null, ap: null },
      queryParamsHandling: 'merge',
    });
  }

  protected onRowSelected(word: UniqueWordListItemViewModel): void {
    this.tableFocus.setFocus({
      rowId: word.id,
      column: 'surahs',
      view: 'surahs',
    });
    this.facade.openDrilldown(word, 'surahs');
    this.updateQueryParams(buildUniqueWordsQueryParams({ wordId: word.id, view: 'surahs', ayahPage: null }));
  }

  protected onDrilldownOpen(event: UniqueWordsDrilldownOpenEvent): void {
    this.tableFocus.handleEvent(event, event.source ?? 'immediate');
  }

  protected onDrilldownClose(): void {
    this.clearTableFocus();

    this.facade.closeDrilldown();
    this.updateQueryParams(buildModalCloseQueryParams());
  }

  protected onDrilldownRetry(): void {
    this.facade.retryDrilldown();
  }

  protected onDrilldownViewChange(view: WordDrilldownView): void {
    this.tableFocus.cancel();
    this.syncTableFocusToView(view);
    this.facade.setDrilldownView(view);
    this.updateQueryParams(buildUniqueWordsQueryParams({ view }));
  }

  protected onAyahPageChange(page: number): void {
    this.tableFocus.cancel();
    this.facade.setAyahPage(page);
    this.updateQueryParams(buildUniqueWordsQueryParams({ ayahPage: page }));
  }

  protected onPaginationPageChange(page: number): void {
    if (page === this.listState().page) {
      return;
    }

    this.clearTableFocus();
    this.updateQueryParams(buildUniqueWordsQueryParams({ page }));
  }

  protected onExplainerToggled(expanded: boolean): void {
    this.explainerExpanded.set(expanded);
    this.explainerPreference.setExpanded('unique', expanded);
  }

  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: changes,
      queryParamsHandling: 'merge',
    });
  }

  private commitDrilldownOpen(event: UniqueWordsDrilldownOpenEvent): void {
    this.facade.openDrilldown(event.word, event.view);
    this.updateQueryParams(buildUniqueWordsQueryParams({ wordId: event.word.id, view: event.view, ayahPage: null }));
  }

  private clearTableFocus(): void {
    this.tableFocus.clear();
  }

  private syncTableFocusToView(view: WordDrilldownView): void {
    const selectedWordId = this.drilldownState().selectedWordId;
    if (selectedWordId === null) {
      this.tableFocus.setFocus(null);
      return;
    }

    this.tableFocus.setFocus({
      rowId: selectedWordId,
      column: this.defaultColumnForView(view),
      view,
    });
  }

  private defaultColumnForView(view: WordDrilldownView): UniqueWordsColumnKey {
    return view;
  }
}
