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
import { UniqueWordsTabsComponent } from '../../components/unique-words-tabs/unique-words-tabs.component';
import { UniqueWordsSearchBarComponent } from '../../components/unique-words-search-bar/unique-words-search-bar.component';
import { UniqueWordCardComponent } from '../../components/unique-word-card/unique-word-card.component';
import { WordDrilldownModalComponent } from '../../components/word-drilldown-modal/word-drilldown-modal.component';
import {
  EMPTY_LIST_LABEL,
  LOADING_LABEL,
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
 * Thin explorer shell for the Unique Words list (US2). Reads the active mode
 * from the route segment and list state (`search`/`sort`/`page`) from query
 * params via the facade, and pushes user-driven changes back through query
 * params so the state is refreshable/shareable. All data/state come from the
 * facade; this component never calls the API directly.
 *
 * Modal drill-down wiring and full URL restore are added in US3/US4.
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
    void this.router.navigate([`/dashboard/words/unique/${mode}`], {
      queryParams: { search: null, sort: null, page: null },
      queryParamsHandling: 'merge',
    });
  }

  protected onDrilldownOpen(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    this.facade.openDrilldown(word, view);
  }

  protected onDrilldownClose(): void {
    this.facade.closeDrilldown();
  }

  protected onDrilldownViewChange(view: WordDrilldownView): void {
    this.facade.setDrilldownView(view);
  }

  protected onAyahPageChange(page: number): void {
    this.facade.setAyahPage(page);
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
