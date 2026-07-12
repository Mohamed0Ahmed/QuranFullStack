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
  WordTypeRowIdentity,
  WordTypeSort,
  WordTypeTableRowDto,
  WordTypeTableView,
  WordTypeTense,
  WordTypeVoice,
  WordTypeTreeDto,
  WordTypesListState,
} from '../models/word-types.models';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';
import {
  buildWordTypesQueryParams,
  canonicalWordTypesDetailPage,
  clearSelectionForTableView,
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
  sort: DEFAULT_WORD_TYPE_SORT,
  page: DEFAULT_WORD_TYPES_PAGE,
  word: null,
  root: null,
  stem: null,
  lemma: null,
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
  // Tracks which tableView the currently-stored rows belong to, so a tab switch can null the
  // stale rows before the new page arrives without also clearing rows on ordinary filter changes
  // (those intentionally keep prior rows visible while loading).
  private lastRowsTableView: WordTypeTableView | null = null;
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
    this.retrySub?.unsubscribe();
    this.routeSub = undefined;
    this.retrySub = undefined;
    this.route = undefined;
  }

  // Re-issues the current list load after a transport error. Failed reads are never cached, so a
  // retry always re-fetches; the cancellable subscription prevents overlapping retries.
  retryList(): void {
    this.retrySub?.unsubscribe();
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

  selectType(type: WordTypeMainType): void {
    // Switching the main type resets its child and secondary filters. Any existing selection belongs to
    // the old grammatical scope, so it must not remain shareable after this transition. The active
    // tableView survives — only the Words tab returns a grouped view to words (locked decision 2).
    this.navigate(
      {
        ...buildWordTypesQueryParams({
          type,
          childCode: null,
          case: DEFAULT_WORD_TYPE_CASE,
          tense: DEFAULT_WORD_TYPE_TENSE,
          voice: DEFAULT_WORD_TYPE_VOICE,
          page: DEFAULT_WORD_TYPES_PAGE,
        }),
        ...clearWordTypesSelection(),
      },
    );
  }

  // Selecting a child node narrows rows to that subtype, resets the page, and clears any selected
  // row so the detail panel never lingers on a row from a different context. The active tableView is
  // preserved through both narrowing and clearing back to the parent (locked decision 2).
  selectChild(childCode: string | null): void {
    this.navigate({
      ...buildWordTypesQueryParams({
        childCode,
        page: DEFAULT_WORD_TYPES_PAGE,
      }),
      ...clearWordTypesSelection(),
    });
  }

  // Switching the table-view tab narrows the same filtered scope to a different aggregation level.
  // It resets the page and clears any selected row (grouped views have no word-row selection).
  selectTableView(tableView: WordTypeTableView): void {
    this.navigate({
      ...buildWordTypesQueryParams({ tableView, page: DEFAULT_WORD_TYPES_PAGE }),
      ...clearSelectionForTableView(tableView),
    });
  }

  // A secondary filter narrows the rows without crossing type boundaries. It resets the page, clears
  // any selected row (the row may no longer exist under the narrowed context), and reloads rows. It
  // never requests scoped tree counts — E1 counts stay unscoped by design.
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

  changeSort(sort: WordTypeSort): void {
    this.navigate(buildWordTypesQueryParams({ sort, page: DEFAULT_WORD_TYPES_PAGE }));
  }

  changePage(page: number): void {
    this.navigate(buildWordTypesQueryParams({ page }));
  }

  private loadList() {
    const query = this.state().query;
    const tree$ = this.cache.getOrLoad(WordTypesCacheKeys.tree, () => this.api.getTree());

    // Null out rows only when the tableView itself changed: previous-view rows must never paint
    // under the new scope, but ordinary filter changes intentionally keep prior rows visible
    // while the next page loads.
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
            tree: null,
            rows: null,
            errorMessage: WORD_TYPES_ERROR_LABEL,
          }));
          this.lastRowsTableView = null;
          return of(undefined);
        }),
        map(() => undefined),
      );
    }

    return forkJoin({
      tree: tree$,
      rows: this.cache.getOrLoad(
        WordTypesCacheKeys.table(query, query.tableView, query.sort, query.page),
        () => this.api.getTableRows({ ...query, pageSize: WORD_TYPES_PAGE_SIZE }),
      ),
    }).pipe(
      tap(({ tree, rows }) => this.handleListResponse(tree, rows, query)),
      catchError(() => {
        // Keep any tree already loaded so the table-view strip stays visible after a rows-only
        // transport failure; only the rows and status change.
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

  private handleTreeOnlyResponse(tree: ApiResponse<WordTypeTreeDto>): void {
    const treeData = tree.data;

    if (!tree.isSuccess || !treeData) {
      this.state.update((current) => ({
        ...current,
        status: 'error',
        tree: treeData ?? null,
        rows: null,
        errorMessage: tree.message ?? WORD_TYPES_ERROR_LABEL,
      }));
      return;
    }

    this.state.update((current) => ({
      ...current,
      status: current.rows === null ? 'selectPrompt' : current.rows.totalCount === 0 ? 'empty' : 'success',
      tree: treeData,
      rows: current.rows,
      errorMessage: '',
    }));
  }

  private handleListResponse(
    tree: ApiResponse<WordTypeTreeDto>,
    rows: ApiResponse<PagedResultDto<WordTypeTableRowDto>>,
    query: ParsedWordTypesQuery,
  ): void {
    if (!tree.isSuccess || !tree.data || !rows.isSuccess || !rows.data) {
      this.state.update((current) => ({
        ...current,
        status: 'error',
        tree: tree.data ?? current.tree,
        rows: null,
        errorMessage: rows.message ?? tree.message ?? WORD_TYPES_ERROR_LABEL,
      }));
      this.lastRowsTableView = null;
      return;
    }

    // Preserve the complete discriminated /table response. The page isolates its temporary
    // word-only renderer from this page-ready state until grouped rendering lands in Phase 6.
    const page = rows.data;

    this.state.update((current) => ({
      ...current,
      status: page.totalCount === 0 ? 'empty' : 'success',
      tree: tree.data!,
      rows: page,
      errorMessage: '',
    }));
    this.lastRowsTableView = query.tableView;
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
    return [query.type, query.childCode, query.tableView, query.case, query.tense, query.voice, query.sort, query.page].join('|');
  }
}
