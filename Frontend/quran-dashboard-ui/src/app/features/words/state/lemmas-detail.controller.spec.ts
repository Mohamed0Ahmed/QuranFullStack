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
import { LemmasCache } from './lemmas-cache';
import { LemmasDetailController, LemmasDetailUrlState } from './lemmas-detail.controller';
import { LemmasDetailViewHandlers, LemmasDetailViewLoader } from './lemmas-detail-view.loader';

/** Unmistakably synthetic, non-scriptural word rows for detail-response fixtures. */
function wordsPageOf(displayText: string): PagedResultDto<LemmaWordItemDto> {
  return {
    page: 1,
    pageSize: 100,
    totalCount: 1,
    items: [{ displayText, occurrencesCount: 1, uniqueWordId: 1 }],
  };
}

/** Unmistakably synthetic, non-scriptural ayah-match rows. */
function ayahsPageOf(marker: string): PagedResultDto<LemmaAyahMatchDto> {
  return {
    page: 1,
    pageSize: 100,
    totalCount: 1,
    items: [
      {
        ayahId: 1,
        pageNumber: 1,
        surahNameArabic: 'سورة-اختبار',
        verseKey: '0:0',
        words: [{ textUthmani: marker, isMatched: true }],
      },
    ],
  };
}

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

  describe('stale DETAIL responses across a lemma transition (Feature 030, C1)', () => {
    function selectLemmaOneThenPendingLemmaTwo(): {
      controller: LemmasDetailController;
      staleHandlers: LemmasDetailViewHandlers;
      lemmaTwoSummary: Subject<ApiResponse<LemmaSummaryDto>>;
    } {
      const lemmaTwoSummary = new Subject<ApiResponse<LemmaSummaryDto>>();
      const { controller, loadActiveView } = createController({
        summary: (id) => (id === 1 ? of(ok(summaryOf(1))) : lemmaTwoSummary.asObservable()),
      });

      controller.applyUrlState(urlState(1));
      const staleHandlers = loadActiveView.mock.calls[0][1] as LemmasDetailViewHandlers;

      controller.applyUrlState(urlState(2));
      expect(controller.panelState().selectedLemmaId).toBe(2);

      return { controller, staleHandlers, lemmaTwoSummary };
    }

    it('ignores the previous lemma detail response while the new lemma summary is pending', () => {
      const { controller, staleHandlers } = selectLemmaOneThenPendingLemmaTwo();

      staleHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.selectedLemmaId).toBe(2);
      expect(panel.words).toBeNull();
      expect(panel.status).toBe('loading');
    });

    it('ignores the previous lemma detail response after the new lemma summary succeeds', () => {
      const { controller, staleHandlers, lemmaTwoSummary } = selectLemmaOneThenPendingLemmaTwo();

      lemmaTwoSummary.next(ok(summaryOf(2)));
      lemmaTwoSummary.complete();
      staleHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.summary?.id).toBe(2);
      expect(panel.words).toBeNull();
    });

    it('ignores the previous lemma detail response after the new lemma summary 404s', () => {
      const { controller, staleHandlers, lemmaTwoSummary } = selectLemmaOneThenPendingLemmaTwo();

      lemmaTwoSummary.error(new HttpErrorResponse({ status: 404 }));
      expect(controller.panelState().status).toBe('notFound');

      staleHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.status).toBe('notFound');
      expect(panel.selectedLemmaId).toBe(2);
      expect(panel.words).toBeNull();
    });

    it('ignores the previous lemma detail response after the new lemma summary fails in transport', () => {
      const { controller, staleHandlers, lemmaTwoSummary } = selectLemmaOneThenPendingLemmaTwo();

      lemmaTwoSummary.error(new HttpErrorResponse({ status: 500 }));
      expect(controller.panelState().status).toBe('error');

      staleHandlers.onError(new HttpErrorResponse({ status: 503 }));
      staleHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.status).toBe('error');
      expect(panel.selectedLemmaId).toBe(2);
      expect(panel.errorMessage).toBe(LEMMAS_ERROR_LABEL);
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
      expect(loadActiveView).toHaveBeenCalledWith(expect.objectContaining({ lemmaId: 4 }), expect.anything());
    });

    it('recovers from a detail transport error by reloading the view without re-reading the summary', () => {
      const { controller, api, loadActiveView } = createController({ summary: (id) => of(ok(summaryOf(id))) });

      controller.applyUrlState(urlState(4));
      const handlers = loadActiveView.mock.calls[0][1] as LemmasDetailViewHandlers;
      handlers.onError(new HttpErrorResponse({ status: 500 }));
      expect(controller.panelState().status).toBe('error');

      controller.retryCurrentIdentity();
      const retryHandlers = loadActiveView.mock.calls[1][1] as LemmasDetailViewHandlers;
      retryHandlers.onWords(ok(wordsPageOf('كلمة-اختبار-٢')));

      const panel = controller.panelState();
      expect(panel.status).toBe('success');
      expect(panel.words?.items[0].displayText).toBe('كلمة-اختبار-٢');
      expect(api.getLemmaSummary).toHaveBeenCalledTimes(1);
      expect(loadActiveView).toHaveBeenCalledTimes(2);
    });

    it('is a no-op without a selected identity', () => {
      const { controller, api } = createController({ summary: (id) => of(ok(summaryOf(id))) });

      controller.retryCurrentIdentity();

      expect(api.getLemmaSummary).not.toHaveBeenCalled();
      expect(controller.panelState().status).toBe('idle');
    });
  });
});

describe('LemmasDetailController identity/filter read reuse (real cache + view loader)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  function wireController(apiStub: object): LemmasDetailController {
    TestBed.configureTestingModule({ providers: [{ provide: LemmasApi, useValue: apiStub }] });
    return new LemmasDetailController(
      TestBed.inject(LemmasApi),
      TestBed.inject(LemmasCache),
      TestBed.inject(LemmasDetailViewLoader),
    );
  }

  it('reads an identity summary and words view once, then reuses both when the identity is re-applied', () => {
    const spies = {
      getLemmaSummary: vi.fn(() => of(ok(summaryOf(7)))),
      getLemmaWords: vi.fn(() => of(ok(wordsPageOf('كلمة-اختبار-٧')))),
    };
    const controller = wireController(spies);

    controller.applyUrlState(urlState(7, { view: 'words', wordView: 'tashkeel', detailPage: 2 }));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().words?.items[0].displayText).toBe('كلمة-اختبار-٧');

    controller.applyUrlState(null);
    controller.applyUrlState(urlState(7, { view: 'words', wordView: 'tashkeel', detailPage: 2 }));

    expect(spies.getLemmaSummary).toHaveBeenCalledTimes(1);
    expect(spies.getLemmaWords).toHaveBeenCalledTimes(1);
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().words?.items[0].displayText).toBe('كلمة-اختبار-٧');
  });

  it('reads the ayahs view once per typeCode filter and reuses each filter on return without cross-serving', () => {
    const spies = {
      getLemmaSummary: vi.fn(() => of(ok(summaryOf(7)))),
      getLemmaAyahMatches: vi.fn((_id: number, _page: number, _size: number, typeCode: string | null) =>
        of(ok(ayahsPageOf(typeCode ?? 'all'))),
      ),
    };
    const controller = wireController(spies);

    controller.applyUrlState(urlState(7, { view: 'ayahs', detailPage: 2, typeCode: null }));
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('all');

    controller.applyUrlState(urlState(7, { view: 'ayahs', detailPage: 2, typeCode: 'V' }));
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('V');
    expect(spies.getLemmaAyahMatches).toHaveBeenCalledTimes(2);

    controller.applyUrlState(urlState(7, { view: 'ayahs', detailPage: 2, typeCode: null }));
    expect(spies.getLemmaAyahMatches).toHaveBeenCalledTimes(2);
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('all');
    expect(spies.getLemmaSummary).toHaveBeenCalledTimes(1);
  });
});
