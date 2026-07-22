import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import {
  WordTypeAyahMatchDto,
  WordTypeRowDto,
  WordTypeScopeCountsDto,
  WordTypeSurahsResponseDto,
  WordTypeTableRowDto,
} from '../models/word-types.models';
import { WordTypeGroupedMemberWordDto, WordTypeGroupedSummaryDto } from '../models/word-types-detail.models';
import { WordTypesApi } from './word-types.api';
import { ok, page, setupApiTestBed, teardownApiTestBed } from './testing/api-test-bed';

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
    ({ api, httpMock } = setupApiTestBed(WordTypesApi));
  });

  afterEach(() => {
    teardownApiTestBed(httpMock);
  });

  it('calls the table endpoint with type/tableView/sort/page/pageSize params', async () => {
    const promise = firstValueFrom(api.getTableRows({
      type: 'noun',
      childCode: null,
      case: 'all',
      tense: 'all',
      voice: 'all',
      search: null,
      hasRoot: null,
      hasStem: null,
      hasLemma: null,
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
    expect(req.request.params.has('search')).toBe(false);

    const response = page<WordTypeTableRowDto>([], 0, 1, 25);
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
      search: null,
      hasRoot: null,
      hasStem: null,
      hasLemma: null,
      tableView: 'words',
      sort: 'alpha',
      page: 2,
      pageSize: 25,
    }));

    const req = httpMock.expectOne((r) => matchTable().test(r.url));
    expect(req.request.params.get('childCode')).toBe('PN');
    expect(req.request.params.get('case')).toBe('nominative');
    expect(req.request.params.get('tableView')).toBe('words');
    req.flush(page<WordTypeTableRowDto>([], 0, 2, 25));

    await promise;
  });

  it('retains getRows for the legacy words endpoint', async () => {
    const response = page<WordTypeRowDto>([], 0, 2, 25);

    const promise = firstValueFrom(api.getRows({
      type: 'noun',
      childCode: 'PN',
      case: 'nominative',
      tense: 'all',
      voice: 'all',
      search: null,
      hasRoot: null,
      hasStem: null,
      hasLemma: null,
      sort: 'alpha',
      page: 2,
      pageSize: 25,
    }));

    const req = httpMock.expectOne((r) => matchRows().test(r.url));
    expect(req.request.params.get('childCode')).toBe('PN');
    expect(req.request.params.get('case')).toBe('nominative');
    expect(req.request.params.has('tableView')).toBe(false);
    expect(req.request.params.has('search')).toBe(false);
    req.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('sends the search param on rows and table reads only when non-empty', async () => {
    const rowsPromise = firstValueFrom(api.getRows({
      type: 'noun', childCode: null, case: 'all', tense: 'all', voice: 'all',
      search: 'كلمة', hasRoot: null, hasStem: null, hasLemma: null, sort: 'occurrences', page: 1, pageSize: 1000,
    }));
    const rowsReq = httpMock.expectOne((r) => matchRows().test(r.url));
    expect(rowsReq.request.params.get('search')).toBe('كلمة');
    rowsReq.flush(page<WordTypeRowDto>([], 0, 1, 1000));
    await rowsPromise;

    const tablePromise = firstValueFrom(api.getTableRows({
      type: 'noun', childCode: null, case: 'all', tense: 'all', voice: 'all',
      search: 'كلمة', hasRoot: null, hasStem: null, hasLemma: null, tableView: 'roots', sort: 'occurrences', page: 1, pageSize: 1000,
    }));
    const tableReq = httpMock.expectOne((r) => matchTable().test(r.url));
    expect(tableReq.request.params.get('search')).toBe('كلمة');
    tableReq.flush(page<WordTypeTableRowDto>([], 0, 1, 1000));
    await tablePromise;
  });

  it('sends only the set presence flags (Feature 026)', async () => {
    const promise = firstValueFrom(api.getRows({
      type: 'noun', childCode: null, case: 'all', tense: 'all', voice: 'all',
      search: null, hasRoot: true, hasStem: false, hasLemma: null, sort: 'occurrences', page: 1, pageSize: 1000,
    }));

    const req = httpMock.expectOne((r) => matchRows().test(r.url));
    expect(req.request.params.get('hasRoot')).toBe('true');
    expect(req.request.params.get('hasStem')).toBe('false');
    expect(req.request.params.has('hasLemma')).toBe(false);
    req.flush(page<WordTypeRowDto>([], 0, 1, 1000));

    await promise;
  });

  it('getScopeCounts sends the full scope but no tableView/sort/paging params (Feature 026, US8)', async () => {
    const promise = firstValueFrom(api.getScopeCounts({
      type: 'noun', childCode: 'PN', case: 'genitive', tense: 'all', voice: 'all',
      search: 'كلمة', hasRoot: true, hasStem: false, hasLemma: null,
    }));

    const req = httpMock.expectOne((r) => /\/api\/words\/word-types\/scope-counts(\?.*)?$/.test(r.url));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('type')).toBe('noun');
    expect(req.request.params.get('childCode')).toBe('PN');
    expect(req.request.params.get('case')).toBe('genitive');
    expect(req.request.params.get('search')).toBe('كلمة');
    expect(req.request.params.get('hasRoot')).toBe('true');
    expect(req.request.params.get('hasStem')).toBe('false');
    expect(req.request.params.has('hasLemma')).toBe(false);
    expect(req.request.params.has('tableView')).toBe(false);
    expect(req.request.params.has('sort')).toBe(false);
    expect(req.request.params.has('page')).toBe(false);
    expect(req.request.params.has('pageSize')).toBe(false);

    req.flush(ok<WordTypeScopeCountsDto>({ wordsCount: 3, rootsCount: 1, stemsCount: 1, lemmasCount: 1 }));
    await promise;
  });

  it('getScopeCounts omits optional scope params when absent', async () => {
    const promise = firstValueFrom(api.getScopeCounts({
      type: 'verb', childCode: null, case: 'all', tense: 'all', voice: 'all',
      search: null, hasRoot: null, hasStem: null, hasLemma: null,
    }));

    const req = httpMock.expectOne((r) => /\/api\/words\/word-types\/scope-counts(\?.*)?$/.test(r.url));
    expect(req.request.params.get('type')).toBe('verb');
    expect(req.request.params.has('childCode')).toBe(false);
    expect(req.request.params.has('case')).toBe(false);
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('hasRoot')).toBe(false);
    req.flush(ok<WordTypeScopeCountsDto>({ wordsCount: 0, rootsCount: 0, stemsCount: 0, lemmasCount: 0 }));
    await promise;
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
    req.flush(
      ok<WordTypeGroupedSummaryDto>({ kind, dimensionId, displayText: 'SYNTH_GROUP', occurrencesCount: 1, ayahsCount: 1, surahsCount: 1 }),
    );

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
    req.flush(page<WordTypeGroupedMemberWordDto>([], 0, 2, 25));

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
    req.flush(page<WordTypeAyahMatchDto>([], 0, 3, 10));

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
    req.flush(ok<WordTypeSurahsResponseDto>({ surahs: [], missingSurahs: [] }));

    await promise;
  });
});
