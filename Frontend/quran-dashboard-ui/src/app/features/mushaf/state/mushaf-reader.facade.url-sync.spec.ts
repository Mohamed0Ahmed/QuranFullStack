import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { BehaviorSubject, of, timer } from 'rxjs';
import { map } from 'rxjs/operators';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { AyahStudyDto, WordAnalysisDto, DEFAULT_MUSHAF_READER_STATE } from '../models/mushaf.models';
import { MushafReaderFacade } from './mushaf-reader.facade';
import { saveMushafReaderSession } from './mushaf-reader-session';
import { MushafUrlSnapshot } from './mushaf-url-sync';
import { mushafAyahMutashabihatApiProvider, mushafSimilarAyahsApiProvider, mushafStudySourceCatalogApiProvider } from './mushaf-study-source-catalog.api.mock';

const pageDto = {
  pageNumber: 5,
  previousPageNumber: 4,
  nextPageNumber: 6,
  surahs: [],
  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
  lines: [],
  markers: [],
};

const ayahStudyDto: AyahStudyDto = {
  ayah: {
    verseKey: '2:25',
    surahNumber: 2,
    surahNameArabic: 'البقرة',
    ayahNumber: 25,
    textUthmani: 'نص تجريبي للآية',
    wordsCount: 5,
    pageFrom: 5,
    pageTo: 5,
    juzNumber: 1,
    hizbNumber: 1,
    rubNumber: 1,
    sajda: null,
  },
  selectedSources: {
    tafsirSource: 'ar-muyassar',
    translationSource: 'en-sahih-international',
    fullI3rabSource: 'muyassar',
  },
  tafsir: null,
  translation: null,
  fullI3rab: null,
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
    lineWordOrder: 3,
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
      wordKeyImlaeiSimple: 'مفتاح-تجريبي',
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

const savedSessionSnapshot: MushafUrlSnapshot = {
  pageNumber: 12,
  ayah: '2:25',
  focusAyah: null,
  word: '2:25:3',
  segment: '2:25:3:1',
  panel: 'word',
  ayahTab: 'translation',
  wordTab: 'morphology',
  sources: {
    tafsirSource: 'ar-muyassar',
    translationSource: 'en-sahih-international',
    fullI3rabSource: 'muyassar',
  },
};

function createFacadeTestBed(queryParams: Record<string, string>) {
  const queryParamMap$ = new BehaviorSubject(convertToParamMap(queryParams));
  const navigate = vi.fn().mockResolvedValue(true);
  const getPage = vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto }));
  const getWordAnalysis = vi.fn(() => of({ isSuccess: true, message: 'ok', data: wordAnalysisDto }));
  const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto }));

  TestBed.configureTestingModule({
    providers: [
      MushafReaderFacade,
      {
        provide: ActivatedRoute,
        useValue: { queryParamMap: queryParamMap$.asObservable() },
      },
      { provide: Router, useValue: { navigate } },
      {
        provide: MushafPagesApi,
        useValue: { getPage },
      },
      {
        provide: MushafAyahStudyApi,
        useValue: { getAyahStudy },
      },
      {
        provide: MushafWordAnalysisApi,
        useValue: {
          getWordAnalysis,
        },
      },
        mushafStudySourceCatalogApiProvider,
        mushafSimilarAyahsApiProvider,
        mushafAyahMutashabihatApiProvider,
    ],
  });

  return {
    facade: TestBed.inject(MushafReaderFacade),
    route: TestBed.inject(ActivatedRoute),
    navigate,
    queryParamMap$,
    getAyahStudy,
    getPage,
    getWordAnalysis,
  };
}

describe('MushafReaderFacade URL sync', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('hydrates page, ayah, word, tabs, sources, and panel from a deep link', () => {
    const { facade, route } = createFacadeTestBed({
      page: '5',
      ayah: '2:25',
      word: '2:25:3',
      segment: '2:25:3:1',
      panel: 'word',
      ayahTab: 'translation',
      wordTab: 'morphology',
      tafsirSource: 'ar-muyassar',
      translationSource: 'en-sahih-international',
      fullI3rabSource: 'muyassar',
    });

    facade.bindToRoute(route);

    expect(facade.pageNumber()).toBe(5);
    expect(facade.selectedAyahKey()).toBe('2:25');
    expect(facade.selectedWordLocation()).toBe('2:25:3');
    expect(facade.selectedSegmentLocation()).toBe('2:25:3:1');
    expect(facade.panel()).toBe('word');
    expect(facade.ayahTab()).toBe('translation');
    expect(facade.wordTab()).toBe('morphology');
    expect(facade.sources().translationSource).toBe('en-sahih-international');
    expect(facade.ayahStudy()?.ayah.verseKey).toBe('2:25');
    expect(facade.wordAnalysis()?.word.wordLocation).toBe('2:25:3');
  });

  it('debounces word-analysis requests when the selected word changes quickly', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, queryParamMap$, getWordAnalysis } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      });

      facade.bindToRoute(route);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:25', word: '2:25:5', panel: 'word' }));

      expect(getWordAnalysis).toHaveBeenCalledTimes(1);

      vi.advanceTimersByTime(699);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);

      vi.advanceTimersByTime(1);
      expect(getWordAnalysis).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('debounces ayah-study requests when the selected ayah changes quickly (UI-001)', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, queryParamMap$, getAyahStudy } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      });

      facade.bindToRoute(route);

      expect(getAyahStudy).toHaveBeenCalledTimes(1);
      const previousAyahStudy = facade.ayahStudy();
      expect(previousAyahStudy).not.toBeNull();

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:26', word: '2:26:1', panel: 'word' }));

      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      expect(facade.ayahStudyLoadState().isLoading).toBe(true);
      expect(facade.ayahStudy()).toBe(previousAyahStudy);

      vi.advanceTimersByTime(699);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);
      expect(facade.ayahStudyLoadState().isLoading).toBe(true);

      vi.advanceTimersByTime(1);

      expect(getAyahStudy).toHaveBeenCalledTimes(2);
      expect(getAyahStudy).toHaveBeenLastCalledWith('2:26', expect.anything());
    } finally {
      vi.useRealTimers();
    }
  });

  it('shows the word-analysis panel as loading for the whole debounce window (UI-001)', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, queryParamMap$, getWordAnalysis } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      });

      facade.bindToRoute(route);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);
      const previousWordAnalysis = facade.wordAnalysis();
      expect(previousWordAnalysis).not.toBeNull();

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:25', word: '2:25:4', panel: 'word' }));

      expect(facade.wordAnalysisLoadState().isLoading).toBe(true);
      expect(facade.wordAnalysis()).toBe(previousWordAnalysis);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);

      vi.advanceTimersByTime(699);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);
      expect(facade.wordAnalysisLoadState().isLoading).toBe(true);

      vi.advanceTimersByTime(1);
      expect(getWordAnalysis).toHaveBeenCalledTimes(2);
      expect(getWordAnalysis).toHaveBeenLastCalledWith('2:25:4');
    } finally {
      vi.useRealTimers();
    }
  });

  it('cancels a pending debounced ayah-study fetch when the ayah is cleared', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, queryParamMap$, getAyahStudy } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      });

      facade.bindToRoute(route);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:26', word: '2:26:1', panel: 'word' }));
      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      queryParamMap$.next(convertToParamMap({ page: '5', panel: 'none' }));

      vi.advanceTimersByTime(1000);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);
      expect(facade.ayahStudyLoadState().isLoading).toBe(false);
      expect(facade.selectedAyahKey()).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });

  it('replaces a pending debounced ayah-study fetch when switching ayahs again before the window elapses', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, queryParamMap$, getAyahStudy } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      });

      facade.bindToRoute(route);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:26', word: '2:26:1', panel: 'word' }));
      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:27', word: '2:27:1', panel: 'word' }));

      vi.advanceTimersByTime(699);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      vi.advanceTimersByTime(1);
      expect(getAyahStudy).toHaveBeenCalledTimes(2);
      expect(getAyahStudy).toHaveBeenLastCalledWith('2:27', expect.anything());
      expect(facade.selectedAyahKey()).toBe('2:27');
    } finally {
      vi.useRealTimers();
    }
  });

  it('loads immediately (not debounced) when only the source changes for the same ayah', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, queryParamMap$, getAyahStudy } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      });

      facade.bindToRoute(route);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      queryParamMap$.next(
        convertToParamMap({
          page: '5',
          ayah: '2:25',
          word: '2:25:3',
          panel: 'word',
          translationSource: 'en-sahih-international',
        }),
      );

      expect(getAyahStudy).toHaveBeenCalledTimes(2);
      expect(getAyahStudy).toHaveBeenLastCalledWith(
        '2:25',
        expect.objectContaining({ translationSource: 'en-sahih-international' }),
      );
    } finally {
      vi.useRealTimers();
    }
  });

  it('applies a cached ayah study immediately on switch — no debounce, no loading overlay (UI-001)', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, queryParamMap$, getAyahStudy } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      });

      facade.bindToRoute(route);

      expect(getAyahStudy).toHaveBeenCalledTimes(1);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:26', word: '2:26:1', panel: 'word' }));
      expect(facade.ayahStudyLoadState().isLoading).toBe(true);
      expect(getAyahStudy).toHaveBeenCalledTimes(1);
      vi.advanceTimersByTime(700);
      expect(getAyahStudy).toHaveBeenCalledTimes(2);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:25', word: '2:25:3', panel: 'word' }));
      expect(facade.ayahStudyLoadState().isLoading).toBe(false);
      expect(facade.ayahStudy()?.ayah.verseKey).toBe('2:25');
      expect(getAyahStudy).toHaveBeenCalledTimes(2);

      vi.advanceTimersByTime(700);
      expect(getAyahStudy).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('applies a cached word analysis immediately on switch — no debounce, no loading overlay (UI-001)', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, queryParamMap$, getWordAnalysis } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      });

      facade.bindToRoute(route);

      expect(getWordAnalysis).toHaveBeenCalledTimes(1);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:25', word: '2:25:5', panel: 'word' }));
      expect(facade.wordAnalysisLoadState().isLoading).toBe(true);
      expect(getWordAnalysis).toHaveBeenCalledTimes(1);
      vi.advanceTimersByTime(700);
      expect(getWordAnalysis).toHaveBeenCalledTimes(2);

      queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:25', word: '2:25:3', panel: 'word' }));
      expect(facade.wordAnalysisLoadState().isLoading).toBe(false);
      expect(facade.wordAnalysis()).not.toBeNull();
      expect(getWordAnalysis).toHaveBeenCalledTimes(2);

      vi.advanceTimersByTime(700);
      expect(getWordAnalysis).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('writes URL updates with replaceUrl merge semantics when selecting an ayah', () => {
    const { facade, route, navigate } = createFacadeTestBed({ page: '5', word: '2:25:3' });
    facade.bindToRoute(route);

    facade.selectAyah('2:26');

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { ayah: '2:26', panel: 'ayah', focusAyah: null, word: null, segment: null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      }),
    );
  });

  it('writes page and focusAyah when peeking at an ayah on another Mushaf page', () => {
    const { facade, route, navigate } = createFacadeTestBed({
      page: '5',
      ayah: '2:25',
      word: '2:25:3',
      ayahTab: 'similar-ayahs',
    });
    facade.bindToRoute(route);

    facade.viewAyahOnPage('4:57', 92);

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { page: 92, focusAyah: '4:57' },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      }),
    );
    expect(navigate).not.toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ ayah: '4:57' }),
      }),
    );
  });

  it('clears focusAyah when returning to the study anchor page via peek', () => {
    const { facade, route, navigate } = createFacadeTestBed({
      page: '92',
      ayah: '2:25',
      focusAyah: '4:57',
      ayahTab: 'similar-ayahs',
    });
    facade.bindToRoute(route);

    facade.viewAyahOnPage('2:25', 5);

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { page: 5, focusAyah: '2:25' },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      }),
    );
  });

  it('does not reload ayah study when peeking at a similar ayah', async () => {
    const { facade, route, getAyahStudy, queryParamMap$ } = createFacadeTestBed({
      page: '5',
      ayah: '2:25',
      ayahTab: 'similar-ayahs',
    });
    facade.bindToRoute(route);

    await vi.waitFor(() => {
      expect(getAyahStudy).toHaveBeenCalledTimes(1);
    });

    facade.viewAyahOnPage('4:57', 92);
    queryParamMap$.next(
      convertToParamMap({
        page: '92',
        ayah: '2:25',
        focusAyah: '4:57',
        ayahTab: 'similar-ayahs',
      }),
    );

    await vi.waitFor(() => {
      expect(facade.focusAyahKey()).toBe('4:57');
    });
    expect(getAyahStudy).toHaveBeenCalledTimes(1);
    expect(facade.selectedAyahKey()).toBe('2:25');
  });

  it('auto-clears focusAyah after 3 seconds without changing the study anchor', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, navigate, queryParamMap$ } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        ayahTab: 'similar-ayahs',
      });
      facade.bindToRoute(route);

      facade.viewAyahOnPage('4:57', 92);
      queryParamMap$.next(
        convertToParamMap({
          page: '92',
          ayah: '2:25',
          focusAyah: '4:57',
          ayahTab: 'similar-ayahs',
        }),
      );

      expect(facade.focusAyahKey()).toBe('4:57');
      vi.advanceTimersByTime(2999);
      expect(facade.focusAyahKey()).toBe('4:57');

      navigate.mockClear();
      vi.advanceTimersByTime(1);

      expect(navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({
          queryParams: { focusAyah: null },
          queryParamsHandling: 'merge',
          replaceUrl: true,
        }),
      );

      queryParamMap$.next(
        convertToParamMap({
          page: '92',
          ayah: '2:25',
          ayahTab: 'similar-ayahs',
        }),
      );

      expect(facade.focusAyahKey()).toBeNull();
      expect(facade.selectedAyahKey()).toBe('2:25');
    } finally {
      vi.useRealTimers();
    }
  });

  it('does not navigate when the peek flash timer fires after unbinding from the route', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, navigate, queryParamMap$ } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        ayahTab: 'similar-ayahs',
      });
      facade.bindToRoute(route);

      facade.viewAyahOnPage('4:57', 92);
      queryParamMap$.next(
        convertToParamMap({
          page: '92',
          ayah: '2:25',
          focusAyah: '4:57',
          ayahTab: 'similar-ayahs',
        }),
      );

      facade.unbindFromRoute();
      navigate.mockClear();

      vi.advanceTimersByTime(3000);
      expect(navigate).not.toHaveBeenCalled();
      expect(facade.focusAyahKey()).toBe('4:57');
    } finally {
      vi.useRealTimers();
    }
  });

  it('resets the peek flash timer when peeking at another ayah within 3 seconds', () => {
    vi.useFakeTimers();

    try {
      const { facade, route, navigate, queryParamMap$ } = createFacadeTestBed({
        page: '5',
        ayah: '2:25',
        ayahTab: 'similar-ayahs',
      });
      facade.bindToRoute(route);

      facade.viewAyahOnPage('4:57', 92);
      queryParamMap$.next(
        convertToParamMap({
          page: '92',
          ayah: '2:25',
          focusAyah: '4:57',
          ayahTab: 'similar-ayahs',
        }),
      );

      vi.advanceTimersByTime(2000);

      facade.viewAyahOnPage('3:10', 50);
      queryParamMap$.next(
        convertToParamMap({
          page: '50',
          ayah: '2:25',
          focusAyah: '3:10',
          ayahTab: 'similar-ayahs',
        }),
      );

      vi.advanceTimersByTime(2000);
      expect(facade.focusAyahKey()).toBe('3:10');

      navigate.mockClear();
      vi.advanceTimersByTime(1000);
      queryParamMap$.next(
        convertToParamMap({
          page: '50',
          ayah: '2:25',
          ayahTab: 'similar-ayahs',
        }),
      );

      expect(navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({
          queryParams: { focusAyah: null },
        }),
      );
      expect(facade.focusAyahKey()).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });

  it('restarts the peek flash timer when the target page finishes loading', () => {
    vi.useFakeTimers();

    try {
      const page92Dto = { ...pageDto, pageNumber: 92 };
      const queryParamMap$ = new BehaviorSubject(
        convertToParamMap({ page: '5', ayah: '2:25', ayahTab: 'similar-ayahs' }),
      );
      const navigate = vi.fn().mockResolvedValue(true);
      const getPage = vi.fn((pageNum: number) => {
        if (pageNum === 92) {
          return timer(2000).pipe(map(() => ({ isSuccess: true, message: 'ok', data: page92Dto })));
        }
        return of({ isSuccess: true, message: 'ok', data: pageDto });
      });
      const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto }));

      TestBed.configureTestingModule({
        providers: [
          MushafReaderFacade,
          {
            provide: ActivatedRoute,
            useValue: { queryParamMap: queryParamMap$.asObservable() },
          },
          { provide: Router, useValue: { navigate } },
          { provide: MushafPagesApi, useValue: { getPage } },
          { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
          {
            provide: MushafWordAnalysisApi,
            useValue: { getWordAnalysis: vi.fn(() => of({ isSuccess: true, message: 'ok', data: wordAnalysisDto })) },
          },
          mushafStudySourceCatalogApiProvider,
          mushafSimilarAyahsApiProvider,
          mushafAyahMutashabihatApiProvider,
        ],
      });

      const facade = TestBed.inject(MushafReaderFacade);
      const route = TestBed.inject(ActivatedRoute);
      facade.bindToRoute(route);

      facade.viewAyahOnPage('4:57', 92);
      queryParamMap$.next(
        convertToParamMap({
          page: '92',
          ayah: '2:25',
          focusAyah: '4:57',
          ayahTab: 'similar-ayahs',
        }),
      );

      vi.advanceTimersByTime(1999);
      expect(facade.pageNumber()).toBe(92);
      expect(facade.focusAyahKey()).toBe('4:57');

      vi.advanceTimersByTime(1);
      expect(facade.pageNumber()).toBe(92);
      expect(facade.focusAyahKey()).toBe('4:57');

      vi.advanceTimersByTime(2999);
      expect(facade.focusAyahKey()).toBe('4:57');

      navigate.mockClear();
      vi.advanceTimersByTime(1);
      expect(navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({
          queryParams: { focusAyah: null },
        }),
      );

      queryParamMap$.next(
        convertToParamMap({
          page: '92',
          ayah: '2:25',
          ayahTab: 'similar-ayahs',
        }),
      );
      expect(facade.focusAyahKey()).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });

  it('sets ayah from the selected word location when selecting a word', () => {
    const { facade, route, navigate } = createFacadeTestBed({ page: '5' });
    facade.bindToRoute(route);

    facade.selectWord('2:25:3');

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { word: '2:25:3', ayah: '2:25', panel: 'word', focusAyah: null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      }),
    );
  });

  it('corrects out-of-scope enum values in the URL after hydration', () => {
    const { facade, route, navigate } = createFacadeTestBed({
      page: '700',
      panel: 'sources',
      ayahTab: 'links',
      wordTab: 'bad',
    });

    facade.bindToRoute(route);

    expect(facade.panel()).toBe('none');
    expect(facade.ayahTab()).toBe('tafsir');
    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({
          page: 604,
          panel: null,
          ayahTab: 'tafsir',
          wordTab: 'segments',
        }),
        replaceUrl: true,
      }),
    );
  });

  it('preserves URL-driven state when viewport layout mode changes', () => {
    const { facade, route } = createFacadeTestBed({
      page: '5',
      ayah: '2:25',
      word: '2:25:3',
      panel: 'word',
      ayahTab: 'tafsir',
      wordTab: 'segments',
    });

    facade.bindToRoute(route);

    const beforeResize = {
      pageNumber: facade.pageNumber(),
      ayah: facade.selectedAyahKey(),
      word: facade.selectedWordLocation(),
      panel: facade.panel(),
      ayahTab: facade.ayahTab(),
      wordTab: facade.wordTab(),
    };

    window.dispatchEvent(new Event('resize'));

    expect({
      pageNumber: facade.pageNumber(),
      ayah: facade.selectedAyahKey(),
      word: facade.selectedWordLocation(),
      panel: facade.panel(),
      ayahTab: facade.ayahTab(),
      wordTab: facade.wordTab(),
    }).toEqual(beforeResize);
  });

  it('clears segment selection when the URL keeps word but omits segment', () => {
    const { facade, route, queryParamMap$ } = createFacadeTestBed({
      page: '5',
      word: '2:25:3',
      segment: '2:25:3:1',
    });

    facade.bindToRoute(route);
    expect(facade.selectedSegmentLocation()).toBe('2:25:3:1');

    queryParamMap$.next(convertToParamMap({ page: '5', word: '2:25:4' }));

    expect(facade.selectedWordLocation()).toBe('2:25:4');
    expect(facade.selectedSegmentLocation()).toBeNull();
  });

  it('clears explicit source params when the URL omits them (browser-back regression)', () => {
    const { facade, route, queryParamMap$, getAyahStudy } = createFacadeTestBed({
      page: '5',
      ayah: '2:25',
      tafsirSource: 'ar-muyassar',
      translationSource: 'en-sahih-international',
      fullI3rabSource: 'muyassar',
    });

    facade.bindToRoute(route);
    expect(facade.sources().translationSource).toBe('en-sahih-international');

    queryParamMap$.next(convertToParamMap({ page: '5', ayah: '2:25' }));

    expect(getAyahStudy).toHaveBeenCalledTimes(2);
    expect(getAyahStudy).toHaveBeenLastCalledWith('2:25', {
      tafsirSource: null,
      translationSource: null,
      fullI3rabSource: null,
    });
  });

  it('restores a saved session when returning to a bare mushaf route', () => {
    saveMushafReaderSession(savedSessionSnapshot);
    const { facade, route, navigate } = createFacadeTestBed({});

    facade.bindToRoute(route);

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: {
          page: 12,
          ayah: '2:25',
          word: '2:25:3',
          segment: '2:25:3:1',
          panel: 'word',
          ayahTab: 'translation',
          wordTab: 'morphology',
          tafsirSource: 'ar-muyassar',
          translationSource: 'en-sahih-international',
          fullI3rabSource: 'muyassar',
        },
        replaceUrl: true,
      }),
    );
    expect(facade.pageNumber()).toBe(1);
    expect(facade.selectedWordLocation()).toBeNull();
  });

  it('hydrates from the restored URL after a bare entry session redirect', () => {
    saveMushafReaderSession(savedSessionSnapshot);
    const { facade, route, navigate, queryParamMap$ } = createFacadeTestBed({});

    facade.bindToRoute(route);
    queryParamMap$.next(
      convertToParamMap({
        page: '12',
        ayah: '2:25',
        word: '2:25:3',
        segment: '2:25:3:1',
        panel: 'word',
        ayahTab: 'translation',
        wordTab: 'morphology',
        tafsirSource: 'ar-muyassar',
        translationSource: 'en-sahih-international',
        fullI3rabSource: 'muyassar',
      }),
    );

    expect(navigate).toHaveBeenCalledTimes(1);
    expect(facade.pageNumber()).toBe(12);
    expect(facade.selectedAyahKey()).toBe('2:25');
    expect(facade.selectedWordLocation()).toBe('2:25:3');
    expect(facade.panel()).toBe('word');
  });

  it('restores a saved session on an overlay-only URL and merges (keeps) the overlay params', () => {
    saveMushafReaderSession(savedSessionSnapshot);
    const { facade, route, navigate } = createFacadeTestBed({
      qdDetail: 'v1~root~999~ayahs~simple~mentioned~1',
      qdDetailOpen: '1',
    });

    facade.bindToRoute(route);

    expect(navigate).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParamsHandling: 'merge',
        replaceUrl: true,
      }),
    );
    const restoredParams = vi.mocked(navigate).mock.calls[0][1]?.queryParams as Record<string, unknown>;
    expect(restoredParams['page']).toBe(12);
    expect(restoredParams['qdDetail']).toBeUndefined();
    expect(restoredParams['qdDetailOpen']).toBeUndefined();
  });

  it('defaults to page 1 on a bare entry when no saved session exists', () => {
    const { facade, route, navigate } = createFacadeTestBed({});

    facade.bindToRoute(route);

    expect(navigate).not.toHaveBeenCalled();
    expect(facade.pageNumber()).toBe(1);
    expect(facade.selectedAyahKey()).toBeNull();
    expect(facade.selectedWordLocation()).toBeNull();
  });

  it('hydrates page 1 on bare entry when the saved session has only defaults', () => {
    saveMushafReaderSession({
      pageNumber: DEFAULT_MUSHAF_READER_STATE.pageNumber,
      ayah: null,
      focusAyah: null,
      word: null,
      segment: null,
      panel: DEFAULT_MUSHAF_READER_STATE.panel,
      ayahTab: DEFAULT_MUSHAF_READER_STATE.ayahTab,
      wordTab: DEFAULT_MUSHAF_READER_STATE.wordTab,
      sources: {
        tafsirSource: null,
        translationSource: null,
        fullI3rabSource: null,
      },
    });
    const { facade, route, navigate, getPage } = createFacadeTestBed({});

    facade.bindToRoute(route);

    expect(navigate).not.toHaveBeenCalled();
    expect(facade.pageNumber()).toBe(1);
    expect(getPage).toHaveBeenCalledWith(1);
    expect(facade.selectedAyahKey()).toBeNull();
    expect(facade.selectedWordLocation()).toBeNull();
  });

  it('prefers explicit deep-link params over a saved session', () => {
    saveMushafReaderSession(savedSessionSnapshot);
    const { facade, route, navigate } = createFacadeTestBed({
      page: '5',
      word: '2:25:4',
      panel: 'word',
    });

    facade.bindToRoute(route);

    expect(navigate).not.toHaveBeenCalled();
    expect(facade.pageNumber()).toBe(5);
    expect(facade.selectedWordLocation()).toBe('2:25:4');
  });

  it('persists the hydrated reader snapshot to sessionStorage', () => {
    const { facade, route } = createFacadeTestBed({
      page: '5',
      ayah: '2:25',
      word: '2:25:3',
      panel: 'word',
    });

    facade.bindToRoute(route);

    expect(JSON.parse(sessionStorage.getItem('qd-mushaf-reader-session') ?? '{}')).toEqual(
      expect.objectContaining({
        pageNumber: 5,
        ayah: '2:25',
        word: '2:25:3',
        panel: 'word',
      }),
    );
  });
});
