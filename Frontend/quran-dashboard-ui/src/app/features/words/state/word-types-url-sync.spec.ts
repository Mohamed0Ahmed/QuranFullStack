import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import * as wordTypesUrlSync from './word-types-url-sync';

import {
  buildWordTypesDeepLink,
  buildWordTypesQueryParams,
  canonicalWordTypesDetailPage,
  clearSelectionForTableView,
  clearWordTypesSelection,
  parseWordTypesQueryParams,
} from './word-types-url-sync';
import { WORD_TYPES_QUERY_KEYS } from '../models/word-types.models';
import { WordTypeDetailSelection } from '../models/word-types-detail.models';

function params(query: string): ParamMap {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

function encodeDetailScope(selection: unknown): Record<string, string | null> | undefined {
  const encoder = (wordTypesUrlSync as unknown as {
    buildWordTypesDetailScopeQuery?: (value: WordTypeDetailSelection) => Record<string, string | null>;
  }).buildWordTypesDetailScopeQuery;
  return encoder?.(selection as WordTypeDetailSelection);
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
      root: 12,
      stem: 34,
      lemma: 56,
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
      WORD_TYPES_QUERY_KEYS.root,
      WORD_TYPES_QUERY_KEYS.stem,
      WORD_TYPES_QUERY_KEYS.lemma,
      WORD_TYPES_QUERY_KEYS.view,
      WORD_TYPES_QUERY_KEYS.detailPage,
      WORD_TYPES_QUERY_KEYS.location,
      WORD_TYPES_QUERY_KEYS.column,
    ]);
    expect(built['dim']).toBeUndefined();
    expect(built[WORD_TYPES_QUERY_KEYS.view]).toBe('surahs');
    expect(built[WORD_TYPES_QUERY_KEYS.detailPage]).toBe('2');
    expect(built[WORD_TYPES_QUERY_KEYS.location]).toBe('2:25:2');
    expect(built[WORD_TYPES_QUERY_KEYS.column]).toBe('analysis');
  });
});

describe('independent detail-scope URL state', () => {
  it('declares the five locked detail-scope key names', () => {
    const keys = WORD_TYPES_QUERY_KEYS as unknown as Record<string, string>;

    expect([
      keys['detailType'],
      keys['detailChildCode'],
      keys['detailCase'],
      keys['detailTense'],
      keys['detailVoice'],
    ]).toEqual(['detailType', 'detailChildCode', 'detailCase', 'detailTense', 'detailVoice']);
  });

  it('encodes the full grouped query scope independently from list scope', () => {
    const encoded = encodeDetailScope({
      kind: 'root',
      rootId: 123,
      scope: {
        type: 'verb',
        childCode: 'present',
        case: 'all',
        tense: 'present',
        voice: 'passive',
      },
    });

    expect(encoded).toEqual({
      detailType: 'verb',
      detailChildCode: 'present',
      detailCase: 'all',
      detailTense: 'present',
      detailVoice: 'passive',
    });
  });

  it('encodes the full word scope needed for exact restored row selection', () => {
    const encoded = encodeDetailScope({
      kind: 'word',
      identity: {
        tashkeelWordId: 191004,
        contextCode: 'present',
        case: 'all',
        tense: 'present',
        voice: 'active',
      },
      scope: {
        type: 'verb',
        childCode: 'present',
        case: 'all',
        tense: 'present',
        voice: 'active',
      },
    });

    expect(encoded).toEqual({
      detailType: 'verb',
      detailChildCode: 'present',
      detailCase: 'all',
      detailTense: 'present',
      detailVoice: 'active',
    });
  });

  it('parses the full stored word scope independently from its current list scope', () => {
    const parsed = parseWordTypesQueryParams(params(
      'type=noun&childCode=ADJ&tableView=words&word=191004&contextCode=present&detailType=verb&detailChildCode=present&detailCase=all&detailTense=present&detailVoice=active&view=ayahs',
    )) as ReturnType<typeof parseWordTypesQueryParams> & Record<string, unknown>;

    expect({
      listType: parsed.type,
      listChildCode: parsed.childCode,
      detailType: parsed['detailType'],
      detailChildCode: parsed['detailChildCode'],
    }).toEqual({
      listType: 'noun',
      listChildCode: 'ADJ',
      detailType: 'verb',
      detailChildCode: 'present',
    });
  });

  it('parses different list and grouped detail scopes without inference', () => {
    const parsed = parseWordTypesQueryParams(params(
      'type=noun&childCode=ADJ&tableView=roots&root=123&detailType=verb&detailChildCode=present&detailCase=all&detailTense=present&detailVoice=passive&view=ayahs',
    )) as ReturnType<typeof parseWordTypesQueryParams> & Record<string, unknown>;

    expect({ type: parsed.type, childCode: parsed.childCode, tense: parsed.tense, voice: parsed.voice }).toEqual({
      type: 'noun', childCode: 'ADJ', tense: 'all', voice: 'all',
    });
    expect({
      detailType: parsed['detailType'],
      detailChildCode: parsed['detailChildCode'],
      detailCase: parsed['detailCase'],
      detailTense: parsed['detailTense'],
      detailVoice: parsed['detailVoice'],
    }).toEqual({
      detailType: 'verb',
      detailChildCode: 'present',
      detailCase: 'all',
      detailTense: 'present',
      detailVoice: 'passive',
    });
  });

  it.each([
    'tableView=roots&root=123&detailChildCode=present&detailCase=all&detailTense=present&detailVoice=all',
    'tableView=roots&root=123&detailType=verb&detailChildCode=future&detailCase=all&detailTense=present&detailVoice=all',
    'tableView=roots&root=123&detailType=noun&detailChildCode=ADJ&detailCase=all&detailTense=present&detailVoice=all',
    'tableView=words&word=191004&contextCode=present&detailType=verb&detailChildCode=present&detailCase=all&detailTense=present',
  ])('fails closed for incomplete or incompatible detail scope: %s', (query) => {
    const parsed = parseWordTypesQueryParams(params(query)) as ReturnType<typeof parseWordTypesQueryParams> & Record<string, unknown>;

    expect(parsed['detailCase']).toBeNull();
    expect(parsed['detailTense']).toBeNull();
    expect(parsed['detailVoice']).toBeNull();
  });

  it('replays list and detail scopes independently for Back and Forward', () => {
    const restored = [
      'type=verb&childCode=present&tableView=roots&root=123&detailType=verb&detailChildCode=present&detailCase=all&detailTense=present&detailVoice=all&view=ayahs',
      'type=noun&childCode=ADJ&tableView=roots&root=123&detailType=verb&detailChildCode=present&detailCase=all&detailTense=present&detailVoice=all&view=ayahs',
      'type=noun&childCode=ADJ&tableView=roots&root=456&detailType=noun&detailChildCode=ADJ&detailCase=all&detailTense=all&detailVoice=all&view=surahs',
    ].map((query) => parseWordTypesQueryParams(params(query)) as ReturnType<typeof parseWordTypesQueryParams> & Record<string, unknown>);

    expect(restored.map((state) => ({
      listType: state.type,
      listChildCode: state.childCode,
      root: state.root,
      detailType: state['detailType'],
      detailChildCode: state['detailChildCode'],
      detailTense: state['detailTense'],
      view: state.view,
    }))).toEqual([
      { listType: 'verb', listChildCode: 'present', root: 123, detailType: 'verb', detailChildCode: 'present', detailTense: 'present', view: 'ayahs' },
      { listType: 'noun', listChildCode: 'ADJ', root: 123, detailType: 'verb', detailChildCode: 'present', detailTense: 'present', view: 'ayahs' },
      { listType: 'noun', listChildCode: 'ADJ', root: 456, detailType: 'noun', detailChildCode: 'ADJ', detailTense: 'all', view: 'surahs' },
    ]);
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
    expect(cleared[WORD_TYPES_QUERY_KEYS.root]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.stem]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.lemma]).toBeNull();
    const detailKeys = WORD_TYPES_QUERY_KEYS as unknown as Record<string, string>;
    expect(cleared[detailKeys['detailType']]).toBeNull();
    expect(cleared[detailKeys['detailChildCode']]).toBeNull();
    expect(cleared[detailKeys['detailCase']]).toBeNull();
    expect(cleared[detailKeys['detailTense']]).toBeNull();
    expect(cleared[detailKeys['detailVoice']]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.view]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.detailPage]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.location]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.column]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.childCode]).toBeUndefined();
  });
});

describe('clearSelectionForTableView', () => {
  it('clears only incompatible selection keys and selection display state', () => {
    const cleared = clearSelectionForTableView('roots');

    expect(cleared[WORD_TYPES_QUERY_KEYS.word]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.contextCode]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.stem]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.lemma]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.root]).toBeUndefined();
    expect(cleared[WORD_TYPES_QUERY_KEYS.view]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.detailPage]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.location]).toBeNull();
    expect(cleared[WORD_TYPES_QUERY_KEYS.column]).toBeNull();
    expect(cleared['dim']).toBeUndefined();
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
    expect(link.queryParams[WORD_TYPES_QUERY_KEYS.detailPage]).toBeNull();
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
    expect(parsed.view).toBe('surahs');
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

describe('parseWordTypesQueryParams — explicit selection identity', () => {
  it.each([
    ['root', 'roots', 123],
    ['stem', 'stems', 456],
    ['lemma', 'lemmas', 789],
  ] as const)('restores %s selection only from tableView=%s and its positive ID', (key, tableView, id) => {
    const parsed = parseWordTypesQueryParams(params(`tableView=${tableView}&${key}=${id}`));

    expect(parsed.tableView).toBe(tableView);
    expect(parsed[key]).toBe(id);
    expect(parsed.root).toBe(key === 'root' ? id : null);
    expect(parsed.stem).toBe(key === 'stem' ? id : null);
    expect(parsed.lemma).toBe(key === 'lemma' ? id : null);
    expect(parsed.view).toBe('words');
    expect(parsed.detailPage).toBe(1);
  });

  it('ignores grouped identity keys that are incompatible with the active tableView', () => {
    const parsed = parseWordTypesQueryParams(
      params('tableView=roots&root=123&stem=456&lemma=789'),
    );

    expect(parsed.root).toBe(123);
    expect(parsed.stem).toBeNull();
    expect(parsed.lemma).toBeNull();
  });

  it.each([
    ['roots', 'root'],
    ['stems', 'stem'],
    ['lemmas', 'lemma'],
  ] as const)('keeps word and contextCode only in words view, not %s', (tableView, key) => {
    const parsed = parseWordTypesQueryParams(
      params(`tableView=${tableView}&${key}=123&word=191004&contextCode=PN`),
    );

    expect(parsed.word).toBeNull();
    expect(parsed.contextCode).toBe('');
  });

  it('normalizes words view back to ayahs for word selections', () => {
    const parsed = parseWordTypesQueryParams(
      params('tableView=words&word=191004&contextCode=PN&view=words'),
    );

    expect(parsed.view).toBe('ayahs');
  });

  it.each([
    ['words', 'word=191004&contextCode=PN', 'words'],
    ['roots', 'root=123', 'ayahs'],
  ] as const)('defaults missing detailPage to one for %s paged %s view', (tableView, selection, view) => {
    const parsed = parseWordTypesQueryParams(params(`tableView=${tableView}&${selection}&view=${view}`));

    expect(parsed.detailPage).toBe(1);
  });

  it('normalizes a grouped surahs URL to internal page one and clears its canonical page param', () => {
    const parsed = parseWordTypesQueryParams(params('tableView=roots&root=123&view=surahs&detailPage=5'));
    const rebuilt = buildWordTypesQueryParams({
      tableView: 'roots',
      root: parsed.root,
      view: parsed.view,
      detailPage: canonicalWordTypesDetailPage(parsed.view, parsed.detailPage),
    });

    expect(parsed.detailPage).toBe(1);
    expect(rebuilt[WORD_TYPES_QUERY_KEYS.detailPage]).toBeNull();
  });

  it('round-trips a grouped selection with its full scope for refresh and sharing', () => {
    const original = params(
      'type=verb&childCode=present&tableView=stems&stem=456&tense=present&voice=passive&view=ayahs&detailPage=2',
    );
    const parsed = parseWordTypesQueryParams(original);
    const rebuilt = buildWordTypesQueryParams({
      type: parsed.type,
      childCode: parsed.childCode,
      tableView: parsed.tableView,
      case: parsed.case,
      tense: parsed.tense,
      voice: parsed.voice,
      stem: parsed.stem,
      view: parsed.view,
      detailPage: canonicalWordTypesDetailPage(parsed.view, parsed.detailPage),
    });

    expect(rebuilt).toMatchObject({
      type: 'verb',
      childCode: 'present',
      tableView: 'stems',
      tense: 'present',
      voice: 'passive',
      stem: '456',
      view: 'ayahs',
      detailPage: '2',
    });
  });

  it('replays root, stem, then root ParamMaps as browser back and forward state', () => {
    const restored = [
      params('tableView=roots&root=123'),
      params('tableView=stems&stem=456'),
      params('tableView=roots&root=123'),
    ].map(parseWordTypesQueryParams);

    expect(restored.map(({ tableView, root, stem, lemma, view, detailPage }) => ({
      tableView,
      root,
      stem,
      lemma,
      view,
      detailPage,
    }))).toEqual([
      { tableView: 'roots', root: 123, stem: null, lemma: null, view: 'words', detailPage: 1 },
      { tableView: 'stems', root: null, stem: 456, lemma: null, view: 'words', detailPage: 1 },
      { tableView: 'roots', root: 123, stem: null, lemma: null, view: 'words', detailPage: 1 },
    ]);
  });
});

describe('canonicalWordTypesDetailPage', () => {
  it.each([
    ['words', 1, null],
    ['ayahs', 1, null],
    ['words', 2, '2'],
    ['ayahs', 3, '3'],
    ['surahs', 1, null],
    ['surahs', 5, null],
  ] as const)('serializes %s page %d as %s', (view, page, expected) => {
    expect(canonicalWordTypesDetailPage(view, page)).toBe(expected);
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
