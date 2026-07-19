import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WORD_TYPES_ERROR_LABEL } from '../models/word-types.labels';
import {
  PagedResultDto,
  RootTableRowDto,
  WORD_TYPES_PAGE_SIZE,
  WordTableRowDto,
  WordTypeMainType,
  WordTypeScopeCountsDto,
  WordTypeTableRowDto,
  WordTypeTreeDto,
} from '../models/word-types.models';
import { WordTypesCache } from './word-types-cache';
import { WordTypesExplorerFacade } from './word-types-explorer.facade';

function wordRow(overrides: Partial<WordTableRowDto> = {}): WordTableRowDto {
  return {
    kind: 'word',
    tashkeelWordId: 191001,
    contextCode: 'INL',
    case: null,
    tense: null,
    voice: null,
    displayText: 'SYNTH_WORD_TEXT',
    typeCode: 'INL',
    typeLabel: { ar: 'SYNTH_TYPE_LABEL' },
    broadLabel: { ar: 'SYNTH_BROAD_LABEL' },
    caseOrFeature: null,
    rootText: null,
    lemmaText: null,
    stemText: null,
    occurrencesCount: 1,
    ayahsCount: 1,
    surahsCount: 1,
    ...overrides,
  };
}

function rootRow(overrides: Partial<RootTableRowDto> = {}): RootTableRowDto {
  return {
    kind: 'root',
    rootId: 190700,
    displayText: 'SYNTH_ROOT_TEXT',
    occurrencesCount: 3,
    ayahsCount: 2,
    surahsCount: 1,
    ...overrides,
  };
}

function okRows(
  items: WordTypeTableRowDto[],
  totalCount = items.length,
  page = 1,
): ApiResponse<PagedResultDto<WordTypeTableRowDto>> {
  return { isSuccess: true, message: 'تم', data: { page, pageSize: WORD_TYPES_PAGE_SIZE, totalCount, items } };
}

function okTree(): ApiResponse<WordTypeTreeDto> {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      mainTypes: [
        {
          code: 'noun', label: { ar: 'SYNTH_NOUN_LABEL' }, count: 2,
          secondaryFilter: { kind: 'case', options: [], voiceOptions: [] },
          children: [
            { code: 'N', childCode: 'N', label: { ar: 'SYNTH_NOUN_CHILD' }, count: 1 },
            { code: 'PN', childCode: 'PN', label: { ar: 'SYNTH_PROPER_CHILD' }, count: 1 },
          ],
        },
        { code: 'verb', label: { ar: 'SYNTH_VERB_LABEL' }, count: 0, secondaryFilter: { kind: 'tense+voice', options: [], voiceOptions: [] }, children: [] },
        { code: 'particle', label: { ar: 'SYNTH_PARTICLE_LABEL' }, count: 0, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
        { code: 'inl', label: { ar: 'SYNTH_INL_LABEL' }, count: 1, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
      ],
    },
  };
}

let activeHttp: HttpTestingController | undefined;

function setup(): { facade: WordTypesExplorerFacade; router: Router; http: HttpTestingController } {
  TestBed.configureTestingModule({
    providers: [
      WordTypesExplorerFacade,
      provideRouter([]),
      provideHttpClient(),
      provideHttpClientTesting(),
    ],
  });

  const facade = TestBed.inject(WordTypesExplorerFacade);
  const router = TestBed.inject(Router);
  const http = TestBed.inject(HttpTestingController);
  activeHttp = http;
  vi.spyOn(router, 'navigate').mockResolvedValue(true);

  return { facade, router, http };
}

function expectTreeRequest(http: HttpTestingController): TestRequest {
  const request = http.expectOne((candidate) =>
    candidate.url.endsWith('/api/words/word-types/tree'),
  );
  expect(request.request.method).toBe('GET');
  expect(request.request.params.keys()).toEqual([]);
  return request;
}

function expectTableRequest(
  http: HttpTestingController,
  expected: {
    type: string;
    childCode?: string | null;
    tableView?: string;
    case?: string | null;
    tense?: string | null;
    voice?: string | null;
    sort?: string;
    page?: number;
  },
): TestRequest {
  const request = http.expectOne((candidate) =>
    candidate.url.endsWith('/api/words/word-types/table'),
  );
  expect(request.request.method).toBe('GET');
  expect(request.request.params.get('type')).toBe(expected.type);
  expect(request.request.params.get('childCode')).toBe(expected.childCode ?? null);
  expect(request.request.params.get('tableView')).toBe(expected.tableView ?? 'words');
  expect(request.request.params.get('case')).toBe(expected.case ?? null);
  expect(request.request.params.get('tense')).toBe(expected.tense ?? null);
  expect(request.request.params.get('voice')).toBe(expected.voice ?? null);
  expect(request.request.params.get('sort')).toBe(expected.sort ?? 'occurrences');
  expect(request.request.params.get('page')).toBe(String(expected.page ?? 1));
  expect(request.request.params.get('pageSize')).toBe(String(WORD_TYPES_PAGE_SIZE));
  return request;
}

function flushLeafList(
  http: HttpTestingController,
  expected: Parameters<typeof expectTableRequest>[1],
  rows: ApiResponse<PagedResultDto<WordTypeTableRowDto>> = okRows([]),
  tree: ApiResponse<WordTypeTreeDto> = okTree(),
): { treeRequest: TestRequest; tableRequest: TestRequest } {
  const treeRequest = expectTreeRequest(http);
  const tableRequest = expectTableRequest(http, expected);
  treeRequest.flush(tree);
  tableRequest.flush(rows);
  return { treeRequest, tableRequest };
}

function flushTreeOnly(
  http: HttpTestingController,
  tree: ApiResponse<WordTypeTreeDto> = okTree(),
): TestRequest {
  const request = expectTreeRequest(http);
  request.flush(tree);
  return request;
}

function failTransport(request: TestRequest): void {
  request.error(new ProgressEvent('error'));
}

function controllableRoute(queryParams: Record<string, string> = {}): {
  route: ActivatedRoute;
  setQueryParams: (next: Record<string, string>) => void;
} {
  const queryParamMap = new BehaviorSubject<ParamMap>(convertToParamMap(queryParams));
  return {
    route: { queryParamMap: queryParamMap.asObservable() } as unknown as ActivatedRoute,
    setQueryParams: (next) => queryParamMap.next(convertToParamMap(next)),
  };
}

describe('WordTypesExplorerFacade — tableView', () => {
  beforeEach(() => getTestBed().resetTestingModule());
  afterEach(() => {
    activeHttp?.verify({ ignoreCancelled: true });
    activeHttp = undefined;
    getTestBed().resetTestingModule();
  });

  it('selectTableView changes only list presentation and preserves the complete detail route state', () => {
    const { facade, router } = setup();
    const route = controllableRoute();
    facade.bindToRoute(route.route);

    facade.selectTableView('roots');

    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: {
        tableView: 'roots',
        page: '1',
      },
      queryParamsHandling: 'merge',
    }));
    facade.unbindFromRoute();
  });

  it('loads through getTableRows with the active tableView, and reloads when tableView changes', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    flushLeafList(http, {
      type: 'inl',
      childCode: null,
      tableView: 'words',
      sort: 'occurrences',
      page: 1,
    }, okRows([wordRow()]));

    route.setQueryParams({ type: 'inl', tableView: 'roots' });

    const groupedRequest = expectTableRequest(http, {
      type: 'inl',
      childCode: null,
      tableView: 'roots',
      sort: 'occurrences',
      page: 1,
    });
    groupedRequest.flush(okRows([rootRow()]));
    facade.unbindFromRoute();
  });

  it('nulls rows after tableView changes and cancels a stale request so only the latest view can win', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    flushLeafList(http, { type: 'inl', tableView: 'words' }, okRows([wordRow()]));
    expect(facade.listState().rows?.items).toHaveLength(1);

    route.setQueryParams({ type: 'inl', tableView: 'roots' });
    expect(facade.listState().rows).toBeNull();
    const staleRequest = expectTableRequest(http, { type: 'inl', tableView: 'roots' });

    route.setQueryParams({ type: 'inl', tableView: 'lemmas' });
    expect(staleRequest.cancelled).toBe(true);
    expect(facade.listState().rows).toBeNull();

    const latestRequest = expectTableRequest(http, { type: 'inl', tableView: 'lemmas' });
    latestRequest.flush(okRows([]));
    expect(facade.listState().query.tableView).toBe('lemmas');
    expect(facade.listState().status).toBe('empty');
    facade.unbindFromRoute();
  });

  it('keeps prior rows visible while a non-tableView filter reloads', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    const initialRow = wordRow();
    flushLeafList(http, { type: 'inl', tableView: 'words' }, okRows([initialRow]));
    expect(facade.listState().rows?.items).toEqual([initialRow]);

    route.setQueryParams({ type: 'noun', childCode: 'PN' });
    expect(facade.listState().status).toBe('loading');
    expect(facade.listState().rows?.items).toEqual([initialRow]);

    expectTableRequest(http, {
      type: 'noun',
      childCode: 'PN',
      tableView: 'words',
    }).flush(okRows([]));
    facade.unbindFromRoute();
  });

  it('preserves nullable word identity fields from the table response', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);
    flushLeafList(http, { type: 'inl', tableView: 'words' }, okRows([wordRow()]));

    const row = facade.listState().rows!.items.find((item): item is WordTableRowDto => item.kind === 'word');
    expect(row?.case).toBeNull();
    expect(row?.tense).toBeNull();
    expect(row?.voice).toBeNull();
    facade.unbindFromRoute();
  });

  it('preserves grouped rows in list state for the active tableView', () => {
    const groupedRow = rootRow();
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);
    flushLeafList(http, {
      type: 'noun',
      childCode: 'PN',
      tableView: 'roots',
    }, okRows([groupedRow]));

    expect(facade.listState().rows?.items).toEqual([groupedRow]);
    expect(facade.listState().rows?.totalCount).toBe(1);
    facade.unbindFromRoute();
  });

  function lastQueryParams(router: Router): Record<string, unknown> {
    return (router.navigate as ReturnType<typeof vi.fn>).mock.calls.at(-1)?.[1].queryParams as Record<string, unknown>;
  }

  function selectScope(facade: WordTypesExplorerFacade, type: WordTypeMainType, childCode: string | null): void {
    facade.selectScope(type, childCode);
  }

  it('clears the active detail selection when committing a cross-parent child', () => {
    const { facade, router } = setup();
    const route = controllableRoute({
      type: 'verb',
      childCode: 'present',
      tableView: 'roots',
      root: '190700',
      detailType: 'verb',
      detailChildCode: 'present',
      detailCase: 'all',
      detailTense: 'present',
      detailVoice: 'all',
      view: 'ayahs',
      detailPage: '2',
    });
    facade.bindToRoute(route.route);

    selectScope(facade, 'noun', 'ADJ');

    const params = lastQueryParams(router);
    expect(params).not.toHaveProperty('tableView');
    // A cross-parent commit resets the secondary filters and page AND clears the stale detail
    // selection (the open root/word detail belonged to the previous main type).
    expect(params).toEqual({
      type: 'noun',
      childCode: 'ADJ',
      case: 'all',
      tense: 'all',
      voice: 'all',
      page: '1',
      word: null,
      contextCode: null,
      root: null,
      stem: null,
      lemma: null,
      detailType: null,
      detailChildCode: null,
      detailCase: null,
      detailTense: null,
      detailVoice: null,
      view: null,
      detailPage: null,
      location: null,
      column: null,
    });
    facade.unbindFromRoute();
  });

  it('preserves compatible secondary filters when committing another child under the same parent', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', case: 'genitive', tableView: 'roots' });
    facade.bindToRoute(route.route);

    selectScope(facade, 'noun', 'ADJ');

    const params = lastQueryParams(router);
    expect(params).not.toHaveProperty('tableView');
    expect(params).toEqual({ type: 'noun', childCode: 'ADJ', page: '1' });
    facade.unbindFromRoute();
  });

  const scopeAndSortActions: ReadonlyArray<[string, (f: WordTypesExplorerFacade) => void]> = [
    ['case', (f) => f.selectCase('genitive')],
    ['tense', (f) => f.selectTense('past')],
    ['voice', (f) => f.selectVoice('passive')],
    ['sort', (f) => f.changeSort('alpha')],
  ];

  it.each(scopeAndSortActions)('keeps the active tableView and resets to page 1 on a %s change', (_name, act) => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);

    act(facade);

    const params = lastQueryParams(router);
    expect(params).not.toHaveProperty('tableView');
    expect(params['page']).toBe('1');
    facade.unbindFromRoute();
  });

  it('selectTableView(words) is the only action that returns a grouped view to words', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);

    facade.selectTableView('words');
    expect(lastQueryParams(router)['tableView']).toBe('words');

    selectScope(facade, 'noun', 'N');
    facade.selectCase('genitive');
    facade.changeSort('alpha');
    facade.changePage(2);

    const otherCalls = (router.navigate as ReturnType<typeof vi.fn>).mock.calls.slice(1);
    for (const call of otherCalls) {
      expect((call[1].queryParams as Record<string, unknown>)['tableView']).not.toBe('words');
    }
    facade.unbindFromRoute();
  });

  it('clears the old scoped selection on scope changes but preserves it on sort and list-page changes', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' });
    facade.bindToRoute(route.route);

    facade.selectCase('genitive');
    expect(lastQueryParams(router)).toEqual(expect.objectContaining({
      word: null, root: null, stem: null, lemma: null, view: null, detailPage: null,
    }));

    facade.changeSort('alpha');
    expect(lastQueryParams(router)).not.toHaveProperty('root');
    expect(lastQueryParams(router)).not.toHaveProperty('word');

    facade.changePage(3);
    expect(lastQueryParams(router)).not.toHaveProperty('root');
    facade.unbindFromRoute();
  });

  it('does not clear a mismatched detail identity when changing table view', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'words', word: '191001', contextCode: 'PN' });
    facade.bindToRoute(route.route);

    facade.selectTableView('roots');

    const params = lastQueryParams(router);
    expect(params).toEqual({ tableView: 'roots', page: '1' });
    facade.unbindFromRoute();
  });

  it('preserves the active tableView and does not request rows for a tree-only parent scope', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'noun', tableView: 'roots' });
    facade.bindToRoute(route.route);

    http.expectNone((candidate) => candidate.url.endsWith('/api/words/word-types/table'));
    flushTreeOnly(http);
    expect(facade.listState().query.tableView).toBe('roots');
    expect(facade.listState().status).toBe('selectPrompt');
    facade.unbindFromRoute();
  });

  it('clears prior leaf rows and enters the subtype prompt when returning to a tree-only parent', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);
    flushLeafList(http, {
      type: 'noun',
      childCode: 'PN',
      tableView: 'roots',
    }, okRows([rootRow()]));

    expect(facade.listState().rows?.items).toEqual([rootRow()]);
    expect(facade.listState().status).toBe('success');

    // Back to the parent (no leaf): the previous scope's rows must not linger under the new scope;
    // the table shows the in-shell subtype prompt while the loaded tree keeps the strip visible.
    route.setQueryParams({ type: 'noun', tableView: 'roots' });

    http.expectNone((candidate) => candidate.url.endsWith('/api/words/word-types/table'));
    expect(facade.listState().rows).toBeNull();
    expect(facade.listState().status).toBe('selectPrompt');
    expect(facade.listState().tree).not.toBeNull();
    facade.unbindFromRoute();
  });

  it('keeps a successfully loaded tree when the initial rows request throws, so the strip survives', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    const treeRequest = expectTreeRequest(http);
    const rowsRequest = expectTableRequest(http, { type: 'inl', tableView: 'words' });
    treeRequest.flush(okTree());
    failTransport(rowsRequest);

    expect(facade.listState().status).toBe('error');
    expect(facade.listState().tree).not.toBeNull();
    expect(facade.listState().rows).toBeNull();
    facade.unbindFromRoute();
  });

  it.each([
    {
      label: 'rows controlled failure',
      failedRequest: 'rows',
      failureKind: 'controlled',
      expectedMessage: 'SYNTH_ROWS_FAILURE',
    },
    {
      label: 'rows transport failure',
      failedRequest: 'rows',
      failureKind: 'transport',
      expectedMessage: WORD_TYPES_ERROR_LABEL,
    },
    {
      label: 'tree controlled failure',
      failedRequest: 'tree',
      failureKind: 'controlled',
      expectedMessage: 'SYNTH_TREE_FAILURE',
    },
    {
      label: 'tree transport failure',
      failedRequest: 'tree',
      failureKind: 'transport',
      expectedMessage: WORD_TYPES_ERROR_LABEL,
    },
  ] as const)('shows only the failed response message for a $label', ({ failedRequest, failureKind, expectedMessage }) => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });

    facade.bindToRoute(route.route);
    const treeRequest = expectTreeRequest(http);
    const rowsRequest = expectTableRequest(http, { type: 'inl', tableView: 'words' });

    if (failedRequest === 'tree') {
      rowsRequest.flush({ ...okRows([wordRow()]), message: 'SYNTH_ROWS_SUCCESS' });
      if (failureKind === 'controlled') {
        treeRequest.flush({ isSuccess: false, message: 'SYNTH_TREE_FAILURE', data: null });
      } else {
        failTransport(treeRequest);
      }
    } else {
      treeRequest.flush({ ...okTree(), message: 'SYNTH_TREE_SUCCESS' });
      if (failureKind === 'controlled') {
        rowsRequest.flush({ isSuccess: false, message: 'SYNTH_ROWS_FAILURE', data: null });
      } else {
        failTransport(rowsRequest);
      }
    }

    expect(facade.listState().status).toBe('error');
    expect(facade.listState().errorMessage).toBe(expectedMessage);
    facade.unbindFromRoute();
  });

  it.each([
    {
      label: 'transport failure',
      failureKind: 'transport',
    },
    {
      label: 'controlled API failure',
      failureKind: 'controlled',
    },
  ] as const)('keeps the previously loaded tree on a tree-only parent $label', ({ failureKind }) => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);
    flushLeafList(http, {
      type: 'noun',
      childCode: 'PN',
      tableView: 'roots',
    }, okRows([rootRow()]));

    const loadedTree = facade.listState().tree;
    expect(loadedTree).not.toBeNull();

    // Tree and detail/list responses share the bounded cache. Fill it so the parent transition must
    // re-fetch the tree while the last valid tree remains available in facade state.
    const cache = TestBed.inject(WordTypesCache);
    for (let index = 0; index < 50; index++) {
      cache.store(`tree-eviction-fixture:${index}`, { isSuccess: true, message: null, data: { index } });
    }

    route.setQueryParams({ type: 'noun', tableView: 'roots' });

    http.expectNone((candidate) => candidate.url.endsWith('/api/words/word-types/table'));
    const failedTreeRequest = expectTreeRequest(http);
    if (failureKind === 'controlled') {
      failedTreeRequest.flush({ isSuccess: false, message: 'SYNTH_TREE_FAILURE', data: null });
    } else {
      failTransport(failedTreeRequest);
    }
    expect(facade.listState().status).toBe('error');
    expect(facade.listState().tree).toBe(loadedTree);
    expect(facade.listState().rows).toBeNull();
    facade.unbindFromRoute();
  });

  it('retryList re-issues the list load after an error', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    const treeRequest = expectTreeRequest(http);
    const failedRowsRequest = expectTableRequest(http, { type: 'inl', tableView: 'words' });
    treeRequest.flush(okTree());
    failTransport(failedRowsRequest);
    expect(facade.listState().status).toBe('error');

    facade.retryList();
    const retryRequest = expectTableRequest(http, {
      type: 'inl',
      childCode: null,
      tableView: 'words',
      sort: 'occurrences',
      page: 1,
    });
    retryRequest.flush(okRows([wordRow()]));
    expect(facade.listState().status).toBe('success');
    facade.unbindFromRoute();
  });

  it('cancels an in-flight retry only when the list request key changes', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);
    const treeRequest = expectTreeRequest(http);
    const failedRowsRequest = expectTableRequest(http, { type: 'inl', tableView: 'words' });
    treeRequest.flush(okTree());
    failTransport(failedRowsRequest);

    facade.retryList();
    const retryRequest = expectTableRequest(http, { type: 'inl', tableView: 'words' });

    route.setQueryParams({ type: 'inl', word: '191001', contextCode: 'INL' });
    expect(retryRequest.cancelled).toBe(false);

    route.setQueryParams({ type: 'noun', childCode: 'PN' });
    expect(retryRequest.cancelled).toBe(true);
    expectTableRequest(http, {
      type: 'noun',
      childCode: 'PN',
      tableView: 'words',
    }).flush(okRows([wordRow({ contextCode: 'PN' })]));
    facade.unbindFromRoute();
  });

  it('keeps the active tableView when selecting a different child within the same type', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);

    selectScope(facade, 'noun', 'N');

    const lastCall = (router.navigate as ReturnType<typeof vi.fn>).mock.calls.at(-1);
    expect(lastCall?.[1].queryParams).not.toHaveProperty('tableView');
    facade.unbindFromRoute();
  });
});

// Feature 026 US8: the scoped four-count summary loads on a SEPARATE trigger from the table — only when
// the scope changes (type/childCode/case/tense/voice/search/flags), never on a tableView or page change —
// and its lifecycle is fully independent of the table (partial-failure isolation + counts-only retry).
describe('WordTypesExplorerFacade — scope counts (US8)', () => {
  beforeEach(() => getTestBed().resetTestingModule());
  afterEach(() => {
    activeHttp?.verify({ ignoreCancelled: true });
    activeHttp = undefined;
    getTestBed().resetTestingModule();
  });

  const SCOPE_COUNTS_URL = '/api/words/word-types/scope-counts';

  function okCounts(
    counts: WordTypeScopeCountsDto = { wordsCount: 3, rootsCount: 1, stemsCount: 1, lemmasCount: 1 },
  ): ApiResponse<WordTypeScopeCountsDto> {
    return { isSuccess: true, message: 'تم', data: counts };
  }

  function expectScopeCountsRequest(
    http: HttpTestingController,
    expected: { type: string; childCode?: string | null; case?: string | null },
  ): TestRequest {
    const request = http.expectOne((candidate) => candidate.url.endsWith(SCOPE_COUNTS_URL));
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('type')).toBe(expected.type);
    expect(request.request.params.get('childCode')).toBe(expected.childCode ?? null);
    expect(request.request.params.get('case')).toBe(expected.case ?? null);
    // Scope-level only: never view/sort/paging params.
    expect(request.request.params.has('tableView')).toBe(false);
    expect(request.request.params.has('sort')).toBe(false);
    expect(request.request.params.has('page')).toBe(false);
    expect(request.request.params.has('pageSize')).toBe(false);
    return request;
  }

  function expectNoScopeCountsRequest(http: HttpTestingController): void {
    http.expectNone((candidate) => candidate.url.endsWith(SCOPE_COUNTS_URL));
  }

  it('loads and exposes the four counts for a confirmed leaf scope', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN' });
    facade.bindToRoute(route.route);

    flushLeafList(http, { type: 'noun', childCode: 'PN', tableView: 'words' }, okRows([wordRow()]));
    expectScopeCountsRequest(http, { type: 'noun', childCode: 'PN' }).flush(okCounts());

    expect(facade.scopeCountsState().status).toBe('success');
    expect(facade.scopeCountsState().counts).toEqual({ wordsCount: 3, rootsCount: 1, stemsCount: 1, lemmasCount: 1 });
    facade.unbindFromRoute();
  });

  it('shows no counts (idle) and issues no request for a parent (non-leaf) scope', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'noun' });
    facade.bindToRoute(route.route);

    expectNoScopeCountsRequest(http);
    flushTreeOnly(http);

    expect(facade.scopeCountsState().status).toBe('idle');
    expect(facade.scopeCountsState().counts).toBeNull();
    facade.unbindFromRoute();
  });

  it('does NOT refetch counts on a tableView change or a page change', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN' });
    facade.bindToRoute(route.route);
    flushLeafList(http, { type: 'noun', childCode: 'PN', tableView: 'words' }, okRows([wordRow()]));
    expectScopeCountsRequest(http, { type: 'noun', childCode: 'PN' }).flush(okCounts());

    route.setQueryParams({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    expectNoScopeCountsRequest(http);
    expectTableRequest(http, { type: 'noun', childCode: 'PN', tableView: 'roots' }).flush(okRows([rootRow()]));

    route.setQueryParams({ type: 'noun', childCode: 'PN', tableView: 'roots', page: '2' });
    expectNoScopeCountsRequest(http);
    expectTableRequest(http, { type: 'noun', childCode: 'PN', tableView: 'roots', page: 2 }).flush(okRows([]));

    expect(facade.scopeCountsState().counts).toEqual({ wordsCount: 3, rootsCount: 1, stemsCount: 1, lemmasCount: 1 });
    facade.unbindFromRoute();
  });

  it('refetches counts when the scope changes (e.g. a case sub-filter)', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN' });
    facade.bindToRoute(route.route);
    flushLeafList(http, { type: 'noun', childCode: 'PN', tableView: 'words' }, okRows([wordRow()]));
    expectScopeCountsRequest(http, { type: 'noun', childCode: 'PN' }).flush(okCounts());

    route.setQueryParams({ type: 'noun', childCode: 'PN', case: 'genitive' });
    expectTableRequest(http, { type: 'noun', childCode: 'PN', case: 'genitive' }).flush(okRows([]));
    expectScopeCountsRequest(http, { type: 'noun', childCode: 'PN', case: 'genitive' })
      .flush(okCounts({ wordsCount: 1, rootsCount: 1, stemsCount: 1, lemmasCount: 1 }));

    expect(facade.scopeCountsState().counts?.wordsCount).toBe(1);
    facade.unbindFromRoute();
  });

  it('isolates a counts failure from the table and retries counts only', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);
    flushLeafList(http, { type: 'inl', tableView: 'words' }, okRows([wordRow()]));

    // Counts fail while the table succeeded: the table stays usable; the strip shows its own error.
    failTransport(expectScopeCountsRequest(http, { type: 'inl' }));
    expect(facade.scopeCountsState().status).toBe('error');
    expect(facade.scopeCountsState().counts).toBeNull();
    expect(facade.listState().status).toBe('success');

    // Retry refetches ONLY the counts — no table request is issued.
    facade.retryScopeCounts();
    expect(facade.scopeCountsState().status).toBe('loading');
    const retry = expectScopeCountsRequest(http, { type: 'inl' });
    http.expectNone((candidate) => candidate.url.endsWith('/api/words/word-types/table'));
    retry.flush(okCounts());
    expect(facade.scopeCountsState().status).toBe('success');
    facade.unbindFromRoute();
  });

  it('treats a controlled (isSuccess:false) counts response as an error', () => {
    const { facade, http } = setup();
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);
    flushLeafList(http, { type: 'inl', tableView: 'words' }, okRows([wordRow()]));

    expectScopeCountsRequest(http, { type: 'inl' }).flush({ isSuccess: false, message: 'SYNTH', data: null });
    expect(facade.scopeCountsState().status).toBe('error');
    facade.unbindFromRoute();
  });
});
