import { describe, expect, it, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { UniqueWordsApi } from './unique-words.api';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { PagedResultDto, UniqueWordListItemDto } from '../models/unique-words.models';

/** Match the request URL by path + params, independent of the configured base URL. */
function matchUniqueWords(kind: string): RegExp {
  return new RegExp(`/api/words/unique/${kind}(\\?.*)?$`);
}

/**
 * Source-safe synthetic placeholder — not Quranic text. Used only to assert
 * DTO typing/passthrough, never compared to canonical Quran content.
 */
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

  afterEach(() => httpMock.verify());

  function setup(): void {
    TestBed.configureTestingModule({
      providers: [UniqueWordsApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(UniqueWordsApi);
    httpMock = TestBed.inject(HttpTestingController);
  }

  it('builds the list URL for the tashkeel mode with sort/page/pageSize params', () => {
    setup();
    api.getList('tashkeel', '', 'mushaf-order', 1, 50).subscribe();

    const req = httpMock.expectOne((r) => matchUniqueWords('tashkeel').test(r.url));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('sort')).toBe('mushaf-order');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.has('search')).toBe(false);
    req.flush({ isSuccess: true, message: 'ok', data: page([], 0) });
  });

  it('includes the search param when search is non-blank', () => {
    setup();
    api.getList('simple', 'اسم', 'alpha', 2, 25).subscribe();

    const req = httpMock.expectOne((r) => matchUniqueWords('simple').test(r.url));
    expect(req.request.params.get('search')).toBe('اسم');
    expect(req.request.params.get('sort')).toBe('alpha');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    req.flush({ isSuccess: true, message: 'ok', data: page([SAMPLE_ITEM], 1) });
  });

  it('omits the search param when search is blank', () => {
    setup();
    api.getList('tashkeel', '   ', 'occurrences', 1, 50).subscribe();

    const req = httpMock.expectOne((r) => matchUniqueWords('tashkeel').test(r.url));
    expect(req.request.params.has('search')).toBe(false);
    req.flush({ isSuccess: true, message: 'ok', data: page([], 0) });
  });

  it('returns the typed ApiResponse<PagedResultDto<UniqueWordListItemDto>> shape', () => {
    setup();
    let emitted: ApiResponse<PagedResultDto<UniqueWordListItemDto>> | undefined;
    api.getList('tashkeel', '', 'mushaf-order', 1, 50).subscribe((r) => (emitted = r));

    httpMock
      .expectOne((r) => matchUniqueWords('tashkeel').test(r.url))
      .flush({ isSuccess: true, message: 'تم', data: page([SAMPLE_ITEM], 1) });

    expect(emitted?.isSuccess).toBe(true);
    expect(emitted?.data?.items).toHaveLength(1);
    expect(emitted?.data?.items[0]?.id).toBe(1);
  });
});
