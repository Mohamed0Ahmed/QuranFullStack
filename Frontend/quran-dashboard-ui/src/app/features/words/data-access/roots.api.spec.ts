import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { RootsApi } from './roots.api';
import {
  RootAyahMatchDto,
  RootLemmasDto,
  RootListItemDto,
  RootMissingSurahsDto,
  RootStemsDto,
  RootSummaryDto,
  RootSurahsDto,
  RootWordItemDto,
} from '../models/roots.models';
import { EMPTY_RANGE_FILTERS, RangeFilters } from '../state/words-range-filters';
import { ok, page, setupApiTestBed, teardownApiTestBed } from './testing/api-test-bed';

const BASE = `${environment.apiBaseUrl}/api/words/roots`;

const SAMPLE_ROOT: RootListItemDto = {
  id: 4210,
  rootText: 'ك ت ب',
  occurrencesCount: 10,
  ayahsCount: 8,
  surahsCount: 5,
  simpleWordsCount: 4,
  tashkeelWordsCount: 6,
  lemmasCount: 2,
  stemsCount: 3,
};

describe('RootsApi', () => {
  let api: RootsApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    ({ api, httpMock } = setupApiTestBed(RootsApi));
  });

  afterEach(() => {
    teardownApiTestBed(httpMock);
  });

  it('getRootsList sends sort/page/pageSize and omits search when blank', async () => {
    const promise = firstValueFrom(api.getRootsList('', 'occurrences', 1, 25));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('sort')).toBe('occurrences');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.has('search')).toBe(false);

    const response = page<RootListItemDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getRootsList sends the trimmed search param when non-blank', async () => {
    const promise = firstValueFrom(api.getRootsList('  كتب  ', 'alpha', 2, 50));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.get('search')).toBe('كتب');

    const response = page<RootListItemDto>([SAMPLE_ROOT], 1, 2, 50);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getRootsList sends only the active range params (Feature 026, US5)', async () => {
    const ranges: RangeFilters = {
      occurrences: { min: 10, max: 100 },
      surahs: { min: 1, max: null },
    };
    const promise = firstValueFrom(api.getRootsList('', 'mushaf-order', 1, 1000, ranges));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.get('occMin')).toBe('10');
    expect(req.request.params.get('occMax')).toBe('100');
    expect(req.request.params.get('surahsMin')).toBe('1');
    expect(req.request.params.has('surahsMax')).toBe(false);
    expect(req.request.params.has('simpleWordsMin')).toBe(false);

    req.flush(page<RootListItemDto>([]));
    await promise;
  });

  it('getRootsList omits every range param for an unfiltered read (backward compat)', async () => {
    const promise = firstValueFrom(api.getRootsList('', 'mushaf-order', 1, 1000, EMPTY_RANGE_FILTERS));

    const req = httpMock.expectOne((r) => r.url === BASE);
    expect(req.request.params.has('occMin')).toBe(false);
    expect(req.request.params.has('ayahsMax')).toBe(false);
    expect(req.request.params.has('lemmasMin')).toBe(false);
    expect(req.request.params.has('stemsMax')).toBe(false);

    req.flush(page<RootListItemDto>([]));
    await promise;
  });

  it('getRootSummary calls GET /api/words/roots/{id}', async () => {
    const response = ok<RootSummaryDto>({ ...SAMPLE_ROOT });
    const promise = firstValueFrom(api.getRootSummary(4210));

    const req = httpMock.expectOne(`${BASE}/4210`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it.each([
    ['simple' as const, `${BASE}/4210/words/simple`],
    ['tashkeel' as const, `${BASE}/4210/words/tashkeel`],
  ])('getRootWords encodes the %s wordView into the path', async (wordView, expectedUrl) => {
    const promise = firstValueFrom(api.getRootWords(4210, wordView, 1, 25));

    const req = httpMock.expectOne(
      (r) => r.url === expectedUrl && r.params.get('page') === '1' && r.params.get('pageSize') === '25',
    );
    expect(req.request.method).toBe('GET');

    const response = page<RootWordItemDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getRootAyahMatches sends page/pageSize to the ayahs endpoint', async () => {
    const promise = firstValueFrom(api.getRootAyahMatches(4210, 2, 10));

    const req = httpMock.expectOne(
      (r) => r.url === `${BASE}/4210/ayahs` && r.params.get('page') === '2' && r.params.get('pageSize') === '10',
    );
    expect(req.request.method).toBe('GET');

    const response = page<RootAyahMatchDto>([]);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getRootMentionedSurahs calls GET .../surahs', async () => {
    const response = ok<RootSurahsDto>({ surahs: [] });
    const promise = firstValueFrom(api.getRootMentionedSurahs(4210));

    const req = httpMock.expectOne(`${BASE}/4210/surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getRootMissingSurahs calls GET .../missing-surahs', async () => {
    const response = ok<RootMissingSurahsDto>({ surahs: [] });
    const promise = firstValueFrom(api.getRootMissingSurahs(4210));

    const req = httpMock.expectOne(`${BASE}/4210/missing-surahs`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getRootLemmas calls GET .../lemmas', async () => {
    const response = ok<RootLemmasDto>({ lemmas: [] });
    const promise = firstValueFrom(api.getRootLemmas(4210));

    const req = httpMock.expectOne(`${BASE}/4210/lemmas`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('getRootStems calls GET .../stems', async () => {
    const response = ok<RootStemsDto>({ stems: [] });
    const promise = firstValueFrom(api.getRootStems(4210));

    const req = httpMock.expectOne(`${BASE}/4210/stems`);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });
});
