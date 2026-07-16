import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { StemsApi } from '../data-access/stems.api';
import { STEMS_ERROR_LABEL, STEMS_NOT_FOUND_LABEL } from '../models/stems.labels';
import {
  PagedResultDto,
  StemAyahMatchDto,
  StemSummaryDto,
  StemWordItemDto,
} from '../models/stems.models';
import { StemsCache, StemsCacheKeys } from './stems-cache';
import { StemsDetailController, StemsDetailUrlState } from './stems-detail.controller';
import { StemsDetailViewLoader } from './stems-detail-view.loader';

function summaryOf(id: number): StemSummaryDto {
  return {
    id,
    stemText: `كَاتِب-${id}`,
    occurrencesCount: 5,
    ayahsCount: 4,
    surahsCount: 3,
    simpleWordsCount: 2,
    tashkeelWordsCount: 2,
    lemmaId: null,
    lemmaText: null,
    rootId: null,
    rootText: null,
    typeDistribution: [],
  };
}

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function urlState(stemId: number, overrides: Partial<StemsDetailUrlState> = {}): StemsDetailUrlState {
  return {
    stemId,
    view: 'words',
    wordView: 'simple',
    surahView: 'mentioned',
    detailPage: 1,
    typeCode: null,
    ...overrides,
  };
}

interface StemsApiStub {
  getStemSummary: ReturnType<typeof vi.fn>;
}

function createController(options: {
  summary: (id: number) => Observable<ApiResponse<StemSummaryDto>>;
}): {
  controller: StemsDetailController;
  api: StemsApiStub;
  cache: StemsCache;
  loadActiveView: ReturnType<typeof vi.fn>;
} {
  const api: StemsApiStub = { getStemSummary: vi.fn((id: number) => options.summary(id)) };
  const cache = new StemsCache();
  const loadActiveView = vi.fn(() => undefined);
  const viewLoader = { loadActiveView } as unknown as StemsDetailViewLoader;
  const controller = new StemsDetailController(api as unknown as StemsApi, cache, viewLoader);

  return { controller, api, cache, loadActiveView };
}

describe('StemsDetailController (route-independent, Feature 029 B4)', () => {
  it('serves an already-loaded summary from StemsCache on identity re-apply without a second API read', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1));
    expect(controller.panelState().summary?.stemText).toBe('كَاتِب-1');

    controller.applyUrlState(null);
    expect(controller.panelState().status).toBe('idle');

    controller.applyUrlState(urlState(1));

    expect(api.getStemSummary).toHaveBeenCalledTimes(1);
    expect(controller.panelState().summary?.stemText).toBe('كَاتِب-1');
    expect(loadActiveView).toHaveBeenCalledTimes(2);
  });

  it('short-circuits a re-apply of the complete identical state', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 2, typeCode: 'V' }));
    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 2, typeCode: 'V' }));

    expect(api.getStemSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(1);
  });

  it('reuses the loaded summary on a same-stem sub-state change and reloads only the view', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1, { view: 'words' }));
    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 3 }));

    expect(api.getStemSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(2);
    expect(loadActiveView).toHaveBeenLastCalledWith(
      expect.objectContaining({ stemId: 1, view: 'ayahs', detailPage: 3 }),
      expect.anything(),
    );

    const panel = controller.panelState();
    expect(panel.summary?.id).toBe(1);
    expect(panel.view).toBe('ayahs');
    expect(panel.detailPage).toBe(3);
    expect(panel.status).toBe('loading');
  });

  it('treats a typeCode change as an identity change and reloads the ayahs view with the new filter', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 2, typeCode: null }));
    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 2, typeCode: 'V' }));

    expect(api.getStemSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(2);
    expect(loadActiveView).toHaveBeenLastCalledWith(
      expect.objectContaining({ stemId: 1, view: 'ayahs', detailPage: 2, ayahTypeCode: 'V' }),
      expect.anything(),
    );
    expect(controller.panelState().ayahTypeCode).toBe('V');
  });

  it('cancels a stale summary response so it cannot overwrite a newer identity', () => {
    const subjects = new Map<number, Subject<ApiResponse<StemSummaryDto>>>();
    const subjectFor = (id: number): Subject<ApiResponse<StemSummaryDto>> => {
      const existing = subjects.get(id) ?? new Subject<ApiResponse<StemSummaryDto>>();
      subjects.set(id, existing);
      return existing;
    };
    const { controller, loadActiveView } = createController({ summary: (id) => subjectFor(id).asObservable() });

    controller.applyUrlState(urlState(1));
    controller.applyUrlState(urlState(2));

    // The first stem's response arrives late — it must be a cancelled no-op.
    subjectFor(1).next(ok(summaryOf(1)));
    subjectFor(1).complete();

    expect(controller.panelState().selectedStemId).toBe(2);
    expect(controller.panelState().summary).toBeNull();
    expect(controller.panelState().status).toBe('loading');
    expect(loadActiveView).not.toHaveBeenCalled();

    subjectFor(2).next(ok(summaryOf(2)));
    subjectFor(2).complete();

    expect(controller.panelState().summary?.stemText).toBe('كَاتِب-2');
    expect(loadActiveView).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledWith(expect.objectContaining({ stemId: 2 }), expect.anything());
  });

  it('maps a 404 summary response to the distinct notFound state', () => {
    const { controller, loadActiveView } = createController({
      summary: () => throwError(() => new HttpErrorResponse({ status: 404 })),
    });

    controller.applyUrlState(urlState(77));

    const panel = controller.panelState();
    expect(panel.status).toBe('notFound');
    expect(panel.selectedStemId).toBe(77);
    expect(panel.errorMessage).toBe(STEMS_NOT_FOUND_LABEL);
    expect(loadActiveView).not.toHaveBeenCalled();
  });

  it('maps a transport failure to the distinct error state', () => {
    const { controller, loadActiveView } = createController({
      summary: () => throwError(() => new HttpErrorResponse({ status: 500 })),
    });

    controller.applyUrlState(urlState(77));

    const panel = controller.panelState();
    expect(panel.status).toBe('error');
    expect(panel.selectedStemId).toBe(77);
    expect(panel.errorMessage).toBe(STEMS_ERROR_LABEL);
    expect(loadActiveView).not.toHaveBeenCalled();
  });
});

describe('StemsDetailController cache keys (existing StemsCacheKeys)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  it('reads the summary and active words view through the existing StemsCacheKeys', () => {
    const wordsPage: PagedResultDto<StemWordItemDto> = {
      page: 2,
      pageSize: 100,
      totalCount: 101,
      items: [{ displayText: 'كاتب', occurrencesCount: 3, uniqueWordId: 11 }],
    };
    TestBed.configureTestingModule({
      providers: [
        {
          provide: StemsApi,
          useValue: {
            getStemSummary: vi.fn(() => of(ok(summaryOf(7)))),
            getStemWords: vi.fn(() => of(ok(wordsPage))),
          },
        },
      ],
    });

    const cache = TestBed.inject(StemsCache);
    const getOrLoad = vi.spyOn(cache, 'getOrLoad');
    const controller = new StemsDetailController(
      TestBed.inject(StemsApi),
      cache,
      TestBed.inject(StemsDetailViewLoader),
    );

    controller.applyUrlState(urlState(7, { view: 'words', wordView: 'tashkeel', detailPage: 2 }));

    const usedKeys = getOrLoad.mock.calls.map(([key]) => key);
    expect(usedKeys).toContain(StemsCacheKeys.summary(7));
    expect(usedKeys).toContain(StemsCacheKeys.words(7, 'tashkeel', 2));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().words?.items[0].displayText).toBe('كاتب');
  });

  it('keys the ayahs view by typeCode so a filter change is a distinct cache identity', () => {
    const ayahsPage: PagedResultDto<StemAyahMatchDto> = {
      page: 2,
      pageSize: 100,
      totalCount: 250,
      items: [
        {
          ayahId: 1,
          pageNumber: 2,
          surahNameArabic: 'البقرة',
          verseKey: '2:2',
          words: [{ textUthmani: 'ٱلْكِتَـٰبُ', isMatched: true }],
        },
      ],
    };
    TestBed.configureTestingModule({
      providers: [
        {
          provide: StemsApi,
          useValue: {
            getStemSummary: vi.fn(() => of(ok(summaryOf(7)))),
            getStemAyahMatches: vi.fn(() => of(ok(ayahsPage))),
          },
        },
      ],
    });

    const cache = TestBed.inject(StemsCache);
    const getOrLoad = vi.spyOn(cache, 'getOrLoad');
    const controller = new StemsDetailController(
      TestBed.inject(StemsApi),
      cache,
      TestBed.inject(StemsDetailViewLoader),
    );

    controller.applyUrlState(urlState(7, { view: 'ayahs', detailPage: 2, typeCode: 'V' }));

    const usedKeys = getOrLoad.mock.calls.map(([key]) => key);
    expect(usedKeys).toContain(StemsCacheKeys.summary(7));
    expect(usedKeys).toContain(StemsCacheKeys.ayahs(7, 2, 100, 'V'));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().ayahs?.totalCount).toBe(250);
  });
});
