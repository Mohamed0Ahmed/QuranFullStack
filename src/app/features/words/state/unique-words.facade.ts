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
  LoadStatus,
  PagedResultDto,
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordListItemViewModel,
  UniqueWordSort,
  UniqueWordsListState,
  WordDrilldownView,
} from '../models/unique-words.models';
import { mapUniqueWordListItems } from '../utils/unique-words-display.mapper';
import { mergeUniqueWordListItems } from '../utils/unique-words-state.helpers';
import { extractDrilldownMessage } from '../utils/unique-words-drilldown.state';
import { parseUniqueWordsQueryParams } from './unique-words-url-sync';
import { UniqueWordsDrilldownFacade } from './unique-words-drilldown.facade';

const CONNECTION_ERROR_MESSAGE = 'تعذّر تحميل الكلمات الفريدة. تحقّق من الاتصال ثم أعد المحاولة.';

@Injectable({ providedIn: 'root' })
export class UniqueWordsFacade {
  private readonly api = inject(UniqueWordsApi);
  private readonly drilldown = inject(UniqueWordsDrilldownFacade);

  private readonly _status = signal<LoadStatus>('idle');
  private readonly _items = signal<readonly UniqueWordListItemViewModel[]>([]);
  private readonly _isLoadingMore = signal<boolean>(false);
  private readonly _loadedPage = signal<number>(0);
  private readonly _page = signal<number>(DEFAULT_LIST_PAGE);
  private readonly _totalCount = signal<number>(0);
  private readonly _mode = signal<UniqueWordKind>(DEFAULT_UNIQUE_WORD_KIND);
  private readonly _search = signal<string>('');
  private readonly _sort = signal<UniqueWordSort>(DEFAULT_UNIQUE_WORD_SORT);
  private readonly _errorMessage = signal<string>('');

  // Read the default page size on access rather than caching it in a field
  // initializer. The experimental @angular/build:unit-test SSR runner resolves
  // a class field initializer's cross-module const read to `undefined` under
  // the multi-entry test build (its export getter swallows the temporal-dead-
  // zone access); a getter defers the read past module init, matching how the
  // other defaults are read. Behavior is identical (production folds this to 50).
  private get _pageSize(): number {
    return DEFAULT_LIST_PAGE_SIZE;
  }
  private routeSub?: Subscription;
  private manualLoadSub?: Subscription;
  private lastFilterKey = '';

  readonly listState = computed<UniqueWordsListState>(() => ({
    status: this._status(),
    items: [...this._items()],
    isLoadingMore: this._isLoadingMore(),
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
  readonly isLoadingMore = this._isLoadingMore.asReadonly();
  readonly mode = this._mode.asReadonly();
  readonly search = this._search.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly page = this._page.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = combineLatest([route.paramMap, route.queryParamMap])
      .pipe(
        tap(([params, queryParams]) => this.applyRouteState(params, queryParams)),
        // Reload the list only when a list-relevant input changes. Modal-only
        // query params (word/view/ap) are still applied by applyRouteState
        // above, but they must not re-run the list query or flash its loading
        // state behind/around the open modal.
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

  // Drill-down/modal surface — delegated to UniqueWordsDrilldownFacade so the
  // page keeps a single facade entry point while the two state slices stay
  // separated.
  openDrilldown(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    this.drilldown.openDrilldown(word, view);
  }

  setDrilldownView(view: WordDrilldownView): void {
    this.drilldown.setDrilldownView(view);
  }

  setAyahPage(page: number): void {
    this.drilldown.setAyahPage(page);
  }

  closeDrilldown(): void {
    this.drilldown.closeDrilldown();
  }

  private applyRouteState(params: ParamMap, queryParams: ParamMap): void {
    const modeParam = params.get('mode');
    const nextMode = modeParam === 'simple' || modeParam === 'tashkeel' ? modeParam : DEFAULT_UNIQUE_WORD_KIND;

    const parsed = parseUniqueWordsQueryParams(queryParams);
    const nextFilterKey = [nextMode, parsed.search, parsed.sort].join('|');

    if (this.lastFilterKey !== nextFilterKey) {
      this.lastFilterKey = nextFilterKey;
      this.resetAccumulatedList();
    }

    this._mode.set(nextMode);
    this._search.set(parsed.search);
    this._sort.set(parsed.sort);
    this._page.set(parsed.page);

    this.drilldown.restoreFromUrl(nextMode, parsed.wordId, parsed.view, parsed.ayahPage);
  }

  private listRequestKey(): string {
    return [this._mode(), this._search(), this._sort(), this._page()].join('|');
  }

  private runListRequest(): Observable<void> {
    const targetPage = this._page();

    if (this._loadedPage() > targetPage) {
      this.resetAccumulatedList();
    }

    if (this._loadedPage() === targetPage && this._items().length > 0) {
      this._isLoadingMore.set(false);
      return of(undefined);
    }

    this._status.set(this._loadedPage() === 0 ? 'loading' : 'success');
    this._isLoadingMore.set(this._loadedPage() > 0);
    this._errorMessage.set('');

    return this.api
      .getList(this._mode(), this._search(), this._sort(), this._loadedPage() + 1, this._pageSize)
      .pipe(
        tap((response) => this.handleListResponse(response, targetPage)),
        catchError((err) => {
          this.handleListError(err);
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private handleListResponse(
    response: ApiResponse<PagedResultDto<UniqueWordListItemDto>>,
    targetPage: number,
  ): void {
    if (!response.isSuccess || !response.data) {
      this.resetAccumulatedList();
      this._status.set('error');
      this._errorMessage.set(response.message ?? EMPTY_LIST_LABEL);
      return;
    }

    const data = response.data;
    const nextRows = mapUniqueWordListItems(data.items, this._mode());
    const mergedRows = mergeUniqueWordListItems(this._items(), nextRows);

    this._items.set(mergedRows);
    this._loadedPage.set(data.page);
    this._totalCount.set(data.totalCount);
    this._status.set(data.totalCount === 0 ? 'empty' : 'success');
    this._errorMessage.set('');

    if (data.totalCount > mergedRows.length && this._loadedPage() < targetPage) {
      this._isLoadingMore.set(true);
      this.loadList();
      return;
    }

    this._isLoadingMore.set(false);
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
    this._isLoadingMore.set(false);
  }
}
