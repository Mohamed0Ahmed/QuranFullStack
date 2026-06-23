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
import { Subscription, debounceTime, Subject } from 'rxjs';

import { UniqueWordsFacade } from '../../state/unique-words.facade';
import {
  buildModalCloseQueryParams,
  buildUniqueWordsQueryParams,
} from '../../state/unique-words-url-sync';
import { UniqueWordsTabsComponent } from '../../components/unique-words-tabs/unique-words-tabs.component';
import { UniqueWordsSearchBarComponent } from '../../components/unique-words-search-bar/unique-words-search-bar.component';
import { UniqueWordsTableComponent } from '../../components/unique-words-table/unique-words-table.component';
import { UniqueWordsListPaginationComponent } from '../../components/unique-words-list-pagination/unique-words-list-pagination.component';
import { WordDrilldownModalComponent } from '../../components/word-drilldown-modal/word-drilldown-modal.component';
import {
  EMPTY_LIST_LABEL,
  LOADING_LABEL,
  RESTORED_WORD_NOT_FOUND_LABEL,
  UNIQUE_WORD_KIND_LABELS,
} from '../../models/unique-words.labels';
import {
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordSort,
  WordDrilldownView,
  UniqueWordListItemViewModel,
} from '../../models/unique-words.models';

@Component({
  selector: 'qd-unique-words-page',
  standalone: true,
  imports: [
    UniqueWordsTabsComponent,
    UniqueWordsSearchBarComponent,
    UniqueWordsTableComponent,
    UniqueWordsListPaginationComponent,
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

  private readonly searchInput = new Subject<string>();
  private searchSub?: Subscription;

  protected readonly listState = this.facade.listState;
  protected readonly drilldownState = this.facade.drilldownState;
  protected readonly emptyLabel = EMPTY_LIST_LABEL;
  protected readonly loadingLabel = LOADING_LABEL;
  protected readonly restoredNotFoundLabel = RESTORED_WORD_NOT_FOUND_LABEL;

  // Drives which single drill-down surface renders: the inline panel at desktop
  // widths, the overlay modal below. Rendering exactly one avoids the previous
  // dual-instance DOM (both surfaces rendered the full drill-down subtree, one
  // hidden by CSS). Defaults to desktop so SSR / non-browser test environments
  // without `matchMedia` render the inline panel. Breakpoint mirrors the SCSS.
  protected readonly isDesktop = signal(true);
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void =>
    this.isDesktop.set(event.matches);

  // Local draft for the search input so typing does not reload on every
  // keystroke; the facade reloads after the debounced emission. Seeded from the
  // active (URL) search so a shared/restored link shows its term in the box.
  protected readonly searchDraft = signal('');

  private readonly table = viewChild(UniqueWordsTableComponent);
  private readonly pendingScrollPage = signal<number | null>(null);

  protected readonly modeLabel = computed(() => UNIQUE_WORD_KIND_LABELS[this.facade.mode()]);

  constructor() {
    // Reseed the search box from the active search whenever the mode changes
    // (including the initial load or a browser back/forward restore). `search`
    // is read untracked so a user's in-progress typing is never overwritten by
    // the debounce-driven query-param sync.
    effect(() => {
      this.facade.mode();
      this.facade.search();
      this.searchDraft.set(untracked(() => this.facade.search()));
    });

    effect(() => {
      const targetPage = this.pendingScrollPage();
      if (targetPage === null) {
        return;
      }

      const state = this.listState();
      const table = this.table();
      if (!table || state.isLoadingMore || state.page !== targetPage) {
        return;
      }

      const requiredRows = (targetPage - 1) * state.pageSize;
      if (state.items.length <= requiredRows && state.totalCount > requiredRows) {
        return;
      }

      untracked(() => {
        table.scrollToPage(targetPage, state.pageSize);
        this.pendingScrollPage.set(null);
      });
    });
  }

  ngOnInit(): void {
    this.facade.bindToRoute(this.route);

    this.searchSub = this.searchInput
      .pipe(debounceTime(300))
      .subscribe((value) => this.updateQueryParams({ search: value || null, page: null }));

    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
      this.desktopQuery = window.matchMedia('(min-width: 1024px)');
      this.isDesktop.set(this.desktopQuery.matches);
      this.desktopQuery.addEventListener('change', this.onDesktopChange);
    }
  }

  ngOnDestroy(): void {
    this.facade.unbindFromRoute();
    this.searchSub?.unsubscribe();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
  }

  protected onSearchChange(value: string): void {
    this.searchDraft.set(value);
    this.searchInput.next(value);
  }

  protected onSortChange(sort: UniqueWordSort): void {
    this.updateQueryParams({ sort, page: null });
  }

  protected onTabActivated(mode: UniqueWordKind): void {
    // Mode is a route segment; navigate to the mode route and reset list page.
    // Modal params are cleared too: a mode switch is a fresh exploration.
    void this.router.navigate([`/dashboard/words/unique/${mode}`], {
      queryParams: { search: null, sort: null, page: null, word: null, view: null, ap: null },
      queryParamsHandling: 'merge',
    });
  }

  protected onRowSelected(word: UniqueWordListItemViewModel): void {
    this.facade.openDrilldown(word, 'surahs');
    this.updateQueryParams(buildUniqueWordsQueryParams({ wordId: word.id, view: 'surahs', ayahPage: null }));
  }

  protected onDrilldownOpen(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    // Open in memory immediately, then reflect modal state to the URL so the
    // link is shareable. The facade's restore guard ignores the re-emit for the
    // same word.
    this.facade.openDrilldown(word, view);
    this.updateQueryParams(buildUniqueWordsQueryParams({ wordId: word.id, view, ayahPage: null }));
  }

  protected onDrilldownClose(): void {
    // Close in memory and clear only the modal params, preserving list context.
    this.facade.closeDrilldown();
    this.updateQueryParams(buildModalCloseQueryParams());
  }

  protected onDrilldownViewChange(view: WordDrilldownView): void {
    this.facade.setDrilldownView(view);
    this.updateQueryParams(buildUniqueWordsQueryParams({ view }));
  }

  protected onAyahPageChange(page: number): void {
    this.facade.setAyahPage(page);
    this.updateQueryParams(buildUniqueWordsQueryParams({ ayahPage: page }));
  }

  protected onLoadMoreRequested(): void {
    const nextPage = this.listState().page + 1;
    this.updateQueryParams(buildUniqueWordsQueryParams({ page: nextPage }));
  }

  protected onPaginationPageChange(page: number): void {
    if (page === this.listState().page) {
      return;
    }

    this.pendingScrollPage.set(page);
    this.updateQueryParams(buildUniqueWordsQueryParams({ page }));
  }

  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: changes,
      queryParamsHandling: 'merge',
    });
  }
}
