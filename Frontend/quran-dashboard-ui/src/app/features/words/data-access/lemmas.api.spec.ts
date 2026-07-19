import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LemmasApi } from './lemmas.api';
import {
  LemmaAyahMatchDto,
  LemmaListItemDto,
  LemmaMissingSurahsDto,
  LemmaStemsDto,
  LemmaSummaryDto,
  LemmaSurahsDto,
  LemmaWordItemDto,
  LemmasAssociation,
} from '../models/lemmas.models';
import { EMPTY_RANGE_FILTERS, RangeFilters } from '../state/words-range-filters';
import { ok, page, setupApiTestBed, teardownApiTestBed } from './testing/api-test-bed';

const BASE = `${environment.apiBaseUrl}/api/words/lemmas`;

const SAMPLE_LEMMA: LemmaListItemDto = {
  id: 6410,
  lemmaText: 'كَتَبَ',
  occurrencesCount: 14,
  ayahsCount: 11,
  surahsCount: 7,
  simpleWordsCount: 6,
  tashkeelWordsCount: 8,
  rootId: 4210,
  rootText: 'ك ت ب',
  stemsCount: 2,
};

describe('LemmasApi', () => {
  let api: LemmasApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    ({ api, httpMock } = setupApiTestBed(LemmasApi));
  });

  afterEach(() => {
    teardownApiTestBed(httpMock);
  });

  it('getLemmasList sends sort/page/pageSize and omits search when blank', async () => {
    const promise = firstValueFrom(api.getLemmasList('', 'occurrences', 1, 25));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('sort')).toBe('occurrences');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('rootId')).toBe(false);

    const response = page<LemmaListItemDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getLemmasList sends the trimmed search param when non-blank', async () => {
    const promise = firstValueFrom(api.getLemmasList('  كتب  ', 'alpha', 2, 50));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.get('search')).toBe('كتب');

    const response = page<LemmaListItemDto>([SAMPLE_LEMMA], 1, 2, 50);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getLemmasList sends only the active range params (Feature 026, US5)', async () => {
    const ranges: RangeFilters = {
      occurrences: { min: 5, max: 60 },
      stems: { min: 1, max: null },
    };
    const promise = firstValueFrom(api.getLemmasList('', 'mushaf-order', 1, 1000, ranges));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.get('occMin')).toBe('5');
    expect(req.request.params.get('occMax')).toBe('60');
    expect(req.request.params.get('stemsMin')).toBe('1');
    expect(req.request.params.has('stemsMax')).toBe(false);
    expect(req.request.params.has('ayahsMin')).toBe(false);

    req.flush(page<LemmaListItemDto>([]));
    await promise;
  });

  it('getLemmasList sends the rootId association param when set (Feature 026, US7)', async () => {
    const association: LemmasAssociation = { rootId: 4210 };
    const promise = firstValueFrom(
      api.getLemmasList('', 'mushaf-order', 1, 1000, EMPTY_RANGE_FILTERS, association),
    );

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.get('rootId')).toBe('4210');

    req.flush(page<LemmaListItemDto>([]));
    await promise;
  });

  it('getLemmasList omits rootId when the association is unset (backward compat)', async () => {
    const promise = firstValueFrom(api.getLemmasList('', 'mushaf-order', 1, 1000));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.has('rootId')).toBe(false);

    req.flush(page<LemmaListItemDto>([]));
    await promise;
  });

  it('getLemmaSummary calls GET /api/words/lemmas/{id}', async () => {
    const response = ok<LemmaSummaryDto>({ ...SAMPLE_LEMMA, typeDistribution: [] });
    const promise = firstValueFrom(api.getLemmaSummary(6410));

    const req = httpMock.expectOne(`${BASE}/6410`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it.each([
    ['simple' as const, `${BASE}/6410/words/simple`],
    ['tashkeel' as const, `${BASE}/6410/words/tashkeel`],
  ])('getLemmaWords encodes the %s wordView into the path', async (wordView, expectedUrl) => {
    const promise = firstValueFrom(api.getLemmaWords(6410, wordView, 1, 25));

    const req = httpMock.expectOne(
      (r) => r.url === expectedUrl && r.params.get('page') === '1' && r.params.get('pageSize') === '25',
    );
    expect(req.request.method).toBe('GET');

    const response = page<LemmaWordItemDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getLemmaAyahMatches sends page/pageSize without typeCode when omitted', async () => {
    const promise = firstValueFrom(api.getLemmaAyahMatches(6410, 2, 10));

    const req = httpMock.expectOne(
      (r) => r.url === `${BASE}/6410/ayahs` && r.params.get('page') === '2' && r.params.get('pageSize') === '10',
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('typeCode')).toBe(false);

    const response = page<LemmaAyahMatchDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getLemmaAyahMatches sends the trimmed typeCode param when provided', async () => {
    const promise = firstValueFrom(api.getLemmaAyahMatches(6410, 1, 25, '  V  '));

    const req = httpMock.expectOne((r) => r.url === `${BASE}/6410/ayahs`);
    expect(req.request.params.get('typeCode')).toBe('V');

    req.flush(page<LemmaAyahMatchDto>([]));
    await promise;
  });

  it('getLemmaMentionedSurahs calls GET .../surahs', async () => {
    const response = ok<LemmaSurahsDto>({ surahs: [] });
    const promise = firstValueFrom(api.getLemmaMentionedSurahs(6410));

    const req = httpMock.expectOne(`${BASE}/6410/surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getLemmaMissingSurahs calls GET .../missing-surahs', async () => {
    const response = ok<LemmaMissingSurahsDto>({ surahs: [] });
    const promise = firstValueFrom(api.getLemmaMissingSurahs(6410));

    const req = httpMock.expectOne(`${BASE}/6410/missing-surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getLemmaStems calls GET .../stems', async () => {
    const response = ok<LemmaStemsDto>({ stems: [] });
    const promise = firstValueFrom(api.getLemmaStems(6410));

    const req = httpMock.expectOne(`${BASE}/6410/stems`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });
});
