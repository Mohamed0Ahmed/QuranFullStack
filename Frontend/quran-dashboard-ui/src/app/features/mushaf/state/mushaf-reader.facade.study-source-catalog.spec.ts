import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafStudySourceCatalogApi } from '../data-access/mushaf-study-sources.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { MushafStudySourceCatalogDto, StudySourceCatalogItemDto } from '../models/mushaf.models';
import { MushafReaderFacade } from './mushaf-reader.facade';
import {
  mushafAyahMutashabihatApiProvider,
  mushafSimilarAyahsApiProvider,
} from './mushaf-study-source-catalog.api.mock';

// F2 regression coverage: loadStudySourceCatalog must not refetch on every mount, must
// treat an empty-but-successful catalogue as "loaded" (not "not loaded"), and must keep
// a failed load retryable.

const catalogItem: StudySourceCatalogItemDto = {
  direction: 'rtl',
  displayNameAr: 'التفسير الميسر',
  displayNameEn: null,
  languageCode: 'ar',
  languageNameAr: 'العربية',
  sourceKey: 'ar-muyassar',
  tafsirKind: 'brief',
  translationType: null,
};

const populatedCatalogDto: MushafStudySourceCatalogDto = {
  tafsirSources: [catalogItem],
  translationSources: [catalogItem],
  fullI3rabSources: [catalogItem],
};

const emptyCatalogDto: MushafStudySourceCatalogDto = {
  tafsirSources: [],
  translationSources: [],
  fullI3rabSources: [],
};

function createFacadeTestBed(getCatalog: ReturnType<typeof vi.fn>) {
  TestBed.configureTestingModule({
    providers: [
      MushafReaderFacade,
      { provide: MushafPagesApi, useValue: { getPage: vi.fn() } },
      { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
      { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
      { provide: MushafStudySourceCatalogApi, useValue: { getCatalog } },
      mushafSimilarAyahsApiProvider,
      mushafAyahMutashabihatApiProvider,
    ],
  });

  return TestBed.inject(MushafReaderFacade);
}

describe('MushafReaderFacade.loadStudySourceCatalog (F2)', () => {
  it('does not re-issue GET /api/mushaf/study-sources on a second mount', () => {
    const getCatalog = vi.fn(() =>
      of<ApiResponse<MushafStudySourceCatalogDto>>({
        isSuccess: true,
        message: 'ok',
        data: populatedCatalogDto,
      }),
    );
    const facade = createFacadeTestBed(getCatalog);

    // First mount.
    facade.loadStudySourceCatalog();
    expect(getCatalog).toHaveBeenCalledTimes(1);
    expect(facade.tafsirSourceOptions()).toHaveLength(1);

    // Second mount (e.g. navigating away and back to the Mushaf page).
    facade.loadStudySourceCatalog();
    expect(getCatalog).toHaveBeenCalledTimes(1);
    expect(facade.tafsirSourceOptions()).toHaveLength(1);
  });

  it('treats an empty-but-successful catalogue as loaded, not as "not loaded"', () => {
    const getCatalog = vi.fn(() =>
      of<ApiResponse<MushafStudySourceCatalogDto>>({
        isSuccess: true,
        message: 'ok',
        data: emptyCatalogDto,
      }),
    );
    const facade = createFacadeTestBed(getCatalog);

    facade.loadStudySourceCatalog();
    expect(getCatalog).toHaveBeenCalledTimes(1);
    expect(facade.tafsirSourceOptions()).toEqual([]);
    expect(facade.translationSourceOptions()).toEqual([]);
    expect(facade.fullI3rabSourceOptions()).toEqual([]);

    // A second mount must still not refetch, even though the first result was empty.
    facade.loadStudySourceCatalog();
    expect(getCatalog).toHaveBeenCalledTimes(1);
  });

  it('keeps a failed catalogue load retryable', () => {
    const getCatalog = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })))
      .mockReturnValueOnce(
        of<ApiResponse<MushafStudySourceCatalogDto>>({
          isSuccess: true,
          message: 'ok',
          data: populatedCatalogDto,
        }),
      );
    const facade = createFacadeTestBed(getCatalog);

    facade.loadStudySourceCatalog();
    expect(getCatalog).toHaveBeenCalledTimes(1);
    expect(facade.tafsirSourceOptions()).toEqual([]);

    // Retry after failure must issue a fresh request rather than staying cached as failed.
    facade.loadStudySourceCatalog();
    expect(getCatalog).toHaveBeenCalledTimes(2);
    expect(facade.tafsirSourceOptions()).toHaveLength(1);
  });
});
