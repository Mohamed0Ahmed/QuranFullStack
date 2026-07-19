import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  WordTypeGroupedMemberWordDto,
  WordTypeGroupedKind,
  WordTypeGroupedSummaryDto,
} from '../models/word-types-detail.models';
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
      displayText: 'SYNTH_ROOT_TEXT',
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
    displayText: 'SYNTH_MEMBER_WORD_TEXT',
    typeCode: 'N',
    typeLabel: { ar: 'SYNTH_MEMBER_TYPE_LABEL' },
    broadLabel: { ar: 'SYNTH_MEMBER_BROAD_LABEL' },
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
          verseKey: '999:999',
          surahNumber: 999,
          ayahNumber: 999,
          pageNumber: 999,
          matchedWordPositions: [1],
          matchedWordIds: [9900001],
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
    data: { surahs: [{ surahNumber: 999, nameArabic: 'SYNTH_SURAH_NAME', occurrencesCount: 3 }], missingSurahs: [] },
  };
}

let activeHttp: HttpTestingController | undefined;

function setup(): { facade: WordTypesDetailFacade; http: HttpTestingController } {
  TestBed.configureTestingModule({
    providers: [
      WordTypesDetailFacade,
      WordTypesDetailViewLoader,
      WordTypesCache,
      provideRouter([]),
      provideHttpClient(),
      provideHttpClientTesting(),
    ],
  });

  const facade = TestBed.inject(WordTypesDetailFacade);
  const http = TestBed.inject(HttpTestingController);
  activeHttp = http;
  return { facade, http };
}

const GROUPED_ROUTE_KIND: Record<WordTypeGroupedKind, string> = {
  root: 'roots',
  stem: 'stems',
  lemma: 'lemmas',
};

function expectGroupedRequest(
  http: HttpTestingController,
  kind: WordTypeGroupedKind,
  dimensionId: number,
  suffix = '',
): TestRequest {
  const routeKind = GROUPED_ROUTE_KIND[kind];
  return http.expectOne((request) =>
    request.url.endsWith(`/api/words/word-types/table/${routeKind}/${dimensionId}${suffix}`),
  );
}

function expectGroupedScope(
  request: TestRequest,
  expected: {
    type: string;
    childCode?: string | null;
    case?: string;
    tense?: string;
    voice?: string;
    page?: number;
  },
): void {
  expect(request.request.method).toBe('GET');
  expect(request.request.params.get('type')).toBe(expected.type);
  expect(request.request.params.get('childCode')).toBe(expected.childCode ?? null);
  expect(request.request.params.get('case')).toBe(expected.case ?? null);
  expect(request.request.params.get('tense')).toBe(expected.tense ?? null);
  expect(request.request.params.get('voice')).toBe(expected.voice ?? null);
  if (expected.page !== undefined) {
    expect(request.request.params.get('page')).toBe(String(expected.page));
    expect(request.request.params.get('pageSize')).toBe(String(WORD_TYPES_DETAIL_PAGE_SIZE));
  } else {
    expect(request.request.params.has('page')).toBe(false);
    expect(request.request.params.has('pageSize')).toBe(false);
  }
}

function flushGroupedSummary(
  http: HttpTestingController,
  kind: WordTypeGroupedKind,
  dimensionId: number,
  response = okGroupedSummary({ kind, dimensionId }),
): TestRequest {
  const request = expectGroupedRequest(http, kind, dimensionId);
  request.flush(response);
  return request;
}

function flushGroupedView(
  http: HttpTestingController,
  kind: WordTypeGroupedKind,
  dimensionId: number,
  view: 'words' | 'ayahs' | 'surahs',
  page = 1,
): TestRequest {
  const request = expectGroupedRequest(http, kind, dimensionId, `/${view}`);
  request.flush(view === 'words' ? okGroupedWords() : view === 'ayahs' ? okAyahs(page) : okSurahs());
  return request;
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

function groupedDetailParams(
  queryParams: Record<string, string>,
  scope: Record<string, string> = {},
): Record<string, string> {
  return {
    detailType: 'noun',
    detailChildCode: 'N',
    detailCase: 'all',
    detailTense: 'all',
    detailVoice: 'all',
    ...queryParams,
    ...scope,
  };
}

function wordDetailParams(queryParams: Record<string, string>): Record<string, string> {
  const type = queryParams['type'] ?? 'noun';
  const childCode = queryParams['childCode'] ?? (type === 'inl' ? null : type === 'verb' ? 'present' : 'N');
  const result: Record<string, string> = {
    detailType: type,
    detailCase: 'all',
    detailTense: 'all',
    detailVoice: 'all',
    ...queryParams,
  };
  if (childCode !== null) {
    result['detailChildCode'] = childCode;
  }
  return result;
}

const groupedCases = [
  { tableView: 'roots', key: 'root', kind: 'root', dimensionId: 190700, idField: 'rootId' },
  { tableView: 'stems', key: 'stem', kind: 'stem', dimensionId: 190600, idField: 'stemId' },
  { tableView: 'lemmas', key: 'lemma', kind: 'lemma', dimensionId: 190500, idField: 'lemmaId' },
] as const;

describe('WordTypesDetailFacade — kind-aware orchestration', () => {
  beforeEach(() => getTestBed().resetTestingModule());
  afterEach(() => {
    activeHttp?.verify({ ignoreCancelled: true });
    activeHttp = undefined;
    getTestBed().resetTestingModule();
  });

  it.each(groupedCases)(
    'restores a $kind selection in words view at internal page 1 without a detailPage',
    ({ tableView, key, kind, dimensionId, idField }) => {
      const { facade, http } = setup();
      const route = controllableRoute(groupedDetailParams({ tableView, type: 'noun', [key]: String(dimensionId) }));

      facade.bindToRoute(route.route);

      const summaryRequest = expectGroupedRequest(http, kind, dimensionId);
      expectGroupedScope(summaryRequest, { type: 'noun', childCode: 'N' });
      summaryRequest.flush(okGroupedSummary({ kind, dimensionId }));
      const wordsRequest = expectGroupedRequest(http, kind, dimensionId, '/words');
      expectGroupedScope(wordsRequest, { type: 'noun', childCode: 'N', page: 1 });
      wordsRequest.flush(okGroupedWords());

      const state = facade.panelState();
      expect(state.selection?.kind).toBe(kind);
      expect((state.selection as unknown as Record<string, number>)[idField]).toBe(dimensionId);
      expect(state.view).toBe('words');
      expect(state.detailPage).toBe(1);
      facade.unbindFromRoute();
    },
  );

  it('restores a grouped page above one, then returns to the omitted page one', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams({ tableView: 'stems', type: 'noun', stem: '190600', view: 'ayahs', detailPage: '2' }));

    facade.bindToRoute(route.route);
    flushGroupedSummary(http, 'stem', 190600);
    const pageTwo = expectGroupedRequest(http, 'stem', 190600, '/ayahs');
    expectGroupedScope(pageTwo, { type: 'noun', childCode: 'N', page: 2 });
    pageTwo.flush(okAyahs(2));
    expect(facade.panelState().detailPage).toBe(2);

    route.setQueryParams(groupedDetailParams({ tableView: 'stems', type: 'noun', stem: '190600', view: 'ayahs' }));
    const pageOne = expectGroupedRequest(http, 'stem', 190600, '/ayahs');
    expectGroupedScope(pageOne, { type: 'noun', childCode: 'N', page: 1 });
    pageOne.flush(okAyahs(1));
    expect(facade.panelState().detailPage).toBe(1);
    facade.unbindFromRoute();
  });

  it('restores a word selection and keeps the ayahs default', () => {
    const { facade, http } = setup();
    const route = controllableRoute(wordDetailParams({ tableView: 'words', type: 'inl', word: '191001', contextCode: 'INL' }));

    facade.bindToRoute(route.route);

    const summaryRequest = http.expectOne((request) => request.url.endsWith('/api/words/word-types/words/191001'));
    expect(summaryRequest.request.params.get('contextCode')).toBe('INL');
    summaryRequest.flush(okWordSummary());
    const ayahsRequest = http.expectOne((request) => request.url.endsWith('/api/words/word-types/words/191001/ayahs'));
    expect(ayahsRequest.request.params.get('contextCode')).toBe('INL');
    expect(ayahsRequest.request.params.get('page')).toBe('1');
    expect(ayahsRequest.request.params.get('pageSize')).toBe(String(WORD_TYPES_DETAIL_PAGE_SIZE));
    ayahsRequest.flush(okAyahs());

    const state = facade.panelState();
    expect(state.selection?.kind).toBe('word');
    expect(state.view).toBe('ayahs');
    http.expectNone((request) => request.url.includes('/api/words/word-types/table/'));
    facade.unbindFromRoute();
  });

  it('replaces kind, summary, and active view across browser back and forward', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '190700' }));

    facade.bindToRoute(route.route);
    flushGroupedSummary(http, 'root', 190700);
    flushGroupedView(http, 'root', 190700, 'words');
    expect(facade.panelState().selection?.kind).toBe('root');

    route.setQueryParams(groupedDetailParams({ tableView: 'stems', type: 'noun', stem: '190600' }));
    flushGroupedSummary(http, 'stem', 190600);
    flushGroupedView(http, 'stem', 190600, 'words');
    expect(facade.panelState().selection?.kind).toBe('stem');
    expect(facade.panelState().groupedSummary?.dimensionId).toBe(190600);

    route.setQueryParams(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '190700' }));
    http.expectNone(() => true);
    expect(facade.panelState().selection?.kind).toBe('root');
    facade.unbindFromRoute();
  });

  it('keeps stored detail scope when only list scope changes, then reloads when detail scope changes', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams(
      { tableView: 'roots', type: 'verb', childCode: 'present', root: '190700', view: 'ayahs' },
      { detailType: 'verb', detailChildCode: 'present', detailTense: 'present' },
    ));

    facade.bindToRoute(route.route);
    const initialSummary = expectGroupedRequest(http, 'root', 190700);
    expectGroupedScope(initialSummary, { type: 'verb', childCode: 'present', tense: 'present' });
    initialSummary.flush(okGroupedSummary());
    const initialAyahs = expectGroupedRequest(http, 'root', 190700, '/ayahs');
    expectGroupedScope(initialAyahs, { type: 'verb', childCode: 'present', tense: 'present', page: 1 });
    initialAyahs.flush(okAyahs());

    route.setQueryParams(groupedDetailParams(
      { tableView: 'roots', type: 'noun', childCode: 'ADJ', root: '190700', view: 'ayahs' },
      { detailType: 'verb', detailChildCode: 'present', detailTense: 'present' },
    ));
    http.expectNone(() => true);

    route.setQueryParams(groupedDetailParams(
      { tableView: 'roots', type: 'noun', childCode: 'ADJ', root: '190700', view: 'ayahs' },
      { detailType: 'noun', detailChildCode: 'ADJ' },
    ));
    const changedSummary = expectGroupedRequest(http, 'root', 190700);
    expectGroupedScope(changedSummary, { type: 'noun', childCode: 'ADJ' });
    changedSummary.flush(okGroupedSummary());
    const changedAyahs = expectGroupedRequest(http, 'root', 190700, '/ayahs');
    expectGroupedScope(changedAyahs, { type: 'noun', childCode: 'ADJ', page: 1 });
    changedAyahs.flush(okAyahs());
    facade.unbindFromRoute();
  });

  it.each(['roots', 'words', 'lemmas'] as const)(
    'keeps a loaded stem detail unchanged without another API request when tableView changes to %s',
    (tableView) => {
      const { facade, http } = setup();
      const preserved = groupedDetailParams(
        { tableView: 'stems', type: 'noun', childCode: 'N', stem: '190600', view: 'ayahs', detailPage: '2' },
      );
      const route = controllableRoute(preserved);

      facade.bindToRoute(route.route);
      flushGroupedSummary(
        http,
        'stem',
        190600,
        okGroupedSummary({ kind: 'stem', dimensionId: 190600, displayText: 'SYNTH_STEM_TEXT' }),
      );
      flushGroupedView(http, 'stem', 190600, 'ayahs', 2);
      const before = facade.panelState();

      route.setQueryParams({ ...preserved, tableView });

      expect(facade.panelState()).toEqual(before);
      http.expectNone(() => true);
      facade.unbindFromRoute();
    },
  );

  it('restores mismatched table and detail kinds independently through refresh and history', () => {
    const { facade, http } = setup();
    const stemDetail = groupedDetailParams({ tableView: 'roots', type: 'noun', stem: '190600', view: 'ayahs', detailPage: '2' });
    const lemmaDetail = groupedDetailParams({ tableView: 'words', type: 'noun', lemma: '190500', view: 'words' });
    const route = controllableRoute(stemDetail);

    facade.bindToRoute(route.route);
    flushGroupedSummary(http, 'stem', 190600);
    flushGroupedView(http, 'stem', 190600, 'ayahs', 2);
    expect(facade.panelState()).toMatchObject({
      selection: { kind: 'stem', stemId: 190600 },
      view: 'ayahs',
      detailPage: 2,
    });

    route.setQueryParams(lemmaDetail);
    flushGroupedSummary(http, 'lemma', 190500);
    flushGroupedView(http, 'lemma', 190500, 'words');
    expect(facade.panelState()).toMatchObject({ selection: { kind: 'lemma', lemmaId: 190500 }, view: 'words' });

    route.setQueryParams(stemDetail);
    http.expectNone(() => true);
    expect(facade.panelState()).toMatchObject({
      selection: { kind: 'stem', stemId: 190600 },
      view: 'ayahs',
      detailPage: 2,
    });
    facade.unbindFromRoute();
  });

  it('restores a grouped detail from its stored scope when list scope differs on direct load', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams(
      { tableView: 'roots', type: 'noun', childCode: 'ADJ', root: '190700', view: 'ayahs' },
      { detailType: 'verb', detailChildCode: 'present', detailTense: 'present', detailVoice: 'passive' },
    ));

    facade.bindToRoute(route.route);

    const summaryRequest = expectGroupedRequest(http, 'root', 190700);
    expectGroupedScope(summaryRequest, {
      type: 'verb',
      childCode: 'present',
      tense: 'present',
      voice: 'passive',
    });
    summaryRequest.flush(okGroupedSummary());
    const ayahsRequest = expectGroupedRequest(http, 'root', 190700, '/ayahs');
    expectGroupedScope(ayahsRequest, {
      type: 'verb',
      childCode: 'present',
      tense: 'present',
      voice: 'passive',
      page: 1,
    });
    ayahsRequest.flush(okAyahs());
    expect(facade.panelState().selection).toMatchObject({ kind: 'root', rootId: 190700 });
    facade.unbindFromRoute();
  });

  it('fails closed without a complete stored detail scope', () => {
    const { facade, http } = setup();
    const route = controllableRoute({
      tableView: 'roots',
      type: 'noun',
      childCode: 'ADJ',
      root: '190700',
      detailType: 'verb',
      detailChildCode: 'present',
      detailCase: 'all',
      detailTense: 'present',
    });

    facade.bindToRoute(route.route);

    expect(facade.panelState().selection).toBeNull();
    http.expectNone(() => true);
    facade.unbindFromRoute();
  });

  it('cancels the earlier summary and keeps the later selection', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '190700' }));

    facade.bindToRoute(route.route);
    const rootSummary = expectGroupedRequest(http, 'root', 190700);
    route.setQueryParams(groupedDetailParams({ tableView: 'stems', type: 'noun', stem: '190600' }));

    expect(rootSummary.cancelled).toBe(true);
    flushGroupedSummary(http, 'stem', 190600);
    flushGroupedView(http, 'stem', 190600, 'words');

    const state = facade.panelState();
    expect(state.selection?.kind).toBe('stem');
    expect(state.groupedSummary?.dimensionId).toBe(190600);
    facade.unbindFromRoute();
  });

  it('cancels an in-flight detail as soon as the route replaces its selection', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '190700' }));

    facade.bindToRoute(route.route);
    flushGroupedSummary(http, 'root', 190700);
    const rootWords = expectGroupedRequest(http, 'root', 190700, '/words');
    route.setQueryParams(groupedDetailParams({ tableView: 'stems', type: 'noun', stem: '190600' }));

    expect(rootWords.cancelled).toBe(true);
    flushGroupedSummary(http, 'stem', 190600);
    flushGroupedView(http, 'stem', 190600, 'words');
    facade.unbindFromRoute();
  });

  it('cancels the earlier detail page and keeps the later page', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '190700', view: 'ayahs', detailPage: '2' }));

    facade.bindToRoute(route.route);
    flushGroupedSummary(http, 'root', 190700);
    const pageTwo = expectGroupedRequest(http, 'root', 190700, '/ayahs');
    route.setQueryParams(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '190700', view: 'ayahs', detailPage: '3' }));

    expect(pageTwo.cancelled).toBe(true);
    const pageThree = expectGroupedRequest(http, 'root', 190700, '/ayahs');
    expectGroupedScope(pageThree, { type: 'noun', childCode: 'N', page: 3 });
    pageThree.flush(okAyahs(3));

    expect(facade.panelState().detailPage).toBe(3);
    expect(facade.panelState().ayahs?.page).toBe(3);
    facade.unbindFromRoute();
  });

  it('produces a kind-aware not-found without dropping the selection', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '999999' }));

    facade.bindToRoute(route.route);
    flushGroupedSummary(
      http,
      'root',
      999999,
      { isSuccess: true, message: 'المجموعة المحددة غير موجودة', data: null },
    );

    const state = facade.panelState();
    expect(state.status).toBe('notFound');
    expect(state.selection?.kind).toBe('root');
    expect(state.summary).toBeNull();
    http.expectNone(() => true);
    facade.unbindFromRoute();
  });

  it('surfaces a retryable error and reloads the current selection on retry', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '190700' }));

    facade.bindToRoute(route.route);
    expectGroupedRequest(http, 'root', 190700).flush(null, { status: 500, statusText: 'Server Error' });
    expect(facade.panelState().status).toBe('error');

    facade.retry();

    flushGroupedSummary(http, 'root', 190700);
    flushGroupedView(http, 'root', 190700, 'words');
    expect(facade.panelState().status).not.toBe('error');
    expect(facade.panelState().selection?.kind).toBe('root');
    facade.unbindFromRoute();
  });

  // Finding M24: unbindFromRoute() used to call only cancelPendingLoads(), which left the
  // facade's own `activeUrlState` set. Re-binding to the SAME query params then short-circuited
  // inside syncFromUrlState() (isSamePanelUrlState matched) and never re-issued the load,
  // stranding the panel on its cancelled 'loading' status with no request in flight.
  it('re-issues the summary load on rebind to the same URL state after a mid-flight unbind', () => {
    const { facade, http } = setup();
    const route = controllableRoute(groupedDetailParams({ tableView: 'roots', type: 'noun', root: '190700' }));

    facade.bindToRoute(route.route);
    const firstSummary = expectGroupedRequest(http, 'root', 190700);
    expect(facade.panelState().status).toBe('loading');

    facade.unbindFromRoute();
    expect(firstSummary.cancelled).toBe(true);

    facade.bindToRoute(route.route);

    const secondSummary = expectGroupedRequest(http, 'root', 190700);
    expect(facade.panelState().status).toBe('loading');

    secondSummary.flush(okGroupedSummary({ kind: 'root', dimensionId: 190700 }));
    const wordsRequest = expectGroupedRequest(http, 'root', 190700, '/words');
    wordsRequest.flush(okGroupedWords());

    const state = facade.panelState();
    expect(state.status).toBe('success');
    expect(state.selection?.kind).toBe('root');
    facade.unbindFromRoute();
  });
});
