import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { TestBed, getTestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { PagedResultDto, WordTypeRowDto, WordTypeTableRowDto } from '../models/word-types.models';
import { WordTypesApi } from './word-types.api';

function matchTable(): RegExp {
  return /\/api\/words\/word-types\/table(\?.*)?$/;
}

function matchRows(): RegExp {
  return /\/api\/words\/word-types\/words(\?.*)?$/;
}

function matchGroupedDetail(kind: 'roots' | 'stems' | 'lemmas', dimensionId: number, suffix = ''): RegExp {
  return new RegExp(`/api/words/word-types/table/${kind}/${dimensionId}${suffix}$`);
}

describe('WordTypesApi', () => {
  let api: WordTypesApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      providers: [WordTypesApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(WordTypesApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    getTestBed().resetTestingModule();
  });

  it('calls the table endpoint with type/tableView/sort/page/pageSize params', async () => {
    const promise = firstValueFrom(api.getTableRows({
      type: 'noun',
      childCode: null,
      case: 'all',
      tense: 'all',
      voice: 'all',
      tableView: 'roots',
      sort: 'occurrences',
      page: 1,
      pageSize: 25,
    }));

    const req = httpMock.expectOne((r) => matchTable().test(r.url));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('type')).toBe('noun');
    expect(req.request.params.get('tableView')).toBe('roots');
    expect(req.request.params.get('sort')).toBe('occurrences');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.has('childCode')).toBe(false);

    const response: ApiResponse<PagedResultDto<WordTypeTableRowDto>> = {
      isSuccess: true,
      message: 'تم',
      data: { page: 1, pageSize: 25, totalCount: 0, items: [] },
    };
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('includes the childCode param when provided', async () => {
    const promise = firstValueFrom(api.getTableRows({
      type: 'noun',
      childCode: 'PN',
      case: 'nominative',
      tense: 'all',
      voice: 'all',
      tableView: 'words',
      sort: 'alpha',
      page: 2,
      pageSize: 25,
    }));

    const req = httpMock.expectOne((r) => matchTable().test(r.url));
    expect(req.request.params.get('childCode')).toBe('PN');
    expect(req.request.params.get('case')).toBe('nominative');
    expect(req.request.params.get('tableView')).toBe('words');
    req.flush({ isSuccess: true, message: 'تم', data: { page: 2, pageSize: 25, totalCount: 0, items: [] } });

    await promise;
  });

  it('retains getRows for the legacy words endpoint', async () => {
    const response: ApiResponse<PagedResultDto<WordTypeRowDto>> = {
      isSuccess: true,
      message: 'تم',
      data: { page: 2, pageSize: 25, totalCount: 0, items: [] },
    };

    const promise = firstValueFrom(api.getRows({
      type: 'noun',
      childCode: 'PN',
      case: 'nominative',
      tense: 'all',
      voice: 'all',
      sort: 'alpha',
      page: 2,
      pageSize: 25,
    }));

    const req = httpMock.expectOne((r) => matchRows().test(r.url));
    expect(req.request.params.get('childCode')).toBe('PN');
    expect(req.request.params.get('case')).toBe('nominative');
    expect(req.request.params.has('tableView')).toBe(false);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it.each([
    ['root', 'roots', 4210],
    ['stem', 'stems', 5310],
    ['lemma', 'lemmas', 6410],
  ] as const)('getGroupedSummary_UsesPluralKindRouteAndPropagatesFullScope for %s', async (kind, routeKind, dimensionId) => {
    const promise = firstValueFrom(api.getGroupedSummary({
      kind,
      dimensionId,
      type: 'verb',
      childCode: 'present',
      case: 'all',
      tense: 'present',
      voice: 'passive',
    }));

    const req = httpMock.expectOne((r) => matchGroupedDetail(routeKind, dimensionId).test(r.url));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('type')).toBe('verb');
    expect(req.request.params.get('childCode')).toBe('present');
    expect(req.request.params.has('case')).toBe(false);
    expect(req.request.params.get('tense')).toBe('present');
    expect(req.request.params.get('voice')).toBe('passive');
    expect(req.request.params.has('page')).toBe(false);
    expect(req.request.params.has('pageSize')).toBe(false);
    expect(req.request.params.has('detailPage')).toBe(false);
    expect(req.request.params.has('sort')).toBe(false);
    req.flush({
      isSuccess: true,
      message: 'تم',
      data: { kind, dimensionId, displayText: 'SYNTH_GROUP', occurrencesCount: 1, ayahsCount: 1, surahsCount: 1 },
    });

    await promise;
  });

  it('getGroupedMemberWords_SendsPageAndPageSize', async () => {
    const promise = firstValueFrom(api.getGroupedMemberWords({
      kind: 'stem',
      dimensionId: 5310,
      type: 'noun',
      childCode: 'PN',
      case: 'accusative',
      tense: 'all',
      voice: 'all',
    }, 2, 25));

    const req = httpMock.expectOne((r) => matchGroupedDetail('stems', 5310, '/words').test(r.url));
    expect(req.request.params.get('type')).toBe('noun');
    expect(req.request.params.get('childCode')).toBe('PN');
    expect(req.request.params.get('case')).toBe('accusative');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.has('sort')).toBe(false);
    req.flush({ isSuccess: true, message: 'تم', data: { page: 2, pageSize: 25, totalCount: 0, items: [] } });

    await promise;
  });

  it('getGroupedAyahMatches_SendsPageAndPageSize', async () => {
    const promise = firstValueFrom(api.getGroupedAyahMatches({
      kind: 'lemma',
      dimensionId: 6410,
      type: 'verb',
      childCode: 'past',
      case: 'all',
      tense: 'past',
      voice: 'active',
    }, 3, 10));

    const req = httpMock.expectOne((r) => matchGroupedDetail('lemmas', 6410, '/ayahs').test(r.url));
    expect(req.request.params.get('type')).toBe('verb');
    expect(req.request.params.get('childCode')).toBe('past');
    expect(req.request.params.get('tense')).toBe('past');
    expect(req.request.params.get('voice')).toBe('active');
    expect(req.request.params.get('page')).toBe('3');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.has('sort')).toBe(false);
    req.flush({ isSuccess: true, message: 'تم', data: { page: 3, pageSize: 10, totalCount: 0, items: [] } });

    await promise;
  });

  it('getGroupedSurahs_SendsNoPagingParams', async () => {
    const promise = firstValueFrom(api.getGroupedSurahs({
      kind: 'root',
      dimensionId: 4210,
      type: 'noun',
      childCode: 'PN',
      case: 'genitive',
      tense: 'all',
      voice: 'all',
    }));

    const req = httpMock.expectOne((r) => matchGroupedDetail('roots', 4210, '/surahs').test(r.url));
    expect(req.request.params.get('type')).toBe('noun');
    expect(req.request.params.get('childCode')).toBe('PN');
    expect(req.request.params.get('case')).toBe('genitive');
    expect(req.request.params.has('page')).toBe(false);
    expect(req.request.params.has('pageSize')).toBe(false);
    expect(req.request.params.has('detailPage')).toBe(false);
    expect(req.request.params.has('sort')).toBe(false);
    req.flush({ isSuccess: true, message: 'تم', data: { surahs: [], missingSurahs: [] } });

    await promise;
  });
});
