import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import { UniqueWordsFacade } from './unique-words.facade';
import {
  PagedResultDto,
  UniqueWordListItemDto,
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
} from '../models/unique-words.models';

function summary(id: number): UniqueWordSummaryDto {
  return {
    id,
    kind: 'tashkeel',
    displayText: `test-word-${id}`,
    occurrencesCount: 3,
    ayahsCount: 2,
    surahsCount: 2,
    missingSurahsCount: 5,
  };
}

function listItem(id: number): UniqueWordListItemDto {
  return {
    id,
    kind: 'tashkeel',
    displayText: `test-word-${id}`,
    occurrencesCount: 3,
    ayahsCount: 2,
    surahsCount: 2,
    missingSurahsCount: 5,
    primaryWordTypeBroadArabicLabel: null,
    primaryWordTypeCode: null,
    rootId: null,
    rootText: null,
  };
}

function listPage(items: UniqueWordListItemDto[]): ApiResponse<PagedResultDto<UniqueWordListItemDto>> {
  return {
    isSuccess: true,
    message: 'تم',
    data: { page: 1, pageSize: 1000, totalCount: items.length, items },
  };
}

function routeController(
  params: Record<string, string>,
  queryParams: Record<string, string>,
): ActivatedRoute {
  return {
    paramMap: new BehaviorSubject(convertToParamMap(params)).asObservable(),
    queryParamMap: new BehaviorSubject(convertToParamMap(queryParams)).asObservable(),
    snapshot: { paramMap: convertToParamMap(params), queryParamMap: convertToParamMap(queryParams) },
  } as unknown as ActivatedRoute;
}

function setup(api: Partial<UniqueWordsApi> = {}): UniqueWordsFacade {
  TestBed.configureTestingModule({
    providers: [
      UniqueWordsFacade,
      {
        provide: UniqueWordsApi,
        useValue: {
          getList: vi.fn(() =>
            of({ isSuccess: true, message: 'تم', data: { page: 1, pageSize: 1000, totalCount: 0, items: [] } }),
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

describe('UniqueWordsFacade unbind cancels drilldown work (perf finding F3)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('disposes an in-flight restored-summary subscription on unbind so a late response no longer mutates state', () => {
    const summarySubject = new Subject<ApiResponse<UniqueWordSummaryDto>>();
    const getSummary = vi.fn(() => summarySubject.asObservable());
    const facade = setup({ getSummary });
    const route = routeController({ mode: 'tashkeel' }, { word: '42', view: 'surahs' });

    facade.bindToRoute(route);
    expect(facade.drilldownState().status).toBe('loading');
    expect(facade.drilldownState().summary).toBeNull();

    facade.unbindFromRoute();

    summarySubject.next({ isSuccess: true, message: 'تم', data: summary(42) });

    expect(facade.drilldownState().summary).toBeNull();
    expect(facade.drilldownState().status).toBe('loading');
  });

  it('reloads cleanly when the same URL is bound again after an interrupted restore', () => {
    const getSummary = vi
      .fn()
      .mockReturnValueOnce(new Subject<ApiResponse<UniqueWordSummaryDto>>().asObservable())
      .mockReturnValueOnce(
        of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
      );
    const getMentionedSurahs = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: { surahs: [] } } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getSummary, getMentionedSurahs });
    const route = routeController({ mode: 'tashkeel' }, { word: '42', view: 'surahs' });

    facade.bindToRoute(route);
    expect(getSummary).toHaveBeenCalledTimes(1);

    facade.unbindFromRoute();

    facade.bindToRoute(route);

    expect(getSummary).toHaveBeenCalledTimes(2);
    expect(facade.drilldownState().isOpen).toBe(true);
    expect(facade.drilldownState().selectedWordId).toBe(42);
    expect(facade.drilldownState().summary?.id).toBe(42);
  });

  it('drops a manual list request on unbind so its late response cannot overwrite the next binding', () => {
    const staleList = new Subject<ApiResponse<PagedResultDto<UniqueWordListItemDto>>>();
    const getList = vi
      .fn()
      .mockReturnValueOnce(staleList.asObservable())
      .mockReturnValueOnce(of(listPage([listItem(7)])));
    const facade = setup({ getList });

    facade.bindToRoute(routeController({ mode: 'tashkeel' }, {}));
    facade.loadList();

    facade.unbindFromRoute();
    facade.bindToRoute(routeController({ mode: 'tashkeel' }, { sort: 'alpha' }));

    expect(facade.items()).toHaveLength(1);
    expect(facade.items()[0].id).toBe(7);

    staleList.next(listPage([listItem(1), listItem(2)]));

    expect(facade.items()).toHaveLength(1);
    expect(facade.items()[0].id).toBe(7);
  });

  it('does not reuse a held summary when the same word id is rebound in the other mode', () => {
    const getSummary = vi.fn((mode: string, id: number) =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { ...summary(id), kind: mode },
      } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: { surahs: [] } } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getSummary, getMentionedSurahs });

    facade.bindToRoute(routeController({ mode: 'tashkeel' }, { word: '42', view: 'surahs' }));
    expect(facade.drilldownState().summary?.kind).toBe('tashkeel');

    facade.unbindFromRoute();

    facade.bindToRoute(routeController({ mode: 'simple' }, { word: '42', view: 'surahs' }));

    expect(facade.drilldownState().summary?.kind).toBe('simple');
    expect(getSummary).toHaveBeenCalledTimes(2);
    expect(getSummary).toHaveBeenLastCalledWith('simple', 42);
    expect(getMentionedSurahs).toHaveBeenLastCalledWith('simple', 42);
  });

  it('preserves detail cache behavior: a completed restore reloads from cache without a duplicate fetch', () => {
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { surahs: [{ surahNumber: 1, nameArabic: 'test-surah-1', occurrencesInSurah: 2 }] },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getSummary, getMentionedSurahs });
    const route = routeController({ mode: 'tashkeel' }, { word: '42', view: 'surahs' });

    facade.bindToRoute(route);
    expect(getSummary).toHaveBeenCalledTimes(1);
    expect(getMentionedSurahs).toHaveBeenCalledTimes(1);
    expect(facade.drilldownState().surahs?.surahs).toHaveLength(1);

    facade.unbindFromRoute();
    facade.bindToRoute(route);

    expect(getSummary).toHaveBeenCalledTimes(1);
    expect(getMentionedSurahs).toHaveBeenCalledTimes(1);
    expect(facade.drilldownState().isOpen).toBe(true);
    expect(facade.drilldownState().selectedWordId).toBe(42);
    expect(facade.drilldownState().surahs?.surahs).toHaveLength(1);
  });
});
