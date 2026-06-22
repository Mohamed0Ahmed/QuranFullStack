import { Injectable, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subscription, combineLatest, of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import { EMPTY_LIST_LABEL } from '../models/unique-words.labels';
import {
  DEFAULT_LIST_PAGE,
  DEFAULT_LIST_PAGE_SIZE,
  DEFAULT_UNIQUE_WORD_KIND,
  DEFAULT_UNIQUE_WORD_SORT,
  LoadStatus,
  PagedResultDto,
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordSort,
  UniqueWordsListState,
  isUniqueWordKind,
  isUniqueWordSort,
} from '../models/unique-words.models';

/**
 * Arabic transport-failure message. Kept local because it is feature-specific
 * connection copy, mirroring the Mushaf facade's connection message.
 */
const CONNECTION_ERROR_MESSAGE = 'تعذّر تحميل الكلمات الفريدة. تحقّق من الاتصال ثم أعد المحاولة.';

/**
 * Facade for the Unique Words explorer list (US2). Owns list state
 * (mode/search/sort/page/loading/empty/error) and `ApiResponse<T>` unwrapping.
 * Child components consume `listState()` and never call the API directly.
 *
 * Modal drill-down state (`WordDrilldownState`) and full URL restore
 * (`word`/`view`/`ap` query params) are added in US3/US4.
 */
@Injectable({ providedIn: 'root' })
export class UniqueWordsFacade {
  private readonly api = inject(UniqueWordsApi);

  private readonly _status = signal<LoadStatus>('idle');
  private readonly _items = signal<readonly UniqueWordListItemDto[]>([]);
  private readonly _page = signal<number>(DEFAULT_LIST_PAGE);
  private readonly _totalCount = signal<number>(0);
  private readonly _mode = signal<UniqueWordKind>(DEFAULT_UNIQUE_WORD_KIND);
  private readonly _search = signal<string>('');
  private readonly _sort = signal<UniqueWordSort>(DEFAULT_UNIQUE_WORD_SORT);
  private readonly _errorMessage = signal<string>('');

  private readonly _pageSize = DEFAULT_LIST_PAGE_SIZE;
  private routeSub?: Subscription;

  /** Page-ready list state consumed by the explorer page and its children. */
  readonly listState = computed<UniqueWordsListState>(() => ({
    status: this._status(),
    items: [...this._items()],
    page: this._page(),
    pageSize: this._pageSize,
    totalCount: this._totalCount(),
    mode: this._mode(),
    search: this._search(),
    sort: this._sort(),
    errorMessage: this._errorMessage(),
  }));

  readonly status = this._status.asReadonly();
  readonly items = this._items.asReadonly();
  readonly mode = this._mode.asReadonly();
  readonly search = this._search.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly page = this._page.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();

  private manualLoadSub?: Subscription;

  /**
   * Binds list state to the active route and reloads on any change. The `:mode`
   * path segment drives `_mode`; the `search`/`sort`/`page` query params drive
   * the rest. Both sources are combined so a pure mode switch (with unchanged
   * query params) still reloads, and `switchMap` supersedes an in-flight
   * request when the route changes again. Called once on page init.
   */
  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = combineLatest([route.paramMap, route.queryParamMap])
      .pipe(
        tap(([params, queryParams]) => this.applyRouteState(params, queryParams)),
        switchMap(() => this.runListRequest()),
      )
      .subscribe();
  }

  /** Stops listening to route changes (called on page destroy). */
  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
  }

  /** Triggers a list reload for the current mode/search/sort/page. */
  loadList(): void {
    this.manualLoadSub?.unsubscribe();
    this.manualLoadSub = this.runListRequest().subscribe();
  }

  /** Reads mode from the path segment and search/sort/page from query params. */
  private applyRouteState(params: ParamMap, queryParams: ParamMap): void {
    const modeParam = params.get('mode');
    this._mode.set(isUniqueWordKind(modeParam) ? modeParam : DEFAULT_UNIQUE_WORD_KIND);

    const sortParam = queryParams.get('sort');
    const pageParam = Number.parseInt(queryParams.get('page') ?? '', 10);

    this._search.set(queryParams.get('search') ?? '');
    this._sort.set(isUniqueWordSort(sortParam) ? sortParam : DEFAULT_UNIQUE_WORD_SORT);
    this._page.set(Number.isFinite(pageParam) && pageParam >= 1 ? pageParam : DEFAULT_LIST_PAGE);
  }

  /**
   * One list read for the current state. Sets loading, then maps the response
   * (or a transport error) into page-ready state. Errors are caught so the
   * outer route stream never dies on a failed request.
   */
  private runListRequest(): Observable<void> {
    this._status.set('loading');
    this._errorMessage.set('');

    return this.api
      .getList(this._mode(), this._search(), this._sort(), this._page(), this._pageSize)
      .pipe(
        tap((response) => this.handleResponse(response)),
        catchError((err) => {
          this.handleError(err);
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  // --- Mutators used by the page to push state changes back through the URL ---
  // US2 keeps these as thin setters; the page owns URL updates so refresh/share
  // work. US4 adds full restore from URL.

  setMode(mode: UniqueWordKind): void {
    if (mode !== this._mode()) {
      this._mode.set(mode);
      this._page.set(DEFAULT_LIST_PAGE);
      this.loadList();
    }
  }

  setSearch(search: string): void {
    if (search !== this._search()) {
      this._search.set(search);
      this._page.set(DEFAULT_LIST_PAGE);
      this.loadList();
    }
  }

  setSort(sort: UniqueWordSort): void {
    if (sort !== this._sort()) {
      this._sort.set(sort);
      this._page.set(DEFAULT_LIST_PAGE);
      this.loadList();
    }
  }

  setPage(page: number): void {
    if (page !== this._page() && page >= 1) {
      this._page.set(page);
      this.loadList();
    }
  }

  private handleResponse(response: ApiResponse<PagedResultDto<UniqueWordListItemDto>>): void {
    if (response.isSuccess && response.data) {
      this._items.set(response.data.items);
      this._totalCount.set(response.data.totalCount);
      this._status.set(response.data.totalCount === 0 ? 'empty' : 'success');
      this._errorMessage.set('');
      return;
    }

    this._items.set([]);
    this._totalCount.set(0);
    this._status.set('error');
    this._errorMessage.set(response.message ?? EMPTY_LIST_LABEL);
  }

  private handleError(err: unknown): void {
    this._items.set([]);
    this._totalCount.set(0);
    this._status.set('error');

    if (err instanceof HttpErrorResponse) {
      const body = err.error as ApiResponse<unknown> | null | undefined;
      this._errorMessage.set(
        typeof body?.message === 'string' && body.message.length > 0
          ? body.message
          : CONNECTION_ERROR_MESSAGE,
      );
      return;
    }

    this._errorMessage.set(CONNECTION_ERROR_MESSAGE);
  }
}
