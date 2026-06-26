import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import {
  buildClearSelectionQueryParams,
  buildLemmasDeepLink,
  buildLemmasQueryParams,
  parseLemmasQueryParams,
} from './lemmas-url-sync';
import { LEMMAS_QUERY_KEYS } from '../models/lemmas.models';

function params(query: string): ParamMap {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

describe('parseLemmasQueryParams', () => {
  it('applies documented defaults when params are absent', () => {
    const parsed = parseLemmasQueryParams(params(''));

    expect(parsed.search).toBe('');
    expect(parsed.sort).toBe('mushaf-order');
    expect(parsed.page).toBe(1);
    expect(parsed.lemmaId).toBeNull();
    expect(parsed.view).toBe('words');
    expect(parsed.wordView).toBe('simple');
    expect(parsed.surahView).toBe('mentioned');
    expect(parsed.detailPage).toBe(1);
  });

  it('reads list state (search/sort/page) verbatim when valid', () => {
    const parsed = parseLemmasQueryParams(params('search=اسم&sort=alpha&page=3'));

    expect(parsed.search).toBe('اسم');
    expect(parsed.sort).toBe('alpha');
    expect(parsed.page).toBe(3);
  });

  it('normalizes unsupported state and rejects malformed numeric values', () => {
    const parsed = parseLemmasQueryParams(
      params('sort=relevance&page=12abc&lemma=1.5&view=bogus&wordView=bogus&surahView=bogus&detailPage=4x'),
    );

    expect(parsed.sort).toBe('mushaf-order');
    expect(parsed.page).toBe(1);
    expect(parsed.lemmaId).toBeNull();
    expect(parsed.view).toBe('words');
    expect(parsed.wordView).toBe('simple');
    expect(parsed.surahView).toBe('mentioned');
    expect(parsed.detailPage).toBe(1);
  });

  it('honors wordView only when view=words and surahView only when view=surahs', () => {
    expect(parseLemmasQueryParams(params('lemma=500&view=words&wordView=tashkeel')).wordView).toBe('tashkeel');
    expect(parseLemmasQueryParams(params('lemma=500&view=ayahs&wordView=tashkeel')).wordView).toBe('simple');
    expect(parseLemmasQueryParams(params('lemma=500&view=surahs&surahView=missing')).surahView).toBe('missing');
    expect(parseLemmasQueryParams(params('lemma=500&view=words&surahView=missing')).surahView).toBe('mentioned');
  });

  it('honors detailPage only for paginated views (ayahs/words)', () => {
    expect(parseLemmasQueryParams(params('lemma=500&view=ayahs&detailPage=4')).detailPage).toBe(4);
    expect(parseLemmasQueryParams(params('lemma=500&view=words&detailPage=2')).detailPage).toBe(2);
    expect(parseLemmasQueryParams(params('lemma=500&view=stems&detailPage=9')).detailPage).toBe(1);
  });
});

describe('buildLemmasQueryParams', () => {
  it('builds only the provided fields so merge preserves the rest', () => {
    expect(buildLemmasQueryParams({ search: 'اسم' })).toEqual({ search: 'اسم' });
    expect(buildLemmasQueryParams({ page: 3 })).toEqual({ page: '3' });
  });

  it('stringifies numeric lemma/page/detailPage and passes null through to remove', () => {
    expect(
      buildLemmasQueryParams({
        page: 2,
        lemmaId: 7,
        detailPage: 4,
        view: 'ayahs',
      }),
    ).toEqual({ page: '2', lemma: '7', detailPage: '4', view: 'ayahs' });

    expect(buildLemmasQueryParams({ search: null, page: null })).toEqual({
      search: null,
      page: null,
    });
  });

  it('round-trips parse -> build for list and selection state', () => {
    const original = params('search=اسم&sort=alpha&page=2&lemma=9&view=words&wordView=tashkeel&detailPage=3');
    const parsed = parseLemmasQueryParams(original);

    const rebuilt = buildLemmasQueryParams({
      search: parsed.search,
      sort: parsed.sort,
      page: parsed.page,
      lemmaId: parsed.lemmaId,
      view: parsed.view,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
    });

    expect(rebuilt['search']).toBe('اسم');
    expect(rebuilt['sort']).toBe('alpha');
    expect(rebuilt['page']).toBe('2');
    expect(rebuilt['lemma']).toBe('9');
    expect(rebuilt['view']).toBe('words');
    expect(rebuilt['wordView']).toBe('tashkeel');
    expect(rebuilt['detailPage']).toBe('3');
  });
});

describe('buildClearSelectionQueryParams', () => {
  it('clears only the selection keys, preserving list context', () => {
    const cleared = buildClearSelectionQueryParams();

    expect(Object.keys(cleared).sort()).toEqual(
      [
        LEMMAS_QUERY_KEYS.lemma,
        LEMMAS_QUERY_KEYS.view,
        LEMMAS_QUERY_KEYS.wordView,
        LEMMAS_QUERY_KEYS.surahView,
        LEMMAS_QUERY_KEYS.detailPage,
      ].sort(),
    );
    expect(Object.values(cleared)).toEqual([null, null, null, null, null]);
  });
});

describe('buildLemmasDeepLink', () => {
  it('builds a stable deep link to the lemmas route', () => {
    expect(
      buildLemmasDeepLink({
        search: 'اسم',
        sort: 'alpha',
        page: 3,
        lemmaId: 55,
        view: 'ayahs',
        detailPage: 2,
      }),
    ).toEqual({
      path: '/dashboard/words/lemmas',
      queryParams: {
        search: 'اسم',
        sort: 'alpha',
        page: '3',
        lemma: '55',
        view: 'ayahs',
        detailPage: '2',
      },
    });
  });
});
