import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import {
  buildClearSelectionQueryParams,
  buildRootsDeepLink,
  buildRootsQueryParams,
  parseRootsQueryParams,
} from './roots-url-sync';
import { ROOTS_QUERY_KEYS, ROOT_SORT_KEYS } from '../models/roots.models';

function params(query: string): ParamMap {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

describe('parseRootsQueryParams', () => {
  it('applies documented defaults when params are absent', () => {
    const parsed = parseRootsQueryParams(params(''));

    expect(parsed.search).toBe('');
    expect(parsed.sort).toBe('mushaf-order');
    expect(parsed.page).toBe(1);
    expect(parsed.rootId).toBeNull();
    expect(parsed.view).toBe('words');
    expect(parsed.column).toBeNull();
    expect(parsed.wordView).toBe('simple');
    expect(parsed.surahView).toBe('mentioned');
    expect(parsed.detailPage).toBe(1);
  });

  it('reads list state (search/sort/page) verbatim when valid', () => {
    const parsed = parseRootsQueryParams(params('search=رحم&sort=alpha&page=3'));

    expect(parsed.search).toBe('رحم');
    expect(parsed.sort).toBe('alpha');
    expect(parsed.page).toBe(3);
  });

  it('normalizes an unsupported sort to the default instead of failing', () => {
    expect(parseRootsQueryParams(params('sort=relevance')).sort).toBe('mushaf-order');
  });

  it('normalizes a non-positive or non-numeric page to the default', () => {
    expect(parseRootsQueryParams(params('page=0')).page).toBe(1);
    expect(parseRootsQueryParams(params('page=-2')).page).toBe(1);
    expect(parseRootsQueryParams(params('page=abc')).page).toBe(1);
    expect(parseRootsQueryParams(params('page=12abc')).page).toBe(1);
  });

  it('reads selection state (root/view) when root is a valid positive int', () => {
    const parsed = parseRootsQueryParams(params('root=55&view=lemmas'));

    expect(parsed.rootId).toBe(55);
    expect(parsed.view).toBe('lemmas');
    expect(parsed.column).toBeNull();
  });

  it('preserves a valid explicit active column for ayahs restore', () => {
    expect(parseRootsQueryParams(params('root=55&view=ayahs&column=ayahs')).column).toBe('ayahs');
  });

  it('ignores view when no root is present (keeps the default view)', () => {
    const parsed = parseRootsQueryParams(params('view=surahs'));

    expect(parsed.rootId).toBeNull();
    expect(parsed.view).toBe('words');
  });

  it('normalizes an unsupported view to the default while keeping the root', () => {
    const parsed = parseRootsQueryParams(params('root=55&view=bogus'));

    expect(parsed.rootId).toBe(55);
    expect(parsed.view).toBe('words');
  });

  it('normalizes a non-positive root id to null', () => {
    expect(parseRootsQueryParams(params('root=0')).rootId).toBeNull();
    expect(parseRootsQueryParams(params('root=-5')).rootId).toBeNull();
    expect(parseRootsQueryParams(params('root=abc')).rootId).toBeNull();
    expect(parseRootsQueryParams(params('root=12abc')).rootId).toBeNull();
  });

  it('honors wordView only when view=words', () => {
    expect(parseRootsQueryParams(params('root=1&view=words&wordView=tashkeel')).wordView).toBe(
      'tashkeel',
    );

    expect(parseRootsQueryParams(params('root=1&view=ayahs&wordView=tashkeel')).wordView).toBe(
      'simple',
    );
    expect(parseRootsQueryParams(params('root=1&view=words&wordView=bogus')).wordView).toBe(
      'simple',
    );
  });

  it('honors surahView only when view=surahs', () => {
    expect(
      parseRootsQueryParams(params('root=1&view=surahs&surahView=missing')).surahView,
    ).toBe('missing');

    expect(
      parseRootsQueryParams(params('root=1&view=ayahs&surahView=missing')).surahView,
    ).toBe('mentioned');
    expect(
      parseRootsQueryParams(params('root=1&view=surahs&surahView=bogus')).surahView,
    ).toBe('mentioned');
  });

  it('honors detailPage only for paginated views (ayahs/words)', () => {
    expect(parseRootsQueryParams(params('root=1&view=ayahs&detailPage=4')).detailPage).toBe(4);
    expect(parseRootsQueryParams(params('root=1&view=words&detailPage=2')).detailPage).toBe(2);

    expect(parseRootsQueryParams(params('root=1&view=surahs&detailPage=9')).detailPage).toBe(1);
    expect(parseRootsQueryParams(params('root=1&view=lemmas&detailPage=9')).detailPage).toBe(1);
  });

  it('defaults detailPage to 1 when the view is paginated but detailPage is missing', () => {
    expect(parseRootsQueryParams(params('root=1&view=ayahs')).detailPage).toBe(1);
  });
});

describe('buildRootsQueryParams', () => {
  it('builds only the provided fields so merge preserves the rest', () => {
    expect(buildRootsQueryParams({ search: 'رحم' })).toEqual({ search: 'رحم' });
    expect(buildRootsQueryParams({ page: 3 })).toEqual({ page: '3' });
  });

  it('stringifies numeric root/page/detailPage and passes null through to remove', () => {
    expect(
      buildRootsQueryParams({
        page: 2,
        rootId: 7,
        detailPage: 4,
        view: 'ayahs',
        column: 'ayahs',
      }),
    ).toEqual({ page: '2', root: '7', detailPage: '4', view: 'ayahs', column: 'ayahs' });

    expect(buildRootsQueryParams({ search: null, page: null })).toEqual({
      search: null,
      page: null,
    });
  });

  it('skips undefined fields so merge preserves the rest', () => {
    const built = buildRootsQueryParams({ sort: 'alpha' });
    expect(built).toEqual({ sort: 'alpha' });
    expect('search' in built).toBe(false);
  });

  it('round-trips parse -> build for list + selection + sub-view state', () => {
    const original = params('search=رحم&sort=alpha&page=2&root=9&view=words&wordView=tashkeel&detailPage=3');
    const parsed = parseRootsQueryParams(original);

    const rebuilt = buildRootsQueryParams({
      search: parsed.search,
      sort: parsed.sort,
      page: parsed.page,
      rootId: parsed.rootId,
      view: parsed.view,
      column: parsed.column,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
    });

    expect(rebuilt['search']).toBe('رحم');
    expect(rebuilt['sort']).toBe('alpha');
    expect(rebuilt['page']).toBe('2');
    expect(rebuilt['root']).toBe('9');
    expect(rebuilt['view']).toBe('words');
    expect(rebuilt['column']).toBeNull();
    expect(rebuilt['wordView']).toBe('tashkeel');
    expect(rebuilt['detailPage']).toBe('3');
  });
});

describe('buildClearSelectionQueryParams', () => {
  it('clears only the selection keys, preserving list context (search/sort/page)', () => {
    const cleared = buildClearSelectionQueryParams();

    expect(Object.keys(cleared).sort()).toEqual(
      [
        ROOTS_QUERY_KEYS.root,
        ROOTS_QUERY_KEYS.view,
        ROOTS_QUERY_KEYS.column,
        ROOTS_QUERY_KEYS.wordView,
        ROOTS_QUERY_KEYS.surahView,
        ROOTS_QUERY_KEYS.detailPage,
      ].sort(),
    );
    expect(Object.values(cleared)).toEqual([null, null, null, null, null, null]);

    expect(cleared).not.toHaveProperty(ROOTS_QUERY_KEYS.search);
    expect(cleared).not.toHaveProperty(ROOTS_QUERY_KEYS.sort);
    expect(cleared).not.toHaveProperty(ROOTS_QUERY_KEYS.page);
  });
});

describe('buildRootsDeepLink', () => {
  it('builds a stable deep link to the roots route', () => {
    expect(
      buildRootsDeepLink({
        search: 'رحم',
        sort: 'alpha',
        page: 3,
        rootId: 55,
        view: 'ayahs',
        detailPage: 2,
      }),
    ).toEqual({
      path: '/dashboard/words/roots',
      queryParams: {
        search: 'رحم',
        sort: 'alpha',
        page: '3',
        root: '55',
        view: 'ayahs',
        detailPage: '2',
      },
    });
  });

  it('omits absent query params when only the route is needed', () => {
    expect(buildRootsDeepLink()).toEqual({
      path: '/dashboard/words/roots',
      queryParams: {},
    });
  });

  it('builds a root-only selection deep link', () => {
    expect(buildRootsDeepLink({ rootId: 42 })).toEqual({
      path: '/dashboard/words/roots',
      queryParams: { root: '42' },
    });
  });
});

describe('parseRootsQueryParams sort tokens (Feature 030, N8)', () => {
  it.each(ROOT_SORT_KEYS)('parses the canonical token "%s" verbatim', (sort) => {
    expect(parseRootsQueryParams(params(`sort=${sort}`)).sort).toBe(sort);
  });

  it.each([
    ['occurrences-desc', 'occurrences'],
    ['ayahs-desc', 'ayahs'],
    ['stems-desc', 'stems'],
    ['alpha-asc', 'alpha'],
  ])('canonicalizes the legacy alias "%s" to "%s"', (alias, canonical) => {
    // An old shared link must not fork the ordering into a second URL/cache spelling.
    expect(parseRootsQueryParams(params(`sort=${alias}`)).sort).toBe(canonical);
  });

  it.each([
    'relevance',
    'relevance-asc',
    'lemma',
    'mushaf-order-asc',
    'mushaf-order-desc',
    '-asc',
  ])('fails closed to the default on the unsupported token "%s"', (sort) => {
    expect(parseRootsQueryParams(params(`sort=${sort}`)).sort).toBe('mushaf-order');
  });

  it('round-trips a suffixed token through build', () => {
    expect(buildRootsQueryParams({ sort: 'occurrences-asc' })).toEqual({ sort: 'occurrences-asc' });
  });

  it('removes the param on release, so the default order stays param-free', () => {
    expect(buildRootsQueryParams({ sort: null, page: null })).toEqual({ sort: null, page: null });
  });
});

describe('parseRootsQueryParams count ranges (Feature 026)', () => {
  it('has no active ranges for a pre-feature URL (backward compat)', () => {
    expect(parseRootsQueryParams(params('search=رحم&sort=alpha&page=2')).ranges).toEqual({});
  });

  it('parses active ranges from their URL keys', () => {
    const ranges = parseRootsQueryParams(params('occ=11..100&stems=2..')).ranges;
    expect(ranges).toEqual({ occurrences: { min: 11, max: 100 }, stems: { min: 2, max: null } });
  });

  it('drops malformed ranges fail-closed while the page still parses', () => {
    const parsed = parseRootsQueryParams(params('occ=9..2&ayahs=abc&surahs=1..50'));
    expect(parsed.ranges).toEqual({ surahs: { min: 1, max: 50 } });
    expect(parsed.page).toBe(1);
  });
});
