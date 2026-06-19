import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafSurahCatalogApi } from '../data-access/mushaf-surah-catalog.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { AyahStudyDto, WordAnalysisDto, DEFAULT_MUSHAF_READER_STATE } from '../models/mushaf.models';
import { MushafReaderFacade } from './mushaf-reader.facade';
import { saveMushafReaderSession } from './mushaf-reader-session';
import { MushafUrlSnapshot } from './mushaf-url-sync';
import { mushafStudySourceCatalogApiProvider } from './mushaf-study-source-catalog.api.mock';

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
          getWordAnalysis: vi.fn(() => of({ isSuccess: true, message: 'ok', data: wordAnalysisDto })),
        },
      },
      { provide: MushafSurahCatalogApi, useValue: { getCatalog: vi.fn() } },
      mushafStudySourceCatalogApiProvider,
    ],
  });

  return {
    facade: TestBed.inject(MushafReaderFacade),
    route: TestBed.inject(ActivatedRoute),
    navigate,
    queryParamMap$,
    getAyahStudy,
    getPage,
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

  it('writes URL updates with replaceUrl merge semantics when selecting an ayah', () => {
    const { facade, route, navigate } = createFacadeTestBed({ page: '5', word: '2:25:3' });
    facade.bindToRoute(route);

    facade.selectAyah('2:26');

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { ayah: '2:26', panel: 'ayah', word: null, segment: null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      }),
    );
  });

  it('sets ayah from the selected word location when selecting a word', () => {
    const { facade, route, navigate } = createFacadeTestBed({ page: '5' });
    facade.bindToRoute(route);

    facade.selectWord('2:25:3');

    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { word: '2:25:3', ayah: '2:25', panel: 'word' },
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
