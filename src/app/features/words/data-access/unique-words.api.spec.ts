import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { TestBed, getTestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { UniqueWordsApi } from './unique-words.api';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  PagedResultDto,
  UniqueWordAyahMatchDto,
  UniqueWordListItemDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSurahsDto,
} from '../models/unique-words.models';

/** Match the list request URL by path + params, independent of the configured base URL. */
function matchUniqueWords(kind: string): RegExp {
  return new RegExp(`/api/words/unique/${kind}(\\?.*)?$`);
}

/** Source-safe synthetic placeholder — not Quranic text. */
const SAMPLE_ITEM: UniqueWordListItemDto = {
  id: 1,
  kind: 'tashkeel',
  displayTextUthmani: 'كلمة-تجريبية',
  occurrencesCount: 3,
  ayahsCount: 3,
  surahsCount: 3,
  missingSurahsCount: 111,
  firstVerseKey: '1:1',
  firstLocation: '1:1:1',
};

function page(items: UniqueWordListItemDto[], totalCount: number): PagedResultDto<UniqueWordListItemDto> {
  return { page: 1, pageSize: 50, totalCount, items };
}

describe('UniqueWordsApi.getList', () => {
  let api: UniqueWordsApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      providers: [UniqueWordsApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(UniqueWordsApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    getTestBed().resetTestingModule();
  });

  it('builds the list URL for the tashkeel mode with sort/page/pageSize params', async () => {
    const promise = firstValueFrom(api.getList('tashkeel', '', 'mushaf-order', 1, 50));

    const req = httpMock.expectOne((r) => matchUniqueWords('tashkeel').test(r.url));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('sort')).toBe('mushaf-order');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.has('search')).toBe(false);
    req.flush({ isSuccess: true, message: 'ok', data: page([], 0) });

    await expect(promise).resolves.toEqual({ isSuccess: true, message: 'ok', data: page([], 0) });
  });

  it('includes the search param when search is non-blank', async () => {
    const response: ApiResponse<PagedResultDto<UniqueWordListItemDto>> = {
      isSuccess: true,
      message: 'ok',
      data: page([SAMPLE_ITEM], 1),
    };

    const promise = firstValueFrom(api.getList('simple', 'اسم', 'alpha', 2, 25));

    const req = httpMock.expectOne((r) => matchUniqueWords('simple').test(r.url));
    expect(req.request.params.get('search')).toBe('اسم');
    expect(req.request.params.get('sort')).toBe('alpha');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('omits the search param when search is blank', async () => {
    const promise = firstValueFrom(api.getList('tashkeel', '   ', 'occurrences', 1, 50));

    const req = httpMock.expectOne((r) => matchUniqueWords('tashkeel').test(r.url));
    expect(req.request.params.has('search')).toBe(false);
    req.flush({ isSuccess: true, message: 'ok', data: page([], 0) });

    await expect(promise).resolves.toEqual({ isSuccess: true, message: 'ok', data: page([], 0) });
  });

  it('returns the typed ApiResponse<PagedResultDto<UniqueWordListItemDto>> shape', async () => {
    const response: ApiResponse<PagedResultDto<UniqueWordListItemDto>> = {
      isSuccess: true,
      message: 'تم',
      data: page([SAMPLE_ITEM], 1),
    };

    const promise = firstValueFrom(api.getList('tashkeel', '', 'mushaf-order', 1, 50));

    httpMock.expectOne((r) => matchUniqueWords('tashkeel').test(r.url)).flush(response);

    const emitted = await promise;
    expect(emitted.isSuccess).toBe(true);
    expect(emitted.data?.items).toHaveLength(1);
    expect(emitted.data?.items[0]?.id).toBe(1);
  });
});

describe('UniqueWordsApi drill-down HTTP', () => {
  let api: UniqueWordsApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      providers: [UniqueWordsApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(UniqueWordsApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    getTestBed().resetTestingModule();
  });

  it('getMentionedSurahs calls the surahs endpoint', async () => {
    const response: ApiResponse<UniqueWordSurahsDto> = {
      isSuccess: true,
      message: 'تم',
      data: {
        id: 1002,
        kind: 'tashkeel',
        displayTextUthmani: 'كلمة-تجريبية',
        surahsCount: 0,
        surahs: [],
      },
    };

    const promise = firstValueFrom(api.getMentionedSurahs('tashkeel', 1002));
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/words/unique/tashkeel/1002/surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getMissingSurahs calls the missing-surahs endpoint', async () => {
    const response: ApiResponse<UniqueWordMissingSurahsDto> = {
      isSuccess: true,
      message: 'تم',
      data: {
        id: 1002,
        kind: 'tashkeel',
        displayTextUthmani: 'كلمة-تجريبية',
        missingSurahsCount: 0,
        surahs: [],
      },
    };

    const promise = firstValueFrom(api.getMissingSurahs('simple', 42));
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/words/unique/simple/42/missing-surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getAyahMatches calls the ayahs endpoint with page and pageSize', async () => {
    const response: ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>> = {
      isSuccess: true,
      message: 'تم',
      data: { page: 2, pageSize: 10, totalCount: 0, items: [] },
    };

    const promise = firstValueFrom(api.getAyahMatches('tashkeel', 2003, 2, 10));
    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiBaseUrl}/api/words/unique/tashkeel/2003/ayahs` &&
        r.params.get('page') === '2' &&
        r.params.get('pageSize') === '10',
    );
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });
});
