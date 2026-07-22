import { Injectable, Signal, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription, forkJoin, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypesApi } from '../data-access/word-types.api';
import { WORD_TYPES_ERROR_LABEL } from '../models/word-types.labels';
import {
  DEFAULT_WORD_TYPE,
  DEFAULT_WORD_TYPE_CASE,
  DEFAULT_WORD_TYPE_SORT,
  DEFAULT_WORD_TYPE_TABLE_VIEW,
  DEFAULT_WORD_TYPE_TENSE,
  DEFAULT_WORD_TYPE_VOICE,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPES_PAGE,
  ParsedWordTypesQuery,
  PagedResultDto,
  WORD_TYPES_PAGE_SIZE,
  WordTypeCase,
  WordTypeMainType,
  WordTypePresenceDimension,
  WordTypeRowIdentity,
  WordTypeScopeCountsDto,
  WordTypeSort,
  WordTypeTableRowDto,
  WordTypeTableView,
  WordTypeTense,
  WordTypeVoice,
  WordTypeTreeDto,
  WordTypesListState,
  WordTypesScopeCountsState,
} from '../models/word-types.models';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';
import {
  buildWordTypesQueryParams,
  canonicalWordTypesDetailPage,
  clearWordTypesSelection,
  parseWordTypesQueryParams,
} from './word-types-url-sync';

const DEFAULT_QUERY: ParsedWordTypesQuery = {
  type: DEFAULT_WORD_TYPE,
  childCode: null,
  tableView: DEFAULT_WORD_TYPE_TABLE_VIEW,
  case: DEFAULT_WORD_TYPE_CASE,
  tense: DEFAULT_WORD_TYPE_TENSE,
  voice: DEFAULT_WORD_TYPE_VOICE,
  search: null,
  hasRoot: null,
  hasStem: null,
  hasLemma: null,
  sort: DEFAULT_WORD_TYPE_SORT,
  page: DEFAULT_WORD_TYPES_PAGE,
  word: null,
  root: null,
  stem: null,
  lemma: null,
  detailType: null,
  detailChildCode: null,
  detailCase: null,
  detailTense: null,
  detailVoice: null,
  tashkeelWordId: 0,
  contextCode: '',
  view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
  detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE,
  location: null,
  column: null,
};

@Injectable({ providedIn: 'root' })
export class WordTypesExplorerFacade {
  private readonly api = inject(WordTypesApi);
  private readonly cache = inject(WordTypesCache);
  private readonly router = inject(Router);

  private route?: ActivatedRoute;
  private routeSub?: Subscription;
  private retrySub?: Subscription;
  private scopeCountsSub?: Subscription;
  private scopeCountsRetrySub?: Subscription;
  private lastScopeQuery: ParsedWordTypesQuery | null = null;
  private lastRowsTableView: WordTypeTableView | null = null;
  private readonly state = signal<WordTypesListState>({
    status: 'idle',
    tree: null,
    rows: null,
    query: DEFAULT_QUERY,
    errorMessage: '',
  });

  readonly listState: Signal<WordTypesListState> = this.state.asReadonly();

  private readonly scopeCounts = signal<WordTypesScopeCountsState>({ status: 'idle', counts: null });
  readonly scopeCountsState: Signal<WordTypesScopeCountsState> = this.scopeCounts.asReadonly();

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();
    this.route = route;
    this.routeSub = route.queryParamMap.pipe(
      map((params) => parseWordTypesQueryParams(params)),
      tap((query) => this.state.update((current) => ({ ...current, query }))),
      map((query) => this.requestKey(query)),
      distinctUntilChanged(),
      tap(() => this.cancelRetry()),
      switchMap(() => this.loadList()),
    ).subscribe();

    this.scopeCountsSub = route.queryParamMap.pipe(
      map((params) => parseWordTypesQueryParams(params)),
      distinctUntilChanged((a, b) => this.scopeKey(a) === this.scopeKey(b)),
      tap(() => this.cancelScopeCountsRetry()),
      switchMap((query) => this.loadScopeCounts(query)),
    ).subscribe();
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.scopeCountsSub?.unsubscribe();
    this.cancelRetry();
    this.cancelScopeCountsRetry();
    this.routeSub = undefined;
    this.scopeCountsSub = undefined;
    this.route = undefined;
  }

  retryScopeCounts(): void {
    const query = this.lastScopeQuery;
    if (!query) {
      return;
    }
    this.cancelScopeCountsRetry();
    this.scopeCounts.set({ status: 'loading', counts: null });
    this.scopeCountsRetrySub = this.fetchScopeCounts(query).subscribe();
  }

  retryList(): void {
    this.cancelRetry();
    this.retrySub = this.loadList().subscribe();
  }

  selectRow(row: WordTypeRowIdentity | null): void {
    if (!row) {
      this.navigate(clearWordTypesSelection());
      return;
    }

    this.navigate(buildWordTypesQueryParams({
      word: row.tashkeelWordId,
      contextCode: row.contextCode,
      view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
      detailPage: canonicalWordTypesDetailPage(DEFAULT_WORD_TYPES_DETAIL_VIEW, DEFAULT_WORD_TYPES_DETAIL_PAGE),
    }));
  }

  selectScope(type: WordTypeMainType, childCode: string | null): void {
    const typeChanged = type !== this.state().query.type;
    this.navigate({
      ...buildWordTypesQueryParams({
        type,
        childCode,
        ...(typeChanged
          ? {
              case: DEFAULT_WORD_TYPE_CASE,
              tense: DEFAULT_WORD_TYPE_TENSE,
              voice: DEFAULT_WORD_TYPE_VOICE,
            }
          : {}),
        page: DEFAULT_WORD_TYPES_PAGE,
      }),
      ...(typeChanged ? clearWordTypesSelection() : {}),
    });
  }

  selectTableView(tableView: WordTypeTableView): void {
    this.navigate(buildWordTypesQueryParams({ tableView, page: DEFAULT_WORD_TYPES_PAGE }));
  }

  selectCase(caseValue: WordTypeCase): void {
    this.navigate({
      ...buildWordTypesQueryParams({ case: caseValue, page: DEFAULT_WORD_TYPES_PAGE }),
      ...clearWordTypesSelection(),
    });
  }

  selectTense(tense: WordTypeTense): void {
    this.navigate({
      ...buildWordTypesQueryParams({ tense, page: DEFAULT_WORD_TYPES_PAGE }),
      ...clearWordTypesSelection(),
    });
  }

  selectVoice(voice: WordTypeVoice): void {
    this.navigate({
      ...buildWordTypesQueryParams({ voice, page: DEFAULT_WORD_TYPES_PAGE }),
      ...clearWordTypesSelection(),
    });
  }

  selectPresenceFlag(dimension: WordTypePresenceDimension, value: boolean | null): void {
    this.navigate({
      ...buildWordTypesQueryParams({ [presenceKeyFor(dimension)]: value, page: DEFAULT_WORD_TYPES_PAGE }),
      ...clearWordTypesSelection(),
    });
  }

  changeSort(sort: WordTypeSort | null): void {
    this.navigate(buildWordTypesQueryParams({ sort, page: DEFAULT_WORD_TYPES_PAGE }));
  }

  changePage(page: number): void {
    this.navigate(buildWordTypesQueryParams({ page }));
  }

  private loadList() {
    const query = this.state().query;
    const tree$ = this.cache.getOrLoad(WordTypesCacheKeys.tree, () => this.api.getTree());

    const tableViewChanged = this.lastRowsTableView !== null && this.lastRowsTableView !== query.tableView;
    this.state.update((current) => ({
      ...current,
      status: 'loading',
      errorMessage: '',
      ...(tableViewChanged ? { rows: null } : {}),
    }));
    if (tableViewChanged) {
      this.lastRowsTableView = null;
    }

    const leafSelected = query.childCode !== null || query.type === 'inl';

    if (!leafSelected) {
      return tree$.pipe(
        tap((tree) => this.handleTreeOnlyResponse(tree)),
        catchError(() => {
          this.state.update((current) => ({
            ...current,
            status: 'error',
            tree: current.tree,
            rows: null,
            errorMessage: WORD_TYPES_ERROR_LABEL,
          }));
          this.lastRowsTableView = null;
          return of(undefined);
        }),
        map(() => undefined),
      );
    }

    const rows$ = this.cache.getOrLoad(
      WordTypesCacheKeys.table(query, query.tableView, query.sort, query.page),
      () => this.api.getTableRows({ ...query, pageSize: WORD_TYPES_PAGE_SIZE }),
    );

    return forkJoin({
      tree: this.settle(tree$),
      rows: this.settle(rows$),
    }).pipe(
      tap(({ tree, rows }) => this.handleListResponse(tree, rows, query)),
      map(() => undefined),
    );
  }

  private settle<T>(source: Observable<ApiResponse<T>>): Observable<ApiResponse<T> | null> {
    return source.pipe(catchError(() => of(null)));
  }

  private loadScopeCounts(query: ParsedWordTypesQuery): Observable<void> {
    const leafSelected = query.childCode !== null || query.type === 'inl';
    if (!leafSelected) {
      this.lastScopeQuery = null;
      this.scopeCounts.set({ status: 'idle', counts: null });
      return of(undefined);
    }

    this.lastScopeQuery = query;
    this.scopeCounts.update((current) => ({ ...current, status: 'loading' }));
    return this.fetchScopeCounts(query);
  }

  private fetchScopeCounts(query: ParsedWordTypesQuery): Observable<void> {
    return this.cache.getOrLoad(
      WordTypesCacheKeys.scopeCounts(query),
      () => this.api.getScopeCounts(query),
    ).pipe(
      tap((response) => this.handleScopeCountsResponse(response)),
      catchError(() => {
        this.scopeCounts.set({ status: 'error', counts: null });
        return of(undefined);
      }),
      map(() => undefined),
    );
  }

  private handleScopeCountsResponse(response: ApiResponse<WordTypeScopeCountsDto>): void {
    const counts = response.isSuccess ? response.data ?? null : null;
    this.scopeCounts.set(counts ? { status: 'success', counts } : { status: 'error', counts: null });
  }

  private cancelRetry(): void {
    this.retrySub?.unsubscribe();
    this.retrySub = undefined;
  }

  private cancelScopeCountsRetry(): void {
    this.scopeCountsRetrySub?.unsubscribe();
    this.scopeCountsRetrySub = undefined;
  }

  private handleTreeOnlyResponse(tree: ApiResponse<WordTypeTreeDto>): void {
    const treeData = tree.data;

    if (!tree.isSuccess || !treeData) {
      this.state.update((current) => ({
        ...current,
        status: 'error',
        tree: current.tree,
        rows: null,
        errorMessage: tree.message ?? WORD_TYPES_ERROR_LABEL,
      }));
      return;
    }

    this.state.update((current) => ({
      ...current,
      status: 'selectPrompt',
      tree: treeData,
      rows: null,
      errorMessage: '',
    }));
    this.lastRowsTableView = null;
  }

  private handleListResponse(
    tree: ApiResponse<WordTypeTreeDto> | null,
    rows: ApiResponse<PagedResultDto<WordTypeTableRowDto>> | null,
    query: ParsedWordTypesQuery,
  ): void {
    const treeData = tree?.isSuccess ? tree.data ?? null : null;
    const rowsData = rows?.isSuccess ? rows.data ?? null : null;

    if (!treeData || !rowsData) {
      this.state.update((current) => ({
        ...current,
        status: 'error',
        tree: treeData ?? current.tree,
        rows: null,
        errorMessage: this.listErrorMessage(tree, rows),
      }));
      this.lastRowsTableView = null;
      return;
    }

    this.state.update((current) => ({
      ...current,
      status: rowsData.totalCount === 0 ? 'empty' : 'success',
      tree: treeData,
      rows: rowsData,
      errorMessage: '',
    }));
    this.lastRowsTableView = query.tableView;
  }

  private listErrorMessage(
    tree: ApiResponse<WordTypeTreeDto> | null,
    rows: ApiResponse<PagedResultDto<WordTypeTableRowDto>> | null,
  ): string {
    return this.failedResponseMessage(rows) ?? this.failedResponseMessage(tree) ?? WORD_TYPES_ERROR_LABEL;
  }

  private failedResponseMessage<T>(response: ApiResponse<T> | null): string | null {
    if (response?.isSuccess === false) {
      return response.message || WORD_TYPES_ERROR_LABEL;
    }

    return response === null || response.data == null ? WORD_TYPES_ERROR_LABEL : null;
  }

  private navigate(queryParams: Record<string, string | null>): void {
    if (!this.route) {
      return;
    }

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: false,
    });
  }

  private requestKey(query: ParsedWordTypesQuery): string {
    return [query.type, query.childCode, query.tableView, query.case, query.tense, query.voice, query.search, query.hasRoot, query.hasStem, query.hasLemma, query.sort, query.page].join('|');
  }

  private scopeKey(query: ParsedWordTypesQuery): string {
    return [query.type, query.childCode, query.case, query.tense, query.voice, query.search, query.hasRoot, query.hasStem, query.hasLemma].join('|');
  }
}

function presenceKeyFor(dimension: WordTypePresenceDimension): 'hasRoot' | 'hasStem' | 'hasLemma' {
  switch (dimension) {
    case 'root': return 'hasRoot';
    case 'stem': return 'hasStem';
    case 'lemma': return 'hasLemma';
  }
}
