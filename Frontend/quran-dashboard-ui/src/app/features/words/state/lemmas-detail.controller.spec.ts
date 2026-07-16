import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LemmasApi } from '../data-access/lemmas.api';
import { LEMMAS_ERROR_LABEL, LEMMAS_NOT_FOUND_LABEL } from '../models/lemmas.labels';
import {
  LemmaAyahMatchDto,
  LemmaSummaryDto,
  LemmaWordItemDto,
  PagedResultDto,
} from '../models/lemmas.models';
import { LemmasCache, LemmasCacheKeys } from './lemmas-cache';
import { LemmasDetailController, LemmasDetailUrlState } from './lemmas-detail.controller';
import { LemmasDetailViewLoader } from './lemmas-detail-view.loader';

function summaryOf(id: number): LemmaSummaryDto {
  return {
    id,
    lemmaText: `كِتَاب-${id}`,
    occurrencesCount: 5,
    ayahsCount: 4,
    surahsCount: 3,
    simpleWordsCount: 2,
    tashkeelWordsCount: 2,
    stemsCount: 1,
    rootId: null,
    rootText: null,
    typeDistribution: [],
  };
}

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function urlState(lemmaId: number, overrides: Partial<LemmasDetailUrlState> = {}): LemmasDetailUrlState {
  return {
    lemmaId,
    view: 'words',
    wordView: 'simple',
    surahView: 'mentioned',
    detailPage: 1,
    typeCode: null,
    ...overrides,
  };
}

interface LemmasApiStub {
  getLemmaSummary: ReturnType<typeof vi.fn>;
}

function createController(options: {
  summary: (id: number) => Observable<ApiResponse<LemmaSummaryDto>>;
}): {
  controller: LemmasDetailController;
  api: LemmasApiStub;
  cache: LemmasCache;
  loadActiveView: ReturnType<typeof vi.fn>;
} {
  const api: LemmasApiStub = { getLemmaSummary: vi.fn((id: number) => options.summary(id)) };
  const cache = new LemmasCache();
  const loadActiveView = vi.fn(() => undefined);
  const viewLoader = { loadActiveView } as unknown as LemmasDetailViewLoader;
  const controller = new LemmasDetailController(api as unknown as LemmasApi, cache, viewLoader);

  return { controller, api, cache, loadActiveView };
}

describe('LemmasDetailController (route-independent, Feature 029 B4)', () => {
  it('serves an already-loaded summary from LemmasCache on identity re-apply without a second API read', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1));
    expect(controller.panelState().summary?.lemmaText).toBe('كِتَاب-1');

    controller.applyUrlState(null);
    expect(controller.panelState().status).toBe('idle');

    controller.applyUrlState(urlState(1));

    expect(api.getLemmaSummary).toHaveBeenCalledTimes(1);
    expect(controller.panelState().summary?.lemmaText).toBe('كِتَاب-1');
    expect(loadActiveView).toHaveBeenCalledTimes(2);
  });

  it('short-circuits a re-apply of the complete identical state', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 2, typeCode: 'V' }));
    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 2, typeCode: 'V' }));

    expect(api.getLemmaSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(1);
  });

  it('reuses the loaded summary on a same-lemma sub-state change and reloads only the view', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1, { view: 'words' }));
    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 3 }));

    expect(api.getLemmaSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(2);
    expect(loadActiveView).toHaveBeenLastCalledWith(
      expect.objectContaining({ lemmaId: 1, view: 'ayahs', detailPage: 3 }),
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

    expect(api.getLemmaSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(2);
    expect(loadActiveView).toHaveBeenLastCalledWith(
      expect.objectContaining({ lemmaId: 1, view: 'ayahs', detailPage: 2, ayahTypeCode: 'V' }),
      expect.anything(),
    );
    expect(controller.panelState().ayahTypeCode).toBe('V');
  });

  it('cancels a stale summary response so it cannot overwrite a newer identity', () => {
    const subjects = new Map<number, Subject<ApiResponse<LemmaSummaryDto>>>();
    const subjectFor = (id: number): Subject<ApiResponse<LemmaSummaryDto>> => {
      const existing = subjects.get(id) ?? new Subject<ApiResponse<LemmaSummaryDto>>();
      subjects.set(id, existing);
      return existing;
    };
    const { controller, loadActiveView } = createController({ summary: (id) => subjectFor(id).asObservable() });

    controller.applyUrlState(urlState(1));
    controller.applyUrlState(urlState(2));

    // The first lemma's response arrives late — it must be a cancelled no-op.
    subjectFor(1).next(ok(summaryOf(1)));
    subjectFor(1).complete();

    expect(controller.panelState().selectedLemmaId).toBe(2);
    expect(controller.panelState().summary).toBeNull();
    expect(controller.panelState().status).toBe('loading');
    expect(loadActiveView).not.toHaveBeenCalled();

    subjectFor(2).next(ok(summaryOf(2)));
    subjectFor(2).complete();

    expect(controller.panelState().summary?.lemmaText).toBe('كِتَاب-2');
    expect(loadActiveView).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledWith(expect.objectContaining({ lemmaId: 2 }), expect.anything());
  });

  it('maps a 404 summary response to the distinct notFound state', () => {
    const { controller, loadActiveView } = createController({
      summary: () => throwError(() => new HttpErrorResponse({ status: 404 })),
    });

    controller.applyUrlState(urlState(77));

    const panel = controller.panelState();
    expect(panel.status).toBe('notFound');
    expect(panel.selectedLemmaId).toBe(77);
    expect(panel.errorMessage).toBe(LEMMAS_NOT_FOUND_LABEL);
    expect(loadActiveView).not.toHaveBeenCalled();
  });

  it('maps a transport failure to the distinct error state', () => {
    const { controller, loadActiveView } = createController({
      summary: () => throwError(() => new HttpErrorResponse({ status: 500 })),
    });

    controller.applyUrlState(urlState(77));

    const panel = controller.panelState();
    expect(panel.status).toBe('error');
    expect(panel.selectedLemmaId).toBe(77);
    expect(panel.errorMessage).toBe(LEMMAS_ERROR_LABEL);
    expect(loadActiveView).not.toHaveBeenCalled();
  });
});

describe('LemmasDetailController cache keys (existing LemmasCacheKeys)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  it('reads the summary and active words view through the existing LemmasCacheKeys', () => {
    const wordsPage: PagedResultDto<LemmaWordItemDto> = {
      page: 2,
      pageSize: 100,
      totalCount: 101,
      items: [{ displayText: 'كتاب', occurrencesCount: 3, uniqueWordId: 11 }],
    };
    TestBed.configureTestingModule({
      providers: [
        {
          provide: LemmasApi,
          useValue: {
            getLemmaSummary: vi.fn(() => of(ok(summaryOf(7)))),
            getLemmaWords: vi.fn(() => of(ok(wordsPage))),
          },
        },
      ],
    });

    const cache = TestBed.inject(LemmasCache);
    const getOrLoad = vi.spyOn(cache, 'getOrLoad');
    const controller = new LemmasDetailController(
      TestBed.inject(LemmasApi),
      cache,
      TestBed.inject(LemmasDetailViewLoader),
    );

    controller.applyUrlState(urlState(7, { view: 'words', wordView: 'tashkeel', detailPage: 2 }));

    const usedKeys = getOrLoad.mock.calls.map(([key]) => key);
    expect(usedKeys).toContain(LemmasCacheKeys.summary(7));
    expect(usedKeys).toContain(LemmasCacheKeys.words(7, 'tashkeel', 2));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().words?.items[0].displayText).toBe('كتاب');
  });

  it('keys the ayahs view by typeCode so a filter change is a distinct cache identity', () => {
    const ayahsPage: PagedResultDto<LemmaAyahMatchDto> = {
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
          provide: LemmasApi,
          useValue: {
            getLemmaSummary: vi.fn(() => of(ok(summaryOf(7)))),
            getLemmaAyahMatches: vi.fn(() => of(ok(ayahsPage))),
          },
        },
      ],
    });

    const cache = TestBed.inject(LemmasCache);
    const getOrLoad = vi.spyOn(cache, 'getOrLoad');
    const controller = new LemmasDetailController(
      TestBed.inject(LemmasApi),
      cache,
      TestBed.inject(LemmasDetailViewLoader),
    );

    controller.applyUrlState(urlState(7, { view: 'ayahs', detailPage: 2, typeCode: 'V' }));

    const usedKeys = getOrLoad.mock.calls.map(([key]) => key);
    expect(usedKeys).toContain(LemmasCacheKeys.summary(7));
    expect(usedKeys).toContain(LemmasCacheKeys.ayahs(7, 2, 100, 'V'));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().ayahs?.totalCount).toBe(250);
  });
});
