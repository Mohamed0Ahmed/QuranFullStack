import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import {
  buildClearSelectionQueryParams,
  buildStemsDeepLink,
  buildStemsQueryParams,
  parseStemsQueryParams,
} from './stems-url-sync';
import { STEMS_QUERY_KEYS, STEM_SORT_KEYS, StemSort, StemView } from '../models/stems.models';

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
    expect(parsed.column).toBeNull();
    expect(parsed.wordView).toBe('simple');
    expect(parsed.surahView).toBe('mentioned');
    expect(parsed.detailPage).toBe(1);
    expect(parsed.typeCode).toBeNull();
  });

  it('reads list state (search/sort/page) verbatim when valid', () => {
    const parsed = parseStemsQueryParams(params('search=علم&sort=alpha&page=3'));

    expect(parsed.search).toBe('علم');
    expect(parsed.sort).toBe('alpha');
    expect(parsed.page).toBe(3);
  });

  describe('sort normalization (data-driven)', () => {
    const validSorts: StemSort[] = ['mushaf-order', 'occurrences', 'alpha'];
    validSorts.forEach((sort) => {
      it(`accepts valid sort "${sort}"`, () => {
        expect(parseStemsQueryParams(params(`sort=${sort}`)).sort).toBe(sort);
      });
    });

    const invalidSorts = ['relevance', 'desc', '', 'ALPHA', 'occurrences '];
    invalidSorts.forEach((sort) => {
      it(`normalizes invalid sort "${sort}" to default`, () => {
        expect(parseStemsQueryParams(params(`sort=${sort}`)).sort).toBe('mushaf-order');
      });
    });
  });

  describe('page normalization (data-driven)', () => {
    const malformedPages = ['0', '-2', 'abc', '12abc', '1.5', ' ', '0x1'];
    malformedPages.forEach((page) => {
      it(`normalizes malformed/non-positive page "${page}" to 1`, () => {
        expect(parseStemsQueryParams(params(`page=${page}`)).page).toBe(1);
      });
    });

    const validPages = ['1', '2', '3', '99999'];
    validPages.forEach((page) => {
      it(`preserves valid positive page "${page}"`, () => {
        expect(parseStemsQueryParams(params(`page=${page}`)).page).toBe(Number(page));
      });
    });

    it('preserves a positive out-of-range page unchanged (backend controls empty result)', () => {
      expect(parseStemsQueryParams(params('page=99999')).page).toBe(99999);
    });
  });

  describe('stemId / selection normalization (data-driven)', () => {
    const malformedIds = ['0', '-5', 'abc', '12abc', '1.5', ' ', '0x1'];
    malformedIds.forEach((stem) => {
      it(`normalizes malformed/non-positive stem "${stem}" to null and keeps default view`, () => {
        const parsed = parseStemsQueryParams(params(`stem=${stem}&view=ayahs`));
        expect(parsed.stemId).toBeNull();
        expect(parsed.view).toBe('words');
      });
    });

    it('accepts a valid positive stem id and reads view', () => {
      const parsed = parseStemsQueryParams(params('stem=600&view=ayahs'));
      expect(parsed.stemId).toBe(600);
      expect(parsed.view).toBe('ayahs');
      expect(parsed.column).toBeNull();
    });

    it('ignores view when no stem is present (keeps the default view, no panel renders)', () => {
      const parsed = parseStemsQueryParams(params('view=surahs'));
      expect(parsed.stemId).toBeNull();
      expect(parsed.view).toBe('words');
    });

    it('does not surface a selected identity when stem is absent even if other keys are present', () => {
      const parsed = parseStemsQueryParams(params('view=ayahs&detailPage=9'));
      expect(parsed.stemId).toBeNull();
      expect(parsed.view).toBe('words');
    });
  });

  describe('view normalization (data-driven)', () => {
    const validViews: StemView[] = ['words', 'ayahs', 'surahs', 'lemmas'];
    validViews.forEach((view) => {
      it(`accepts valid view "${view}" when stem is present`, () => {
        expect(parseStemsQueryParams(params(`stem=1&view=${view}`)).view).toBe(view);
      });
    });

    const invalidViews = ['overview', 'root', '', 'WORDS', 'ayahs '];
    invalidViews.forEach((view) => {
      it(`normalizes invalid view "${view}" to default while keeping stem`, () => {
        const parsed = parseStemsQueryParams(params(`stem=1&view=${view}`));
        expect(parsed.stemId).toBe(1);
        expect(parsed.view).toBe('words');
      });
    });
  });

  it('preserves a valid explicit active column for ayahs restore', () => {
    expect(parseStemsQueryParams(params('stem=600&view=ayahs&column=ayahs')).column).toBe('ayahs');
  });

  describe('sub-view scope rules (wordView / surahView)', () => {
    it('honors wordView only when view=words', () => {
      expect(parseStemsQueryParams(params('stem=600&view=words&wordView=tashkeel')).wordView).toBe('tashkeel');
      expect(parseStemsQueryParams(params('stem=600&view=ayahs&wordView=tashkeel')).wordView).toBe('simple');
      expect(parseStemsQueryParams(params('stem=600&view=surahs&wordView=tashkeel')).wordView).toBe('simple');
      expect(parseStemsQueryParams(params('stem=600&view=lemmas&wordView=tashkeel')).wordView).toBe('simple');
      expect(parseStemsQueryParams(params('stem=600&view=words&wordView=bogus')).wordView).toBe('simple');
    });

    it('honors surahView only when view=surahs', () => {
      expect(parseStemsQueryParams(params('stem=600&view=surahs&surahView=missing')).surahView).toBe('missing');
      expect(parseStemsQueryParams(params('stem=600&view=words&surahView=missing')).surahView).toBe('mentioned');
      expect(parseStemsQueryParams(params('stem=600&view=ayahs&surahView=missing')).surahView).toBe('mentioned');
      expect(parseStemsQueryParams(params('stem=600&view=lemmas&surahView=missing')).surahView).toBe('mentioned');
      expect(parseStemsQueryParams(params('stem=600&view=surahs&surahView=bogus')).surahView).toBe('mentioned');
    });
  });

  describe('detailPage scope rules', () => {
    it('honors detailPage only for paginated views (ayahs/words)', () => {
      expect(parseStemsQueryParams(params('stem=600&view=ayahs&detailPage=4')).detailPage).toBe(4);
      expect(parseStemsQueryParams(params('stem=600&view=words&detailPage=2')).detailPage).toBe(2);
      expect(parseStemsQueryParams(params('stem=600&view=surahs&detailPage=9')).detailPage).toBe(1);
      expect(parseStemsQueryParams(params('stem=600&view=lemmas&detailPage=9')).detailPage).toBe(1);
    });

    it('defaults detailPage to 1 when paginated view is set but detailPage is missing', () => {
      expect(parseStemsQueryParams(params('stem=600&view=ayahs')).detailPage).toBe(1);
      expect(parseStemsQueryParams(params('stem=600&view=words')).detailPage).toBe(1);
    });

    it('normalizes malformed detailPage to 1 even for paginated views', () => {
      expect(parseStemsQueryParams(params('stem=600&view=ayahs&detailPage=0')).detailPage).toBe(1);
      expect(parseStemsQueryParams(params('stem=600&view=words&detailPage=abc')).detailPage).toBe(1);
      expect(parseStemsQueryParams(params('stem=600&view=words&detailPage=12abc')).detailPage).toBe(1);
    });

    it('preserves positive out-of-range detailPage for paginated views', () => {
      expect(parseStemsQueryParams(params('stem=600&view=words&detailPage=99999')).detailPage).toBe(99999);
    });
  });

  describe('typeCode scope rules', () => {
    it('honors typeCode only for view=ayahs and trims it', () => {
      expect(parseStemsQueryParams(params('stem=600&view=ayahs&typeCode=N')).typeCode).toBe('N');
      expect(parseStemsQueryParams(params('stem=600&view=ayahs&typeCode=%20N%20')).typeCode).toBe('N');
      expect(parseStemsQueryParams(params('stem=600&view=words&typeCode=N')).typeCode).toBeNull();
      expect(parseStemsQueryParams(params('stem=600&view=surahs&typeCode=N')).typeCode).toBeNull();
      expect(parseStemsQueryParams(params('stem=600&view=ayahs&typeCode=%20%20')).typeCode).toBeNull();
    });
  });

  it('ignores irrelevant query keys without breaking normalization', () => {
    const parsed = parseStemsQueryParams(
      params('foo=bar&debug=1&stem=600&view=words&wordView=simple&detailPage=2&random=xyz'),
    );
    expect(parsed.stemId).toBe(600);
    expect(parsed.view).toBe('words');
    expect(parsed.wordView).toBe('simple');
    expect(parsed.detailPage).toBe(2);
  });
});

describe('buildStemsQueryParams', () => {
  it('builds only the provided fields so merge preserves the rest', () => {
    expect(buildStemsQueryParams({ search: 'علم' })).toEqual({ search: 'علم' });
    expect(buildStemsQueryParams({ page: 3 })).toEqual({ page: '3' });
  });

  it('skips undefined fields so merge preserves the rest', () => {
    const built = buildStemsQueryParams({ sort: 'alpha' });
    expect(built).toEqual({ sort: 'alpha' });
    expect('search' in built).toBe(false);
  });

  it('stringifies numeric stem/page/detailPage and passes null through to remove', () => {
    expect(
      buildStemsQueryParams({
        page: 2,
        stemId: 7,
        detailPage: 4,
        view: 'ayahs',
        column: 'ayahs',
      }),
    ).toEqual({ page: '2', stem: '7', detailPage: '4', view: 'ayahs', column: 'ayahs' });

    expect(buildStemsQueryParams({ search: null, page: null })).toEqual({
      search: null,
      page: null,
    });

    expect(buildStemsQueryParams({ typeCode: ' N ' })).toEqual({ typeCode: 'N' });
  });

  it('round-trips parse -> build for list + selection + sub-view state', () => {
    const original = params('search=علم&sort=alpha&page=2&stem=9&view=ayahs&typeCode=N&detailPage=3');
    const parsed = parseStemsQueryParams(original);

    const rebuilt = buildStemsQueryParams({
      search: parsed.search,
      sort: parsed.sort,
      page: parsed.page,
      stemId: parsed.stemId,
      view: parsed.view,
      column: parsed.column,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
      typeCode: parsed.typeCode,
    });

    expect(rebuilt['search']).toBe('علم');
    expect(rebuilt['sort']).toBe('alpha');
    expect(rebuilt['page']).toBe('2');
    expect(rebuilt['stem']).toBe('9');
    expect(rebuilt['view']).toBe('ayahs');
    expect(rebuilt['column']).toBeNull();
    expect(rebuilt['typeCode']).toBe('N');
    expect(rebuilt['detailPage']).toBe('3');
  });
});

describe('buildClearSelectionQueryParams', () => {
  it('clears only the selection keys, preserving list context (search/sort/page)', () => {
    const cleared = buildClearSelectionQueryParams();

    expect(Object.keys(cleared).sort()).toEqual(
      [
        STEMS_QUERY_KEYS.stem,
        STEMS_QUERY_KEYS.view,
        STEMS_QUERY_KEYS.column,
        STEMS_QUERY_KEYS.wordView,
        STEMS_QUERY_KEYS.surahView,
        STEMS_QUERY_KEYS.detailPage,
        STEMS_QUERY_KEYS.typeCode,
      ].sort(),
    );
    expect(Object.values(cleared)).toEqual([null, null, null, null, null, null, null]);

    expect(cleared).not.toHaveProperty(STEMS_QUERY_KEYS.search);
    expect(cleared).not.toHaveProperty(STEMS_QUERY_KEYS.sort);
    expect(cleared).not.toHaveProperty(STEMS_QUERY_KEYS.page);
  });
});

describe('buildStemsDeepLink', () => {
  it('builds a stable deep link to the stems route', () => {
    expect(
      buildStemsDeepLink({
        search: 'علم',
        sort: 'alpha',
        page: 3,
        stemId: 55,
        view: 'ayahs',
        typeCode: 'N',
        detailPage: 2,
      }),
    ).toEqual({
      path: '/dashboard/words/stems',
      queryParams: {
        search: 'علم',
        sort: 'alpha',
        page: '3',
        stem: '55',
        view: 'ayahs',
        typeCode: 'N',
        detailPage: '2',
      },
    });
  });

  it('omits absent query params when only the route is needed', () => {
    expect(buildStemsDeepLink()).toEqual({
      path: '/dashboard/words/stems',
      queryParams: {},
    });
  });

  it('builds the canonical stem-only selection deep link (words/simple default)', () => {
    expect(buildStemsDeepLink({ stemId: 42, view: 'words', wordView: 'simple' })).toEqual({
      path: '/dashboard/words/stems',
      queryParams: { stem: '42', view: 'words', wordView: 'simple' },
    });
  });

  it('builds a related-lemmas deep link targeting the lemmas explorer', () => {
    expect(buildStemsDeepLink({ stemId: 7, view: 'lemmas' })).toEqual({
      path: '/dashboard/words/stems',
      queryParams: { stem: '7', view: 'lemmas' },
    });
  });

  it('produces independent query-param objects per call (no shared mutation)', () => {
    const a = buildStemsDeepLink({ stemId: 1 });
    const b = buildStemsDeepLink({ stemId: 2 });
    expect(a.queryParams['stem']).toBe('1');
    expect(b.queryParams['stem']).toBe('2');
  });
});

describe('parseStemsQueryParams association filters (Feature 026, US7)', () => {
  it('is absent for a pre-feature URL (backward compat)', () => {
    expect(parseStemsQueryParams(params('search=حكم&sort=alpha&page=2')).association).toEqual({
      rootId: null,
      lemmaId: null,
    });
  });

  it('parses positive rootId/lemmaId (primary-association filters)', () => {
    expect(parseStemsQueryParams(params('rootId=701&lemmaId=502')).association).toEqual({
      rootId: 701,
      lemmaId: 502,
    });
  });

  it('fails closed on non-positive or non-numeric ids', () => {
    expect(parseStemsQueryParams(params('rootId=0')).association.rootId).toBeNull();
    expect(parseStemsQueryParams(params('lemmaId=-2')).association.lemmaId).toBeNull();
    expect(parseStemsQueryParams(params('rootId=abc&lemmaId=x')).association).toEqual({ rootId: null, lemmaId: null });
  });

  it('serializes rootId/lemmaId and passes null through to remove', () => {
    expect(buildStemsQueryParams({ rootId: 701, lemmaId: 502 })).toEqual({ rootId: '701', lemmaId: '502' });
    expect(buildStemsQueryParams({ rootId: null, lemmaId: null })).toEqual({ rootId: null, lemmaId: null });
  });
});

describe('parseStemsQueryParams count ranges (Feature 026)', () => {
  it('has no active ranges for a pre-feature URL (backward compat)', () => {
    expect(parseStemsQueryParams(params('search=حكم&sort=alpha&page=2')).ranges).toEqual({});
  });

  it('parses active ranges from their URL keys', () => {
    const ranges = parseStemsQueryParams(params('occ=11..100&tashkeel=2..')).ranges;
    expect(ranges).toEqual({ occurrences: { min: 11, max: 100 }, tashkeelWords: { min: 2, max: null } });
  });

  it('drops malformed ranges fail-closed while the page still parses', () => {
    const parsed = parseStemsQueryParams(params('occ=9..2&surahs=1..50'));
    expect(parsed.ranges).toEqual({ surahs: { min: 1, max: 50 } });
    expect(parsed.page).toBe(1);
  });
});

describe('parseStemsQueryParams sort tokens (Feature 030, N8)', () => {
  it.each(STEM_SORT_KEYS)('parses the canonical token "%s" verbatim', (sort) => {
    expect(parseStemsQueryParams(params(`sort=${sort}`)).sort).toBe(sort);
  });

  it.each([
    ['occurrences-desc', 'occurrences'],
    ['ayahs-desc', 'ayahs'],
    ['surahs-desc', 'surahs'],
    ['alpha-asc', 'alpha'],
  ])('canonicalizes the legacy alias "%s" to "%s"', (alias, canonical) => {
    expect(parseStemsQueryParams(params(`sort=${alias}`)).sort).toBe(canonical);
  });

  it.each([
    'relevance',
    'relevance-asc',
    'stems',
    'lemmas',
    'mushaf-order-asc',
    'mushaf-order-desc',
    '-asc',
  ])('fails closed to the default on the unsupported token "%s"', (sort) => {
    expect(parseStemsQueryParams(params(`sort=${sort}`)).sort).toBe('mushaf-order');
  });

  it('round-trips a suffixed token through build', () => {
    expect(buildStemsQueryParams({ sort: 'occurrences-asc' })).toEqual({ sort: 'occurrences-asc' });
  });

  it('removes the param on release, so the default order stays param-free', () => {
    expect(buildStemsQueryParams({ sort: null, page: null })).toEqual({ sort: null, page: null });
  });
});
