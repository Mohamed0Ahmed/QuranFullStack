import { Injectable, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subscription, combineLatest, of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import {
  DRILLDOWN_EMPTY_AYAHS_LABEL,
  DRILLDOWN_EMPTY_MISSING_LABEL,
  DRILLDOWN_EMPTY_SURAHS_LABEL,
  DRILLDOWN_ERROR_LABEL,
  EMPTY_LIST_LABEL,
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
  UniqueWordMissingSurahsDto,
  UniqueWordSort,
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
  UniqueWordsListState,
  WordDrilldownState,
  WordDrilldownView,
} from '../models/unique-words.models';

const CONNECTION_ERROR_MESSAGE = 'تعذّر تحميل الكلمات الفريدة. تحقّق من الاتصال ثم أعد المحاولة.';

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
  private readonly _items = signal<readonly UniqueWordListItemDto[]>([]);
  private readonly _page = signal<number>(DEFAULT_LIST_PAGE);
  private readonly _totalCount = signal<number>(0);
  private readonly _mode = signal<UniqueWordKind>(DEFAULT_UNIQUE_WORD_KIND);
  private readonly _search = signal<string>('');
  private readonly _sort = signal<UniqueWordSort>(DEFAULT_UNIQUE_WORD_SORT);
  private readonly _errorMessage = signal<string>('');

  private readonly _drilldown = signal<WordDrilldownState>(INITIAL_DRILLDOWN);

  private readonly _pageSize = DEFAULT_LIST_PAGE_SIZE;
  private routeSub?: Subscription;
  private manualLoadSub?: Subscription;
  private drilldownSub?: Subscription;

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

  readonly drilldownState = computed(() => this._drilldown());

  readonly status = this._status.asReadonly();
  readonly items = this._items.asReadonly();
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
    const summary = this.toSummary(word);
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

    this._drilldown.update((s) => ({
      ...s,
      view,
      status: 'loading',
      errorMessage: '',
      ayahPage: view === 'ayahs' ? s.ayahPage : DEFAULT_AYAH_PAGE,
    }));
    this.loadDrilldownView(view, current.summary.kind, current.selectedWordId, this._drilldown().ayahPage);
  }

  setAyahPage(page: number): void {
    const current = this._drilldown();
    if (!current.isOpen || current.selectedWordId === null || current.summary === null || page < 1) {
      return;
    }

    this._drilldown.update((s) => ({ ...s, ayahPage: page, status: 'loading', errorMessage: '' }));
    this.loadDrilldownView('ayahs', current.summary.kind, current.selectedWordId, page);
  }

  closeDrilldown(): void {
    this.drilldownSub?.unsubscribe();
    this.drilldownSub = undefined;
    this._drilldown.set(INITIAL_DRILLDOWN);
  }

  private applyRouteState(params: ParamMap, queryParams: ParamMap): void {
    const modeParam = params.get('mode');
    this._mode.set(modeParam === 'simple' || modeParam === 'tashkeel' ? modeParam : DEFAULT_UNIQUE_WORD_KIND);

    const sortParam = queryParams.get('sort');
    const pageParam = Number.parseInt(queryParams.get('page') ?? '', 10);

    this._search.set(queryParams.get('search') ?? '');
    this._sort.set(
      sortParam === 'occurrences' || sortParam === 'alpha' || sortParam === 'mushaf-order'
        ? sortParam
        : DEFAULT_UNIQUE_WORD_SORT,
    );
    this._page.set(Number.isFinite(pageParam) && pageParam >= 1 ? pageParam : DEFAULT_LIST_PAGE);
  }

  private runListRequest(): Observable<void> {
    this._status.set('loading');
    this._errorMessage.set('');

    return this.api
      .getList(this._mode(), this._search(), this._sort(), this._page(), this._pageSize)
      .pipe(
        tap((response) => this.handleListResponse(response)),
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
      this.drilldownSub = this.api
        .getMissingSurahs(kind, wordId)
        .pipe(
          tap((response) => this.handleMissingSurahsResponse(response)),
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

  private handleListResponse(response: ApiResponse<PagedResultDto<UniqueWordListItemDto>>): void {
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

  private handleListError(err: unknown): void {
    this._items.set([]);
    this._totalCount.set(0);
    this._status.set('error');
    this._errorMessage.set(this.extractErrorMessage(err, CONNECTION_ERROR_MESSAGE));
  }

  private handleSurahsResponse(response: ApiResponse<UniqueWordSurahsDto>): void {
    if (!response.isSuccess || !response.data) {
      this._drilldown.update((s) => ({
        ...s,
        status: 'error',
        errorMessage: response.message ?? DRILLDOWN_ERROR_LABEL,
      }));
      return;
    }

    this._drilldown.update((s) => ({
      ...s,
      surahs: response.data!,
      status: response.data!.surahs.length === 0 ? 'empty' : 'success',
      errorMessage: '',
    }));
  }

  private handleMissingSurahsResponse(response: ApiResponse<UniqueWordMissingSurahsDto>): void {
    if (!response.isSuccess || !response.data) {
      this._drilldown.update((s) => ({
        ...s,
        status: 'error',
        errorMessage: response.message ?? DRILLDOWN_ERROR_LABEL,
      }));
      return;
    }

    this._drilldown.update((s) => ({
      ...s,
      missingSurahs: response.data!,
      status: response.data!.surahs.length === 0 ? 'empty' : 'success',
      errorMessage: '',
    }));
  }

  private handleAyahsResponse(response: ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>>): void {
    if (!response.isSuccess || !response.data) {
      this._drilldown.update((s) => ({
        ...s,
        status: 'error',
        errorMessage: response.message ?? DRILLDOWN_ERROR_LABEL,
      }));
      return;
    }

    const data = response.data!;
    this._drilldown.update((s) => ({
      ...s,
      ayahs: data,
      ayahPage: data.page,
      status: data.totalCount === 0 ? 'empty' : 'success',
      errorMessage: '',
    }));
  }

  private handleDrilldownError(err: unknown): void {
    this._drilldown.update((s) => ({
      ...s,
      status: 'error',
      errorMessage: this.extractErrorMessage(err, DRILLDOWN_ERROR_LABEL),
    }));
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as ApiResponse<unknown> | null | undefined;
      return typeof body?.message === 'string' && body.message.length > 0 ? body.message : fallback;
    }
    return fallback;
  }

  private toSummary(word: UniqueWordListItemDto): UniqueWordSummaryDto {
    return {
      id: word.id,
      kind: word.kind,
      displayTextUthmani: word.displayTextUthmani,
      occurrencesCount: word.occurrencesCount,
      ayahsCount: word.ayahsCount,
      surahsCount: word.surahsCount,
      missingSurahsCount: word.missingSurahsCount,
      firstVerseKey: word.firstVerseKey,
      firstLocation: word.firstLocation,
    };
  }
}
