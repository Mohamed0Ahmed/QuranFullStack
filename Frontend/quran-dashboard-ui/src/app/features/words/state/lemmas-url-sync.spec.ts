import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import {
  buildClearSelectionQueryParams,
  buildLemmasDeepLink,
  buildLemmasQueryParams,
  parseLemmasQueryParams,
} from './lemmas-url-sync';
import { LEMMAS_QUERY_KEYS, LEMMA_SORT_KEYS, LemmaSort, LemmaView } from '../models/lemmas.models';

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
    expect(parsed.column).toBeNull();
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

  describe('sort normalization (data-driven)', () => {
    const validSorts: LemmaSort[] = ['mushaf-order', 'occurrences', 'alpha'];
    validSorts.forEach((sort) => {
      it(`accepts valid sort "${sort}"`, () => {
        expect(parseLemmasQueryParams(params(`sort=${sort}`)).sort).toBe(sort);
      });
    });

    const invalidSorts = ['relevance', 'desc', '', 'ALPHA', 'occurrences '];
    invalidSorts.forEach((sort) => {
      it(`normalizes invalid sort "${sort}" to default`, () => {
        expect(parseLemmasQueryParams(params(`sort=${sort}`)).sort).toBe('mushaf-order');
      });
    });
  });

  describe('page normalization (data-driven)', () => {
    const malformedPages = ['0', '-2', 'abc', '12abc', '1.5', ' ', '0x1'];
    malformedPages.forEach((page) => {
      it(`normalizes malformed/non-positive page "${page}" to 1`, () => {
        expect(parseLemmasQueryParams(params(`page=${page}`)).page).toBe(1);
      });
    });

    const validPages = ['1', '2', '3', '99999'];
    validPages.forEach((page) => {
      it(`preserves valid positive page "${page}"`, () => {
        expect(parseLemmasQueryParams(params(`page=${page}`)).page).toBe(Number(page));
      });
    });

    it('preserves a positive out-of-range page unchanged (backend controls empty result)', () => {
      expect(parseLemmasQueryParams(params('page=99999')).page).toBe(99999);
    });
  });

  describe('lemmaId / selection normalization (data-driven)', () => {
    const malformedIds = ['0', '-5', 'abc', '12abc', '1.5', ' ', '0x1'];
    malformedIds.forEach((lemma) => {
      it(`normalizes malformed/non-positive lemma "${lemma}" to null and keeps default view`, () => {
        const parsed = parseLemmasQueryParams(params(`lemma=${lemma}&view=ayahs`));
        expect(parsed.lemmaId).toBeNull();
        expect(parsed.view).toBe('words');
      });
    });

    it('accepts a valid positive lemma id and reads view', () => {
      const parsed = parseLemmasQueryParams(params('lemma=500&view=ayahs'));
      expect(parsed.lemmaId).toBe(500);
      expect(parsed.view).toBe('ayahs');
      expect(parsed.column).toBeNull();
    });

    it('preserves a valid explicit active column for ayahs restore', () => {
      expect(parseLemmasQueryParams(params('lemma=500&view=ayahs&column=ayahs')).column).toBe('ayahs');
    });

    it('ignores view when no lemma is present (keeps the default view, no panel renders)', () => {
      const parsed = parseLemmasQueryParams(params('view=surahs'));
      expect(parsed.lemmaId).toBeNull();
      expect(parsed.view).toBe('words');
    });

    it('does not surface a selected identity when lemma is absent even if other keys are present', () => {
      const parsed = parseLemmasQueryParams(params('view=ayahs&detailPage=9'));
      expect(parsed.lemmaId).toBeNull();
      expect(parsed.view).toBe('words');
    });
  });

  describe('view normalization (data-driven)', () => {
    const validViews: LemmaView[] = ['words', 'ayahs', 'surahs', 'stems'];
    validViews.forEach((view) => {
      it(`accepts valid view "${view}" when lemma is present`, () => {
        expect(parseLemmasQueryParams(params(`lemma=1&view=${view}`)).view).toBe(view);
      });
    });

    const invalidViews = ['overview', 'root', '', 'WORDS', 'ayahs '];
    invalidViews.forEach((view) => {
      it(`normalizes invalid view "${view}" to default while keeping lemma`, () => {
        const parsed = parseLemmasQueryParams(params(`lemma=1&view=${view}`));
        expect(parsed.lemmaId).toBe(1);
        expect(parsed.view).toBe('words');
      });
    });
  });

  describe('sub-view scope rules (wordView / surahView)', () => {
    it('honors wordView only when view=words', () => {
      expect(parseLemmasQueryParams(params('lemma=500&view=words&wordView=tashkeel')).wordView).toBe('tashkeel');
      expect(parseLemmasQueryParams(params('lemma=500&view=ayahs&wordView=tashkeel')).wordView).toBe('simple');
      expect(parseLemmasQueryParams(params('lemma=500&view=surahs&wordView=tashkeel')).wordView).toBe('simple');
      expect(parseLemmasQueryParams(params('lemma=500&view=stems&wordView=tashkeel')).wordView).toBe('simple');
      expect(parseLemmasQueryParams(params('lemma=500&view=words&wordView=bogus')).wordView).toBe('simple');
    });

    it('honors surahView only when view=surahs', () => {
      expect(parseLemmasQueryParams(params('lemma=500&view=surahs&surahView=missing')).surahView).toBe('missing');
      expect(parseLemmasQueryParams(params('lemma=500&view=words&surahView=missing')).surahView).toBe('mentioned');
      expect(parseLemmasQueryParams(params('lemma=500&view=ayahs&surahView=missing')).surahView).toBe('mentioned');
      expect(parseLemmasQueryParams(params('lemma=500&view=stems&surahView=missing')).surahView).toBe('mentioned');
      expect(parseLemmasQueryParams(params('lemma=500&view=surahs&surahView=bogus')).surahView).toBe('mentioned');
    });
  });

  describe('detailPage scope rules', () => {
    it('honors detailPage only for paginated views (ayahs/words)', () => {
      expect(parseLemmasQueryParams(params('lemma=500&view=ayahs&detailPage=4')).detailPage).toBe(4);
      expect(parseLemmasQueryParams(params('lemma=500&view=words&detailPage=2')).detailPage).toBe(2);
      expect(parseLemmasQueryParams(params('lemma=500&view=surahs&detailPage=9')).detailPage).toBe(1);
      expect(parseLemmasQueryParams(params('lemma=500&view=stems&detailPage=9')).detailPage).toBe(1);
    });

    it('defaults detailPage to 1 when paginated view is set but detailPage is missing', () => {
      expect(parseLemmasQueryParams(params('lemma=500&view=ayahs')).detailPage).toBe(1);
      expect(parseLemmasQueryParams(params('lemma=500&view=words')).detailPage).toBe(1);
    });

    it('normalizes malformed detailPage to 1 even for paginated views', () => {
      expect(parseLemmasQueryParams(params('lemma=500&view=ayahs&detailPage=0')).detailPage).toBe(1);
      expect(parseLemmasQueryParams(params('lemma=500&view=words&detailPage=abc')).detailPage).toBe(1);
      expect(parseLemmasQueryParams(params('lemma=500&view=words&detailPage=12abc')).detailPage).toBe(1);
    });

    it('preserves positive out-of-range detailPage for paginated views', () => {
      expect(parseLemmasQueryParams(params('lemma=500&view=words&detailPage=99999')).detailPage).toBe(99999);
    });
  });

  it('ignores irrelevant query keys without breaking normalization', () => {
    const parsed = parseLemmasQueryParams(
      params('foo=bar&debug=1&lemma=500&view=words&wordView=simple&detailPage=2&random=xyz'),
    );
    expect(parsed.lemmaId).toBe(500);
    expect(parsed.view).toBe('words');
    expect(parsed.wordView).toBe('simple');
    expect(parsed.detailPage).toBe(2);
  });
});

describe('buildLemmasQueryParams', () => {
  it('builds only the provided fields so merge preserves the rest', () => {
    expect(buildLemmasQueryParams({ search: 'اسم' })).toEqual({ search: 'اسم' });
    expect(buildLemmasQueryParams({ page: 3 })).toEqual({ page: '3' });
  });

  it('skips undefined fields so merge preserves the rest', () => {
    const built = buildLemmasQueryParams({ sort: 'alpha' });
    expect(built).toEqual({ sort: 'alpha' });
    expect('search' in built).toBe(false);
  });

  it('stringifies numeric lemma/page/detailPage and passes null through to remove', () => {
    expect(
      buildLemmasQueryParams({
        page: 2,
        lemmaId: 7,
        detailPage: 4,
        view: 'ayahs',
        column: 'ayahs',
      }),
    ).toEqual({ page: '2', lemma: '7', detailPage: '4', view: 'ayahs', column: 'ayahs' });

    expect(buildLemmasQueryParams({ search: null, page: null })).toEqual({
      search: null,
      page: null,
    });
  });

  it('round-trips parse -> build for list + selection + sub-view state', () => {
    const original = params('search=اسم&sort=alpha&page=2&lemma=9&view=words&wordView=tashkeel&detailPage=3');
    const parsed = parseLemmasQueryParams(original);

    const rebuilt = buildLemmasQueryParams({
      search: parsed.search,
      sort: parsed.sort,
      page: parsed.page,
      lemmaId: parsed.lemmaId,
      view: parsed.view,
      column: parsed.column,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
    });

    expect(rebuilt['search']).toBe('اسم');
    expect(rebuilt['sort']).toBe('alpha');
    expect(rebuilt['page']).toBe('2');
    expect(rebuilt['lemma']).toBe('9');
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
        LEMMAS_QUERY_KEYS.lemma,
        LEMMAS_QUERY_KEYS.view,
        LEMMAS_QUERY_KEYS.column,
        LEMMAS_QUERY_KEYS.wordView,
        LEMMAS_QUERY_KEYS.surahView,
        LEMMAS_QUERY_KEYS.detailPage,
        LEMMAS_QUERY_KEYS.typeCode,
      ].sort(),
    );
    expect(Object.values(cleared)).toEqual([null, null, null, null, null, null, null]);

    expect(cleared).not.toHaveProperty(LEMMAS_QUERY_KEYS.search);
    expect(cleared).not.toHaveProperty(LEMMAS_QUERY_KEYS.sort);
    expect(cleared).not.toHaveProperty(LEMMAS_QUERY_KEYS.page);
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

  it('omits absent query params when only the route is needed', () => {
    expect(buildLemmasDeepLink()).toEqual({
      path: '/dashboard/words/lemmas',
      queryParams: {},
    });
  });

  it('builds the canonical lemma-only selection deep link (words/simple default)', () => {
    expect(buildLemmasDeepLink({ lemmaId: 42, view: 'words', wordView: 'simple' })).toEqual({
      path: '/dashboard/words/lemmas',
      queryParams: { lemma: '42', view: 'words', wordView: 'simple' },
    });
  });

  it('builds a related-stems deep link targeting the stems explorer', () => {
    expect(buildLemmasDeepLink({ lemmaId: 7, view: 'stems' })).toEqual({
      path: '/dashboard/words/lemmas',
      queryParams: { lemma: '7', view: 'stems' },
    });
  });

  it('produces independent query-param objects per call (no shared mutation)', () => {
    const a = buildLemmasDeepLink({ lemmaId: 1 });
    const b = buildLemmasDeepLink({ lemmaId: 2 });
    expect(a.queryParams['lemma']).toBe('1');
    expect(b.queryParams['lemma']).toBe('2');
  });
});

describe('parseLemmasQueryParams association filter (Feature 026, US7)', () => {
  it('is absent for a pre-feature URL (backward compat)', () => {
    expect(parseLemmasQueryParams(params('search=كلمة&sort=alpha&page=2')).association).toEqual({ rootId: null });
  });

  it('parses a positive rootId (owned-root FK filter)', () => {
    expect(parseLemmasQueryParams(params('rootId=701')).association).toEqual({ rootId: 701 });
  });

  it('fails closed on a non-positive or non-numeric rootId', () => {
    expect(parseLemmasQueryParams(params('rootId=0')).association.rootId).toBeNull();
    expect(parseLemmasQueryParams(params('rootId=-1')).association.rootId).toBeNull();
    expect(parseLemmasQueryParams(params('rootId=abc')).association.rootId).toBeNull();
  });

  it('serializes rootId and passes null through to remove', () => {
    expect(buildLemmasQueryParams({ rootId: 701 })).toEqual({ rootId: '701' });
    expect(buildLemmasQueryParams({ rootId: null })).toEqual({ rootId: null });
  });
});

describe('parseLemmasQueryParams count ranges (Feature 026)', () => {
  it('has no active ranges for a pre-feature URL (backward compat)', () => {
    expect(parseLemmasQueryParams(params('search=كلمة&sort=alpha&page=2')).ranges).toEqual({});
  });

  it('parses active ranges from their URL keys', () => {
    const ranges = parseLemmasQueryParams(params('occ=11..100&stems=2..')).ranges;
    expect(ranges).toEqual({ occurrences: { min: 11, max: 100 }, stems: { min: 2, max: null } });
  });

  it('drops malformed ranges fail-closed while the page still parses', () => {
    const parsed = parseLemmasQueryParams(params('occ=9..2&surahs=1..50'));
    expect(parsed.ranges).toEqual({ surahs: { min: 1, max: 50 } });
    expect(parsed.page).toBe(1);
  });
});

describe('parseLemmasQueryParams sort tokens (Feature 030, N8)', () => {
  it.each(LEMMA_SORT_KEYS)('parses the canonical token "%s" verbatim', (sort) => {
    expect(parseLemmasQueryParams(params(`sort=${sort}`)).sort).toBe(sort);
  });

  it.each([
    ['occurrences-desc', 'occurrences'],
    ['ayahs-desc', 'ayahs'],
    ['stems-desc', 'stems'],
    ['alpha-asc', 'alpha'],
  ])('canonicalizes the legacy alias "%s" to "%s"', (alias, canonical) => {
    // An old shared link must not fork the ordering into a second URL/cache spelling.
    expect(parseLemmasQueryParams(params(`sort=${alias}`)).sort).toBe(canonical);
  });

  it.each([
    'relevance',
    'relevance-asc',
    'lemmas',
    'mushaf-order-asc',
    'mushaf-order-desc',
    '-asc',
  ])('fails closed to the default on the unsupported token "%s"', (sort) => {
    expect(parseLemmasQueryParams(params(`sort=${sort}`)).sort).toBe('mushaf-order');
  });

  it('round-trips a suffixed token through build', () => {
    expect(buildLemmasQueryParams({ sort: 'occurrences-asc' })).toEqual({ sort: 'occurrences-asc' });
  });

  it('removes the param on release, so the default order stays param-free', () => {
    expect(buildLemmasQueryParams({ sort: null, page: null })).toEqual({ sort: null, page: null });
  });
});
