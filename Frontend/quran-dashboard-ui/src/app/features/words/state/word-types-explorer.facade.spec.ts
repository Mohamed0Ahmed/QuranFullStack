import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypesApi } from '../data-access/word-types.api';
import {
  PagedResultDto,
  RootTableRowDto,
  WORD_TYPES_PAGE_SIZE,
  WordTableRowDto,
  WordTypeTableRowDto,
  WordTypeTreeDto,
} from '../models/word-types.models';
import { WordTypesExplorerFacade } from './word-types-explorer.facade';

function wordRow(overrides: Partial<WordTableRowDto> = {}): WordTableRowDto {
  return {
    kind: 'word',
    tashkeelWordId: 191001,
    contextCode: 'INL',
    case: null,
    tense: null,
    voice: null,
    displayText: 'الٓمٓ',
    typeCode: 'INL',
    typeLabel: { ar: 'حروف مقطّعة' },
    broadLabel: { ar: 'حروف مقطّعة' },
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
    displayText: 'ك ل م',
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
          code: 'noun', label: { ar: 'اسم' }, count: 2,
          secondaryFilter: { kind: 'case', options: [], voiceOptions: [] },
          children: [
            { code: 'N', childCode: 'N', label: { ar: 'اسم' }, count: 1 },
            { code: 'PN', childCode: 'PN', label: { ar: 'اسم علم' }, count: 1 },
          ],
        },
        { code: 'verb', label: { ar: 'فعل' }, count: 0, secondaryFilter: { kind: 'tense+voice', options: [], voiceOptions: [] }, children: [] },
        { code: 'particle', label: { ar: 'حرف وأداة' }, count: 0, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
        { code: 'inl', label: { ar: 'حروف مقطّعة' }, count: 1, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
      ],
    },
  };
}

function setup(apiOverrides: Partial<{ getTree: unknown; getTableRows: unknown }> = {}) {
  TestBed.configureTestingModule({
    providers: [
      WordTypesExplorerFacade,
      provideRouter([]),
      {
        provide: WordTypesApi,
        useValue: {
          getTree: vi.fn(() => of(okTree())),
          getTableRows: vi.fn(() => of(okRows([]))),
          ...apiOverrides,
        },
      },
    ],
  });

  const facade = TestBed.inject(WordTypesExplorerFacade);
  const router = TestBed.inject(Router);
  vi.spyOn(router, 'navigate').mockResolvedValue(true);
  const api = TestBed.inject(WordTypesApi) as unknown as { getTree: ReturnType<typeof vi.fn>; getTableRows: ReturnType<typeof vi.fn> };

  return { facade, router, api };
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
  afterEach(() => getTestBed().resetTestingModule());

  it('selectTableView resets page to 1, clears selection, and navigates with the new tableView', () => {
    const { facade, router } = setup();
    const route = controllableRoute();
    facade.bindToRoute(route.route);

    facade.selectTableView('roots');

    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({
        tableView: 'roots',
        page: '1',
        word: null,
        contextCode: null,
        view: null,
        detailPage: null,
        location: null,
        column: null,
      }),
      queryParamsHandling: 'merge',
    }));
    facade.unbindFromRoute();
  });

  it('loads through getTableRows with the active tableView, and reloads when tableView changes', () => {
    const getTableRows = vi.fn(() => of(okRows([wordRow()])));
    const { facade, api } = setup({ getTableRows });
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    expect(api.getTableRows).toHaveBeenCalledTimes(1);
    expect(api.getTableRows).toHaveBeenLastCalledWith(expect.objectContaining({ type: 'inl', tableView: 'words', pageSize: WORD_TYPES_PAGE_SIZE }));

    route.setQueryParams({ type: 'inl', tableView: 'roots' });

    expect(api.getTableRows).toHaveBeenCalledTimes(2);
    expect(api.getTableRows).toHaveBeenLastCalledWith(expect.objectContaining({ tableView: 'roots' }));
    facade.unbindFromRoute();
  });

  it('nulls rows while loading after tableView changes, so the previous view cannot paint under the new scope', () => {
    const first$ = new Subject<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>();
    const second$ = new Subject<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>();
    const getTableRows = vi.fn().mockReturnValueOnce(first$).mockReturnValueOnce(second$);
    const { facade } = setup({ getTableRows });
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    first$.next(okRows([wordRow()]));
    first$.complete();
    expect(facade.listState().rows?.items).toHaveLength(1);

    route.setQueryParams({ type: 'inl', tableView: 'roots' });
    expect(facade.listState().rows).toBeNull();

    second$.next(okRows([]));
    second$.complete();
    facade.unbindFromRoute();
  });

  it('keeps prior rows visible while a non-tableView filter reloads', () => {
    const first$ = new Subject<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>();
    const second$ = new Subject<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>();
    const getTableRows = vi.fn().mockReturnValueOnce(first$).mockReturnValueOnce(second$);
    const { facade } = setup({ getTableRows });
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    const initialRow = wordRow();
    first$.next(okRows([initialRow]));
    first$.complete();
    expect(facade.listState().rows?.items).toEqual([initialRow]);

    route.setQueryParams({ type: 'noun', childCode: 'PN' });
    expect(facade.listState().status).toBe('loading');
    expect(facade.listState().rows?.items).toEqual([initialRow]);

    second$.next(okRows([]));
    second$.complete();
    facade.unbindFromRoute();
  });

  it('preserves nullable word identity fields from the table response', () => {
    const getTableRows = vi.fn(() => of(okRows([wordRow()])));
    const { facade } = setup({ getTableRows });
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    const row = facade.listState().rows!.items.find((item): item is WordTableRowDto => item.kind === 'word');
    expect(row?.case).toBeNull();
    expect(row?.tense).toBeNull();
    expect(row?.voice).toBeNull();
    facade.unbindFromRoute();
  });

  it('preserves grouped rows in list state for the active tableView', () => {
    const groupedRow = rootRow();
    const getTableRows = vi.fn(() => of(okRows([groupedRow])));
    const { facade } = setup({ getTableRows });
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);

    expect(facade.listState().rows?.items).toEqual([groupedRow]);
    expect(facade.listState().rows?.totalCount).toBe(1);
    facade.unbindFromRoute();
  });

  function lastQueryParams(router: Router): Record<string, unknown> {
    return (router.navigate as ReturnType<typeof vi.fn>).mock.calls.at(-1)?.[1].queryParams as Record<string, unknown>;
  }

  it('preserves the active tableView when selecting a main type', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'inl', tableView: 'roots' });
    facade.bindToRoute(route.route);

    facade.selectType('noun');

    const params = lastQueryParams(router);
    expect(params).not.toHaveProperty('tableView');
    expect(params).toEqual(expect.objectContaining({ type: 'noun', page: '1', word: null, root: null, stem: null, lemma: null }));
    facade.unbindFromRoute();
  });

  it('preserves the active tableView when clearing back to the parent (selectChild(null))', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);

    facade.selectChild(null);

    const params = lastQueryParams(router);
    expect(params).not.toHaveProperty('tableView');
    expect(params).toEqual(expect.objectContaining({ childCode: null, page: '1' }));
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

    facade.selectType('noun');
    facade.selectChild('N');
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

  it('clears only incompatible selection keys when changing table view', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'words', word: '191001', contextCode: 'PN' });
    facade.bindToRoute(route.route);

    facade.selectTableView('roots');

    const params = lastQueryParams(router);
    expect(params).toEqual(expect.objectContaining({ tableView: 'roots', word: null, contextCode: null, stem: null, lemma: null }));
    expect(params).not.toHaveProperty('root');
    facade.unbindFromRoute();
  });

  it('preserves the active tableView and does not request rows for a tree-only parent scope', () => {
    const getTableRows = vi.fn(() => of(okRows([])));
    const { facade, api } = setup({ getTableRows });
    const route = controllableRoute({ type: 'noun', tableView: 'roots' });
    facade.bindToRoute(route.route);

    expect(api.getTableRows).not.toHaveBeenCalled();
    expect(facade.listState().query.tableView).toBe('roots');
    facade.unbindFromRoute();
  });

  it('retryList re-issues the list load after an error', () => {
    const first$ = new Subject<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>();
    const second$ = new Subject<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>();
    const getTableRows = vi.fn().mockReturnValueOnce(first$).mockReturnValueOnce(second$);
    const { facade, api } = setup({ getTableRows });
    const route = controllableRoute({ type: 'inl' });
    facade.bindToRoute(route.route);

    first$.error(new Error('network'));
    expect(facade.listState().status).toBe('error');

    facade.retryList();
    expect(api.getTableRows).toHaveBeenCalledTimes(2);

    second$.next(okRows([wordRow()]));
    second$.complete();
    expect(facade.listState().status).toBe('success');
    facade.unbindFromRoute();
  });

  it('keeps the active tableView when selecting a different child within the same type', () => {
    const { facade, router } = setup();
    const route = controllableRoute({ type: 'noun', childCode: 'PN', tableView: 'roots' });
    facade.bindToRoute(route.route);

    facade.selectChild('N');

    const lastCall = (router.navigate as ReturnType<typeof vi.fn>).mock.calls.at(-1);
    expect(lastCall?.[1].queryParams).not.toHaveProperty('tableView');
    facade.unbindFromRoute();
  });
});
