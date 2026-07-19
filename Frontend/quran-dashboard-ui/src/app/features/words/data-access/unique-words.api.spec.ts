import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { UniqueWordsApi } from './unique-words.api';
import {
  UniqueWordAyahMatchDto,
  UniqueWordListItemDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
} from '../models/unique-words.models';
import { ok, page, setupApiTestBed, teardownApiTestBed } from './testing/api-test-bed';

function matchUniqueWords(kind: string): RegExp {
  return new RegExp(`/api/words/unique/${kind}(\\?.*)?$`);
}

const SAMPLE_ITEM: UniqueWordListItemDto = {
  id: 1,
  kind: 'tashkeel',
  displayText: 'كلمة-تجريبية',
  occurrencesCount: 3,
  ayahsCount: 3,
  surahsCount: 3,
  missingSurahsCount: 111,
  primaryWordTypeCode: 'N',
  primaryWordTypeBroadArabicLabel: 'اسم',
  rootId: 5001,
  rootText: 'ك ل م',
};

describe('UniqueWordsApi.getList', () => {
  let api: UniqueWordsApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    ({ api, httpMock } = setupApiTestBed(UniqueWordsApi));
  });

  afterEach(() => {
    teardownApiTestBed(httpMock);
  });

  it('builds the list URL for the tashkeel mode with sort/page/pageSize params', async () => {
    const promise = firstValueFrom(api.getList('tashkeel', '', 'mushaf-order', 1, 50));

    const req = httpMock.expectOne((r) => matchUniqueWords('tashkeel').test(r.url));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('sort')).toBe('mushaf-order');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.has('search')).toBe(false);

    const response = page<UniqueWordListItemDto>([], 0);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('includes the search param when search is non-blank', async () => {
    const response = page<UniqueWordListItemDto>([SAMPLE_ITEM], 1, 2, 25);

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

    const response = page<UniqueWordListItemDto>([], 0);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('sends only the active count-range params (Feature 026)', async () => {
    const promise = firstValueFrom(
      api.getList('tashkeel', '', 'mushaf-order', 1, 1000, {
        occurrences: { min: 11, max: 100 },
        surahs: { min: 1, max: null },
      }),
    );

    const req = httpMock.expectOne((r) => matchUniqueWords('tashkeel').test(r.url));
    expect(req.request.params.get('occMin')).toBe('11');
    expect(req.request.params.get('occMax')).toBe('100');
    expect(req.request.params.get('surahsMin')).toBe('1');
    expect(req.request.params.has('surahsMax')).toBe(false);
    expect(req.request.params.has('ayahsMin')).toBe(false);

    const response = page<UniqueWordListItemDto>([], 0);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('omits every range param for an unfiltered read (backward compat)', async () => {
    const promise = firstValueFrom(api.getList('tashkeel', '', 'mushaf-order', 1, 1000));

    const req = httpMock.expectOne((r) => matchUniqueWords('tashkeel').test(r.url));
    expect(req.request.params.has('occMin')).toBe(false);
    expect(req.request.params.has('ayahsMax')).toBe(false);
    expect(req.request.params.has('surahsMin')).toBe(false);

    const response = page<UniqueWordListItemDto>([], 0);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('returns the typed ApiResponse<PagedResultDto<UniqueWordListItemDto>> shape', async () => {
    const response = page<UniqueWordListItemDto>([SAMPLE_ITEM], 1);

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
    ({ api, httpMock } = setupApiTestBed(UniqueWordsApi));
  });

  afterEach(() => {
    teardownApiTestBed(httpMock);
  });

  it('getMentionedSurahs calls the surahs endpoint', async () => {
    const response = ok<UniqueWordSurahsDto>({ surahs: [] });

    const promise = firstValueFrom(api.getMentionedSurahs('tashkeel', 1002));
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/words/unique/tashkeel/1002/surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getSummary calls the summary endpoint (kind/id, no suffix)', async () => {
    const response = ok<UniqueWordSummaryDto>({
      id: 1002,
      kind: 'tashkeel',
      displayText: 'كلمة-تجريبية',
      occurrencesCount: 5,
      ayahsCount: 5,
      surahsCount: 5,
      missingSurahsCount: 109,
    });

    const promise = firstValueFrom(api.getSummary('tashkeel', 1002));
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/words/unique/tashkeel/1002`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getMissingSurahs calls the missing-surahs endpoint', async () => {
    const response = ok<UniqueWordMissingSurahsDto>({ surahs: [] });

    const promise = firstValueFrom(api.getMissingSurahs('simple', 42));
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/words/unique/simple/42/missing-surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getAyahMatches calls the ayahs endpoint with page and pageSize', async () => {
    const response = page<UniqueWordAyahMatchDto>([], 0, 2, 10);

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
