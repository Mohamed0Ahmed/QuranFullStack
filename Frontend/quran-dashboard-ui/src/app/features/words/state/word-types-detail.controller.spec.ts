import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypesApi } from '../data-access/word-types.api';
import { WORD_TYPES_ERROR_LABEL, WORD_TYPES_NOT_FOUND_LABEL } from '../models/word-types.labels';
import {
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeRowIdentity,
  WordTypeSummaryDto,
  WordTypeSurahsResponseDto,
} from '../models/word-types.models';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';
import { WordTypesDetailController, WordTypesWordDetailUrlState } from './word-types-detail.controller';
import { WordTypesDetailViewHandlers, WordTypesDetailViewLoader } from './word-types-detail-view.loader';

function identityOf(overrides: Partial<WordTypeRowIdentity> = {}): WordTypeRowIdentity {
  return { tashkeelWordId: 7, contextCode: 'noun', case: 'all', tense: 'all', voice: 'all', ...overrides };
}

/** Unmistakably synthetic, non-scriptural ayah-match rows for detail-response fixtures. */
function ayahsPageOf(wordText: string): PagedResultDto<WordTypeAyahMatchDto> {
  return {
    page: 1,
    pageSize: 100,
    totalCount: 1,
    items: [
      {
        ayahNumber: 1,
        verseKey: '1:1',
        pageNumber: 1,
        surahNumber: 1,
        matchedWordIds: [1],
        matchedWordPositions: [1],
        words: [{ quranWordId: 1, textUthmani: wordText, isAyahMarker: false }],
      },
    ],
  };
}

function summaryOf(identity: WordTypeRowIdentity): WordTypeSummaryDto {
  return {
    ...identity,
    displayText: `كلمة-${identity.tashkeelWordId}-${identity.contextCode}-${identity.case}-${identity.tense}-${identity.voice}`,
    typeCode: identity.contextCode,
    typeLabel: { ar: 'اسم' },
    broadLabel: { ar: 'اسم' },
    caseOrFeature: null,
    rootText: null,
    lemmaText: null,
    stemText: null,
    occurrencesCount: 5,
    ayahsCount: 4,
    surahsCount: 3,
  };
}

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function urlState(
  identity: WordTypeRowIdentity,
  overrides: Partial<Omit<WordTypesWordDetailUrlState, 'identity'>> = {},
): WordTypesWordDetailUrlState {
  return { identity, view: 'ayahs', detailPage: 1, ...overrides };
}

interface WordTypesApiStub {
  getSummary: ReturnType<typeof vi.fn>;
}

function createController(options: {
  summary: (identity: WordTypeRowIdentity) => Observable<ApiResponse<WordTypeSummaryDto>>;
}): {
  controller: WordTypesDetailController;
  api: WordTypesApiStub;
  cache: WordTypesCache;
  loadActiveView: ReturnType<typeof vi.fn>;
} {
  const api: WordTypesApiStub = {
    getSummary: vi.fn((identity: WordTypeRowIdentity) => options.summary(identity)),
  };
  const cache = new WordTypesCache();
  const loadActiveView = vi.fn(() => undefined);
  const viewLoader = { loadActiveView } as unknown as WordTypesDetailViewLoader;
  const controller = new WordTypesDetailController(api as unknown as WordTypesApi, cache, viewLoader);

  return { controller, api, cache, loadActiveView };
}

describe('WordTypesDetailController (route-independent, Feature 029 B4)', () => {
  it('serves an already-loaded summary from WordTypesCache on identity re-apply without a second API read', () => {
    const { controller, api, loadActiveView } = createController({
      summary: (identity) => of(ok(summaryOf(identity))),
    });
    const identity = identityOf();

    controller.applyUrlState(urlState(identity));
    expect(controller.panelState().summary?.displayText).toBe(summaryOf(identity).displayText);

    controller.applyUrlState(null);
    expect(controller.panelState().status).toBe('idle');

    controller.applyUrlState(urlState(identity));

    expect(api.getSummary).toHaveBeenCalledTimes(1);
    expect(controller.panelState().summary?.displayText).toBe(summaryOf(identity).displayText);
    expect(loadActiveView).toHaveBeenCalledTimes(2);
  });

  it('short-circuits a re-apply of the complete identical state', () => {
    const { controller, api, loadActiveView } = createController({
      summary: (identity) => of(ok(summaryOf(identity))),
    });

    controller.applyUrlState(urlState(identityOf(), { view: 'ayahs', detailPage: 2 }));
    controller.applyUrlState(urlState(identityOf(), { view: 'ayahs', detailPage: 2 }));

    expect(api.getSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(1);
  });

  it('reuses the loaded summary on a same-identity sub-state change and reloads only the view', () => {
    const { controller, api, loadActiveView } = createController({
      summary: (identity) => of(ok(summaryOf(identity))),
    });
    const identity = identityOf();

    controller.applyUrlState(urlState(identity, { view: 'ayahs', detailPage: 1 }));
    controller.applyUrlState(urlState(identity, { view: 'surahs', detailPage: 1 }));

    expect(api.getSummary).toHaveBeenCalledTimes(1);
    expect(loadActiveView).toHaveBeenCalledTimes(2);
    expect(loadActiveView).toHaveBeenLastCalledWith(
      expect.objectContaining({ view: 'surahs', detailPage: 1 }),
      expect.anything(),
    );

    const panel = controller.panelState();
    expect(panel.summary?.tashkeelWordId).toBe(identity.tashkeelWordId);
    expect(panel.view).toBe('surahs');
    expect(panel.status).toBe('loading');
  });

  it('treats the full composite identity as the identity: same word id with a different case/tense/voice reloads and never cross-serves', () => {
    const { controller, api, loadActiveView } = createController({
      summary: (identity) => of(ok(summaryOf(identity))),
    });
    const nominal = identityOf({ case: 'all' });
    const accusative = identityOf({ case: 'accusative' });

    controller.applyUrlState(urlState(nominal));
    expect(controller.panelState().summary?.displayText).toBe(summaryOf(nominal).displayText);

    controller.applyUrlState(urlState(accusative));

    expect(api.getSummary).toHaveBeenCalledTimes(2);
    expect(api.getSummary).toHaveBeenLastCalledWith(accusative);
    expect(controller.panelState().summary?.displayText).toBe(summaryOf(accusative).displayText);
    expect(controller.panelState().selectedRow).toEqual(accusative);
    expect(loadActiveView).toHaveBeenCalledTimes(2);
  });

  it('treats a different contextCode for the same word id as a different identity', () => {
    const { controller, api } = createController({
      summary: (identity) => of(ok(summaryOf(identity))),
    });

    controller.applyUrlState(urlState(identityOf({ contextCode: 'noun' })));
    controller.applyUrlState(urlState(identityOf({ contextCode: 'past' })));

    expect(api.getSummary).toHaveBeenCalledTimes(2);
    expect(controller.panelState().summary?.displayText).toBe(
      summaryOf(identityOf({ contextCode: 'past' })).displayText,
    );
  });

  it('cancels a stale summary response so it cannot overwrite a newer identity', () => {
    const subjects = new Map<string, Subject<ApiResponse<WordTypeSummaryDto>>>();
    const subjectFor = (identity: WordTypeRowIdentity): Subject<ApiResponse<WordTypeSummaryDto>> => {
      const key = WordTypesCacheKeys.summary(identity);
      const existing = subjects.get(key) ?? new Subject<ApiResponse<WordTypeSummaryDto>>();
      subjects.set(key, existing);
      return existing;
    };
    const { controller, loadActiveView } = createController({
      summary: (identity) => subjectFor(identity).asObservable(),
    });
    const first = identityOf({ tashkeelWordId: 1 });
    const second = identityOf({ tashkeelWordId: 2 });

    controller.applyUrlState(urlState(first));
    controller.applyUrlState(urlState(second));

    // The first identity's response arrives late — it must be a cancelled no-op.
    subjectFor(first).next(ok(summaryOf(first)));
    subjectFor(first).complete();

    expect(controller.panelState().selectedRow).toEqual(second);
    expect(controller.panelState().summary).toBeNull();
    expect(controller.panelState().status).toBe('loading');
    expect(loadActiveView).not.toHaveBeenCalled();

    subjectFor(second).next(ok(summaryOf(second)));
    subjectFor(second).complete();

    expect(controller.panelState().summary?.displayText).toBe(summaryOf(second).displayText);
    expect(loadActiveView).toHaveBeenCalledTimes(1);
  });

  it('maps a 404 summary response to the distinct notFound state', () => {
    const { controller, loadActiveView } = createController({
      summary: () => throwError(() => new HttpErrorResponse({ status: 404 })),
    });
    const identity = identityOf({ tashkeelWordId: 77 });

    controller.applyUrlState(urlState(identity));

    const panel = controller.panelState();
    expect(panel.status).toBe('notFound');
    expect(panel.selectedRow).toEqual(identity);
    expect(panel.errorMessage).toBe(WORD_TYPES_NOT_FOUND_LABEL);
    expect(loadActiveView).not.toHaveBeenCalled();
  });

  it('maps a transport failure to the distinct error state', () => {
    const { controller, loadActiveView } = createController({
      summary: () => throwError(() => new HttpErrorResponse({ status: 500 })),
    });
    const identity = identityOf({ tashkeelWordId: 77 });

    controller.applyUrlState(urlState(identity));

    const panel = controller.panelState();
    expect(panel.status).toBe('error');
    expect(panel.selectedRow).toEqual(identity);
    expect(panel.errorMessage).toBe(WORD_TYPES_ERROR_LABEL);
    expect(loadActiveView).not.toHaveBeenCalled();
  });

  describe('stale DETAIL responses across a composite word identity transition (Feature 030, C1)', () => {
    // The SAME tashkeel word id under two different contexts: two distinct composite
    // identities that must never cross-serve each other's detail responses.
    const nounContext = identityOf({ tashkeelWordId: 5, contextCode: 'noun' });
    const verbContext = identityOf({ tashkeelWordId: 5, contextCode: 'past' });

    /**
     * Selects the noun-context row (summary resolves, so its detail load is
     * registered and left in flight), then selects the same word id under the verb
     * context, whose summary stays pending. Returns the noun row's captured detail
     * handlers so the test can land its response late.
     */
    function selectNounContextThenPendingVerbContext(): {
      controller: WordTypesDetailController;
      staleHandlers: WordTypesDetailViewHandlers;
      verbContextSummary: Subject<ApiResponse<WordTypeSummaryDto>>;
    } {
      const verbContextSummary = new Subject<ApiResponse<WordTypeSummaryDto>>();
      const { controller, loadActiveView } = createController({
        summary: (identity) =>
          identity.contextCode === 'noun' ? of(ok(summaryOf(nounContext))) : verbContextSummary.asObservable(),
      });

      controller.applyUrlState(urlState(nounContext));
      const staleHandlers = loadActiveView.mock.calls[0][1] as WordTypesDetailViewHandlers;

      controller.applyUrlState(urlState(verbContext));
      expect(controller.panelState().selectedRow).toEqual(verbContext);

      return { controller, staleHandlers, verbContextSummary };
    }

    it('ignores the previous identity detail response while the new identity summary is pending', () => {
      const { controller, staleHandlers } = selectNounContextThenPendingVerbContext();

      staleHandlers.onAyahs(ok(ayahsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.selectedRow).toEqual(verbContext);
      expect(panel.ayahs).toBeNull();
      expect(panel.status).toBe('loading');
    });

    it('ignores the previous identity detail response after the new identity summary succeeds', () => {
      const { controller, staleHandlers, verbContextSummary } = selectNounContextThenPendingVerbContext();

      verbContextSummary.next(ok(summaryOf(verbContext)));
      verbContextSummary.complete();
      staleHandlers.onAyahs(ok(ayahsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.summary?.displayText).toBe(summaryOf(verbContext).displayText);
      expect(panel.ayahs).toBeNull();
    });

    it('ignores the previous identity detail response after the new identity summary 404s', () => {
      const { controller, staleHandlers, verbContextSummary } = selectNounContextThenPendingVerbContext();

      verbContextSummary.error(new HttpErrorResponse({ status: 404 }));
      expect(controller.panelState().status).toBe('notFound');

      staleHandlers.onAyahs(ok(ayahsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.status).toBe('notFound');
      expect(panel.selectedRow).toEqual(verbContext);
      expect(panel.ayahs).toBeNull();
    });

    it('ignores the previous identity detail response after the new identity summary fails in transport', () => {
      const { controller, staleHandlers, verbContextSummary } = selectNounContextThenPendingVerbContext();

      verbContextSummary.error(new HttpErrorResponse({ status: 500 }));
      expect(controller.panelState().status).toBe('error');

      staleHandlers.onError(new HttpErrorResponse({ status: 503 }));
      staleHandlers.onAyahs(ok(ayahsPageOf('كلمة-اختبار-١')));

      const panel = controller.panelState();
      expect(panel.status).toBe('error');
      expect(panel.selectedRow).toEqual(verbContext);
      expect(panel.errorMessage).toBe(WORD_TYPES_ERROR_LABEL);
      expect(panel.ayahs).toBeNull();
    });
  });

  describe('retryCurrentIdentity (Feature 030, M3)', () => {
    it('recovers from a summary transport error and loads the same identity on retry', () => {
      let attempt = 0;
      const { controller, loadActiveView } = createController({
        summary: (identity) => {
          attempt += 1;
          return attempt === 1
            ? throwError(() => new HttpErrorResponse({ status: 500 }))
            : of(ok(summaryOf(identity)));
        },
      });
      const identity = identityOf({ tashkeelWordId: 4 });

      controller.applyUrlState(urlState(identity));
      expect(controller.panelState().status).toBe('error');

      controller.retryCurrentIdentity();

      const panel = controller.panelState();
      expect(panel.status).toBe('loading');
      expect(panel.summary?.displayText).toBe(summaryOf(identity).displayText);
      expect(loadActiveView).toHaveBeenCalledTimes(1);
      expect(loadActiveView).toHaveBeenCalledWith(
        expect.objectContaining({ view: 'ayahs', detailPage: 1 }),
        expect.anything(),
      );
    });

    it('recovers from a detail transport error by reloading the view without re-reading the summary', () => {
      const { controller, api, loadActiveView } = createController({
        summary: (identity) => of(ok(summaryOf(identity))),
      });

      controller.applyUrlState(urlState(identityOf({ tashkeelWordId: 4 })));
      const handlers = loadActiveView.mock.calls[0][1] as WordTypesDetailViewHandlers;
      handlers.onError(new HttpErrorResponse({ status: 500 }));
      expect(controller.panelState().status).toBe('error');

      controller.retryCurrentIdentity();
      const retryHandlers = loadActiveView.mock.calls[1][1] as WordTypesDetailViewHandlers;
      retryHandlers.onAyahs(ok(ayahsPageOf('كلمة-اختبار-٢')));

      const panel = controller.panelState();
      expect(panel.status).toBe('success');
      expect(panel.ayahs?.items[0].words[0].textUthmani).toBe('كلمة-اختبار-٢');
      expect(api.getSummary).toHaveBeenCalledTimes(1);
      expect(loadActiveView).toHaveBeenCalledTimes(2);
    });

    it('is a no-op without a selected identity', () => {
      const { controller, api } = createController({ summary: (identity) => of(ok(summaryOf(identity))) });

      controller.retryCurrentIdentity();

      expect(api.getSummary).not.toHaveBeenCalled();
      expect(controller.panelState().status).toBe('idle');
    });
  });
});

describe('WordTypesDetailController identity/page read isolation (real cache + view loader)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  /**
   * Wires a controller onto the REAL WordTypesCache and WordTypesDetailViewLoader
   * the page panel and overlay share. Colliding composite identities and distinct
   * pages must never cross-serve each other's reads, and a re-applied identity/page
   * is served from cache — asserted through response ISOLATION and API call counts
   * rather than the internal cache-key strings (key formatting is a cache concern).
   */
  function wireController(apiStub: object): WordTypesDetailController {
    TestBed.configureTestingModule({ providers: [{ provide: WordTypesApi, useValue: apiStub }] });
    return new WordTypesDetailController(
      TestBed.inject(WordTypesApi),
      TestBed.inject(WordTypesCache),
      TestBed.inject(WordTypesDetailViewLoader),
    );
  }

  it('isolates two colliding composite identities (same word id, different case) across summary and ayahs', () => {
    const nominal = identityOf({ tashkeelWordId: 5, case: 'all' });
    const accusative = identityOf({ tashkeelWordId: 5, case: 'accusative' });
    const spies = {
      getSummary: vi.fn((identity: WordTypeRowIdentity) => of(ok(summaryOf(identity)))),
      getAyahMatches: vi.fn((identity: WordTypeRowIdentity) => of(ok(ayahsPageOf(`كلمة-اختبار-${identity.case}`)))),
    };
    const controller = wireController(spies);

    controller.applyUrlState(urlState(nominal, { view: 'ayahs', detailPage: 1 }));
    expect(controller.panelState().summary?.displayText).toBe(summaryOf(nominal).displayText);
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('كلمة-اختبار-all');

    controller.applyUrlState(urlState(accusative, { view: 'ayahs', detailPage: 1 }));
    expect(controller.panelState().summary?.displayText).toBe(summaryOf(accusative).displayText);
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('كلمة-اختبار-accusative');
    expect(spies.getSummary).toHaveBeenCalledTimes(2);
    expect(spies.getAyahMatches).toHaveBeenCalledTimes(2);

    // Return to the first identity: both reads come from cache and never cross-serve the second.
    controller.applyUrlState(urlState(nominal, { view: 'ayahs', detailPage: 1 }));
    expect(spies.getSummary).toHaveBeenCalledTimes(2);
    expect(spies.getAyahMatches).toHaveBeenCalledTimes(2);
    expect(controller.panelState().summary?.displayText).toBe(summaryOf(nominal).displayText);
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('كلمة-اختبار-all');
  });

  it('isolates ayahs pages for one identity and reuses a page already read', () => {
    const identity = identityOf({ tashkeelWordId: 9, contextCode: 'past', case: 'null', tense: 'past', voice: 'passive' });
    const spies = {
      getSummary: vi.fn(() => of(ok(summaryOf(identity)))),
      getAyahMatches: vi.fn((_identity: WordTypeRowIdentity, page: number) =>
        of(ok(ayahsPageOf(`كلمة-اختبار-p${page}`))),
      ),
    };
    const controller = wireController(spies);

    controller.applyUrlState(urlState(identity, { view: 'ayahs', detailPage: 1 }));
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('كلمة-اختبار-p1');

    controller.applyUrlState(urlState(identity, { view: 'ayahs', detailPage: 2 }));
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('كلمة-اختبار-p2');
    expect(spies.getAyahMatches).toHaveBeenCalledTimes(2);

    // Page 1 was already read: returning to it reuses its cache, never page 2's data.
    controller.applyUrlState(urlState(identity, { view: 'ayahs', detailPage: 1 }));
    expect(spies.getAyahMatches).toHaveBeenCalledTimes(2);
    expect(controller.panelState().ayahs?.items[0].words[0].textUthmani).toBe('كلمة-اختبار-p1');
    expect(spies.getSummary).toHaveBeenCalledTimes(1);
  });

  it('reads the single-shot surahs view once and reuses it on return', () => {
    const identity = identityOf({ tashkeelWordId: 11, contextCode: 'noun', case: 'genitive' });
    const surahs: WordTypeSurahsResponseDto = {
      surahs: [{ surahNumber: 1, nameArabic: 'سورة-اختبار', occurrencesCount: 2 }],
      missingSurahs: [{ surahNumber: 9, nameArabic: 'سورة-اختبار-٢' }],
    };
    const spies = {
      getSummary: vi.fn(() => of(ok(summaryOf(identity)))),
      getSurahs: vi.fn(() => of(ok(surahs))),
    };
    const controller = wireController(spies);

    controller.applyUrlState(urlState(identity, { view: 'surahs', detailPage: 1 }));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().surahs?.surahs).toHaveLength(1);
    expect(controller.panelState().surahs?.missingSurahs).toHaveLength(1);

    // Leave and return: the single-shot surahs read is served from cache.
    controller.applyUrlState(null);
    controller.applyUrlState(urlState(identity, { view: 'surahs', detailPage: 1 }));
    expect(spies.getSurahs).toHaveBeenCalledTimes(1);
    expect(spies.getSummary).toHaveBeenCalledTimes(1);
    expect(controller.panelState().surahs?.surahs).toHaveLength(1);
  });
});
