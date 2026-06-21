import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafAyahMutashabihatApi } from '../data-access/mushaf-ayah-mutashabihat.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { MushafReaderFacade } from './mushaf-reader.facade';
import { mushafSimilarAyahsApiProvider, mushafStudySourceCatalogApiProvider } from './mushaf-study-source-catalog.api.mock';

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
    mutashabihatGroupCount: 2,
    mutashabihatOccurrenceCount: 3,
  },
};

const mutashabihatDto = {
  verseKey: '2:25',
  groupCount: 1,
  groups: [
    {
      groupKey: 'mutashabihat:90001',
      sourceGroupId: 90001,
      representativeVerseKey: '2:25',
      representativeWordFrom: 1,
      representativeWordTo: 2,
      phraseTextUthmani: 'عبارة-تجريبية-أولى',
      occurrenceCount: 2,
      distinctAyahCount: 2,
      distinctSurahCount: 1,
      selectedOccurrences: [
        {
          verseKey: '2:25',
          wordFrom: 1,
          wordTo: 2,
          isRepresentative: true,
          phraseTextUthmani: 'عبارة-تجريبية-أولى',
        },
      ],
      occurrences: [
        {
          verseKey: '2:25',
          surahNumber: 2,
          surahNameArabic: 'البقرة',
          ayahNumber: 25,
          pageNumber: 5,
          wordFrom: 1,
          wordTo: 2,
          isSelectedAyah: true,
          isRepresentative: true,
          textUthmani: 'نص-مجموعة-أولى',
          phraseTextUthmani: 'عبارة-تجريبية-أولى',
        },
        {
          verseKey: '2:26',
          surahNumber: 2,
          surahNameArabic: 'البقرة',
          ayahNumber: 26,
          pageNumber: 5,
          wordFrom: 1,
          wordTo: 1,
          isSelectedAyah: false,
          isRepresentative: false,
          textUthmani: 'نص-مجموعة-ثان',
          phraseTextUthmani: 'كلمة-تجريبية',
        },
      ],
    },
  ],
};

function createFacade(initialParams: Record<string, string>) {
  const queryParamMap$ = new BehaviorSubject(convertToParamMap(initialParams));
  const getAyahMutashabihat = vi.fn(() => of({ isSuccess: true, message: 'ok', data: mutashabihatDto }));

  TestBed.configureTestingModule({
    providers: [
      MushafReaderFacade,
      mushafStudySourceCatalogApiProvider,
      mushafSimilarAyahsApiProvider,
      {
        provide: ActivatedRoute,
        useValue: { queryParamMap: queryParamMap$.asObservable() },
      },
      {
        provide: Router,
        useValue: { navigate: vi.fn().mockResolvedValue(true) },
      },
      {
        provide: MushafPagesApi,
        useValue: {
          getPage: vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto })),
        },
      },
      {
        provide: MushafAyahStudyApi,
        useValue: {
          getAyahStudy: vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto })),
        },
      },
      {
        provide: MushafAyahMutashabihatApi,
        useValue: { getAyahMutashabihat },
      },
      {
        provide: MushafWordAnalysisApi,
        useValue: {
          getWordAnalysis: vi.fn(() => of({ isSuccess: true, message: 'ok', data: null })),
        },
      },
    ],
  });

  const facade = TestBed.inject(MushafReaderFacade);
  facade.bindToRoute(TestBed.inject(ActivatedRoute));

  return { facade, getAyahMutashabihat, queryParamMap$ };
}

describe('MushafReaderFacade mutashabihat lazy loading (US3)', () => {
  it('does not request mutashabihat detail until the mutashabihat tab is active', () => {
    const { facade, getAyahMutashabihat } = createFacade({ page: '5', ayah: '2:25', panel: 'ayah' });

    expect(facade.selectedAyahKey()).toBe('2:25');
    expect(facade.ayahTab()).toBe('tafsir');
    expect(getAyahMutashabihat).not.toHaveBeenCalled();
    expect(facade.mutashabihat()).toBeNull();
  });

  it('lazy-loads mutashabihat when the mutashabihat tab becomes active', async () => {
    const { facade, getAyahMutashabihat } = createFacade({
      page: '5',
      ayah: '2:25',
      panel: 'ayah',
      ayahTab: 'mutashabihat',
    });

    await vi.waitFor(() => {
      expect(getAyahMutashabihat).toHaveBeenCalledWith('2:25');
    });

    expect(facade.mutashabihat()).toEqual(mutashabihatDto);
    expect(facade.mutashabihatLoadState()).toEqual({
      isLoading: false,
      isEmpty: false,
      errorMessage: null,
    });
  });

  it('dedupes repeated mutashabihat requests via the reader cache', async () => {
    const { facade, getAyahMutashabihat } = createFacade({
      page: '5',
      ayah: '2:25',
      panel: 'ayah',
      ayahTab: 'mutashabihat',
    });

    await vi.waitFor(() => {
      expect(facade.mutashabihat()).toEqual(mutashabihatDto);
    });

    facade.applyUrlState({
      panel: 'ayah',
      ayah: '2:25',
      word: null,
      segment: null,
      ayahTab: 'tafsir',
      wordTab: 'segments',
      sources: { tafsirSource: null, translationSource: null, fullI3rabSource: null },
    });

    facade.applyUrlState({
      panel: 'ayah',
      ayah: '2:25',
      word: null,
      segment: null,
      ayahTab: 'mutashabihat',
      wordTab: 'segments',
      sources: { tafsirSource: null, translationSource: null, fullI3rabSource: null },
    });

    expect(getAyahMutashabihat).toHaveBeenCalledTimes(1);
    expect(facade.mutashabihat()).toEqual(mutashabihatDto);
  });
});
