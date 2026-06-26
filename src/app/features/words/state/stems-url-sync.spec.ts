import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import {
  buildClearSelectionQueryParams,
  buildStemsDeepLink,
  buildStemsQueryParams,
  parseStemsQueryParams,
} from './stems-url-sync';
import { STEMS_QUERY_KEYS } from '../models/stems.models';

function params(query: string): ParamMap {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

describe('parseStemsQueryParams', () => {
  it('applies documented defaults when params are absent', () => {
    const parsed = parseStemsQueryParams(params(''));

    expect(parsed.search).toBe('');
    expect(parsed.sort).toBe('mushaf-order');
    expect(parsed.page).toBe(1);
    expect(parsed.stemId).toBeNull();
    expect(parsed.view).toBe('words');
    expect(parsed.wordView).toBe('simple');
    expect(parsed.surahView).toBe('mentioned');
    expect(parsed.detailPage).toBe(1);
  });

  it('reads list state (search/sort/page) verbatim when valid', () => {
    const parsed = parseStemsQueryParams(params('search=اسم&sort=alpha&page=3'));

    expect(parsed.search).toBe('اسم');
    expect(parsed.sort).toBe('alpha');
    expect(parsed.page).toBe(3);
  });

  it('normalizes unsupported state and rejects malformed numeric values', () => {
    const parsed = parseStemsQueryParams(
      params('sort=relevance&page=12abc&stem=1.5&view=bogus&wordView=bogus&surahView=bogus&detailPage=4x'),
    );

    expect(parsed.sort).toBe('mushaf-order');
    expect(parsed.page).toBe(1);
    expect(parsed.stemId).toBeNull();
    expect(parsed.view).toBe('words');
    expect(parsed.wordView).toBe('simple');
    expect(parsed.surahView).toBe('mentioned');
    expect(parsed.detailPage).toBe(1);
  });

  it('honors wordView only when view=words and surahView only when view=surahs', () => {
    expect(parseStemsQueryParams(params('stem=600&view=words&wordView=tashkeel')).wordView).toBe('tashkeel');
    expect(parseStemsQueryParams(params('stem=600&view=ayahs&wordView=tashkeel')).wordView).toBe('simple');
    expect(parseStemsQueryParams(params('stem=600&view=surahs&surahView=missing')).surahView).toBe('missing');
    expect(parseStemsQueryParams(params('stem=600&view=words&surahView=missing')).surahView).toBe('mentioned');
  });

  it('honors detailPage only for paginated views (ayahs/words)', () => {
    expect(parseStemsQueryParams(params('stem=600&view=ayahs&detailPage=4')).detailPage).toBe(4);
    expect(parseStemsQueryParams(params('stem=600&view=words&detailPage=2')).detailPage).toBe(2);
    expect(parseStemsQueryParams(params('stem=600&view=lemmas&detailPage=9')).detailPage).toBe(1);
  });
});

describe('buildStemsQueryParams', () => {
  it('builds only the provided fields so merge preserves the rest', () => {
    expect(buildStemsQueryParams({ search: 'اسم' })).toEqual({ search: 'اسم' });
    expect(buildStemsQueryParams({ page: 3 })).toEqual({ page: '3' });
  });

  it('stringifies numeric stem/page/detailPage and passes null through to remove', () => {
    expect(
      buildStemsQueryParams({
        page: 2,
        stemId: 7,
        detailPage: 4,
        view: 'ayahs',
      }),
    ).toEqual({ page: '2', stem: '7', detailPage: '4', view: 'ayahs' });

    expect(buildStemsQueryParams({ search: null, page: null })).toEqual({
      search: null,
      page: null,
    });
  });

  it('round-trips parse -> build for list and selection state', () => {
    const original = params('search=اسم&sort=alpha&page=2&stem=9&view=words&wordView=tashkeel&detailPage=3');
    const parsed = parseStemsQueryParams(original);

    const rebuilt = buildStemsQueryParams({
      search: parsed.search,
      sort: parsed.sort,
      page: parsed.page,
      stemId: parsed.stemId,
      view: parsed.view,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
    });

    expect(rebuilt['search']).toBe('اسم');
    expect(rebuilt['sort']).toBe('alpha');
    expect(rebuilt['page']).toBe('2');
    expect(rebuilt['stem']).toBe('9');
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
        STEMS_QUERY_KEYS.stem,
        STEMS_QUERY_KEYS.view,
        STEMS_QUERY_KEYS.wordView,
        STEMS_QUERY_KEYS.surahView,
        STEMS_QUERY_KEYS.detailPage,
      ].sort(),
    );
    expect(Object.values(cleared)).toEqual([null, null, null, null, null]);
  });
});

describe('buildStemsDeepLink', () => {
  it('builds a stable deep link to the stems route', () => {
    expect(
      buildStemsDeepLink({
        search: 'اسم',
        sort: 'alpha',
        page: 3,
        stemId: 55,
        view: 'ayahs',
        detailPage: 2,
      }),
    ).toEqual({
      path: '/dashboard/words/stems',
      queryParams: {
        search: 'اسم',
        sort: 'alpha',
        page: '3',
        stem: '55',
        view: 'ayahs',
        detailPage: '2',
      },
    });
  });
});
