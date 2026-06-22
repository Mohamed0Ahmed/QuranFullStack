import { Injectable, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subscription, combineLatest, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import {
  DRILLDOWN_EMPTY_AYAHS_LABEL,
  DRILLDOWN_EMPTY_MISSING_LABEL,
  DRILLDOWN_EMPTY_SURAHS_LABEL,
  DRILLDOWN_ERROR_LABEL,
  EMPTY_LIST_LABEL,
  RESTORED_WORD_LOAD_ERROR_LABEL,
  RESTORED_WORD_NOT_FOUND_LABEL,
} from '../models/unique-words.labels';
import {
  DEFAULT_AYAH_PAGE,
  DEFAULT_AYAH_PAGE_SIZE,
  DEFAULT_LIST_PAGE,
  DEFAULT_LIST_PAGE_SIZE,
  DEFAULT_UNIQUE_WORD_KIND,
  DEFAULT_UNIQUE_WORD_SORT,
  LoadStatus,
  PagedResultDto,
  UniqueWordAyahMatchDto,
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordListItemViewModel,
  UniqueWordSort,
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
  UniqueWordsListState,
  WordDrilldownState,
  WordDrilldownView,
} from '../models/unique-words.models';
import { mapUniqueWordListItems } from '../utils/unique-words-display.mapper';
import { buildMissingSurahsPayload } from '../utils/unique-words-surahs';
import { mergeUniqueWordListItems, toUniqueWordSummary } from '../utils/unique-words-state.helpers';
import {
  buildAyahsDrilldownUpdate,
  buildDrilldownErrorUpdate,
  buildRestoredWordLoadError,
  buildRestoredWordNotFound,
  buildSurahsDrilldownUpdate,
  extractDrilldownMessage,
} from '../utils/unique-words-drilldown.state';
import { parseUniqueWordsQueryParams } from './unique-words-url-sync';

const CONNECTION_ERROR_MESSAGE = 'تعذّر تحميل الكلمات الفريدة. تحقّق من الاتصال ثم أعد المحاولة.';

interface ModalUrlState {
  readonly wordId: number;
  readonly view: WordDrilldownView;
  readonly ayahPage: number;
}

const INITIAL_DRILLDOWN: WordDrilldownState = {
  isOpen: false,
  selectedWordId: null,
  view: 'surahs',
  summary: null,
  surahs: null,
  missingSurahs: null,
  ayahs: null,
  ayahPage: DEFAULT_AYAH_PAGE,
  status: 'idle',
  errorMessage: '',
};

@Injectable({ providedIn: 'root' })
export class UniqueWordsFacade {
  private readonly api = inject(UniqueWordsApi);

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

  private readonly _drilldown = signal<WordDrilldownState>(INITIAL_DRILLDOWN);

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
  private drilldownSub?: Subscription;
  private summarySub?: Subscription;
  private lastFilterKey = '';

  /**
   * Modal state currently reflected by the URL or an in-app action. This tracks
   * the full modal tuple, not just the word ID, so browser back/forward can
   * restore same-word `view` and `ap` changes.
   */
  private activeModalUrlState: ModalUrlState | null = null;

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

  readonly drilldownState = computed(() => this._drilldown());

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

  openDrilldown(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    const summary = toUniqueWordSummary(word);
    this.activeModalUrlState = {
      wordId: word.id,
      view,
      ayahPage: DEFAULT_AYAH_PAGE,
    };
    this._drilldown.set({
      ...INITIAL_DRILLDOWN,
      isOpen: true,
      selectedWordId: word.id,
      view,
      summary,
      ayahPage: DEFAULT_AYAH_PAGE,
      status: 'loading',
    });
    this.loadDrilldownView(view, word.kind, word.id, DEFAULT_AYAH_PAGE);
  }

  setDrilldownView(view: WordDrilldownView): void {
    const current = this._drilldown();
    if (!current.isOpen || current.selectedWordId === null || current.summary === null) {
      return;
    }

    if (view === current.view) {
      return;
    }

    const nextAyahPage = view === 'ayahs' ? current.ayahPage : DEFAULT_AYAH_PAGE;
    this.activeModalUrlState = {
      wordId: current.selectedWordId,
      view,
      ayahPage: nextAyahPage,
    };
    this._drilldown.update((s) => ({
      ...s,
      view,
      status: 'loading',
      errorMessage: '',
      ayahPage: nextAyahPage,
    }));
    this.loadDrilldownView(view, current.summary.kind, current.selectedWordId, this._drilldown().ayahPage);
  }

  setAyahPage(page: number): void {
    const current = this._drilldown();
    if (!current.isOpen || current.selectedWordId === null || current.summary === null || page < 1) {
      return;
    }

    this.activeModalUrlState = {
      wordId: current.selectedWordId,
      view: 'ayahs',
      ayahPage: page,
    };
    this._drilldown.update((s) => ({ ...s, ayahPage: page, status: 'loading', errorMessage: '' }));
    this.loadDrilldownView('ayahs', current.summary.kind, current.selectedWordId, page);
  }

  closeDrilldown(): void {
    this.summarySub?.unsubscribe();
    this.summarySub = undefined;
    this.drilldownSub?.unsubscribe();
    this.drilldownSub = undefined;
    this.activeModalUrlState = null;
    this._drilldown.set(INITIAL_DRILLDOWN);
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

    this.restoreModalFromUrl(parsed.wordId, parsed.view, parsed.ayahPage);
  }

  private restoreModalFromUrl(
    wordId: number | null,
    view: WordDrilldownView | null,
    ayahPage: number | null,
  ): void {
    // No modal requested.
    if (wordId === null) {
      this.closeDrilldown();
      return;
    }

    const nextState: ModalUrlState = {
      wordId,
      view: view ?? 'surahs',
      ayahPage: view === 'ayahs' ? ayahPage ?? DEFAULT_AYAH_PAGE : DEFAULT_AYAH_PAGE,
    };

    if (this.isSameModalUrlState(this.activeModalUrlState, nextState)) {
      return;
    }

    this.activeModalUrlState = nextState;
    this.restoreOrUpdateModal(nextState);
  }

  private restoreOrUpdateModal(nextState: ModalUrlState): void {
    const current = this._drilldown();
    if (
      current.isOpen &&
      current.selectedWordId === nextState.wordId &&
      current.summary !== null
    ) {
      this._drilldown.update((s) => ({
        ...s,
        view: nextState.view,
        ayahPage: nextState.ayahPage,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadDrilldownView(
        nextState.view,
        current.summary.kind,
        nextState.wordId,
        nextState.ayahPage,
      );
      return;
    }

    this.loadSummaryAndRestore(nextState);
  }

  private loadSummaryAndRestore(nextState: ModalUrlState): void {
    this.summarySub?.unsubscribe();
    this._drilldown.set({
      ...INITIAL_DRILLDOWN,
      isOpen: true,
      selectedWordId: nextState.wordId,
      view: nextState.view,
      ayahPage: nextState.ayahPage,
      status: 'loading',
    });

    this.summarySub = this.api
      .getSummary(this._mode(), nextState.wordId)
      .pipe(
        tap((response) => {
          if (!response.isSuccess || !response.data) {
            this.handleRestoredWordNotFound(response.message ?? '');
            return;
          }
          this.openRestoredDrilldown(response.data, nextState);
        }),
        catchError((err) => {
          if (err instanceof HttpErrorResponse && err.status === 404) {
            const message = this.extractErrorMessage(err, RESTORED_WORD_NOT_FOUND_LABEL);
            this.handleRestoredWordNotFound(message);
            return of(undefined);
          }

          const message = this.extractErrorMessage(err, RESTORED_WORD_LOAD_ERROR_LABEL);
          this.handleRestoredWordLoadError(message);
          return of(undefined);
        }),
      )
      .subscribe();
  }

  private openRestoredDrilldown(
    summary: UniqueWordSummaryDto,
    nextState: ModalUrlState,
  ): void {
    this._drilldown.update((s) => ({
      ...s,
      summary,
      view: nextState.view,
      ayahPage: nextState.ayahPage,
      status: 'loading',
    }));
    this.loadDrilldownView(nextState.view, summary.kind, summary.id, nextState.ayahPage);
  }

  private handleRestoredWordNotFound(message: string): void {
    // Controlled not-found: keep the modal surface closed, surface a not-found
    // status, and keep the list fully usable. `activeModalUrlState` stays set to
    // the attempted state so a lingering bad `word` param is not re-fetched on
    // later list-only navigation. The page renders a controlled Arabic message;
    // no Quranic text is invented.
    this._drilldown.set({ ...INITIAL_DRILLDOWN, ...buildRestoredWordNotFound(message) });
  }

  private handleRestoredWordLoadError(message: string): void {
    // Mirror not-found handling: keep `activeModalUrlState` so the failed
    // restore is not re-attempted on every later list-only navigation.
    this._drilldown.set({ ...INITIAL_DRILLDOWN, ...buildRestoredWordLoadError(message) });
  }

  private isSameModalUrlState(
    current: ModalUrlState | null,
    next: ModalUrlState,
  ): boolean {
    return (
      current !== null &&
      current.wordId === next.wordId &&
      current.view === next.view &&
      current.ayahPage === next.ayahPage
    );
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

  private loadDrilldownView(
    view: WordDrilldownView,
    kind: UniqueWordKind,
    wordId: number,
    ayahPage: number,
  ): void {
    this.drilldownSub?.unsubscribe();

    if (view === 'surahs') {
      this.drilldownSub = this.api
        .getMentionedSurahs(kind, wordId)
        .pipe(
          tap((response) => this.handleSurahsResponse(response)),
          catchError((err) => {
            this.handleDrilldownError(err);
            return of(undefined);
          }),
        )
        .subscribe();
      return;
    }

    if (view === 'missing') {
      const current = this._drilldown();
      if (current.missingSurahs !== null) {
        const missingSurahs = current.missingSurahs;
        this._drilldown.update((s) => ({
          ...s,
          missingSurahs,
          status: missingSurahs.surahs.length === 0 ? 'empty' : 'success',
          errorMessage: '',
        }));
        return;
      }

      if (current.surahs !== null) {
        const surahs = current.surahs;
        this._drilldown.update((s) => ({
          ...s,
          missingSurahs: buildMissingSurahsPayload(surahs),
          status: surahs.surahs.length === 0 ? 'empty' : 'success',
          errorMessage: '',
        }));
        return;
      }

      this.drilldownSub = this.api
        .getMentionedSurahs(kind, wordId)
        .pipe(
          tap((response) => {
            if (!response.isSuccess || !response.data) {
              this._drilldown.update((s) => ({
                ...s,
                status: 'error',
                errorMessage: response.message ?? DRILLDOWN_ERROR_LABEL,
              }));
              return;
            }

            const missingSurahs = buildMissingSurahsPayload(response.data);

            this._drilldown.update((s) => ({
              ...s,
              missingSurahs,
              status: missingSurahs.surahs.length === 0 ? 'empty' : 'success',
              errorMessage: '',
            }));
          }),
          catchError((err) => {
            this.handleDrilldownError(err);
            return of(undefined);
          }),
        )
        .subscribe();
      return;
    }

    this.drilldownSub = this.api
      .getAyahMatches(kind, wordId, ayahPage, DEFAULT_AYAH_PAGE_SIZE)
      .pipe(
        tap((response) => this.handleAyahsResponse(response)),
        catchError((err) => {
          this.handleDrilldownError(err);
          return of(undefined);
        }),
      )
      .subscribe();
  }

  setMode(mode: UniqueWordKind): void {
    if (mode !== this._mode()) {
      this._mode.set(mode);
      this._page.set(DEFAULT_LIST_PAGE);
      this.resetAccumulatedList();
      this.loadList();
    }
  }

  setSearch(search: string): void {
    if (search !== this._search()) {
      this._search.set(search);
      this._page.set(DEFAULT_LIST_PAGE);
      this.resetAccumulatedList();
      this.loadList();
    }
  }

  setSort(sort: UniqueWordSort): void {
    if (sort !== this._sort()) {
      this._sort.set(sort);
      this._page.set(DEFAULT_LIST_PAGE);
      this.resetAccumulatedList();
      this.loadList();
    }
  }

  setPage(page: number): void {
    if (page !== this._page() && page >= 1) {
      this._page.set(page);
      this.loadList();
    }
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
    this._errorMessage.set(this.extractErrorMessage(err, CONNECTION_ERROR_MESSAGE));
  }

  private handleSurahsResponse(response: ApiResponse<UniqueWordSurahsDto>): void {
    this._drilldown.update((s) => ({ ...s, ...buildSurahsDrilldownUpdate(response) }));
  }

  private handleAyahsResponse(response: ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>>): void {
    this._drilldown.update((s) => ({ ...s, ...buildAyahsDrilldownUpdate(response) }));
  }

  private handleDrilldownError(err: unknown): void {
    this._drilldown.update((s) => ({ ...s, ...buildDrilldownErrorUpdate(err, DRILLDOWN_ERROR_LABEL) }));
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    return extractDrilldownMessage(err, fallback);
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
