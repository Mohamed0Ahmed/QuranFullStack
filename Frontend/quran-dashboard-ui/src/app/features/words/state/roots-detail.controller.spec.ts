import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { RootsApi } from '../data-access/roots.api';
import { ROOTS_ERROR_LABEL, ROOTS_NOT_FOUND_LABEL } from '../models/roots.labels';
import { PagedResultDto, RootSummaryDto, RootWordItemDto } from '../models/roots.models';
import { RootsCache, RootsCacheKeys } from './roots-cache';
import { RootsDetailController, RootsDetailUrlState } from './roots-detail.controller';
import { RootsDetailViewHandlers, RootsDetailViewLoader } from './roots-detail-view.loader';

/** Unmistakably synthetic, non-scriptural word rows for detail-response fixtures. */
function wordsPageOf(displayText: string): PagedResultDto<RootWordItemDto> {
  return {
    page: 1,
    pageSize: 100,
    totalCount: 1,
    items: [{ displayText, kind: 'simple', occurrencesCount: 1, uniqueWordId: 1 }],
  };
}

function summaryOf(id: number): RootSummaryDto {
  return {
    id,
    rootText: `جذر-${id}`,
    occurrencesCount: 5,
    ayahsCount: 4,
    surahsCount: 3,
    simpleWordsCount: 2,
    tashkeelWordsCount: 2,
    lemmasCount: 1,
    stemsCount: 1,
  };
}

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function urlState(rootId: number, overrides: Partial<RootsDetailUrlState> = {}): RootsDetailUrlState {
  return { rootId, view: 'words', wordView: 'simple', surahView: 'mentioned', detailPage: 1, ...overrides };
}

interface RootsApiStub {
  getRootSummary: ReturnType<typeof vi.fn>;
}

function createController(options: {
  summary: (id: number) => Observable<ApiResponse<RootSummaryDto>>;
}): {
  controller: RootsDetailController;
  api: RootsApiStub;
  cache: RootsCache;
  loadActiveView: ReturnType<typeof vi.fn>;
} {
  const api: RootsApiStub = { getRootSummary: vi.fn((id: number) => options.summary(id)) };
  const cache = new RootsCache();
  const loadActiveView = vi.fn(() => undefined);
  const viewLoader = { loadActiveView } as unknown as RootsDetailViewLoader;
  const controller = new RootsDetailController(api as unknown as RootsApi, cache, viewLoader);

  return { controller, api, cache, loadActiveView };
}

describe('RootsDetailController (route-independent, Feature 029 B4)', () => {
  it('serves an already-loaded summary from RootsCache on identity re-apply without a second API read', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1));
    expect(controller.panelState().summary?.rootText).toBe('جذر-1');

    controller.applyUrlState(null);
    expect(controller.panelState().status).toBe('idle');

    controller.applyUrlState(urlState(1));

    expect(api.getRootSummary).toHaveBeenCalledTimes(1);
    expect(controller.panelState().summary?.rootText).toBe('جذر-1');
    expect(loadActiveView).toHaveBeenCalledTimes(2);
  });

  it('short-circuits a re-apply of the complete identical state', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 2 }));
    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 2 }));

    expect(api.getRootSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(1);
  });

  it('reuses the loaded summary on a same-root sub-state change and reloads only the view', () => {
    const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

    controller.applyUrlState(urlState(1, { view: 'words' }));
    controller.applyUrlState(urlState(1, { view: 'ayahs', detailPage: 3 }));

    expect(api.getRootSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(2);
    expect(loadActiveView).toHaveBeenLastCalledWith(
      expect.objectContaining({ rootId: 1, view: 'ayahs', detailPage: 3 }),
      expect.anything(),
    );

    const panel = controller.panelState();
    expect(panel.summary?.id).toBe(1);
    expect(panel.view).toBe('ayahs');
    expect(panel.detailPage).toBe(3);
    expect(panel.status).toBe('loading');
  });

  it('cancels a stale summary response so it cannot overwrite a newer identity', () => {
    const subjects = new Map<number, Subject<ApiResponse<RootSummaryDto>>>();
    const subjectFor = (id: number): Subject<ApiResponse<RootSummaryDto>> => {
      const existing = subjects.get(id) ?? new Subject<ApiResponse<RootSummaryDto>>();
      subjects.set(id, existing);
      return existing;
    };
    const { controller, loadActiveView } = createController({ summary: (id) => subjectFor(id).asObservable() });

    controller.applyUrlState(urlState(1));
    controller.applyUrlState(urlState(2));

    // The first root's response arrives late — it must be a cancelled no-op.
    subjectFor(1).next(ok(summaryOf(1)));
    subjectFor(1).complete();

    expect(controller.panelState().selectedRootId).toBe(2);
    expect(controller.panelState().summary).toBeNull();
    expect(controller.panelState().status).toBe('loading');
    expect(loadActiveView).not.toHaveBeenCalled();

    subjectFor(2).next(ok(summaryOf(2)));
    subjectFor(2).complete();

    expect(controller.panelState().summary?.rootText).toBe('جذر-2');
    expect(loadActiveView).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledWith(expect.objectContaining({ rootId: 2 }), expect.anything());
  });

  it('maps a 404 summary response to the distinct notFound state', () => {
    const { controller, loadActiveView } = createController({
      summary: () => throwError(() => new HttpErrorResponse({ status: 404 })),
    });

    controller.applyUrlState(urlState(77));

    const panel = controller.panelState();
    expect(panel.status).toBe('notFound');
    expect(panel.selectedRootId).toBe(77);
    expect(panel.errorMessage).toBe(ROOTS_NOT_FOUND_LABEL);
    expect(loadActiveView).not.toHaveBeenCalled();
  });

  it('maps a transport failure to the distinct error state', () => {
    const { controller, loadActiveView } = createController({
      summary: () => throwError(() => new HttpErrorResponse({ status: 500 })),
    });

    controller.applyUrlState(urlState(77));

    const panel = controller.panelState();
    expect(panel.status).toBe('error');
    expect(panel.selectedRootId).toBe(77);
    expect(panel.errorMessage).toBe(ROOTS_ERROR_LABEL);
    expect(loadActiveView).not.toHaveBeenCalled();
  });

  describe('stale DETAIL responses across a root transition (Feature 030, C1)', () => {
    /**
     * Selects root 1 (summary resolves, so its detail load is registered and left
     * in flight), then selects root 2 whose summary stays pending. Returns root 1's
     * captured detail handlers so the test can land its response late.
     */
    function selectRootOneThenPendingRootTwo(): {
      controller: RootsDetailController;
      staleHandlers: RootsDetailViewHandlers;
      rootTwoSummary: Subject<ApiResponse<RootSummaryDto>>;
    } {
      const rootTwoSummary = new Subject<ApiResponse<RootSummaryDto>>();
      const { controller, loadActiveView } = createController({
        summary: (id) => (id === 1 ? of(ok(summaryOf(1))) : rootTwoSummary.asObservable()),
      });

      controller.applyUrlState(urlState(1));
      const staleHandlers = loadActiveView.mock.calls[0][1] as RootsDetailViewHandlers;

      controller.applyUrlState(urlState(2));
      expect(controller.panelState().selectedRootId).toBe(2);

      return { controller, staleHandlers, rootTwoSummary };
    }

    it('ignores the previous root detail response while the new root summary is pending', () => {
      const { controller, staleHandlers } = selectRootOneThenPendingRootTwo();

      staleHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.selectedRootId).toBe(2);
      expect(panel.words).toBeNull();
      expect(panel.status).toBe('loading');
    });

    it('ignores the previous root detail response after the new root summary succeeds', () => {
      const { controller, staleHandlers, rootTwoSummary } = selectRootOneThenPendingRootTwo();

      rootTwoSummary.next(ok(summaryOf(2)));
      rootTwoSummary.complete();
      staleHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.summary?.id).toBe(2);
      expect(panel.words).toBeNull();
    });

    it('ignores the previous root detail response after the new root summary 404s', () => {
      const { controller, staleHandlers, rootTwoSummary } = selectRootOneThenPendingRootTwo();

      rootTwoSummary.error(new HttpErrorResponse({ status: 404 }));
      expect(controller.panelState().status).toBe('notFound');

      staleHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.status).toBe('notFound');
      expect(panel.selectedRootId).toBe(2);
      expect(panel.words).toBeNull();
    });

    it('ignores the previous root detail response after the new root summary fails in transport', () => {
      const { controller, staleHandlers, rootTwoSummary } = selectRootOneThenPendingRootTwo();

      rootTwoSummary.error(new HttpErrorResponse({ status: 500 }));
      expect(controller.panelState().status).toBe('error');

      staleHandlers.onError(new HttpErrorResponse({ status: 503 }));
      staleHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.status).toBe('error');
      expect(panel.selectedRootId).toBe(2);
      expect(panel.errorMessage).toBe(ROOTS_ERROR_LABEL);
      expect(panel.words).toBeNull();
    });
  });

  describe('retryCurrentIdentity (Feature 030, M3)', () => {
    it('recovers from a summary transport error and loads the same identity on retry', () => {
      let attempt = 0;
      const { controller, loadActiveView } = createController({
        summary: (id) => {
          attempt += 1;
          return attempt === 1
            ? throwError(() => new HttpErrorResponse({ status: 500 }))
            : of(ok(summaryOf(id)));
        },
      });

      controller.applyUrlState(urlState(4));
      expect(controller.panelState().status).toBe('error');

      controller.retryCurrentIdentity();

      const panel = controller.panelState();
      expect(panel.status).toBe('loading');
      expect(panel.summary?.id).toBe(4);
      expect(loadActiveView).toHaveBeenCalledTimes(1);
      expect(loadActiveView).toHaveBeenCalledWith(expect.objectContaining({ rootId: 4 }), expect.anything());
    });

    it('recovers from a detail transport error by reloading the view without re-reading the summary', () => {
      const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

      controller.applyUrlState(urlState(4));
      const handlers = loadActiveView.mock.calls[0][1] as RootsDetailViewHandlers;
      handlers.onError(new HttpErrorResponse({ status: 500 }));
      expect(controller.panelState().status).toBe('error');

      controller.retryCurrentIdentity();
      const retryHandlers = loadActiveView.mock.calls[1][1] as RootsDetailViewHandlers;
      retryHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-٢')));

      const panel = controller.panelState();
      expect(panel.status).toBe('success');
      expect(panel.words?.items[0].displayText).toBe('كلمة-اختبار-٢');
      expect(api.getRootSummary).toHaveBeenCalledTimes(1);
      expect(loadActiveView).toHaveBeenCalledTimes(2);
    });

    it('is a no-op without a selected identity', () => {
      const { controller, api } = createController({ summary: (id) => of(ok(summaryOf(id))) });

      controller.retryCurrentIdentity();

      expect(api.getRootSummary).not.toHaveBeenCalled();
      expect(controller.panelState().status).toBe('idle');
    });
  });
});

describe('RootsDetailController cache keys (existing RootsCacheKeys)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  it('reads the summary and active view through the existing RootsCacheKeys', () => {
    const wordsPage: PagedResultDto<RootWordItemDto> = {
      page: 2,
      pageSize: 100,
      totalCount: 101,
      items: [{ displayText: 'كتاب', kind: 'tashkeel', occurrencesCount: 3, uniqueWordId: 11 }],
    };
    TestBed.configureTestingModule({
      providers: [
        {
          provide: RootsApi,
          useValue: {
            getRootSummary: vi.fn(() => of(ok(summaryOf(7)))),
            getRootWords: vi.fn(() => of(ok(wordsPage))),
          },
        },
      ],
    });

    const cache = TestBed.inject(RootsCache);
    const getOrLoad = vi.spyOn(cache, 'getOrLoad');
    const controller = new RootsDetailController(
      TestBed.inject(RootsApi),
      cache,
      TestBed.inject(RootsDetailViewLoader),
    );

    controller.applyUrlState(urlState(7, { view: 'words', wordView: 'tashkeel', detailPage: 2 }));

    const usedKeys = getOrLoad.mock.calls.map(([key]) => key);
    expect(usedKeys).toContain(RootsCacheKeys.summary(7));
    expect(usedKeys).toContain(RootsCacheKeys.words(7, 'tashkeel', 2));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().words?.items[0].displayText).toBe('كتاب');
  });
});
