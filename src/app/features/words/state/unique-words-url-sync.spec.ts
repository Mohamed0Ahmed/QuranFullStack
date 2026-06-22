import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import {
  buildModalCloseQueryParams,
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
    const original = params('search=اسم&sort=alpha&page=2&word=9&view=missing');
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
    expect(rebuilt['view']).toBe('missing');
    // ayahPage parsed as null for non-ayahs views; null clears the param.
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
