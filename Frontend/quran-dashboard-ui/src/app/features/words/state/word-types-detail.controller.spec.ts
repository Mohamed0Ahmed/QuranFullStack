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
import { WordTypesDetailViewLoader } from './word-types-detail-view.loader';

function identityOf(overrides: Partial<WordTypeRowIdentity> = {}): WordTypeRowIdentity {
  return { tashkeelWordId: 7, contextCode: 'noun', case: 'all', tense: 'all', voice: 'all', ...overrides };
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
});

describe('WordTypesDetailController cache keys (existing WordTypesCacheKeys)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  function configure(identity: WordTypeRowIdentity, options: {
    ayahs?: PagedResultDto<WordTypeAyahMatchDto>;
    surahs?: WordTypeSurahsResponseDto;
  } = {}) {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: WordTypesApi,
          useValue: {
            getSummary: vi.fn(() => of(ok(summaryOf(identity)))),
            getAyahMatches: vi.fn(() => of(ok(options.ayahs ?? { page: 1, pageSize: 100, totalCount: 0, items: [] }))),
            getSurahs: vi.fn(() => of(ok(options.surahs ?? { surahs: [], missingSurahs: [] }))),
          },
        },
      ],
    });

    const cache = TestBed.inject(WordTypesCache);
    const getOrLoad = vi.spyOn(cache, 'getOrLoad');
    const controller = new WordTypesDetailController(
      TestBed.inject(WordTypesApi),
      cache,
      TestBed.inject(WordTypesDetailViewLoader),
    );

    return { cache, controller, getOrLoad };
  }

  it('reads the summary and paged ayahs through keys carrying the full composite identity', () => {
    const identity = identityOf({ tashkeelWordId: 9, contextCode: 'past', case: 'null', tense: 'past', voice: 'passive' });
    const ayahsPage: PagedResultDto<WordTypeAyahMatchDto> = {
      page: 2,
      pageSize: 100,
      totalCount: 101,
      items: [
        {
          ayahNumber: 2,
          verseKey: '2:2',
          pageNumber: 2,
          surahNumber: 2,
          matchedWordIds: [1],
          matchedWordPositions: [1],
          words: [{ quranWordId: 1, textUthmani: 'ٱلْكِتَـٰبُ', isAyahMarker: false }],
        },
      ],
    };
    const { controller, getOrLoad } = configure(identity, { ayahs: ayahsPage });

    controller.applyUrlState(urlState(identity, { view: 'ayahs', detailPage: 2 }));

    const usedKeys = getOrLoad.mock.calls.map(([key]) => key);
    expect(usedKeys).toContain(WordTypesCacheKeys.summary(identity));
    expect(usedKeys).toContain(WordTypesCacheKeys.ayahs(identity, 2));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().ayahs?.items).toHaveLength(1);
  });

  it('reads the single-shot surahs view through the identity-complete surahs key', () => {
    const identity = identityOf({ tashkeelWordId: 11, contextCode: 'noun', case: 'genitive' });
    const surahs: WordTypeSurahsResponseDto = {
      surahs: [{ surahNumber: 1, nameArabic: 'الفاتحة', occurrencesCount: 2 }],
      missingSurahs: [{ surahNumber: 9, nameArabic: 'التوبة' }],
    };
    const { controller, getOrLoad } = configure(identity, { surahs });

    controller.applyUrlState(urlState(identity, { view: 'surahs', detailPage: 1 }));

    const usedKeys = getOrLoad.mock.calls.map(([key]) => key);
    expect(usedKeys).toContain(WordTypesCacheKeys.summary(identity));
    expect(usedKeys).toContain(WordTypesCacheKeys.surahs(identity));
    expect(controller.panelState().status).toBe('success');
    expect(controller.panelState().surahs?.surahs).toHaveLength(1);
    expect(controller.panelState().surahs?.missingSurahs).toHaveLength(1);
  });
});
