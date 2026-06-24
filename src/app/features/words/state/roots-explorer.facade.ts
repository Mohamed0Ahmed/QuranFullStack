import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, of, Subscription, combineLatest } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { RootsApi } from '../data-access/roots.api';
import {
  DEFAULT_ROOTS_LIST_PAGE,
  DEFAULT_ROOT_SORT,
  ROOTS_LIST_PAGE_SIZE,
  LoadStatus,
  PagedResultDto,
  RootListItemDto,
  RootListItemViewModel,
  RootSort,
  RootsListState,
} from '../models/roots.models';
import { ROOTS_LIST_ERROR_LABEL } from '../models/roots.labels';
import { parseRootsQueryParams } from './roots-url-sync';
import { RootsCache, RootsCacheKeys } from './roots-cache';

const CONNECTION_ERROR_MESSAGE = ROOTS_LIST_ERROR_LABEL;

function toRootListItemViewModel(item: RootListItemDto): RootListItemViewModel {
  return { ...item, displayText: item.rootText };
}

/**
 * Roots Explorer (Feature 015) list-state facade (US1, T028). Owns the roots
 * list state signals, loads the list via `RootsApi` + the shared `RootsCache`,
 * maps `ApiResponse` → page-ready state, exposes search/sort/page actions, and
 * reflects list state in the URL. The persistent detail/panel surface is a
 * separate facade; this one does NOT load any detail endpoint (no detail API
 * calls fire on table render).
 *
 * Modeled on `UniqueWordsFacade`, but the detail surface is a persistent
 * panel, not a modal here.
 */
@Injectable({ providedIn: 'root' })
export class RootsExplorerFacade {
  private readonly api = inject(RootsApi);
  private readonly cache = inject(RootsCache);

  private readonly _status = signal<LoadStatus>('idle');
  private readonly _items = signal<readonly RootListItemViewModel[]>([]);
  private readonly _page = signal<number>(DEFAULT_ROOTS_LIST_PAGE);
  private readonly _totalCount = signal<number>(0);
  private readonly _search = signal<string>('');
  private readonly _sort = signal<RootSort>(DEFAULT_ROOT_SORT);
  private readonly _errorMessage = signal<string>('');

  // Read the fixed page size via a getter rather than a class-field initializer
  // so the experimental @angular/build:unit-test SSR runner resolves the
  // cross-module const correctly (mirrors the UniqueWordsFacade workaround).
  private get pageSize(): number {
    return ROOTS_LIST_PAGE_SIZE;
  }

  private routeSub?: Subscription;
  private manualLoadSub?: Subscription;

  readonly listState = computed<RootsListState>(() => ({
    status: this._status(),
    items: this._items(),
    page: this._page(),
    pageSize: this.pageSize,
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

  /** Binds list state to the route; reloads only when list-relevant inputs change. */
  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = combineLatest([route.paramMap, route.queryParamMap])
      .pipe(
        tap(([, queryParams]) => this.applyRouteState(queryParams)),
        // Reload the list only when a list-relevant input changes. Selection/
        // panel params (root/view/wordView/surahView/detailPage) must NOT re-run
        // the list query or flash its loading state behind the panel.
        map(() => this.listRequestKey()),
        distinctUntilChanged(),
        switchMap(() => this.runListRequest()),
      )
      .subscribe();
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
  }

  /** Imperative list reload. */
  loadList(): void {
    this.manualLoadSub?.unsubscribe();
    this.manualLoadSub = this.runListRequest().subscribe();
  }

  private applyRouteState(queryParams: ParamMap): void {
    const parsed = parseRootsQueryParams(queryParams);
    this._search.set(parsed.search);
    this._sort.set(parsed.sort);
    this._page.set(parsed.page);
    // The detail facade (US2+) owns selection/panel restore from the same URL.
  }

  private listRequestKey(): string {
    return [this._search(), this._sort(), this._page()].join('|');
  }

  private runListRequest(): Observable<void> {
    const targetPage = this._page();
    const cacheKey = RootsCacheKeys.list(this._search(), this._sort(), targetPage);

    this._status.set('loading');
    this._errorMessage.set('');

    return this.cache
      .getOrLoad(cacheKey, () =>
        this.api.getRootsList(this._search(), this._sort(), targetPage, this.pageSize),
      )
      .pipe(
        tap((response) => this.handleListResponse(response)),
        catchError(() => {
          this.handleListError();
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private handleListResponse(response: ApiResponse<PagedResultDto<RootListItemDto>>): void {
    if (!response.isSuccess || !response.data) {
      this._items.set([]);
      this._totalCount.set(0);
      this._status.set('error');
      this._errorMessage.set(response.message ?? CONNECTION_ERROR_MESSAGE);
      return;
    }

    const data = response.data;
    this._items.set(data.items.map(toRootListItemViewModel));
    this._totalCount.set(data.totalCount);

    // 'empty' = no results at all (e.g. an unmatched search); a successful page
    // is distinct from empty so the UI renders the right state.
    this._status.set(data.totalCount === 0 ? 'empty' : 'success');
    this._errorMessage.set('');
  }

  private handleListError(): void {
    this._items.set([]);
    this._totalCount.set(0);
    this._status.set('error');
    this._errorMessage.set(CONNECTION_ERROR_MESSAGE);
  }
}
