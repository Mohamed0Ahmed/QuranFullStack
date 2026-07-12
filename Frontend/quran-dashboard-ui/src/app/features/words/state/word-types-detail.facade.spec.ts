import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, ParamMap, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  WordTypeGroupedMemberWordDto,
  WordTypeGroupedSummaryDto,
  WordTypesApi,
} from '../data-access/word-types.api';
import {
  PagedResultDto,
  WORD_TYPES_DETAIL_PAGE_SIZE,
  WordTypeAyahMatchDto,
  WordTypeSummaryDto,
  WordTypeSurahsResponseDto,
} from '../models/word-types.models';
import { WordTypesCache } from './word-types-cache';
import { WordTypesDetailFacade } from './word-types-detail.facade';
import { WordTypesDetailViewLoader } from './word-types-detail-view.loader';

function okWordSummary(overrides: Partial<WordTypeSummaryDto> = {}): ApiResponse<WordTypeSummaryDto> {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      tashkeelWordId: 191001,
      contextCode: 'INL',
      case: 'all',
      tense: 'all',
      voice: 'all',
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
    },
  };
}

function okGroupedSummary(overrides: Partial<WordTypeGroupedSummaryDto> = {}): ApiResponse<WordTypeGroupedSummaryDto> {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      kind: 'root',
      dimensionId: 190700,
      displayText: 'ك ل م',
      occurrencesCount: 3,
      ayahsCount: 2,
      surahsCount: 1,
      ...overrides,
    },
  };
}

function memberWord(): WordTypeGroupedMemberWordDto {
  return {
    tashkeelWordId: 191001,
    contextCode: 'N',
    case: 'all',
    tense: null,
    voice: null,
    displayText: 'كلمة',
    typeCode: 'N',
    typeLabel: { ar: 'اسم' },
    broadLabel: { ar: 'اسم' },
    caseOrFeature: null,
    rootText: null,
    lemmaText: null,
    stemText: null,
    occurrencesCount: 3,
    ayahsCount: 2,
    surahsCount: 1,
  };
}

function okGroupedWords(): ApiResponse<PagedResultDto<WordTypeGroupedMemberWordDto>> {
  return { isSuccess: true, message: 'تم', data: { page: 1, pageSize: WORD_TYPES_DETAIL_PAGE_SIZE, totalCount: 1, items: [memberWord()] } };
}

function okAyahs(page = 1): ApiResponse<PagedResultDto<WordTypeAyahMatchDto>> {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      page,
      pageSize: WORD_TYPES_DETAIL_PAGE_SIZE,
      totalCount: 1,
      items: [
        {
          verseKey: '1:1',
          surahNumber: 1,
          ayahNumber: 1,
          pageNumber: 1,
          matchedWordPositions: [1],
          matchedWordIds: [1],
          words: [],
        },
      ],
    },
  };
}

function okSurahs(): ApiResponse<WordTypeSurahsResponseDto> {
  return {
    isSuccess: true,
    message: 'تم',
    data: { surahs: [{ surahNumber: 1, nameArabic: 'الفاتحة', occurrencesCount: 3 }], missingSurahs: [] },
  };
}

interface ApiMock {
  getSummary: ReturnType<typeof vi.fn>;
  getAyahMatches: ReturnType<typeof vi.fn>;
  getSurahs: ReturnType<typeof vi.fn>;
  getGroupedSummary: ReturnType<typeof vi.fn>;
  getGroupedMemberWords: ReturnType<typeof vi.fn>;
  getGroupedAyahMatches: ReturnType<typeof vi.fn>;
  getGroupedSurahs: ReturnType<typeof vi.fn>;
}

function setup(apiOverrides: Partial<ApiMock> = {}): { facade: WordTypesDetailFacade; api: ApiMock } {
  TestBed.configureTestingModule({
    providers: [
      WordTypesDetailFacade,
      WordTypesDetailViewLoader,
      WordTypesCache,
      provideRouter([]),
      {
        provide: WordTypesApi,
        useValue: {
          getSummary: vi.fn(() => of(okWordSummary())),
          getAyahMatches: vi.fn(() => of(okAyahs())),
          getSurahs: vi.fn(() => of(okSurahs())),
          getGroupedSummary: vi.fn(() => of(okGroupedSummary())),
          getGroupedMemberWords: vi.fn(() => of(okGroupedWords())),
          getGroupedAyahMatches: vi.fn(() => of(okAyahs())),
          getGroupedSurahs: vi.fn(() => of(okSurahs())),
          ...apiOverrides,
        },
      },
    ],
  });

  const facade = TestBed.inject(WordTypesDetailFacade);
  const api = TestBed.inject(WordTypesApi) as unknown as ApiMock;
  return { facade, api };
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

const groupedCases = [
  { tableView: 'roots', key: 'root', kind: 'root', dimensionId: 190700, idField: 'rootId' },
  { tableView: 'stems', key: 'stem', kind: 'stem', dimensionId: 190600, idField: 'stemId' },
  { tableView: 'lemmas', key: 'lemma', kind: 'lemma', dimensionId: 190500, idField: 'lemmaId' },
] as const;

describe('WordTypesDetailFacade — kind-aware orchestration', () => {
  beforeEach(() => getTestBed().resetTestingModule());
  afterEach(() => getTestBed().resetTestingModule());

  it.each(groupedCases)(
    'restores a $kind selection in words view at internal page 1 without a detailPage',
    ({ tableView, key, kind, dimensionId, idField }) => {
      const getGroupedSummary = vi.fn(() => of(okGroupedSummary({ kind, dimensionId })));
      const getGroupedMemberWords = vi.fn(() => of(okGroupedWords()));
      const { facade, api } = setup({ getGroupedSummary, getGroupedMemberWords });
      const route = controllableRoute({ tableView, type: 'noun', [key]: String(dimensionId) });

      facade.bindToRoute(route.route);

      const state = facade.panelState();
      expect(state.selection?.kind).toBe(kind);
      expect((state.selection as unknown as Record<string, number>)[idField]).toBe(dimensionId);
      expect(state.view).toBe('words');
      expect(state.detailPage).toBe(1);
      const expectedRequest = { kind, dimensionId, type: 'noun', childCode: null, case: 'all', tense: 'all', voice: 'all' };
      expect(api.getGroupedSummary).toHaveBeenCalledWith(expectedRequest);
      expect(api.getGroupedMemberWords).toHaveBeenCalledWith(expectedRequest, 1, WORD_TYPES_DETAIL_PAGE_SIZE);
      facade.unbindFromRoute();
    },
  );

  it('restores a grouped page above one, then returns to the omitted page one', () => {
    const getGroupedSummary = vi.fn(() => of(okGroupedSummary({ kind: 'stem', dimensionId: 190600 })));
    const getGroupedAyahMatches = vi.fn((_request: unknown, page: number) => of(okAyahs(page)));
    const { facade, api } = setup({ getGroupedSummary, getGroupedAyahMatches });
    const route = controllableRoute({ tableView: 'stems', type: 'noun', stem: '190600', view: 'ayahs', detailPage: '2' });

    facade.bindToRoute(route.route);
    expect(facade.panelState().detailPage).toBe(2);
    expect(api.getGroupedAyahMatches).toHaveBeenLastCalledWith(expect.objectContaining({ dimensionId: 190600 }), 2, WORD_TYPES_DETAIL_PAGE_SIZE);

    route.setQueryParams({ tableView: 'stems', type: 'noun', stem: '190600', view: 'ayahs' });
    expect(facade.panelState().detailPage).toBe(1);
    expect(api.getGroupedAyahMatches).toHaveBeenLastCalledWith(expect.objectContaining({ dimensionId: 190600 }), 1, WORD_TYPES_DETAIL_PAGE_SIZE);
    facade.unbindFromRoute();
  });

  it('restores a word selection and keeps the ayahs default', () => {
    const { facade, api } = setup();
    const route = controllableRoute({ tableView: 'words', type: 'inl', word: '191001', contextCode: 'INL' });

    facade.bindToRoute(route.route);

    const state = facade.panelState();
    expect(state.selection?.kind).toBe('word');
    expect(state.view).toBe('ayahs');
    expect(api.getSummary).toHaveBeenCalled();
    expect(api.getAyahMatches).toHaveBeenCalledWith(expect.objectContaining({ tashkeelWordId: 191001, contextCode: 'INL' }), 1, WORD_TYPES_DETAIL_PAGE_SIZE);
    expect(api.getGroupedSummary).not.toHaveBeenCalled();
    facade.unbindFromRoute();
  });

  it('replaces kind, summary, and active view across browser back and forward', () => {
    const getGroupedSummary = vi.fn((request: { kind: string; dimensionId: number }) =>
      of(okGroupedSummary({ kind: request.kind as WordTypeGroupedSummaryDto['kind'], dimensionId: request.dimensionId })),
    );
    const { facade } = setup({ getGroupedSummary });
    const route = controllableRoute({ tableView: 'roots', type: 'noun', root: '190700' });

    facade.bindToRoute(route.route);
    expect(facade.panelState().selection?.kind).toBe('root');

    route.setQueryParams({ tableView: 'stems', type: 'noun', stem: '190600' });
    expect(facade.panelState().selection?.kind).toBe('stem');
    expect(facade.panelState().groupedSummary?.dimensionId).toBe(190600);

    route.setQueryParams({ tableView: 'roots', type: 'noun', root: '190700' });
    expect(facade.panelState().selection?.kind).toBe('root');
    facade.unbindFromRoute();
  });

  it('loads a new scoped summary when only the scope changes for the same dimension id', () => {
    const getGroupedSummary = vi.fn(() => of(okGroupedSummary({ kind: 'root', dimensionId: 190700 })));
    const { facade, api } = setup({ getGroupedSummary });
    const route = controllableRoute({ tableView: 'roots', type: 'noun', root: '190700' });

    facade.bindToRoute(route.route);
    expect(api.getGroupedSummary).toHaveBeenCalledTimes(1);
    expect(api.getGroupedSummary).toHaveBeenLastCalledWith(expect.objectContaining({ childCode: null }));

    route.setQueryParams({ tableView: 'roots', type: 'noun', childCode: 'PN', root: '190700' });
    expect(api.getGroupedSummary).toHaveBeenCalledTimes(2);
    expect(api.getGroupedSummary).toHaveBeenLastCalledWith(expect.objectContaining({ childCode: 'PN' }));
    facade.unbindFromRoute();
  });

  it('keeps the later selection when an earlier summary responds late', () => {
    const first$ = new Subject<ApiResponse<WordTypeGroupedSummaryDto>>();
    const second$ = new Subject<ApiResponse<WordTypeGroupedSummaryDto>>();
    const getGroupedSummary = vi.fn().mockReturnValueOnce(first$).mockReturnValueOnce(second$);
    const { facade } = setup({ getGroupedSummary });
    const route = controllableRoute({ tableView: 'roots', type: 'noun', root: '190700' });

    facade.bindToRoute(route.route);
    route.setQueryParams({ tableView: 'stems', type: 'noun', stem: '190600' });

    second$.next(okGroupedSummary({ kind: 'stem', dimensionId: 190600 }));
    second$.complete();
    first$.next(okGroupedSummary({ kind: 'root', dimensionId: 190700 }));
    first$.complete();

    const state = facade.panelState();
    expect(state.selection?.kind).toBe('stem');
    expect(state.groupedSummary?.dimensionId).toBe(190600);
    facade.unbindFromRoute();
  });

  it('keeps the later view or page when an earlier detail responds late', () => {
    const first$ = new Subject<ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>>();
    const second$ = new Subject<ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>>();
    const getGroupedAyahMatches = vi.fn().mockReturnValueOnce(first$).mockReturnValueOnce(second$);
    const getGroupedSummary = vi.fn(() => of(okGroupedSummary({ kind: 'root', dimensionId: 190700 })));
    const { facade } = setup({ getGroupedSummary, getGroupedAyahMatches });
    const route = controllableRoute({ tableView: 'roots', type: 'noun', root: '190700', view: 'ayahs', detailPage: '2' });

    facade.bindToRoute(route.route);
    route.setQueryParams({ tableView: 'roots', type: 'noun', root: '190700', view: 'ayahs', detailPage: '3' });

    second$.next(okAyahs(3));
    second$.complete();
    first$.next(okAyahs(2));
    first$.complete();

    expect(facade.panelState().detailPage).toBe(3);
    expect(facade.panelState().ayahs?.page).toBe(3);
    facade.unbindFromRoute();
  });

  it('produces a kind-aware not-found without dropping the selection', () => {
    const getGroupedSummary = vi.fn(() => of({ isSuccess: true, message: 'المجموعة المحددة غير موجودة', data: null } as ApiResponse<WordTypeGroupedSummaryDto>));
    const { facade, api } = setup({ getGroupedSummary });
    const route = controllableRoute({ tableView: 'roots', type: 'noun', root: '999999' });

    facade.bindToRoute(route.route);

    const state = facade.panelState();
    expect(state.status).toBe('notFound');
    expect(state.selection?.kind).toBe('root');
    expect(state.summary).toBeNull();
    expect(api.getGroupedMemberWords).not.toHaveBeenCalled();
    facade.unbindFromRoute();
  });

  it('surfaces a retryable error and reloads the current selection on retry', () => {
    let attempt = 0;
    const getGroupedSummary = vi.fn(() =>
      attempt++ === 0
        ? throwError(() => new HttpErrorResponse({ status: 500 }))
        : of(okGroupedSummary({ kind: 'root', dimensionId: 190700 })),
    );
    const { facade, api } = setup({ getGroupedSummary });
    const route = controllableRoute({ tableView: 'roots', type: 'noun', root: '190700' });

    facade.bindToRoute(route.route);
    expect(facade.panelState().status).toBe('error');

    facade.retry();

    expect(api.getGroupedSummary).toHaveBeenCalledTimes(2);
    expect(facade.panelState().status).not.toBe('error');
    expect(facade.panelState().selection?.kind).toBe('root');
    facade.unbindFromRoute();
  });
});
