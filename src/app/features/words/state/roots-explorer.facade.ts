import { Injectable, computed, signal } from '@angular/core';

import {
  DEFAULT_ROOTS_LIST_PAGE,
  DEFAULT_ROOT_SORT,
  ROOTS_LIST_PAGE_SIZE,
  RootListItemViewModel,
  RootSort,
  RootsListState,
} from '../models/roots.models';

/**
 * Roots Explorer (Feature 015) list-state facade. Foundational skeleton: owns
 * the list state signals and exposes action stubs; the list load/search/sort/
 * page/selection logic lands in User Story 1 (T028). Modeled on
 * `UniqueWordsFacade`, but the detail/panel surface is a separate persistent
 * facade (`RootsDetailFacade`), not a modal here.
 */
@Injectable({ providedIn: 'root' })
export class RootsExplorerFacade {
  private readonly _status = signal<RootsListState['status']>('idle');
  private readonly _items = signal<readonly RootListItemViewModel[]>([]);
  private readonly _page = signal<number>(DEFAULT_ROOTS_LIST_PAGE);
  private readonly _totalCount = signal<number>(0);
  private readonly _search = signal<string>('');
  private readonly _sort = signal<RootSort>(DEFAULT_ROOT_SORT);
  private readonly _errorMessage = signal<string>('');

  // Read the fixed page size via a getter rather than a class-field initializer
  // so the experimental @angular/build:unit-test SSR runner resolves the
  // cross-module const correctly (mirrors the UniqueWordsFacade workaround).
  private get _pageSize(): number {
    return ROOTS_LIST_PAGE_SIZE;
  }

  readonly listState = computed<RootsListState>(() => ({
    status: this._status(),
    items: this._items(),
    page: this._page(),
    pageSize: this._pageSize,
    totalCount: this._totalCount(),
    search: this._search(),
    sort: this._sort(),
    errorMessage: this._errorMessage(),
  }));

  readonly status = this._status.asReadonly();
  readonly items = this._items.asReadonly();
  readonly page = this._page.asReadonly();
  readonly search = this._search.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();

  // --- US1 action stubs (implemented in T028) ---

  /** Binds list state to the route. Implemented in T028. */
  bindToRoute(): void {
    // US1: wire route paramMap + queryParamMap → list load.
  }

  /** Unbinds the route subscription. Implemented in T028. */
  unbindFromRoute(): void {
    // US1: unsubscribe.
  }

  /** Imperative list reload. Implemented in T028. */
  loadList(): void {
    // US1: runListRequest().
  }
}
