import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import {
  buildModalCloseQueryParams,
  buildUniqueWordsDeepLink,
  buildUniqueWordsQueryParams,
  parseUniqueWordsQueryParams,
} from './unique-words-url-sync';
import { UNIQUE_WORDS_QUERY_KEYS } from '../models/unique-words.models';

function params(query: string): ParamMap {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

describe('parseUniqueWordsQueryParams', () => {
  it('applies documented defaults when params are absent', () => {
    const parsed = parseUniqueWordsQueryParams(params(''));

    expect(parsed.search).toBe('');
    expect(parsed.sort).toBe('mushaf-order');
    expect(parsed.page).toBe(1);
    expect(parsed.wordId).toBeNull();
    expect(parsed.view).toBeNull();
    expect(parsed.ayahPage).toBeNull();
  });

  it('reads list state (search/sort/page) verbatim when valid', () => {
    const parsed = parseUniqueWordsQueryParams(params('search=اسم&sort=alpha&page=3'));

    expect(parsed.search).toBe('اسم');
    expect(parsed.sort).toBe('alpha');
    expect(parsed.page).toBe(3);
  });

  it('normalizes an unsupported sort to the default instead of failing', () => {
    const parsed = parseUniqueWordsQueryParams(params('sort=relevance'));

    expect(parsed.sort).toBe('mushaf-order');
  });

  it('normalizes a non-positive or non-numeric page to the default', () => {
    expect(parseUniqueWordsQueryParams(params('page=0')).page).toBe(1);
    expect(parseUniqueWordsQueryParams(params('page=-2')).page).toBe(1);
    expect(parseUniqueWordsQueryParams(params('page=abc')).page).toBe(1);
    expect(parseUniqueWordsQueryParams(params('page=12abc')).page).toBe(1);
  });

  it('reads modal state (word/view/ap) when word is present', () => {
    const parsed = parseUniqueWordsQueryParams(params('word=42&view=ayahs&ap=2'));

    expect(parsed.wordId).toBe(42);
    expect(parsed.view).toBe('ayahs');
    expect(parsed.ayahPage).toBe(2);
  });

  it('ignores view/ap when no word is present', () => {
    const parsed = parseUniqueWordsQueryParams(params('view=ayahs&ap=3'));

    expect(parsed.wordId).toBeNull();
    expect(parsed.view).toBeNull();
    expect(parsed.ayahPage).toBeNull();
  });

  it('normalizes an unsupported view to null while keeping the word', () => {
    const parsed = parseUniqueWordsQueryParams(params('word=42&view=bogus'));

    expect(parsed.wordId).toBe(42);
    expect(parsed.view).toBeNull();
  });

  it('normalizes a non-positive word id to null', () => {
    expect(parseUniqueWordsQueryParams(params('word=0')).wordId).toBeNull();
    expect(parseUniqueWordsQueryParams(params('word=-5')).wordId).toBeNull();
    expect(parseUniqueWordsQueryParams(params('word=abc')).wordId).toBeNull();
    expect(parseUniqueWordsQueryParams(params('word=12abc')).wordId).toBeNull();
  });

  it('defaults ayahPage to 1 when view is ayahs but ap is missing', () => {
    const parsed = parseUniqueWordsQueryParams(params('word=42&view=ayahs'));

    expect(parsed.ayahPage).toBe(1);
  });

  it('ignores ap when view is not ayahs', () => {
    const parsed = parseUniqueWordsQueryParams(params('word=42&view=surahs&ap=9'));

    expect(parsed.view).toBe('surahs');
    expect(parsed.ayahPage).toBeNull();
  });
});

describe('buildUniqueWordsQueryParams', () => {
  it('builds only the provided fields so merge preserves the rest', () => {
    expect(buildUniqueWordsQueryParams({ search: 'اسم' })).toEqual({ search: 'اسم' });
    expect(buildUniqueWordsQueryParams({ page: 3 })).toEqual({ page: '3' });
  });

  it('stringifies numeric page/word/ap and passes null through to remove', () => {
    expect(
      buildUniqueWordsQueryParams({ page: 2, wordId: 7, ayahPage: 4, view: 'ayahs' }),
    ).toEqual({ page: '2', word: '7', ap: '4', view: 'ayahs' });

    expect(buildUniqueWordsQueryParams({ search: null, page: null })).toEqual({
      search: null,
      page: null,
    });
  });

  it('skips undefined fields so merge preserves the rest', () => {
    const built = buildUniqueWordsQueryParams({ sort: 'alpha' });
    expect(built).toEqual({ sort: 'alpha' });
    expect('search' in built).toBe(false);
  });

  it('round-trips parse -> build for list and modal state', () => {
    const original = params('search=اسم&sort=alpha&page=2&word=9&view=bogus');
    const parsed = parseUniqueWordsQueryParams(original);

    const rebuilt = buildUniqueWordsQueryParams({
      search: parsed.search,
      sort: parsed.sort,
      page: parsed.page,
      wordId: parsed.wordId,
      view: parsed.view,
      ayahPage: parsed.ayahPage,
    });

    expect(rebuilt['search']).toBe('اسم');
    expect(rebuilt['sort']).toBe('alpha');
    expect(rebuilt['page']).toBe('2');
    expect(rebuilt['word']).toBe('9');
    expect(rebuilt['view']).toBeNull();

    expect(rebuilt['ap']).toBeNull();
  });
});

describe('buildModalCloseQueryParams', () => {
  it('clears only the modal keys, preserving list context', () => {
    const cleared = buildModalCloseQueryParams();

    expect(Object.keys(cleared)).toEqual([
      UNIQUE_WORDS_QUERY_KEYS.word,
      UNIQUE_WORDS_QUERY_KEYS.view,
      UNIQUE_WORDS_QUERY_KEYS.ayahPage,
    ]);
    expect(Object.values(cleared)).toEqual([null, null, null]);
  });
});

describe('buildUniqueWordsDeepLink', () => {
  it('builds a stable deep link from the words route and stable word id', () => {
    expect(
      buildUniqueWordsDeepLink('simple', {
        search: 'اسم',
        sort: 'alpha',
        page: 3,
        wordId: 42,
        view: 'ayahs',
        ayahPage: 2,
      }),
    ).toEqual({
      path: '/dashboard/words/unique/simple',
      queryParams: {
        search: 'اسم',
        sort: 'alpha',
        page: '3',
        word: '42',
        view: 'ayahs',
        ap: '2',
      },
    });
  });

  it('omits absent query params when only the route is needed', () => {
    expect(buildUniqueWordsDeepLink('tashkeel')).toEqual({
      path: '/dashboard/words/unique/tashkeel',
      queryParams: {},
    });
  });
});

describe('parseUniqueWordsQueryParams count ranges (Feature 026)', () => {
  it('has no active ranges for a pre-feature URL (backward compat)', () => {
    expect(parseUniqueWordsQueryParams(params('search=اسم&sort=alpha&page=2')).ranges).toEqual({});
  });

  it('parses active ranges from their URL keys', () => {
    const ranges = parseUniqueWordsQueryParams(params('occ=11..100&surahs=1..1')).ranges;
    expect(ranges).toEqual({ occurrences: { min: 11, max: 100 }, surahs: { min: 1, max: 1 } });
  });

  it('drops malformed ranges fail-closed while the page still parses', () => {
    const parsed = parseUniqueWordsQueryParams(params('occ=9..2&ayahs=2..10'));
    expect(parsed.ranges).toEqual({ ayahs: { min: 2, max: 10 } });
    expect(parsed.page).toBe(1);
  });
});
