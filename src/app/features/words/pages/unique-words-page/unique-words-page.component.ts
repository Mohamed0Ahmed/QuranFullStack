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
import { UniqueWordCardComponent } from '../../components/unique-word-card/unique-word-card.component';
import { WordDrilldownModalComponent } from '../../components/word-drilldown-modal/word-drilldown-modal.component';
import {
  EMPTY_LIST_LABEL,
  LOADING_LABEL,
  RESTORED_WORD_NOT_FOUND_LABEL,
  UNIQUE_WORD_KIND_LABELS,
} from '../../models/unique-words.labels';
import {
  DEFAULT_LIST_PAGE,
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordSort,
  WordDrilldownView,
} from '../../models/unique-words.models';

/**
 * Thin explorer shell for the Unique Words list (US2) and modal drill-downs
 * (US3), with full URL restore/share (US4). The active mode is read from the
 * `:mode` route segment; list state (`search`/`sort`/`page`) and modal state
 * (`word`/`view`/`ap`) live in query params. The page pushes user-driven changes
 * back through query params so state is refreshable/shareable; the facade owns
 * in-memory state and reads the URL on load to restore it. This component never
 * calls the API directly.
 */
@Component({
  selector: 'qd-unique-words-page',
  standalone: true,
  imports: [
    UniqueWordsTabsComponent,
    UniqueWordsSearchBarComponent,
    UniqueWordCardComponent,
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

  // Local draft for the search input so typing does not reload on every
  // keystroke; the facade reloads after the debounced emission. Seeded from the
  // active (URL) search so a shared/restored link shows its term in the box.
  protected readonly searchDraft = signal('');

  /** Active mode label, derived from the facade's route-driven mode signal. */
  protected readonly modeLabel = computed(() => UNIQUE_WORD_KIND_LABELS[this.facade.mode()]);

  /** Last reachable page based on the total count and page size. */
  protected readonly lastPage = computed(() => {
    const state = this.listState();
    return Math.max(1, Math.ceil(state.totalCount / state.pageSize));
  });

  constructor() {
    // Reseed the search box from the active search whenever the mode changes
    // (including the initial load). `search` is read untracked so a user's
    // in-progress typing — which only changes query params, not the mode — is
    // never overwritten.
    effect(() => {
      this.facade.mode();
      this.searchDraft.set(untracked(() => this.facade.search()));
    });
  }

  ngOnInit(): void {
    this.facade.bindToRoute(this.route);

    this.searchSub = this.searchInput
      .pipe(debounceTime(300))
      .subscribe((value) => this.updateQueryParams({ search: value || null, page: null }));
  }

  ngOnDestroy(): void {
    this.facade.unbindFromRoute();
    this.searchSub?.unsubscribe();
  }

  protected onSearchChange(value: string): void {
    this.searchDraft.set(value);
    this.searchInput.next(value);
  }

  protected onSortChange(sort: UniqueWordSort): void {
    this.updateQueryParams({ sort, page: null });
  }

  protected onPrevPage(): void {
    const current = this.listState().page;
    if (current > DEFAULT_LIST_PAGE) {
      this.updateQueryParams({ page: String(current - 1) });
    }
  }

  protected onNextPage(): void {
    const state = this.listState();
    const lastPage = Math.max(1, Math.ceil(state.totalCount / state.pageSize));
    if (state.page < lastPage) {
      this.updateQueryParams({ page: String(state.page + 1) });
    }
  }

  protected onTabActivated(mode: UniqueWordKind): void {
    // Mode is a route segment; navigate to the mode route and reset list page.
    // Modal params are cleared too: a mode switch is a fresh exploration.
    void this.router.navigate([`/dashboard/words/unique/${mode}`], {
      queryParams: { search: null, sort: null, page: null, word: null, view: null, ap: null },
      queryParamsHandling: 'merge',
    });
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

  /** Replaces the named query params, preserving the others. */
  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: changes,
      queryParamsHandling: 'merge',
    });
  }
}
