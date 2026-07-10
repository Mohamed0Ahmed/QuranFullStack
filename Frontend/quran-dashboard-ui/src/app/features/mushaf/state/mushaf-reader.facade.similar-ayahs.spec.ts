import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafSimilarAyahsApi } from '../data-access/mushaf-similar-ayahs.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { MushafReaderFacade } from './mushaf-reader.facade';
import { mushafAyahMutashabihatApiProvider, mushafStudySourceCatalogApiProvider } from './mushaf-study-source-catalog.api.mock';

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
    similarAyahCount: 2,
    mutashabihatGroupCount: 0,
    mutashabihatOccurrenceCount: 0,
  },
};

const similarAyahsDto = {
  verseKey: '2:25',
  count: 1,
  items: [
    {
      targetVerseKey: '2:26',
      surahNumber: 2,
      surahNameArabic: 'البقرة',
      ayahNumber: 26,
      pageNumber: 5,
      juzNumber: 1,
      hizbNumber: 1,
      rubNumber: 2,
      textUthmani: 'نص-مرتبط-تجريبي',
      score: 80,
      coverage: 90,
      matchedWordsCount: 3,
      relationshipDirection: 'bidirectional' as const,
      hasReverseLink: true,
    },
  ],
};

function createFacade(initialParams: Record<string, string>) {
  const queryParamMap$ = new BehaviorSubject(convertToParamMap(initialParams));
  const getSimilarAyahs = vi.fn(() => of({ isSuccess: true, message: 'ok', data: similarAyahsDto }));

  TestBed.configureTestingModule({
    providers: [
      MushafReaderFacade,
      mushafStudySourceCatalogApiProvider,
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
        provide: MushafSimilarAyahsApi,
        useValue: { getSimilarAyahs },
      },
      mushafAyahMutashabihatApiProvider,
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

  return { facade, getSimilarAyahs, queryParamMap$ };
}

describe('MushafReaderFacade similar ayahs lazy loading (US2)', () => {
  it('does not request similar ayahs detail until the similar-ayahs tab is active', () => {
    const { facade, getSimilarAyahs } = createFacade({ page: '5', ayah: '2:25', panel: 'ayah' });

    expect(facade.selectedAyahKey()).toBe('2:25');
    expect(facade.ayahTab()).toBe('tafsir');
    expect(getSimilarAyahs).not.toHaveBeenCalled();
    expect(facade.similarAyahs()).toBeNull();
  });

  it('lazy-loads similar ayahs when the similar-ayahs tab becomes active', async () => {
    const { facade, getSimilarAyahs } = createFacade({
      page: '5',
      ayah: '2:25',
      panel: 'ayah',
      ayahTab: 'similar-ayahs',
    });

    await vi.waitFor(() => {
      expect(getSimilarAyahs).toHaveBeenCalledWith('2:25');
    });

    expect(facade.similarAyahs()).toEqual(similarAyahsDto);
    expect(facade.similarAyahsLoadState()).toEqual({
      isLoading: false,
      isEmpty: false,
      errorMessage: null,
    });
  });

  it('dedupes repeated similar ayahs requests via the reader cache', async () => {
    const { facade, getSimilarAyahs } = createFacade({
      page: '5',
      ayah: '2:25',
      panel: 'ayah',
      ayahTab: 'similar-ayahs',
    });

    await vi.waitFor(() => {
      expect(facade.similarAyahs()).toEqual(similarAyahsDto);
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
      ayahTab: 'similar-ayahs',
      wordTab: 'segments',
      sources: { tafsirSource: null, translationSource: null, fullI3rabSource: null },
    });

    expect(getSimilarAyahs).toHaveBeenCalledTimes(1);
    expect(facade.similarAyahs()).toEqual(similarAyahsDto);
  });

  it('does not re-fetch similar ayahs when focusAyah auto-clears', async () => {
    vi.useFakeTimers();

    try {
      const { facade, getSimilarAyahs, queryParamMap$ } = createFacade({
        page: '5',
        ayah: '2:25',
        panel: 'ayah',
        ayahTab: 'similar-ayahs',
      });

      await vi.waitFor(() => {
        expect(getSimilarAyahs).toHaveBeenCalledTimes(1);
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

      vi.advanceTimersByTime(3000);
      queryParamMap$.next(
        convertToParamMap({
          page: '92',
          ayah: '2:25',
          ayahTab: 'similar-ayahs',
        }),
      );

      expect(getSimilarAyahs).toHaveBeenCalledTimes(1);
    } finally {
      vi.useRealTimers();
    }
  });
});
