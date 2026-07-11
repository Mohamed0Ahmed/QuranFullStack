import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import {
  buildWordTypesDeepLink,
  buildWordTypesQueryParams,
  clearWordTypesSelection,
  parseWordTypesQueryParams,
} from './word-types-url-sync';
import { WORD_TYPES_QUERY_KEYS } from '../models/word-types.models';

function params(query: string): ParamMap {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

describe('parseWordTypesQueryParams — child codes', () => {
  it('keeps a valid noun child code (POS code) preserving case', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun&childCode=PN'));

    expect(parsed.type).toBe('noun');
    expect(parsed.childCode).toBe('PN');
  });

  it('keeps a valid verb tense child code', () => {
    const parsed = parseWordTypesQueryParams(params('type=verb&childCode=present'));

    expect(parsed.type).toBe('verb');
    expect(parsed.childCode).toBe('present');
  });

  it('clears an invalid verb child code that is not a tense literal', () => {
    const parsed = parseWordTypesQueryParams(params('type=verb&childCode=future'));

    expect(parsed.childCode).toBeNull();
  });

  it('clears a verb child code that is actually a noun POS code', () => {
    // POS codes are not verb tense literals; the parser drops them under the verb type.
    const parsed = parseWordTypesQueryParams(params('type=verb&childCode=N'));

    expect(parsed.childCode).toBeNull();
  });

  it('keeps a valid particle child code', () => {
    const parsed = parseWordTypesQueryParams(params('type=particle&childCode=PRO'));

    expect(parsed.childCode).toBe('PRO');
  });

  it('drops child code entirely for inl (leaf node)', () => {
    const parsed = parseWordTypesQueryParams(params('type=inl&childCode=INL'));

    expect(parsed.childCode).toBeNull();
  });

  it('treats a blank child code as no selection', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun&childCode='));

    expect(parsed.childCode).toBeNull();
  });
});

describe('buildWordTypesQueryParams — child selection', () => {
  it('emits the childCode param when a child is selected', () => {
    const built = buildWordTypesQueryParams({ childCode: 'PN' });

    expect(built[WORD_TYPES_QUERY_KEYS.childCode]).toBe('PN');
  });

  it('emits null for childCode when clearing back to the parent', () => {
    const built = buildWordTypesQueryParams({ childCode: null });

    expect(built[WORD_TYPES_QUERY_KEYS.childCode]).toBeNull();
  });
});

describe('buildWordTypesQueryParams — canonical ordering', () => {
  it('emits list and detail keys in stable route order', () => {
    const built = buildWordTypesQueryParams({
      type: 'verb',
      childCode: 'present',
      case: 'all',
      tense: 'present',
      voice: 'passive',
      sort: 'ayahs',
      page: 3,
      word: 191004,
      contextCode: 'present',
      view: 'surahs',
      detailPage: 2,
      location: '2:25:2',
      column: 'analysis',
    });

    expect(Object.keys(built)).toEqual([
      WORD_TYPES_QUERY_KEYS.type,
      WORD_TYPES_QUERY_KEYS.childCode,
      WORD_TYPES_QUERY_KEYS.case,
      WORD_TYPES_QUERY_KEYS.tense,
      WORD_TYPES_QUERY_KEYS.voice,
      WORD_TYPES_QUERY_KEYS.sort,
      WORD_TYPES_QUERY_KEYS.page,
      WORD_TYPES_QUERY_KEYS.word,
      WORD_TYPES_QUERY_KEYS.contextCode,
      WORD_TYPES_QUERY_KEYS.view,
      WORD_TYPES_QUERY_KEYS.detailPage,
      WORD_TYPES_QUERY_KEYS.location,
      WORD_TYPES_QUERY_KEYS.column,
    ]);
    expect(built[WORD_TYPES_QUERY_KEYS.view]).toBe('surahs');
    expect(built[WORD_TYPES_QUERY_KEYS.detailPage]).toBe('2');
    expect(built[WORD_TYPES_QUERY_KEYS.location]).toBe('2:25:2');
    expect(built[WORD_TYPES_QUERY_KEYS.column]).toBe('analysis');
  });
});

describe('parseWordTypesQueryParams — stale deep-link tolerance', () => {
  it('falls back stale analysis view to ayahs while keeping tolerated location params', () => {
    const parsed = parseWordTypesQueryParams(params('view=analysis&location=1:1:2&column=analysis'));

    expect(parsed.view).toBe('ayahs');
    expect(parsed.location).toBe('1:1:2');
    expect(parsed.column).toBe('analysis');
  });
});

describe('clearWordTypesSelection', () => {
  it('clears selection params but preserves list filter params', () => {
    const cleared = clearWordTypesSelection();

    expect(cleared[WORD_TYPES_QUERY_KEYS.word]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.contextCode]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.view]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.detailPage]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.location]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.column]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.childCode]).toBeUndefined();
  });
});

describe('buildWordTypesDeepLink', () => {
  it('targets the word types route with a full selected-row query', () => {
    const link = buildWordTypesDeepLink({
      type: 'noun',
      childCode: 'PN',
      page: 1,
      word: 191001,
      contextCode: 'PN',
      view: 'surahs',
      detailPage: 2,
      location: '1:1:2',
      column: 'analysis',
    });

    expect(link.path).toContain('types');
    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.childCode]).toBe('PN');
    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.word]).toBe('191001');
    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.contextCode]).toBe('PN');
    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.view]).toBe('surahs');
    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.detailPage]).toBe('2');
    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.location]).toBe('1:1:2');
    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.column]).toBe('analysis');
  });

  it('preserves particle child codes in the deep-link query', () => {
    const link = buildWordTypesDeepLink({
      type: 'particle',
      childCode: 'PRO',
      page: 1,
    });

    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.childCode]).toBe('PRO');
  });
});

describe('parseWordTypesQueryParams — secondary filters', () => {
  it('keeps a valid noun case filter', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun&case=genitive'));

    expect(parsed.case).toBe('genitive');
  });

  it('keeps a valid verb tense and voice filter', () => {
    const parsed = parseWordTypesQueryParams(params('type=verb&tense=present&voice=passive'));

    expect(parsed.tense).toBe('present');
    expect(parsed.voice).toBe('passive');
  });

  it('keeps the null case filter value meaning غير محدد', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun&case=null'));

    expect(parsed.case).toBe('null');
  });

  it('ignores a case filter when the type is not noun (cross-type normalization)', () => {
    const parsed = parseWordTypesQueryParams(params('type=verb&case=genitive'));

    expect(parsed.case).toBe('all');
  });

  it('ignores tense/voice filters when the type is not verb', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun&tense=past&voice=active'));

    expect(parsed.tense).toBe('all');
    expect(parsed.voice).toBe('all');
  });

  it('drops particle and inl secondary filters entirely', () => {
    const particleParsed = parseWordTypesQueryParams(params('type=particle&case=genitive&tense=past'));
    const inlParsed = parseWordTypesQueryParams(params('type=inl&voice=active'));

    expect(particleParsed.case).toBe('all');
    expect(particleParsed.tense).toBe('all');
    expect(inlParsed.voice).toBe('all');
  });

  it('clears an unknown case value to the default', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun&case=bogus'));

    expect(parsed.case).toBe('all');
  });

  it('defaults every secondary filter to all when missing', () => {
    const parsed = parseWordTypesQueryParams(params('type=verb'));

    expect(parsed.case).toBe('all');
    expect(parsed.tense).toBe('all');
    expect(parsed.voice).toBe('all');
  });
});

describe('buildWordTypesQueryParams — secondary filters', () => {
  it('emits a concrete case value', () => {
    const built = buildWordTypesQueryParams({ case: 'nominative' });

    expect(built[WORD_TYPES_QUERY_KEYS.case]).toBe('nominative');
  });

  it('emits null when resetting a secondary filter to all', () => {
    const built = buildWordTypesQueryParams({ tense: null });

    expect(built[WORD_TYPES_QUERY_KEYS.tense]).toBeNull();
  });
});

describe('parseWordTypesQueryParams — tableView', () => {
  it('defaults a missing tableView to words', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun'));

    expect(parsed.tableView).toBe('words');
  });

  it('defaults an unknown tableView to words', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun&tableView=bogus'));

    expect(parsed.tableView).toBe('words');
  });

  it('keeps a valid tableView', () => {
    const parsed = parseWordTypesQueryParams(params('type=noun&tableView=roots'));

    expect(parsed.tableView).toBe('roots');
  });

  it('clears stale word-selection params when tableView is not words, even if the URL supplies them', () => {
    const parsed = parseWordTypesQueryParams(
      params('type=noun&childCode=PN&tableView=roots&word=123&contextCode=PN&view=surahs&detailPage=2&location=2:1:2&column=analysis'),
    );

    expect(parsed.tableView).toBe('roots');
    expect(parsed.word).toBeNull();
    expect(parsed.tashkeelWordId).toBe(0);
    expect(parsed.contextCode).toBe('');
    expect(parsed.view).toBe('ayahs');
    expect(parsed.detailPage).toBe(1);
    expect(parsed.location).toBeNull();
    expect(parsed.column).toBeNull();
  });

  it('keeps selection params when tableView is words', () => {
    const parsed = parseWordTypesQueryParams(
      params('type=noun&childCode=PN&tableView=words&word=123&contextCode=PN'),
    );

    expect(parsed.word).toBe(123);
    expect(parsed.contextCode).toBe('PN');
  });
});

describe('buildWordTypesQueryParams — tableView', () => {
  it('emits a concrete tableView value', () => {
    const built = buildWordTypesQueryParams({ tableView: 'lemmas' });

    expect(built[WORD_TYPES_QUERY_KEYS.tableView]).toBe('lemmas');
  });

  it('places tableView right after childCode in the canonical order', () => {
    const built = buildWordTypesQueryParams({
      type: 'noun',
      childCode: 'PN',
      tableView: 'roots',
      case: 'all',
      sort: 'occurrences',
      page: 1,
    });

    expect(Object.keys(built)).toEqual([
      WORD_TYPES_QUERY_KEYS.type,
      WORD_TYPES_QUERY_KEYS.childCode,
      WORD_TYPES_QUERY_KEYS.tableView,
      WORD_TYPES_QUERY_KEYS.case,
      WORD_TYPES_QUERY_KEYS.sort,
      WORD_TYPES_QUERY_KEYS.page,
    ]);
  });
});
