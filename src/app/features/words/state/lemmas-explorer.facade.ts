import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, of, Subscription, combineLatest } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LemmasApi } from '../data-access/lemmas.api';
import {
  DEFAULT_LEMMAS_LIST_PAGE,
  DEFAULT_LEMMA_SORT,
  LEMMAS_LIST_PAGE_SIZE,
  LoadStatus,
  LemmaListItemDto,
  LemmaListItemViewModel,
  LemmaSort,
  LemmasListState,
  PagedResultDto,
} from '../models/lemmas.models';
import { LEMMAS_LIST_ERROR_LABEL } from '../models/lemmas.labels';
import { parseLemmasQueryParams } from './lemmas-url-sync';
import { LemmasCache, LemmasCacheKeys } from './lemmas-cache';

const CONNECTION_ERROR_MESSAGE = LEMMAS_LIST_ERROR_LABEL;

function toLemmaListItemViewModel(item: LemmaListItemDto): LemmaListItemViewModel {
  return { ...item, displayText: item.lemmaText };
}

/**
 * Lemmas Explorer catalogue facade (Feature 016, US1). Sibling of
 * `RootsExplorerFacade`. Owns catalogue loading, `ApiResponse` mapping,
 * normalized search, sort/page actions, and the row selection default. The list
 * request cache key never embeds raw search in a retained server key — search,
 * sort, and paging are applied against the bounded whole-summary list. No eager
 * detail calls are issued on catalogue render.
 */
@Injectable({ providedIn: 'root' })
export class LemmasExplorerFacade {
  private readonly api = inject(LemmasApi);
  private readonly cache = inject(LemmasCache);

  private readonly _status = signal<LoadStatus>('idle');
  private readonly _items = signal<readonly LemmaListItemViewModel[]>([]);
  private readonly _page = signal<number>(DEFAULT_LEMMAS_LIST_PAGE);
  private readonly _totalCount = signal<number>(0);
  private readonly _search = signal<string>('');
  private readonly _sort = signal<LemmaSort>(DEFAULT_LEMMA_SORT);
  private readonly _errorMessage = signal<string>('');

  private get pageSize(): number {
    return LEMMAS_LIST_PAGE_SIZE;
  }

  private routeSub?: Subscription;
  private manualLoadSub?: Subscription;

  readonly listState = computed<LemmasListState>(() => ({
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

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = combineLatest([route.paramMap, route.queryParamMap])
      .pipe(
        tap(([, queryParams]) => this.applyRouteState(queryParams)),
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

  loadList(): void {
    this.manualLoadSub?.unsubscribe();
    this.manualLoadSub = this.runListRequest().subscribe();
  }

  private applyRouteState(queryParams: ParamMap): void {
    const parsed = parseLemmasQueryParams(queryParams);
    this._search.set(parsed.search);
    this._sort.set(parsed.sort);
    this._page.set(parsed.page);
  }

  private listRequestKey(): string {
    return [this._search(), this._sort(), this._page()].join('|');
  }

  private runListRequest(): Observable<void> {
    const targetPage = this._page();
    const cacheKey = LemmasCacheKeys.list(this._search(), this._sort(), targetPage);

    this._status.set('loading');
    this._errorMessage.set('');

    return this.cache
      .getOrLoad(cacheKey, () =>
        this.api.getLemmasList(this._search(), this._sort(), targetPage, this.pageSize),
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

  private handleListResponse(response: ApiResponse<PagedResultDto<LemmaListItemDto>>): void {
    if (!response.isSuccess || !response.data) {
      this._items.set([]);
      this._totalCount.set(0);
      this._status.set('error');
      this._errorMessage.set(response.message ?? CONNECTION_ERROR_MESSAGE);
      return;
    }

    const data = response.data;
    this._items.set(data.items.map(toLemmaListItemViewModel));
    this._totalCount.set(data.totalCount);

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
