import { Injectable, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, Subscription, combineLatest, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import { EMPTY_LIST_LABEL } from '../models/unique-words.labels';
import {
  DEFAULT_LIST_PAGE,
  DEFAULT_LIST_PAGE_SIZE,
  DEFAULT_UNIQUE_WORD_KIND,
  DEFAULT_UNIQUE_WORD_SORT,
  EMPTY_UNIQUE_WORDS_ASSOCIATION,
  LoadStatus,
  PagedResultDto,
  UniqueWordKind,
  UniqueWordListItemDto,
  UNIQUE_WORDS_RANGE_METRICS,
  UniqueWordListItemViewModel,
  UniqueWordSort,
  UniqueWordsAssociation,
  UniqueWordsListState,
  WordDrilldownView,
} from '../models/unique-words.models';
import { mapUniqueWordListItems } from '../utils/unique-words-display.mapper';
import { extractDrilldownMessage } from '../utils/unique-words-drilldown.state';
import { parseUniqueWordsQueryParams } from './unique-words-url-sync';
import { UniqueWordsCache, UniqueWordsCacheKeys } from './unique-words-cache';
import { UniqueWordsDrilldownFacade } from './unique-words-drilldown.facade';
import { EMPTY_RANGE_FILTERS, RangeFilters, serializeRangeFiltersKey } from './words-range-filters';
import { serializeAssociationKey } from './words-association-filters';

const CONNECTION_ERROR_MESSAGE = 'تعذّر تحميل الكلمات الفريدة. تحقّق من الاتصال ثم أعد المحاولة.';

@Injectable({ providedIn: 'root' })
export class UniqueWordsFacade {
  private readonly api = inject(UniqueWordsApi);
  private readonly cache = inject(UniqueWordsCache);
  private readonly drilldown = inject(UniqueWordsDrilldownFacade);

  private readonly _status = signal<LoadStatus>('idle');
  private readonly _items = signal<readonly UniqueWordListItemViewModel[]>([]);
  private readonly _loadedPage = signal<number>(0);
  private readonly _page = signal<number>(DEFAULT_LIST_PAGE);
  private readonly _totalCount = signal<number>(0);
  private readonly _mode = signal<UniqueWordKind>(DEFAULT_UNIQUE_WORD_KIND);
  private readonly _search = signal<string>('');
  private readonly _sort = signal<UniqueWordSort>(DEFAULT_UNIQUE_WORD_SORT);
  private readonly _ranges = signal<RangeFilters>(EMPTY_RANGE_FILTERS);
  private readonly _association = signal<UniqueWordsAssociation>(EMPTY_UNIQUE_WORDS_ASSOCIATION);
  private readonly _errorMessage = signal<string>('');

  private get _pageSize(): number {
    return DEFAULT_LIST_PAGE_SIZE;
  }
  private routeSub?: Subscription;
  private manualLoadSub?: Subscription;
  private lastFilterKey = '';

  readonly listState = computed<UniqueWordsListState>(() => ({
    status: this._status(),
    items: this._items(),
    page: this._page(),
    pageSize: this._pageSize,
    totalCount: this._totalCount(),
    mode: this._mode(),
    search: this._search(),
    sort: this._sort(),
    errorMessage: this._errorMessage(),
  }));

  readonly drilldownState = this.drilldown.drilldownState;

  readonly status = this._status.asReadonly();
  readonly items = this._items.asReadonly();
  readonly mode = this._mode.asReadonly();
  readonly search = this._search.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly page = this._page.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly association = this._association.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = combineLatest([route.paramMap, route.queryParamMap])
      .pipe(
        tap(([params, queryParams]) => this.applyRouteState(params, queryParams)),

        map(() => this.listRequestKey()),
        distinctUntilChanged(),
        switchMap(() => this.runListRequest()),
      )
      .subscribe();
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
    // The route-driven list request is cancelled by the switchMap above, but a manual one
    // (retry / explicit reload) has no such guard: its late response would write list state
    // belonging to whatever binding replaced this one.
    this.manualLoadSub?.unsubscribe();
    this.manualLoadSub = undefined;
    // Perf finding F3: the drilldown facade is a root singleton whose summary/detail HTTP
    // subscriptions must not keep running (and mutating offscreen state) after this page unbinds.
    // Cancelling here preserves the URL selection needed to reload cleanly on return.
    this.drilldown.cancelPendingWork();
  }

  loadList(): void {
    this.manualLoadSub?.unsubscribe();
    this.manualLoadSub = this.runListRequest().subscribe();
  }

  openDrilldown(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    this.drilldown.openDrilldown(word, view);
  }

  setDrilldownView(view: WordDrilldownView): void {
    this.drilldown.setDrilldownView(view);
  }

  setAyahPage(page: number): void {
    this.drilldown.setAyahPage(page);
  }

  setAyahTypeCode(typeCode: string | null): void {
    this.drilldown.setAyahTypeCode(typeCode);
  }

  closeDrilldown(): void {
    this.drilldown.closeDrilldown();
  }

  retryDrilldown(): void {
    this.drilldown.retryCurrentIdentity();
  }

  private applyRouteState(params: ParamMap, queryParams: ParamMap): void {
    const modeParam = params.get('mode');
    const nextMode = modeParam === 'simple' || modeParam === 'tashkeel' ? modeParam : DEFAULT_UNIQUE_WORD_KIND;

    const parsed = parseUniqueWordsQueryParams(queryParams);
    const rangesKey = serializeRangeFiltersKey(parsed.ranges, UNIQUE_WORDS_RANGE_METRICS);
    const associationKey = this.associationKeyFor(parsed.association);
    const nextFilterKey = [nextMode, parsed.search, parsed.sort, rangesKey, associationKey].join('|');

    if (this.lastFilterKey !== nextFilterKey) {
      this.lastFilterKey = nextFilterKey;
      this.resetAccumulatedList();
    }

    this._mode.set(nextMode);
    this._search.set(parsed.search);
    this._sort.set(parsed.sort);
    this._ranges.set(parsed.ranges);
    this._association.set(parsed.association);
    this._page.set(parsed.page);

    this.drilldown.restoreFromUrl(nextMode, parsed.wordId, parsed.view, parsed.ayahPage, parsed.typeCode);
  }

  private listRequestKey(): string {
    return [this._mode(), this._search(), this._sort(), this.rangesKey(), this.associationKey(), this._page()].join('|');
  }

  private rangesKey(): string {
    return serializeRangeFiltersKey(this._ranges(), UNIQUE_WORDS_RANGE_METRICS);
  }

  private associationKey(): string {
    return this.associationKeyFor(this._association());
  }

  private associationKeyFor(association: UniqueWordsAssociation): string {
    return serializeAssociationKey([
      ['primaryType', association.primaryType],
      ['rootId', association.rootId],
    ]);
  }

  private runListRequest(): Observable<void> {
    const targetPage = this._page();
    const ranges = this._ranges();
    const association = this._association();
    const cacheKey = UniqueWordsCacheKeys.list(
      this._mode(),
      this._sort(),
      this._search(),
      targetPage,
      this.rangesKey(),
      this.associationKey(),
    );

    this._status.set('loading');
    this._errorMessage.set('');

    return this.cache
      .getOrLoad(cacheKey, () =>
        this.api.getList(this._mode(), this._search(), this._sort(), targetPage, this._pageSize, ranges, association),
      )
      .pipe(
        tap((response) => this.handleListResponse(response)),
        catchError((err) => {
          this.handleListError(err);
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private handleListResponse(response: ApiResponse<PagedResultDto<UniqueWordListItemDto>>): void {
    if (!response.isSuccess || !response.data) {
      this.resetAccumulatedList();
      this._status.set('error');
      this._errorMessage.set(response.message ?? EMPTY_LIST_LABEL);
      return;
    }

    const data = response.data;
    const nextRows = mapUniqueWordListItems(data.items);

    this._items.set(nextRows);
    this._loadedPage.set(data.page);
    this._totalCount.set(data.totalCount);
    this._status.set(data.totalCount === 0 ? 'empty' : 'success');
    this._errorMessage.set('');
  }

  private handleListError(err: unknown): void {
    this.resetAccumulatedList();
    this._status.set('error');
    this._errorMessage.set(extractDrilldownMessage(err, CONNECTION_ERROR_MESSAGE));
  }

  private resetAccumulatedList(): void {
    this._items.set([]);
    this._loadedPage.set(0);
    this._totalCount.set(0);
    this._status.set('idle');
    this._errorMessage.set('');
  }
}
