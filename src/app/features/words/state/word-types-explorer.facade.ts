import { Injectable, Signal, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, forkJoin, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypesApi } from '../data-access/word-types.api';
import { WORD_TYPES_ERROR_LABEL } from '../models/word-types.labels';
import {
  DEFAULT_WORD_TYPE,
  DEFAULT_WORD_TYPE_CASE,
  DEFAULT_WORD_TYPE_SORT,
  DEFAULT_WORD_TYPE_TENSE,
  DEFAULT_WORD_TYPE_VOICE,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPES_PAGE,
  ParsedWordTypesQuery,
  PagedResultDto,
  WORD_TYPES_PAGE_SIZE,
  WordTypeMainType,
  WordTypeRowIdentity,
  WordTypeSort,
  WordTypeRowDto,
  WordTypeTreeDto,
  WordTypesListState,
} from '../models/word-types.models';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';
import { buildWordTypesQueryParams, clearWordTypesSelection, parseWordTypesQueryParams } from './word-types-url-sync';

const DEFAULT_QUERY: ParsedWordTypesQuery = {
  type: DEFAULT_WORD_TYPE,
  childCode: null,
  case: DEFAULT_WORD_TYPE_CASE,
  tense: DEFAULT_WORD_TYPE_TENSE,
  voice: DEFAULT_WORD_TYPE_VOICE,
  sort: DEFAULT_WORD_TYPE_SORT,
  page: DEFAULT_WORD_TYPES_PAGE,
  word: null,
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
  private readonly state = signal<WordTypesListState>({
    status: 'idle',
    tree: null,
    rows: null,
    query: DEFAULT_QUERY,
    errorMessage: '',
  });

  readonly listState: Signal<WordTypesListState> = this.state.asReadonly();

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();
    this.route = route;
    this.routeSub = route.queryParamMap.pipe(
      map((params) => parseWordTypesQueryParams(params)),
      tap((query) => this.state.update((current) => ({ ...current, query }))),
      map((query) => this.requestKey(query)),
      distinctUntilChanged(),
      switchMap(() => this.loadList()),
    ).subscribe();
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
    this.route = undefined;
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
      detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE,
    }));
  }

  selectType(type: WordTypeMainType): void {
    this.navigate({
      ...buildWordTypesQueryParams({ type, childCode: null, page: DEFAULT_WORD_TYPES_PAGE }),
      ...clearWordTypesSelection(),
    });
  }

  changeSort(sort: WordTypeSort): void {
    this.navigate({
      ...buildWordTypesQueryParams({ sort, page: DEFAULT_WORD_TYPES_PAGE }),
      ...clearWordTypesSelection(),
    });
  }

  changePage(page: number): void {
    this.navigate(buildWordTypesQueryParams({ page }));
  }

  private loadList() {
    const query = this.state().query;
    this.state.update((current) => ({ ...current, status: 'loading', errorMessage: '' }));

    return forkJoin({
      tree: this.cache.getOrLoad(WordTypesCacheKeys.tree, () => this.api.getTree()),
      rows: this.cache.getOrLoad(
        WordTypesCacheKeys.rows(query, query.sort, query.page),
        () => this.api.getRows({ ...query, pageSize: WORD_TYPES_PAGE_SIZE }),
      ),
    }).pipe(
      tap(({ tree, rows }) => this.handleListResponse(tree, rows, query)),
      catchError(() => {
        this.state.update((current) => ({
          ...current,
          status: 'error',
          tree: null,
          rows: null,
          errorMessage: WORD_TYPES_ERROR_LABEL,
        }));
        return of(undefined);
      }),
      map(() => undefined),
    );
  }

  private handleListResponse(
    tree: ApiResponse<WordTypeTreeDto>,
    rows: ApiResponse<PagedResultDto<WordTypeRowDto>>,
    query: ParsedWordTypesQuery,
  ): void {
    if (!tree.isSuccess || !tree.data || !rows.isSuccess || !rows.data) {
      this.state.update((current) => ({
        ...current,
        status: 'error',
        tree: tree.data ?? null,
        rows: null,
        errorMessage: rows.message ?? tree.message ?? WORD_TYPES_ERROR_LABEL,
      }));
      return;
    }

    const page = {
      ...rows.data,
      items: rows.data.items.map((row) => ({
        ...row,
        case: query.case,
        tense: query.tense,
        voice: query.voice,
      })),
    };

    this.state.update((current) => ({
      ...current,
      status: page.totalCount === 0 ? 'empty' : 'success',
      tree: tree.data!,
      rows: page,
      errorMessage: '',
    }));
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
    return [query.type, query.childCode, query.case, query.tense, query.voice, query.sort, query.page].join('|');
  }
}
