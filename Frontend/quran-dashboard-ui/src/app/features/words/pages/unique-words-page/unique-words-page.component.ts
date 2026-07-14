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
import { ExplorerResultCountComponent } from '../../components/explorer-result-count/explorer-result-count.component';
import { UniqueWordsTabsComponent } from '../../components/unique-words-tabs/unique-words-tabs.component';
import { UniqueWordsSearchBarComponent } from '../../components/unique-words-search-bar/unique-words-search-bar.component';
import { UniqueWordsTableComponent } from '../../components/unique-words-table/unique-words-table.component';
import { WordDrilldownModalComponent } from '../../components/word-drilldown-modal/word-drilldown-modal.component';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import {
  ACTIVE_HUB_SECTION,
  EMPTY_LIST_LABEL,
  RESTORED_WORD_NOT_FOUND_LABEL,
  UNIQUE_WORDS_PRIMARY_ROOT_FILTER_LABEL,
  UNIQUE_WORDS_PRIMARY_ROOT_FILTER_PLACEHOLDER,
  UNIQUE_WORDS_PRIMARY_TYPE_FILTER_LABEL,
  UNIQUE_WORDS_PRIMARY_TYPE_FILTER_PLACEHOLDER,
  UNIQUE_WORDS_RESULT_COUNT_LABEL,
  UNIQUE_WORD_KIND_LABELS,
  UNIQUE_WORD_LIST_PAGINATION_LABEL,
  UNIQUE_WORD_PANEL_SURFACE_LABEL,
} from '../../models/unique-words.labels';
import {
  UNIQUE_WORDS_RANGE_METRICS,
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
    UniqueWordsSearchBarComponent,
    UniqueWordsTableComponent,
    PaginationComponent,
    WordDrilldownModalComponent,
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
  protected readonly selectedRootLabel = signal<string | null>(null);
  protected get primaryTypeLabel(): string { return UNIQUE_WORDS_PRIMARY_TYPE_FILTER_LABEL; }
  protected get primaryTypePlaceholder(): string { return UNIQUE_WORDS_PRIMARY_TYPE_FILTER_PLACEHOLDER; }
  protected get primaryRootLabel(): string { return UNIQUE_WORDS_PRIMARY_ROOT_FILTER_LABEL; }
  protected get primaryRootPlaceholder(): string { return UNIQUE_WORDS_PRIMARY_ROOT_FILTER_PLACEHOLDER; }

  protected readonly listState = this.facade.listState;
  protected readonly drilldownState = this.facade.drilldownState;
  protected readonly emptyLabel = EMPTY_LIST_LABEL;
  protected readonly restoredNotFoundLabel = RESTORED_WORD_NOT_FOUND_LABEL;
  protected readonly pageTitle = ACTIVE_HUB_SECTION.labelAr;
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
      .subscribe((options) => this.typeOptions.set(options));

    this.rootSearchSub = this.rootSearchInput
      .pipe(debounceTime(300), switchMap((term) => this.associationOptions.searchRoots(term)))
      .subscribe((options) => {
        this.rootOptions.set(options);
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

  protected onSortChange(sort: UniqueWordSort): void {
    this.clearTableFocus();
    this.updateQueryParams({ sort, page: null });
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
