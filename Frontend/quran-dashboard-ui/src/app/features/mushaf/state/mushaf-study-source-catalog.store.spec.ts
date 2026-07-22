import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafStudySourceCatalogApi } from '../data-access/mushaf-study-sources.api';
import { MushafStudySourceCatalogDto, StudySourceCatalogItemDto } from '../models/mushaf.models';
import { MushafStudySourceCatalogStore } from './mushaf-study-source-catalog.store';

function placeholderItem(sourceKey: string, displayName: string): StudySourceCatalogItemDto {
  return {
    direction: 'rtl',
    displayNameAr: displayName,
    displayNameEn: null,
    languageCode: 'xx',
    languageNameAr: 'Placeholder language',
    sourceKey,
    tafsirKind: null,
    translationType: null,
  };
}

const populatedCatalog: MushafStudySourceCatalogDto = {
  tafsirSources: [placeholderItem('placeholder-tafsir-a', 'Placeholder tafsir A')],
  translationSources: [
    placeholderItem('placeholder-translation-a', 'Placeholder translation A'),
    placeholderItem('placeholder-translation-b', 'Placeholder translation B'),
  ],
  fullI3rabSources: [placeholderItem('placeholder-i3rab-a', 'Placeholder i3rab A')],
};

const emptyCatalog: MushafStudySourceCatalogDto = {
  tafsirSources: [],
  translationSources: [],
  fullI3rabSources: [],
};

function successResponse(
  data: MushafStudySourceCatalogDto,
): ApiResponse<MushafStudySourceCatalogDto> {
  return { isSuccess: true, message: 'ok', data };
}

function createStore(getCatalog: ReturnType<typeof vi.fn>): MushafStudySourceCatalogStore {
  TestBed.configureTestingModule({
    providers: [
      MushafStudySourceCatalogStore,
      { provide: MushafStudySourceCatalogApi, useValue: { getCatalog } },
    ],
  });
  return TestBed.inject(MushafStudySourceCatalogStore);
}

describe('MushafStudySourceCatalogStore lifecycle (F2)', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('deduplicates concurrent loads while the first request is still in flight', () => {
    const catalog$ = new Subject<ApiResponse<MushafStudySourceCatalogDto>>();
    const getCatalog = vi.fn(() => catalog$.asObservable());
    const store = createStore(getCatalog);

    store.load();
    store.load();
    expect(getCatalog).toHaveBeenCalledTimes(1);

    catalog$.next(successResponse(populatedCatalog));
    catalog$.complete();

    expect(store.tafsirSourceOptions().map((option) => option.key)).toEqual(['placeholder-tafsir-a']);
    expect(store.translationSourceOptions()).toHaveLength(2);
    expect(store.fullI3rabSourceOptions()).toHaveLength(1);
  });

  it('treats an empty-but-successful response as loaded and does not refetch', () => {
    const getCatalog = vi.fn(() => of(successResponse(emptyCatalog)));
    const store = createStore(getCatalog);

    store.load();
    expect(getCatalog).toHaveBeenCalledTimes(1);
    expect(store.tafsirSourceOptions()).toEqual([]);
    expect(store.translationSourceOptions()).toEqual([]);
    expect(store.fullI3rabSourceOptions()).toEqual([]);

    store.load();
    expect(getCatalog).toHaveBeenCalledTimes(1);
  });

  it('caches a successful catalogue so a later load reuses it without refetching', () => {
    const getCatalog = vi.fn(() => of(successResponse(populatedCatalog)));
    const store = createStore(getCatalog);

    store.load();
    expect(getCatalog).toHaveBeenCalledTimes(1);
    const firstOptions = store.tafsirSourceOptions();
    expect(firstOptions.map((option) => option.key)).toEqual(['placeholder-tafsir-a']);

    store.load();
    expect(getCatalog).toHaveBeenCalledTimes(1);
    expect(store.tafsirSourceOptions()).toBe(firstOptions);
  });

  it('retries the fetch after a failed load instead of caching the failure', () => {
    const getCatalog = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })))
      .mockReturnValueOnce(of(successResponse(populatedCatalog)));
    const store = createStore(getCatalog);

    store.load();
    expect(getCatalog).toHaveBeenCalledTimes(1);
    expect(store.tafsirSourceOptions()).toEqual([]);

    store.load();
    expect(getCatalog).toHaveBeenCalledTimes(2);
    expect(store.tafsirSourceOptions().map((option) => option.key)).toEqual(['placeholder-tafsir-a']);
  });
});
