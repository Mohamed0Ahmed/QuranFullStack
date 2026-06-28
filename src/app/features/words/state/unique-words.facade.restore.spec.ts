import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import { UniqueWordsFacade } from './unique-words.facade';
import {
  DEFAULT_AYAH_PAGE_SIZE,
  UniqueWordAyahMatchDto,
  UniqueWordListItemDto,
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
} from '../models/unique-words.models';
import {
  RESTORED_WORD_LOAD_ERROR_LABEL,
  RESTORED_WORD_NOT_FOUND_LABEL,
} from '../models/unique-words.labels';

function word(id: number): UniqueWordListItemDto {
  return {
    id,
    kind: 'tashkeel',
    displayText: `كلمة-تجريبية-${id}`,
    occurrencesCount: 3,
    ayahsCount: 2,
    surahsCount: 2,
    missingSurahsCount: 112,
    primaryWordTypeCode: null,
    primaryWordTypeBroadArabicLabel: null,
    rootId: null,
    rootText: null,
  };
}

function summary(id: number): UniqueWordSummaryDto {
  return {
    id,
    kind: 'tashkeel',
    displayText: `كلمة-تجريبية-${id}`,
    occurrencesCount: 3,
    ayahsCount: 2,
    surahsCount: 2,
    missingSurahsCount: 112,
  };
}

function activateRouteWith(
  params: Record<string, string>,
  queryParams: Record<string, string>,
): ActivatedRoute {
  const paramMap = of(convertToParamMap(params));
  const queryParamMap = of(convertToParamMap(queryParams));
  return {
    paramMap,
    queryParamMap,
    snapshot: { paramMap: convertToParamMap(params), queryParamMap: convertToParamMap(queryParams) },
  } as unknown as ActivatedRoute;
}

function routeController(
  params: Record<string, string>,
  queryParams: Record<string, string>,
): {
  route: ActivatedRoute;
  setQueryParams: (next: Record<string, string>) => void;
} {
  const paramMap = new BehaviorSubject(convertToParamMap(params));
  const queryParamMap = new BehaviorSubject(convertToParamMap(queryParams));
  return {
    route: {
      paramMap: paramMap.asObservable(),
      queryParamMap: queryParamMap.asObservable(),
      snapshot: { paramMap: convertToParamMap(params), queryParamMap: convertToParamMap(queryParams) },
    } as unknown as ActivatedRoute,
    setQueryParams: (next) => queryParamMap.next(convertToParamMap(next)),
  };
}

function setup(api: Partial<UniqueWordsApi> = {}): UniqueWordsFacade {
  TestBed.configureTestingModule({
    providers: [
      UniqueWordsFacade,
      provideRouter([]),
      {
        provide: UniqueWordsApi,
        useValue: {
          getList: vi.fn(() =>
            of({
              isSuccess: true,
              message: 'تم',
              data: { page: 1, pageSize: 1000, totalCount: 0, items: [] },
            }),
          ),
          getSummary: vi.fn(),
          getMentionedSurahs: vi.fn(),
          getMissingSurahs: vi.fn(),
          getAyahMatches: vi.fn(),
          ...api,
        },
      },
    ],
  });
  return TestBed.inject(UniqueWordsFacade);
}

describe('UniqueWordsFacade URL restore (US4)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('restores modal state from word/view params by loading summary then the view', () => {
    const getSummary = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: summary(42),
      } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: {
          surahs: [],
        },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getSummary, getMentionedSurahs });

    facade.bindToRoute(
      activateRouteWith({ mode: 'tashkeel' }, { word: '42', view: 'surahs' }),
    );

    expect(facade.drilldownState().isOpen).toBe(true);
    expect(facade.drilldownState().selectedWordId).toBe(42);
    expect(facade.drilldownState().view).toBe('surahs');
    expect(facade.drilldownState().summary?.id).toBe(42);
    expect(getSummary).toHaveBeenCalledWith('tashkeel', 42);
  });

  it('normalizes an invalid view to the default surahs when a word is restored', () => {
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { surahs: [] },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getSummary, getMentionedSurahs });

    facade.bindToRoute(activateRouteWith({ mode: 'tashkeel' }, { word: '42', view: 'bogus' }));

    expect(facade.drilldownState().isOpen).toBe(true);
    expect(facade.drilldownState().view).toBe('surahs');
  });

  it('restores ayah page from ap when view is ayahs', () => {
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getAyahMatches = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { page: 2, pageSize: DEFAULT_AYAH_PAGE_SIZE, totalCount: 0, items: [] as UniqueWordAyahMatchDto[] },
      }),
    );
    const facade = setup({ getSummary, getAyahMatches });

    facade.bindToRoute(activateRouteWith({ mode: 'tashkeel' }, { word: '42', view: 'ayahs', ap: '2' }));

    expect(facade.drilldownState().view).toBe('ayahs');
    expect(facade.drilldownState().ayahPage).toBe(2);
    expect(getAyahMatches).toHaveBeenCalledWith('tashkeel', 42, 2, DEFAULT_AYAH_PAGE_SIZE);
  });

  it('reconciles same-word view and ayah-page changes from URL navigation', () => {
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { surahs: [] },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const getAyahMatches = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { page: 3, pageSize: DEFAULT_AYAH_PAGE_SIZE, totalCount: 0, items: [] as UniqueWordAyahMatchDto[] },
      }),
    );
    const facade = setup({ getSummary, getMentionedSurahs, getAyahMatches });
    const route = routeController({ mode: 'tashkeel' }, { word: '42', view: 'surahs' });

    facade.bindToRoute(route.route);
    route.setQueryParams({ word: '42', view: 'ayahs', ap: '3' });

    expect(facade.drilldownState().view).toBe('ayahs');
    expect(facade.drilldownState().ayahPage).toBe(3);
    expect(getSummary).toHaveBeenCalledTimes(1);
    expect(getAyahMatches).toHaveBeenCalledWith('tashkeel', 42, 3, DEFAULT_AYAH_PAGE_SIZE);
  });

  it('closes restored modal state when browser navigation removes the word param', () => {
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { surahs: [] },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getSummary, getMentionedSurahs });
    const route = routeController(
      { mode: 'tashkeel' },
      { search: 'اسم', page: '2', word: '42', view: 'surahs' },
    );

    facade.bindToRoute(route.route);
    expect(facade.drilldownState().isOpen).toBe(true);

    route.setQueryParams({ search: 'اسم', page: '2' });

    expect(facade.drilldownState().isOpen).toBe(false);
    expect(facade.drilldownState().selectedWordId).toBeNull();
    expect(facade.drilldownState().status).toBe('idle');
    expect(facade.listState().search).toBe('اسم');
    expect(facade.listState().page).toBe(2);
  });

  it('surfaces a controlled notFound state for an unknown word ID (backend isSuccess:false)', () => {
    const backendMessage = 'الكلمة غير موجودة';
    const getSummary = vi.fn(() =>
      of({ isSuccess: false, message: backendMessage, data: null } as ApiResponse<UniqueWordSummaryDto>),
    );
    const facade = setup({ getSummary });

    facade.bindToRoute(activateRouteWith({ mode: 'tashkeel' }, { word: '999999' }));

    expect(facade.drilldownState().status).toBe('notFound');
    expect(facade.drilldownState().isOpen).toBe(false);
    expect(facade.drilldownState().errorMessage).toBe(backendMessage);

    expect(facade.listState().status).not.toBe('error');
  });

  it('surfaces a controlled error state on summary transport failure', () => {
    const getSummary = vi.fn(() => throwError(() => new Error('network')));
    const facade = setup({ getSummary });

    facade.bindToRoute(activateRouteWith({ mode: 'tashkeel' }, { word: '999999' }));

    expect(facade.drilldownState().status).toBe('error');
    expect(facade.drilldownState().errorMessage).toBe(RESTORED_WORD_LOAD_ERROR_LABEL);
    expect(facade.drilldownState().isOpen).toBe(false);
  });

  it('surfaces a controlled notFound state on summary 404', () => {
    const getSummary = vi.fn(() =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 404,
            error: { isSuccess: false, message: RESTORED_WORD_NOT_FOUND_LABEL },
          }),
      ),
    );
    const facade = setup({ getSummary });

    facade.bindToRoute(activateRouteWith({ mode: 'tashkeel' }, { word: '999999' }));

    expect(facade.drilldownState().status).toBe('notFound');
    expect(facade.drilldownState().errorMessage).toBe(RESTORED_WORD_NOT_FOUND_LABEL);
    expect(facade.drilldownState().isOpen).toBe(false);
  });

  it('keeps the modal closed when no word param is present', () => {
    const getSummary = vi.fn();
    const facade = setup({ getSummary });

    facade.bindToRoute(activateRouteWith({ mode: 'tashkeel' }, { search: 'اسم', page: '2' }));

    expect(facade.drilldownState().isOpen).toBe(false);
    expect(getSummary).not.toHaveBeenCalled();
  });

  it('does not re-fetch the summary when openDrilldown reopens the restored word', () => {
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { surahs: [] },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getSummary, getMentionedSurahs });

    facade.bindToRoute(activateRouteWith({ mode: 'tashkeel' }, { word: '42', view: 'surahs' }));
    const callsAfterOpen = getSummary.mock.calls.length;

    facade.openDrilldown(word(42), 'surahs');
    expect(getSummary.mock.calls.length).toBe(callsAfterOpen);
  });

  it('does not re-fetch the summary when only list params change while a modal is open', () => {
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { surahs: [] },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getSummary, getMentionedSurahs });
    const route = routeController({ mode: 'tashkeel' }, { word: '42', view: 'surahs' });

    facade.bindToRoute(route.route);
    expect(getSummary).toHaveBeenCalledTimes(1);

    route.setQueryParams({ search: 'اسم', word: '42', view: 'surahs' });

    expect(getSummary).toHaveBeenCalledTimes(1);
    expect(facade.drilldownState().isOpen).toBe(true);
    expect(facade.drilldownState().selectedWordId).toBe(42);
  });

  it('does not reload the list when only modal params change', () => {
    const getList = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { page: 1, pageSize: DEFAULT_AYAH_PAGE_SIZE, totalCount: 0, items: [] },
      }),
    );
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { surahs: [] },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getList, getSummary, getMentionedSurahs });
    const route = routeController({ mode: 'tashkeel' }, {});

    facade.bindToRoute(route.route);
    expect(getList).toHaveBeenCalledTimes(1);

    route.setQueryParams({ word: '42', view: 'surahs' });

    expect(getList).toHaveBeenCalledTimes(1);
  });

  it('does not re-fetch the summary for a lingering unknown word on list-only changes', () => {
    const getSummary = vi.fn(() =>
      of({
        isSuccess: false,
        message: 'الكلمة غير موجودة',
        data: null,
      } as ApiResponse<UniqueWordSummaryDto>),
    );
    const facade = setup({ getSummary });
    const route = routeController({ mode: 'tashkeel' }, { word: '999999' });

    facade.bindToRoute(route.route);
    expect(getSummary).toHaveBeenCalledTimes(1);
    expect(facade.drilldownState().status).toBe('notFound');

    route.setQueryParams({ search: 'اسم', word: '999999' });

    expect(getSummary).toHaveBeenCalledTimes(1);
    expect(facade.drilldownState().status).toBe('notFound');
  });
});
