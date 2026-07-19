import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { StemsApi } from './stems.api';
import {
  StemAyahMatchDto,
  StemLemmasDto,
  StemListItemDto,
  StemMissingSurahsDto,
  StemSummaryDto,
  StemSurahsDto,
  StemWordItemDto,
  StemsAssociation,
} from '../models/stems.models';
import { EMPTY_RANGE_FILTERS, RangeFilters } from '../state/words-range-filters';
import { ok, page, setupApiTestBed, teardownApiTestBed } from './testing/api-test-bed';

const BASE = `${environment.apiBaseUrl}/api/words/stems`;

const SAMPLE_STEM: StemListItemDto = {
  id: 5310,
  stemText: 'كَتَبَ',
  occurrencesCount: 12,
  ayahsCount: 9,
  surahsCount: 6,
  simpleWordsCount: 5,
  tashkeelWordsCount: 7,
  rootId: 4210,
  rootText: 'ك ت ب',
  lemmaId: 6410,
  lemmaText: 'كَتَبَ',
};

describe('StemsApi', () => {
  let api: StemsApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    ({ api, httpMock } = setupApiTestBed(StemsApi));
  });

  afterEach(() => {
    teardownApiTestBed(httpMock);
  });

  it('getStemsList sends sort/page/pageSize and omits search when blank', async () => {
    const promise = firstValueFrom(api.getStemsList('', 'occurrences', 1, 25));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('sort')).toBe('occurrences');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('rootId')).toBe(false);
    expect(req.request.params.has('lemmaId')).toBe(false);

    const response = page<StemListItemDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getStemsList sends the trimmed search param when non-blank', async () => {
    const promise = firstValueFrom(api.getStemsList('  كتب  ', 'alpha', 2, 50));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.get('search')).toBe('كتب');

    const response = page<StemListItemDto>([SAMPLE_STEM], 1, 2, 50);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getStemsList sends only the active range params (Feature 026, US5)', async () => {
    const ranges: RangeFilters = {
      occurrences: { min: 5, max: 50 },
      ayahs: { min: 1, max: null },
    };
    const promise = firstValueFrom(api.getStemsList('', 'mushaf-order', 1, 1000, ranges));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.get('occMin')).toBe('5');
    expect(req.request.params.get('occMax')).toBe('50');
    expect(req.request.params.get('ayahsMin')).toBe('1');
    expect(req.request.params.has('ayahsMax')).toBe(false);
    expect(req.request.params.has('surahsMin')).toBe(false);

    req.flush(page<StemListItemDto>([]));
    await promise;
  });

  it('getStemsList sends the rootId/lemmaId association params when set (Feature 026, US7)', async () => {
    const association: StemsAssociation = { rootId: 4210, lemmaId: 6410 };
    const promise = firstValueFrom(
      api.getStemsList('', 'mushaf-order', 1, 1000, EMPTY_RANGE_FILTERS, association),
    );

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.get('rootId')).toBe('4210');
    expect(req.request.params.get('lemmaId')).toBe('6410');

    req.flush(page<StemListItemDto>([]));
    await promise;
  });

  it('getStemsList omits rootId/lemmaId when the association is unset (backward compat)', async () => {
    const promise = firstValueFrom(api.getStemsList('', 'mushaf-order', 1, 1000));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.has('rootId')).toBe(false);
    expect(req.request.params.has('lemmaId')).toBe(false);

    req.flush(page<StemListItemDto>([]));
    await promise;
  });

  it('getStemSummary calls GET /api/words/stems/{id}', async () => {
    const response = ok<StemSummaryDto>({ ...SAMPLE_STEM, typeDistribution: [] });
    const promise = firstValueFrom(api.getStemSummary(5310));

    const req = httpMock.expectOne(`${BASE}/5310`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it.each([
    ['simple' as const, `${BASE}/5310/words/simple`],
    ['tashkeel' as const, `${BASE}/5310/words/tashkeel`],
  ])('getStemWords encodes the %s wordView into the path', async (wordView, expectedUrl) => {
    const promise = firstValueFrom(api.getStemWords(5310, wordView, 1, 25));

    const req = httpMock.expectOne(
      (r) => r.url === expectedUrl && r.params.get('page') === '1' && r.params.get('pageSize') === '25',
    );
    expect(req.request.method).toBe('GET');

    const response = page<StemWordItemDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getStemAyahMatches sends page/pageSize without typeCode when omitted', async () => {
    const promise = firstValueFrom(api.getStemAyahMatches(5310, 2, 10));

    const req = httpMock.expectOne(
      (r) => r.url === `${BASE}/5310/ayahs` && r.params.get('page') === '2' && r.params.get('pageSize') === '10',
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('typeCode')).toBe(false);

    const response = page<StemAyahMatchDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getStemAyahMatches sends the trimmed typeCode param when provided', async () => {
    const promise = firstValueFrom(api.getStemAyahMatches(5310, 1, 25, '  V  '));

    const req = httpMock.expectOne((r) => r.url === `${BASE}/5310/ayahs`);
    expect(req.request.params.get('typeCode')).toBe('V');

    req.flush(page<StemAyahMatchDto>([]));
    await promise;
  });

  it('getStemMentionedSurahs calls GET .../surahs', async () => {
    const response = ok<StemSurahsDto>({ surahs: [] });
    const promise = firstValueFrom(api.getStemMentionedSurahs(5310));

    const req = httpMock.expectOne(`${BASE}/5310/surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getStemMissingSurahs calls GET .../missing-surahs', async () => {
    const response = ok<StemMissingSurahsDto>({ surahs: [] });
    const promise = firstValueFrom(api.getStemMissingSurahs(5310));

    const req = httpMock.expectOne(`${BASE}/5310/missing-surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getStemLemmas calls GET .../lemmas', async () => {
    const response = ok<StemLemmasDto>({ lemmas: [] });
    const promise = firstValueFrom(api.getStemLemmas(5310));

    const req = httpMock.expectOne(`${BASE}/5310/lemmas`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });
});
