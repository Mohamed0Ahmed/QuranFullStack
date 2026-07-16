import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { AyahStudyDto, WordAnalysisDto } from '../models/mushaf.models';
import { AYAH_STUDY_SWITCH_DELAY_MS } from './mushaf-ayah-study-load.runner';
import { WORD_ANALYSIS_SWITCH_DELAY_MS } from './mushaf-word-analysis-load.runner';
import { MushafReaderFacade } from './mushaf-reader.facade';
import {
  ayahStudyDtoMock,
  mushafAyahMutashabihatApiProvider,
  mushafSimilarAyahsApiProvider,
  mushafStudySourceCatalogApiProvider,
} from './mushaf-study-source-catalog.api.mock';

// F1 regression coverage: leave-while-loading then same-URL return must dispose the
// stranded HTTP subscription, keep suppressing any late/stale response, and actually
// reload/resolve instead of staying stuck in a loading state forever.

const pageDto = {
  pageNumber: 5,
  previousPageNumber: 4,
  nextPageNumber: 6,
  surahs: [],
  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
  lines: [
    {
      lineNumber: 1,
      lineType: 'ayah' as const,
      isCentered: false,
      surahNumber: null,
      words: [
        {
          wordLocation: '2:25:3',
          verseKey: '2:25',
          wordNumber: 3,
          lineWordOrder: 1,
          textUthmani: 'كلمة-تجريبية',
          isAyahMarker: false,
        },
      ],
    },
  ],
  markers: [],
};

// Reuses the shared ayah-study fixture rather than re-declaring its ayah/source metadata here;
// only the similarity counts differ, and these tests never assert on them.
const ayahStudyDto: AyahStudyDto = {
  ...ayahStudyDtoMock,
  similaritySummary: {
    similarAyahCount: 0,
    mutashabihatGroupCount: 0,
    mutashabihatOccurrenceCount: 0,
  },
};

const wordAnalysisDto: WordAnalysisDto = {
  word: {
    quranWordId: 2003,
    wordLocation: '2:25:3',
    verseKey: '2:25',
    surahNumber: 2,
    ayahNumber: 25,
    wordNumber: 3,
    pageNumber: 5,
    lineNumber: 1,
    lineWordOrder: 1,
    textUthmani: 'كلمة-تجريبية',
    textUthmaniSimple: 'كلمة-مبسطة',
    textImlaeiSimple: 'كلمة-مبسطة',
    qpcGlyph: 'glyph-test-1',
  },
  identity: {
    orderedTashkeel: { occurrencesCount: 1, ayahsCount: 1, surahsCount: 1 },
    orderedSimple: { occurrencesCount: 1, ayahsCount: 1, surahsCount: 1 },
    uniqueTashkeel: { id: 1, occurrencesCount: 1, ayahsCount: 1, surahsCount: 1 },
    uniqueSimple: {
      id: 1,
      occurrencesCount: 1,
      ayahsCount: 1,
      surahsCount: 1,
      wordKeyImlaeiSimple: 'مفتاح-كلمة-تجريبي',
    },
  },
  morphology: {
    headPos: 'V',
    headPosLabel: { ar: 'فعل', en: 'Verb' },
    root: null,
    lemma: null,
    stem: null,
    isVerb: true,
    verbTense: 'past',
    verbVoice: 'active',
    caseFeature: null,
  },
  renderedWordSegments: [],
};

function createFacadeTestBed(
  queryParams: Record<string, string>,
  overrides: {
    getAyahStudy?: ReturnType<typeof vi.fn>;
    getWordAnalysis?: ReturnType<typeof vi.fn>;
  } = {},
) {
  const queryParamMap$ = new BehaviorSubject(convertToParamMap(queryParams));
  const navigate = vi.fn().mockResolvedValue(true);
  const getPage = vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto }));
  const getAyahStudy = overrides.getAyahStudy ?? vi.fn();
  const getWordAnalysis = overrides.getWordAnalysis ?? vi.fn();

  TestBed.configureTestingModule({
    providers: [
      MushafReaderFacade,
      { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$.asObservable() } },
      { provide: Router, useValue: { navigate } },
      { provide: MushafPagesApi, useValue: { getPage } },
      { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
      { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis } },
      mushafStudySourceCatalogApiProvider,
      mushafSimilarAyahsApiProvider,
      mushafAyahMutashabihatApiProvider,
    ],
  });

  return {
    facade: TestBed.inject(MushafReaderFacade),
    route: TestBed.inject(ActivatedRoute),
    queryParamMap$,
    getAyahStudy,
    getWordAnalysis,
  };
}

function subjectFactory<T>(subjects: Subject<ApiResponse<T>>[]) {
  return () => {
    const subject = new Subject<ApiResponse<T>>();
    subjects.push(subject);
    return subject.asObservable();
  };
}

describe('MushafReaderFacade lifecycle (F1 leave-while-loading recovery)', () => {
  it('ayah study: disposes the stranded subscription on teardown, suppresses the late response, and reloads to a resolved state on same-URL return', () => {
    const ayahStudySubjects: Subject<ApiResponse<AyahStudyDto>>[] = [];
    const getAyahStudy = vi.fn(subjectFactory(ayahStudySubjects));

    const { facade, route } = createFacadeTestBed(
      { page: '5', ayah: '2:25' },
      { getAyahStudy },
    );

    facade.bindToRoute(route);

    expect(getAyahStudy).toHaveBeenCalledTimes(1);
    expect(facade.ayahStudyLoadState().isLoading).toBe(true);
    expect(ayahStudySubjects[0].observed).toBe(true);

    // Leave while loading: route teardown (ngOnDestroy).
    facade.unbindFromRoute();

    // (a) the in-flight HTTP subscription is disposed on teardown.
    expect(ayahStudySubjects[0].observed).toBe(false);
    expect(facade.ayahStudyLoadState().isLoading).toBe(true);

    // (b) a late/stale response arriving after teardown is still suppressed.
    ayahStudySubjects[0].next({
      isSuccess: true,
      message: 'stale',
      data: { ...ayahStudyDto, ayah: { ...ayahStudyDto.ayah, textUthmani: 'نص متأخر يجب تجاهله' } },
    });
    ayahStudySubjects[0].complete();
    expect(facade.ayahStudy()).toBeNull();
    expect(facade.ayahStudyLoadState().isLoading).toBe(true);

    // Return to the exact same URL (ngOnInit re-mount): identity is unchanged, but the
    // previous load never resolved, so this must reload rather than stay stuck.
    facade.bindToRoute(route);

    // (c) same-URL re-entry reloads cleanly.
    expect(getAyahStudy).toHaveBeenCalledTimes(2);
    expect(ayahStudySubjects[1].observed).toBe(true);

    ayahStudySubjects[1].next({ isSuccess: true, message: 'ok', data: ayahStudyDto });
    ayahStudySubjects[1].complete();

    // (c) ...and reaches a resolved state, not stuck loading.
    expect(facade.ayahStudyLoadState().isLoading).toBe(false);
    expect(facade.ayahStudy()?.ayah.verseKey).toBe('2:25');
    expect(facade.ayahStudy()?.ayah.textUthmani).toBe('نص تجريبي للآية');
  });

  it('word analysis: disposes the stranded subscription on teardown, suppresses the late response, and reloads to a resolved state on same-URL return', () => {
    vi.useFakeTimers();

    try {
      const wordAnalysisSubjects: Subject<ApiResponse<WordAnalysisDto>>[] = [];
      const getWordAnalysis = vi.fn(subjectFactory(wordAnalysisSubjects));

      const { facade, route } = createFacadeTestBed(
        { page: '5', ayah: '2:25', word: '2:25:3', panel: 'word' },
        { getWordAnalysis, getAyahStudy: vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto })) },
      );

      facade.bindToRoute(route);

      expect(getWordAnalysis).toHaveBeenCalledTimes(1);
      expect(facade.wordAnalysisLoadState().isLoading).toBe(true);
      expect(wordAnalysisSubjects[0].observed).toBe(true);

      // Leave while loading.
      facade.unbindFromRoute();

      // (a) disposed on teardown.
      expect(wordAnalysisSubjects[0].observed).toBe(false);
      expect(facade.wordAnalysisLoadState().isLoading).toBe(true);

      // (b) late response suppressed.
      wordAnalysisSubjects[0].next({ isSuccess: true, message: 'stale', data: wordAnalysisDto });
      wordAnalysisSubjects[0].complete();
      expect(facade.wordAnalysis()).toBeNull();
      expect(facade.wordAnalysisLoadState().isLoading).toBe(true);

      // Return to the same URL. The word was already selected, so recovery reuses the
      // existing debounced reselection path (schedule) rather than a fresh call site.
      facade.bindToRoute(route);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);
      expect(facade.wordAnalysisLoadState().isLoading).toBe(true);

      vi.advanceTimersByTime(WORD_ANALYSIS_SWITCH_DELAY_MS);

      // (c) same-URL re-entry reloads cleanly and reaches a resolved state.
      expect(getWordAnalysis).toHaveBeenCalledTimes(2);
      expect(wordAnalysisSubjects[1].observed).toBe(true);

      wordAnalysisSubjects[1].next({ isSuccess: true, message: 'ok', data: wordAnalysisDto });
      wordAnalysisSubjects[1].complete();

      expect(facade.wordAnalysisLoadState().isLoading).toBe(false);
      expect(facade.wordAnalysis()?.word.wordLocation).toBe('2:25:3');
    } finally {
      vi.useRealTimers();
    }
  });

  it('keeps the ayah-study debounce window unchanged when switching ayahs quickly (not a leave/return)', () => {
    vi.useFakeTimers();

    try {
      const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto }));
      const { facade, route, queryParamMap$ } = createFacadeTestBed(
        { page: '5', ayah: '2:25' },
        { getAyahStudy },
      );

      facade.bindToRoute(route);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:26' }));
      expect(getAyahStudy).toHaveBeenCalledTimes(1);
      expect(facade.ayahStudyLoadState().isLoading).toBe(true);

      vi.advanceTimersByTime(AYAH_STUDY_SWITCH_DELAY_MS - 1);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      vi.advanceTimersByTime(1);
      expect(getAyahStudy).toHaveBeenCalledTimes(2);
      expect(getAyahStudy).toHaveBeenLastCalledWith('2:26', expect.anything());
    } finally {
      vi.useRealTimers();
    }
  });

  it('keeps explicit retry behavior unchanged: a failed ayah-study load can be retried and succeeds', () => {
    const getAyahStudy = vi
      .fn()
      .mockReturnValueOnce(
        of<ApiResponse<AyahStudyDto>>({ isSuccess: false, message: 'تعذّر الاتصال بالخادم.', data: null }),
      )
      .mockReturnValueOnce(of<ApiResponse<AyahStudyDto>>({ isSuccess: true, message: 'ok', data: ayahStudyDto }));

    const { facade } = createFacadeTestBed({ page: '5' }, { getAyahStudy });

    facade.loadAyahStudy('2:25');
    expect(getAyahStudy).toHaveBeenCalledTimes(1);
    expect(facade.ayahStudyLoadState().isLoading).toBe(false);
    expect(facade.ayahStudyLoadState().errorMessage).toBe('تعذّر الاتصال بالخادم.');
    expect(facade.ayahStudy()).toBeNull();

    // Explicit retry (e.g. a retry action re-issuing the same load).
    facade.loadAyahStudy('2:25');
    expect(getAyahStudy).toHaveBeenCalledTimes(2);
    expect(facade.ayahStudyLoadState().isLoading).toBe(false);
    expect(facade.ayahStudyLoadState().errorMessage).toBeNull();
    expect(facade.ayahStudy()?.ayah.verseKey).toBe('2:25');
  });

  // Rebind-scoping regression: the stranded-load recovery must fire ONLY on the first
  // hydration after a (re)bind, never on an ordinary in-place URL patch that lands while a
  // request is still in flight. Otherwise an unrelated patch cancels and restarts the live
  // request (ayah study) or re-arms the debounce (word analysis).
  it('does NOT restart an in-flight ayah-study request when an unrelated in-place URL patch (study tab) lands mid-load', () => {
    const ayahStudySubjects: Subject<ApiResponse<AyahStudyDto>>[] = [];
    const getAyahStudy = vi.fn(subjectFactory(ayahStudySubjects));

    const { facade, route, queryParamMap$ } = createFacadeTestBed(
      { page: '5', ayah: '2:25', ayahTab: 'tafsir' },
      { getAyahStudy },
    );

    facade.bindToRoute(route);
    expect(getAyahStudy).toHaveBeenCalledTimes(1);
    expect(facade.ayahStudyLoadState().isLoading).toBe(true);
    expect(ayahStudySubjects[0].observed).toBe(true);

    // Unrelated in-place patch on the SAME live binding: only the study tab changes; the
    // ayah and the selected sources are untouched, and the request is still in flight.
    queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:25', ayahTab: 'translation' }));

    // The original in-flight request must be left alone: not cancelled, not restarted.
    expect(getAyahStudy).toHaveBeenCalledTimes(1);
    expect(ayahStudySubjects).toHaveLength(1);
    expect(ayahStudySubjects[0].observed).toBe(true);
    expect(facade.ayahStudyLoadState().isLoading).toBe(true);

    // It still resolves normally from that single original request.
    ayahStudySubjects[0].next({ isSuccess: true, message: 'ok', data: ayahStudyDto });
    ayahStudySubjects[0].complete();
    expect(facade.ayahStudyLoadState().isLoading).toBe(false);
    expect(facade.ayahStudy()?.ayah.verseKey).toBe('2:25');
  });

  it('does NOT re-arm the word-analysis debounce when an unrelated in-place URL patch (word tab) lands mid-load', () => {
    vi.useFakeTimers();

    try {
      const wordAnalysisSubjects: Subject<ApiResponse<WordAnalysisDto>>[] = [];
      const getWordAnalysis = vi.fn(subjectFactory(wordAnalysisSubjects));

      const { facade, route, queryParamMap$ } = createFacadeTestBed(
        { page: '5', ayah: '2:25', word: '2:25:3', panel: 'word', wordTab: 'morphology' },
        {
          getWordAnalysis,
          getAyahStudy: vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto })),
        },
      );

      facade.bindToRoute(route);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);
      expect(facade.wordAnalysisLoadState().isLoading).toBe(true);
      expect(wordAnalysisSubjects[0].observed).toBe(true);

      // Unrelated in-place patch while the word-analysis request is still in flight: only the
      // word tab changes; the word location is unchanged.
      queryParamMap$.next(
        convertToParamMap({ page: '5', ayah: '2:25', word: '2:25:3', panel: 'word', wordTab: 'segments' }),
      );

      // No new request, and the 700ms debounce must not be re-armed by the unrelated patch.
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);
      expect(wordAnalysisSubjects).toHaveLength(1);
      expect(wordAnalysisSubjects[0].observed).toBe(true);

      vi.advanceTimersByTime(WORD_ANALYSIS_SWITCH_DELAY_MS);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);

      // The single original request still resolves cleanly.
      wordAnalysisSubjects[0].next({ isSuccess: true, message: 'ok', data: wordAnalysisDto });
      wordAnalysisSubjects[0].complete();
      expect(facade.wordAnalysisLoadState().isLoading).toBe(false);
      expect(facade.wordAnalysis()?.word.wordLocation).toBe('2:25:3');
    } finally {
      vi.useRealTimers();
    }
  });
});
