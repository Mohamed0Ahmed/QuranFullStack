import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafSurahCatalogApi } from '../data-access/mushaf-surah-catalog.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { MushafReaderFacade } from './mushaf-reader.facade';

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

describe('MushafReaderFacade.loadPage', () => {
  it('clamps page numbers to the Mushaf range 1–604 before calling the API', () => {
    const getPage = vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafSurahCatalogApi, useValue: { getCatalog: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
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
        { provide: MushafSurahCatalogApi, useValue: { getCatalog: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
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
        { provide: MushafSurahCatalogApi, useValue: { getCatalog: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
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
  it('resolves a surah start page from the loaded catalog', () => {
    const getCatalog = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'ok',
        data: {
          surahs: [{ surahNumber: 2, nameArabic: 'البقرة', startPageNumber: 5 }],
        },
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage: vi.fn() } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafSurahCatalogApi, useValue: { getCatalog } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadSurahCatalog();

    expect(facade.resolveSurahStartPage(2)).toBe(5);
    expect(facade.resolveSurahStartPage(99)).toBeNull();
  });
});
