import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';

import { MushafAyahMutashabihatApi } from '../data-access/mushaf-ayah-mutashabihat.api';
import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafSimilarAyahsApi } from '../data-access/mushaf-similar-ayahs.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { MushafReaderFacade } from './mushaf-reader.facade';
import { saveMushafReaderSession } from './mushaf-reader-session';
import { mushafAyahMutashabihatApiProvider, mushafSimilarAyahsApiProvider, mushafStudySourceCatalogApiProvider } from './mushaf-study-source-catalog.api.mock';

const pageDto = {
  pageNumber: 1,
  previousPageNumber: null,
  nextPageNumber: 2,
  surahs: [],
  ayahRange: { firstVerseKey: '1:1', lastVerseKey: '1:1' },
  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
  lines: [],
  markers: [],
};

const wordNavigationPageDto = {
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
          lineWordOrder: 3,
          textUthmani: 'كلمة-١',
          isAyahMarker: false,
        },
        {
          wordLocation: '2:25:4',
          verseKey: '2:25',
          wordNumber: 4,
          lineWordOrder: 4,
          textUthmani: 'علامة-آية',
          isAyahMarker: true,
        },
        {
          wordLocation: '2:25:5',
          verseKey: '2:25',
          wordNumber: 5,
          lineWordOrder: 5,
          textUthmani: 'كلمة-٢',
          isAyahMarker: false,
        },
      ],
    },
  ],
  markers: [],
};

const wordAnalysisDto = {
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
    textUthmani: 'كلمة-١',
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

const ayahStudyDto = {
  ayah: {
    verseKey: '2:25',
    surahNumber: 2,
    surahNameArabic: 'البقرة',
    ayahNumber: 25,
    textUthmani: 'نص-تجريبي',
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

function createWordNavigationFacade(initialWordLocation: string) {
  const queryParamMap$ = new BehaviorSubject(
    convertToParamMap({ page: '5', ayah: '2:25', word: initialWordLocation, panel: 'word' }),
  );
  const navigate = vi.fn().mockResolvedValue(true);
  const getPage = vi.fn(() => of({ isSuccess: true, message: 'ok', data: wordNavigationPageDto }));
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
      { provide: MushafPagesApi, useValue: { getPage } },
      { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
      { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis } },
      mushafStudySourceCatalogApiProvider,
      mushafSimilarAyahsApiProvider,
      mushafAyahMutashabihatApiProvider,
    ],
  });

  const facade = TestBed.inject(MushafReaderFacade);
  const route = TestBed.inject(ActivatedRoute);

  facade.bindToRoute(route);

  return { facade, navigate };
}

describe('MushafReaderFacade.loadPage', () => {
  it('clamps page numbers to the Mushaf range 1–604 before calling the API', () => {
    const getPage = vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
        mushafSimilarAyahsApiProvider,
        mushafAyahMutashabihatApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadPage(0);
    facade.loadPage(999);

    expect(getPage).toHaveBeenCalledWith(1);
    expect(getPage).toHaveBeenCalledWith(604);
  });

  it('maps a successful ApiResponse into page view state', () => {
    const getPage = vi.fn(() => of({ isSuccess: true, message: 'تم', data: pageDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
        mushafSimilarAyahsApiProvider,
        mushafAyahMutashabihatApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadPage(1);

    expect(facade.page()?.pageNumber).toBe(1);
    expect(facade.pageLoadState().errorMessage).toBeNull();
    expect(facade.pageLoadState().isEmpty).toBe(false);
  });

  it('surfaces backend Arabic messages from HTTP 404 responses', () => {
    const getPage = vi.fn(() =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 404,
            error: { isSuccess: false, message: 'المورد غير موجود', errors: [] },
          }),
      ),
    );

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
        mushafSimilarAyahsApiProvider,
        mushafAyahMutashabihatApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadPage(2);

    expect(facade.page()).toBeNull();
    expect(facade.pageLoadState().isEmpty).toBe(true);
    expect(facade.pageLoadState().errorMessage).toBe('المورد غير موجود');
  });
});

describe('MushafReaderFacade surah jump', () => {
  it('resolves a surah start page from the static catalog', () => {
    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage: vi.fn() } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
        mushafSimilarAyahsApiProvider,
        mushafAyahMutashabihatApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);

    expect(facade.resolveSurahStartPage(2)).toBe(2);
    expect(facade.resolveSurahStartPage(114)).toBe(604);
    expect(facade.resolveSurahStartPage(999)).toBeNull();
    expect(facade.surahCatalogByJuz().length).toBe(30);
  });
});

describe('MushafReaderFacade moveSelectedWord', () => {
  it('moves to the next selectable word and skips ayah markers', () => {
    const { facade, navigate } = createWordNavigationFacade('2:25:3');

    facade.moveSelectedWord('next');

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { word: '2:25:5', ayah: '2:25', panel: 'word' },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      }),
    );
  });

  it('moves to the previous selectable word and skips ayah markers', () => {
    const { facade, navigate } = createWordNavigationFacade('2:25:5');

    facade.moveSelectedWord('previous');

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { word: '2:25:3', ayah: '2:25', panel: 'word' },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      }),
    );
  });
});

describe('MushafReaderFacade word analysis cancellation', () => {
  it('drops an in-flight analysis response after the selection is cleared', () => {
    const wordAnalysis$ = new Subject<{
      isSuccess: boolean;
      message: string;
      data: typeof wordAnalysisDto;
    }>();
    const getWordAnalysis = vi.fn(() => wordAnalysis$.asObservable());

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage: vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto })) } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis } },
        mushafStudySourceCatalogApiProvider,
        mushafSimilarAyahsApiProvider,
        mushafAyahMutashabihatApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadPage(1);
    facade.loadWordAnalysis('2:25:3');

    facade.applyUrlState({
      panel: 'none',
      ayah: null,
      word: null,
      segment: null,
      ayahTab: 'tafsir',
      wordTab: 'morphology',
      sources: {
        tafsirSource: null,
        translationSource: null,
        fullI3rabSource: null,
      },
    });

    wordAnalysis$.next({ isSuccess: true, message: 'ok', data: wordAnalysisDto });

    expect(facade.selectedWordLocation()).toBeNull();
    expect(facade.wordAnalysis()).toBeNull();
  });
});

describe('MushafReaderFacade ayah study similarity summary (US1)', () => {
  it('maps similaritySummary from ayah study without loading similarity detail APIs', () => {
    const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage: vi.fn() } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
        mushafSimilarAyahsApiProvider,
        mushafAyahMutashabihatApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadAyahStudy('2:25');

    expect(getAyahStudy).toHaveBeenCalledTimes(1);
    expect(facade.ayahStudy()?.similaritySummary).toEqual({
      similarAyahCount: 0,
      mutashabihatGroupCount: 0,
      mutashabihatOccurrenceCount: 0,
    });
  });
});

describe('MushafReaderFacade similarity ayah tabs', () => {
  describe('US1 tab preservation', () => {
    it('preserves ayahTab=mutashabihat through URL hydration without normalizing to default', () => {
      const queryParamMap$ = new BehaviorSubject(
        convertToParamMap({ page: '5', ayah: '2:25', ayahTab: 'mutashabihat' }),
      );
      const navigate = vi.fn().mockResolvedValue(true);
      const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));

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
            useValue: {
              getPage: vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto })),
            },
          },
          { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
          { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
          mushafStudySourceCatalogApiProvider,
          mushafSimilarAyahsApiProvider,
          mushafAyahMutashabihatApiProvider,
        ],
      });

      const facade = TestBed.inject(MushafReaderFacade);
      const route = TestBed.inject(ActivatedRoute);
      facade.bindToRoute(route);

      expect(facade.ayahTab()).toBe('mutashabihat');
      expect(navigate).not.toHaveBeenCalledWith(
        [],
        expect.objectContaining({
          queryParams: expect.objectContaining({ ayahTab: 'tafsir' }),
        }),
      );
    });

    it('setAyahTab writes the widened tab to the URL without refetching ayah study', () => {
      const queryParamMap$ = new BehaviorSubject(
        convertToParamMap({ page: '5', ayah: '2:25', ayahTab: 'tafsir' }),
      );
      const navigate = vi.fn().mockResolvedValue(true);
      const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));

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
            useValue: {
              getPage: vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto })),
            },
          },
          { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
          { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
          mushafStudySourceCatalogApiProvider,
          mushafSimilarAyahsApiProvider,
          mushafAyahMutashabihatApiProvider,
        ],
      });

      const facade = TestBed.inject(MushafReaderFacade);
      const route = TestBed.inject(ActivatedRoute);
      facade.bindToRoute(route);
      const ayahCallsAfterHydration = getAyahStudy.mock.calls.length;

      facade.setAyahTab('similar-ayahs');

      expect(navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({
          queryParams: { ayahTab: 'similar-ayahs' },
          queryParamsHandling: 'merge',
          replaceUrl: true,
        }),
      );
      expect(getAyahStudy.mock.calls.length).toBe(ayahCallsAfterHydration);
    });
  });

  describe('US4 deep-link restoration', () => {
    beforeEach(() => {
      sessionStorage.clear();
    });

    afterEach(() => {
      sessionStorage.clear();
    });

    it('restores ayahTab=similar-ayahs from URL and lazy-loads only the similar ayahs detail API', async () => {
      const queryParamMap$ = new BehaviorSubject(
        convertToParamMap({ page: '5', ayah: '2:25', ayahTab: 'similar-ayahs' }),
      );
      const navigate = vi.fn().mockResolvedValue(true);
      const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));
      const getSimilarAyahs = vi.fn(() =>
        of({ isSuccess: true, message: 'ok', data: { verseKey: '2:25', count: 0, items: [] } }),
      );
      const getAyahMutashabihat = vi.fn(() =>
        of({ isSuccess: true, message: 'ok', data: { verseKey: '2:25', groupCount: 0, groups: [] } }),
      );

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
            useValue: {
              getPage: vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto })),
            },
          },
          { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
          { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
          mushafStudySourceCatalogApiProvider,
          { provide: MushafSimilarAyahsApi, useValue: { getSimilarAyahs } },
          { provide: MushafAyahMutashabihatApi, useValue: { getAyahMutashabihat } },
        ],
      });

      const facade = TestBed.inject(MushafReaderFacade);
      const route = TestBed.inject(ActivatedRoute);
      facade.bindToRoute(route);

      expect(facade.ayahTab()).toBe('similar-ayahs');
      expect(facade.pageNumber()).toBe(5);
      expect(facade.selectedAyahKey()).toBe('2:25');
      await vi.waitFor(() => {
        expect(getSimilarAyahs).toHaveBeenCalledWith('2:25');
      });
      expect(getAyahMutashabihat).not.toHaveBeenCalled();
    });

    it('restores ayahTab=mutashabihat from URL and lazy-loads only the mutashabihat detail API', async () => {
      const queryParamMap$ = new BehaviorSubject(
        convertToParamMap({ page: '5', ayah: '2:25', ayahTab: 'mutashabihat' }),
      );
      const navigate = vi.fn().mockResolvedValue(true);
      const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));
      const getSimilarAyahs = vi.fn(() =>
        of({ isSuccess: true, message: 'ok', data: { verseKey: '2:25', count: 0, items: [] } }),
      );
      const getAyahMutashabihat = vi.fn(() =>
        of({ isSuccess: true, message: 'ok', data: { verseKey: '2:25', groupCount: 0, groups: [] } }),
      );

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
            useValue: {
              getPage: vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto })),
            },
          },
          { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
          { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
          mushafStudySourceCatalogApiProvider,
          { provide: MushafSimilarAyahsApi, useValue: { getSimilarAyahs } },
          { provide: MushafAyahMutashabihatApi, useValue: { getAyahMutashabihat } },
        ],
      });

      const facade = TestBed.inject(MushafReaderFacade);
      const route = TestBed.inject(ActivatedRoute);
      facade.bindToRoute(route);

      expect(facade.ayahTab()).toBe('mutashabihat');
      expect(facade.pageNumber()).toBe(5);
      expect(facade.selectedAyahKey()).toBe('2:25');
      await vi.waitFor(() => {
        expect(getAyahMutashabihat).toHaveBeenCalledWith('2:25');
      });
      expect(getSimilarAyahs).not.toHaveBeenCalled();
    });

    it('restores a bare-entry saved session with ayahTab=similar-ayahs and lazy-loads similar ayahs after URL hydration', async () => {
      const savedSimilarAyahsSession = {
        pageNumber: 5,
        ayah: '2:25',
        focusAyah: null,
        word: null,
        segment: null,
        panel: 'ayah' as const,
        ayahTab: 'similar-ayahs' as const,
        wordTab: 'segments' as const,
        sources: {
          tafsirSource: null,
          translationSource: null,
          fullI3rabSource: null,
        },
      };
      saveMushafReaderSession(savedSimilarAyahsSession);

      const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));
      const navigate = vi.fn().mockResolvedValue(true);
      const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));
      const getSimilarAyahs = vi.fn(() =>
        of({ isSuccess: true, message: 'ok', data: { verseKey: '2:25', count: 0, items: [] } }),
      );
      const getAyahMutashabihat = vi.fn(() =>
        of({ isSuccess: true, message: 'ok', data: { verseKey: '2:25', groupCount: 0, groups: [] } }),
      );

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
            useValue: {
              getPage: vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto })),
            },
          },
          { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
          { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
          mushafStudySourceCatalogApiProvider,
          { provide: MushafSimilarAyahsApi, useValue: { getSimilarAyahs } },
          { provide: MushafAyahMutashabihatApi, useValue: { getAyahMutashabihat } },
        ],
      });

      const facade = TestBed.inject(MushafReaderFacade);
      const route = TestBed.inject(ActivatedRoute);
      facade.bindToRoute(route);

      expect(navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({
          queryParams: {
            page: 5,
            ayah: '2:25',
            panel: 'ayah',
            ayahTab: 'similar-ayahs',
          },
          replaceUrl: true,
        }),
      );

      queryParamMap$.next(
        convertToParamMap({
          page: '5',
          ayah: '2:25',
          panel: 'ayah',
          ayahTab: 'similar-ayahs',
        }),
      );

      expect(facade.ayahTab()).toBe('similar-ayahs');
      expect(facade.pageNumber()).toBe(5);
      expect(facade.selectedAyahKey()).toBe('2:25');
      await vi.waitFor(() => {
        expect(getSimilarAyahs).toHaveBeenCalledWith('2:25');
      });
      expect(getAyahMutashabihat).not.toHaveBeenCalled();
    });
  });
});
