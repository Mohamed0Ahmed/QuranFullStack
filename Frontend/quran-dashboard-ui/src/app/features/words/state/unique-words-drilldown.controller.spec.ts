import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import {
  DRILLDOWN_ERROR_LABEL,
  RESTORED_WORD_LOAD_ERROR_LABEL,
  RESTORED_WORD_NOT_FOUND_LABEL,
} from '../models/unique-words.labels';
import {
  UniqueWordKind,
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
} from '../models/unique-words.models';
import { UniqueWordsCache } from './unique-words-cache';
import {
  UniqueWordsDrilldownController,
  UniqueWordsDrilldownUrlState,
} from './unique-words-drilldown.controller';

// Synthetic, non-scriptural fixture rows (Quranic-data source safety).
function surahsOf(nameArabic: string): UniqueWordSurahsDto {
  return { surahs: [{ nameArabic, occurrencesInSurah: 1, surahNumber: 1 }] };
}

function summaryOf(mode: UniqueWordKind, id: number): UniqueWordSummaryDto {
  return {
    id,
    kind: mode,
    displayText: `كلمة-اختبار-${mode}-${id}`,
    occurrencesCount: 3,
    ayahsCount: 2,
    surahsCount: 2,
    missingSurahsCount: 112,
  };
}

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function urlState(
  wordId: number,
  overrides: Partial<UniqueWordsDrilldownUrlState> = {},
): UniqueWordsDrilldownUrlState {
  return { mode: 'tashkeel', wordId, view: 'surahs', ayahPage: null, ...overrides };
}

interface UniqueWordsApiStub {
  getSummary: ReturnType<typeof vi.fn>;
  getMentionedSurahs: ReturnType<typeof vi.fn>;
}

function createController(options: {
  summary: (mode: UniqueWordKind, id: number) => Observable<ApiResponse<UniqueWordSummaryDto>>;
  surahs: (mode: UniqueWordKind, id: number) => Observable<ApiResponse<UniqueWordSurahsDto>>;
}): { controller: UniqueWordsDrilldownController; api: UniqueWordsApiStub } {
  const api: UniqueWordsApiStub = {
    getSummary: vi.fn((mode: UniqueWordKind, id: number) => options.summary(mode, id)),
    getMentionedSurahs: vi.fn((mode: UniqueWordKind, id: number) => options.surahs(mode, id)),
  };
  const controller = new UniqueWordsDrilldownController(
    api as unknown as UniqueWordsApi,
    new UniqueWordsCache(),
  );

  return { controller, api };
}

describe('UniqueWordsDrilldownController (route-independent, Feature 029 B4)', () => {
  describe('stale DETAIL responses across a word transition (Feature 030, C1)', () => {
    // Keyed by (mode, wordId): simple and tashkeel are distinct word spaces, so each needs
    // its own landing seam.
    function surahSubjects(): {
      subjectFor: (mode: UniqueWordKind, id: number) => Subject<ApiResponse<UniqueWordSurahsDto>>;
      read: (mode: UniqueWordKind, id: number) => Observable<ApiResponse<UniqueWordSurahsDto>>;
    } {
      const subjects = new Map<string, Subject<ApiResponse<UniqueWordSurahsDto>>>();
      const subjectFor = (mode: UniqueWordKind, id: number) => {
        const key = `${mode}:${id}`;
        const existing = subjects.get(key) ?? new Subject<ApiResponse<UniqueWordSurahsDto>>();
        subjects.set(key, existing);
        return existing;
      };
      return { subjectFor, read: (mode, id) => subjectFor(mode, id).asObservable() };
    }

    // Word 1's summary resolves synchronously so its surahs drilldown load is left in flight;
    // word 2's summary stays pending. Returns word 1's seam to land its response late.
    function openWordOneThenPendingWordTwo(): {
      controller: UniqueWordsDrilldownController;
      staleSurahs: Subject<ApiResponse<UniqueWordSurahsDto>>;
      wordTwoSummary: Subject<ApiResponse<UniqueWordSummaryDto>>;
    } {
      const wordTwoSummary = new Subject<ApiResponse<UniqueWordSummaryDto>>();
      const surahs = surahSubjects();
      const { controller } = createController({
        summary: (mode, id) => (id === 1 ? of(ok(summaryOf(mode, 1))) : wordTwoSummary.asObservable()),
        surahs: surahs.read,
      });

      controller.applyUrlState(urlState(1));
      expect(controller.drilldownState().summary?.id).toBe(1);

      controller.applyUrlState(urlState(2));
      expect(controller.drilldownState().selectedWordId).toBe(2);

      return { controller, staleSurahs: surahs.subjectFor('tashkeel', 1), wordTwoSummary };
    }

    it('ignores the previous word drilldown response while the new word summary is pending', () => {
      const { controller, staleSurahs } = openWordOneThenPendingWordTwo();

      staleSurahs.next(ok(surahsOf('سورة-اختبار-١')));

      const drilldown = controller.drilldownState();
      expect(drilldown.selectedWordId).toBe(2);
      expect(drilldown.surahs).toBeNull();
      expect(drilldown.status).toBe('loading');
    });

    it('ignores the previous word drilldown response after the new word summary succeeds', () => {
      const { controller, staleSurahs, wordTwoSummary } = openWordOneThenPendingWordTwo();

      wordTwoSummary.next(ok(summaryOf('tashkeel', 2)));
      wordTwoSummary.complete();
      staleSurahs.next(ok(surahsOf('سورة-اختبار-١')));

      const drilldown = controller.drilldownState();
      expect(drilldown.summary?.id).toBe(2);
      expect(drilldown.surahs).toBeNull();
      expect(drilldown.status).toBe('loading');
    });

    it('ignores the previous word drilldown response after the new word summary 404s', () => {
      const { controller, staleSurahs, wordTwoSummary } = openWordOneThenPendingWordTwo();

      wordTwoSummary.error(new HttpErrorResponse({ status: 404 }));
      expect(controller.drilldownState().status).toBe('notFound');

      staleSurahs.next(ok(surahsOf('سورة-اختبار-١')));

      const drilldown = controller.drilldownState();
      expect(drilldown.status).toBe('notFound');
      expect(drilldown.errorMessage).toBe(RESTORED_WORD_NOT_FOUND_LABEL);
      expect(drilldown.surahs).toBeNull();
    });

    it('ignores the previous word drilldown response after the new word summary fails in transport', () => {
      const { controller, staleSurahs, wordTwoSummary } = openWordOneThenPendingWordTwo();

      wordTwoSummary.error(new HttpErrorResponse({ status: 500 }));
      expect(controller.drilldownState().status).toBe('error');

      staleSurahs.next(ok(surahsOf('سورة-اختبار-١')));
      staleSurahs.error(new HttpErrorResponse({ status: 503 }));

      const drilldown = controller.drilldownState();
      expect(drilldown.status).toBe('error');
      expect(drilldown.errorMessage).toBe(RESTORED_WORD_LOAD_ERROR_LABEL);
      expect(drilldown.surahs).toBeNull();
    });

    it('treats the same word id in the other mode as a different word and ignores the first mode response', () => {
      const tashkeelSummary = new Subject<ApiResponse<UniqueWordSummaryDto>>();
      const surahs = surahSubjects();
      const { controller, api } = createController({
        summary: (mode, id) =>
          mode === 'simple' ? of(ok(summaryOf('simple', id))) : tashkeelSummary.asObservable(),
        surahs: surahs.read,
      });

      controller.applyUrlState(urlState(1, { mode: 'simple' }));
      expect(controller.drilldownState().summary?.kind).toBe('simple');

      controller.applyUrlState(urlState(1, { mode: 'tashkeel' }));

      // The simple word's held summary must never be reused for the tashkeel word space.
      expect(api.getSummary).toHaveBeenCalledWith('tashkeel', 1);
      expect(controller.drilldownState().summary).toBeNull();

      surahs.subjectFor('simple', 1).next(ok(surahsOf('سورة-اختبار-٢')));

      const drilldown = controller.drilldownState();
      expect(drilldown.selectedWordId).toBe(1);
      expect(drilldown.surahs).toBeNull();
      expect(drilldown.status).toBe('loading');
    });
  });

  describe('retryCurrentIdentity (Feature 030, M3)', () => {
    it('recovers from a summary transport error and loads the same identity on retry', () => {
      let attempt = 0;
      const { controller, api } = createController({
        summary: (mode, id) => {
          attempt += 1;
          return attempt === 1
            ? throwError(() => new HttpErrorResponse({ status: 500 }))
            : of(ok(summaryOf(mode, id)));
        },
        surahs: () => of(ok(surahsOf('سورة-اختبار-٣'))),
      });

      controller.applyUrlState(urlState(4));
      expect(controller.drilldownState().status).toBe('error');

      controller.retryCurrentIdentity();

      const drilldown = controller.drilldownState();
      expect(drilldown.status).toBe('success');
      expect(drilldown.isOpen).toBe(true);
      expect(drilldown.summary?.id).toBe(4);
      expect(drilldown.surahs?.surahs[0].nameArabic).toBe('سورة-اختبار-٣');
      expect(api.getSummary).toHaveBeenCalledTimes(2);
    });

    it('recovers from a drilldown transport error by reloading the view without re-reading the summary', () => {
      let attempt = 0;
      const { controller, api } = createController({
        summary: (mode, id) => of(ok(summaryOf(mode, id))),
        surahs: () => {
          attempt += 1;
          return attempt === 1
            ? throwError(() => new HttpErrorResponse({ status: 500 }))
            : of(ok(surahsOf('سورة-اختبار-٤')));
        },
      });

      controller.applyUrlState(urlState(4));
      expect(controller.drilldownState().status).toBe('error');
      expect(controller.drilldownState().errorMessage).toBe(DRILLDOWN_ERROR_LABEL);

      controller.retryCurrentIdentity();

      const drilldown = controller.drilldownState();
      expect(drilldown.status).toBe('success');
      expect(drilldown.surahs?.surahs[0].nameArabic).toBe('سورة-اختبار-٤');
      expect(api.getSummary).toHaveBeenCalledTimes(1);
      expect(api.getMentionedSurahs).toHaveBeenCalledTimes(2);
    });

    it('is a no-op without a selected word', () => {
      const { controller, api } = createController({
        summary: (mode, id) => of(ok(summaryOf(mode, id))),
        surahs: () => of(ok(surahsOf('سورة-اختبار-٥'))),
      });

      controller.retryCurrentIdentity();

      expect(api.getSummary).not.toHaveBeenCalled();
      expect(controller.drilldownState().status).toBe('idle');
    });
  });
});
