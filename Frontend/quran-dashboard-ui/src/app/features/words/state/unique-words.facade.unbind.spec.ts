import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import { UniqueWordsFacade } from './unique-words.facade';
import { UniqueWordSummaryDto, UniqueWordSurahsDto } from '../models/unique-words.models';

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

    // The in-flight request finally resolves after the page/facade was torn down — it must be a no-op.
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

    // Returning to the exact same URL (same wordId/view/ayahPage) must re-drive a real reload,
    // not be swallowed by the "unchanged selection" fast path.
    facade.bindToRoute(route);

    expect(getSummary).toHaveBeenCalledTimes(2);
    expect(facade.drilldownState().isOpen).toBe(true);
    expect(facade.drilldownState().selectedWordId).toBe(42);
    expect(facade.drilldownState().summary?.id).toBe(42);
  });

  it('preserves detail cache behavior: a completed restore reloads from cache without a duplicate fetch', () => {
    const getSummary = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: summary(42) } as ApiResponse<UniqueWordSummaryDto>),
    );
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { surahs: [{ surahNumber: 1, nameArabic: 'سورة-تجريبية', occurrencesInSurah: 2 }] },
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

    // The selection had already resolved before unbind, so re-binding restores from the in-memory
    // drilldown/detail cache — no repeat network call for either the summary or the surahs detail.
    expect(getSummary).toHaveBeenCalledTimes(1);
    expect(getMentionedSurahs).toHaveBeenCalledTimes(1);
    expect(facade.drilldownState().isOpen).toBe(true);
    expect(facade.drilldownState().selectedWordId).toBe(42);
    expect(facade.drilldownState().surahs?.surahs).toHaveLength(1);
  });
});
