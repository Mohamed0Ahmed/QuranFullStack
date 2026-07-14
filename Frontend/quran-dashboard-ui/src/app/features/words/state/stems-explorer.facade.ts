import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, of, Subscription, combineLatest } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { StemsApi } from '../data-access/stems.api';
import {
  DEFAULT_STEMS_LIST_PAGE,
  DEFAULT_STEM_SORT,
  LoadStatus,
  PagedResultDto,
  StemListItemDto,
  StemListItemViewModel,
  StemSort,
  StemsListState,
  STEMS_LIST_PAGE_SIZE,
} from '../models/stems.models';
import { STEMS_RANGE_METRICS } from '../models/stems.models';
import { STEMS_LIST_ERROR_LABEL } from '../models/stems.labels';
import { parseStemsQueryParams } from './stems-url-sync';
import { StemsCache, StemsCacheKeys } from './stems-cache';
import { EMPTY_RANGE_FILTERS, RangeFilters, serializeRangeFiltersKey } from './words-range-filters';

const CONNECTION_ERROR_MESSAGE = STEMS_LIST_ERROR_LABEL;

function toStemListItemViewModel(item: StemListItemDto): StemListItemViewModel {
  return { ...item, displayText: item.stemText };
}

@Injectable({ providedIn: 'root' })
export class StemsExplorerFacade {
  private readonly api = inject(StemsApi);
  private readonly cache = inject(StemsCache);

  private readonly _status = signal<LoadStatus>('idle');
  private readonly _items = signal<readonly StemListItemViewModel[]>([]);
  private readonly _page = signal<number>(DEFAULT_STEMS_LIST_PAGE);
  private readonly _totalCount = signal<number>(0);
  private readonly _ranges = signal<RangeFilters>(EMPTY_RANGE_FILTERS);
  private readonly _search = signal<string>('');
  private readonly _sort = signal<StemSort>(DEFAULT_STEM_SORT);
  private readonly _errorMessage = signal<string>('');

  private get pageSize(): number {
    return STEMS_LIST_PAGE_SIZE;
  }

  private routeSub?: Subscription;
  private manualLoadSub?: Subscription;

  readonly listState = computed<StemsListState>(() => ({
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
    const parsed = parseStemsQueryParams(queryParams);
    this._search.set(parsed.search);
    this._sort.set(parsed.sort);
    this._ranges.set(parsed.ranges);
    this._page.set(parsed.page);
  }

  private listRequestKey(): string {
    return [this._search(), this._sort(), this.rangesKey(), this._page()].join('|');
  }

  private rangesKey(): string {
    return serializeRangeFiltersKey(this._ranges(), STEMS_RANGE_METRICS);
  }

  private runListRequest(): Observable<void> {
    const targetPage = this._page();
    const ranges = this._ranges();
    const cacheKey = StemsCacheKeys.list(this._search(), this._sort(), targetPage, this.rangesKey());

    this._status.set('loading');
    this._errorMessage.set('');

    return this.cache
      .getOrLoad(cacheKey, () => this.api.getStemsList(this._search(), this._sort(), targetPage, this.pageSize, ranges))
      .pipe(
        tap((response) => this.handleListResponse(response)),
        catchError(() => {
          this.handleListError();
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private handleListResponse(response: ApiResponse<PagedResultDto<StemListItemDto>>): void {
    if (!response.isSuccess || !response.data) {
      this._items.set([]);
      this._totalCount.set(0);
      this._status.set('error');
      this._errorMessage.set(response.message ?? CONNECTION_ERROR_MESSAGE);
      return;
    }

    const data = response.data;
    this._items.set(data.items.map(toStemListItemViewModel));
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
