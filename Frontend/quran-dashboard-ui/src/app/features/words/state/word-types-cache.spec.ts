import { describe, expect, it } from 'vitest';

import { WordTypeGroupedRequestParams } from '../models/word-types-detail.models';
import { WordTypesCacheFilter, WordTypesCacheKeys } from './word-types-cache';

const filter: WordTypesCacheFilter = { type: 'noun', childCode: 'PN', case: 'all', tense: 'all', voice: 'all', search: null, hasRoot: null, hasStem: null, hasLemma: null };
const groupedRequest: WordTypeGroupedRequestParams = {
  kind: 'root',
  dimensionId: 4210,
  type: 'noun',
  childCode: 'PN',
  case: 'nominative',
  tense: 'all',
  voice: 'all',
};

const groupedIdentityScopeVariants: readonly WordTypeGroupedRequestParams[] = [
  groupedRequest,
  { ...groupedRequest, kind: 'stem' },
  { ...groupedRequest, kind: 'lemma' },
  { ...groupedRequest, dimensionId: 4211 },
  { ...groupedRequest, type: 'verb' },
  { ...groupedRequest, childCode: 'N' },
  { ...groupedRequest, childCode: null },
  { ...groupedRequest, case: 'accusative' },
  { ...groupedRequest, tense: 'past' },
  { ...groupedRequest, voice: 'active' },
];

describe('WordTypesCacheKeys.table', () => {
  it('differs by tableView for the same filter/sort/page', () => {
    const words = WordTypesCacheKeys.table(filter, 'words', 'occurrences', 1);
    const roots = WordTypesCacheKeys.table(filter, 'roots', 'occurrences', 1);
    const stems = WordTypesCacheKeys.table(filter, 'stems', 'occurrences', 1);
    const lemmas = WordTypesCacheKeys.table(filter, 'lemmas', 'occurrences', 1);

    const keys = new Set([words, roots, stems, lemmas]);
    expect(keys.size).toBe(4);
  });

  it('is distinct from the plain rows(...) key for the same filter/sort/page', () => {
    const rowsKey = WordTypesCacheKeys.rows(filter, 'occurrences', 1);
    const tableKey = WordTypesCacheKeys.table(filter, 'words', 'occurrences', 1);

    expect(tableKey).not.toBe(rowsKey);
  });
});

describe('WordTypesCacheKeys search isolation', () => {
  const searched: WordTypesCacheFilter = { ...filter, search: 'كلمة' };

  it('keeps the pre-feature key when search is absent', () => {
    // A null search must not alter the key (warm entries stay valid).
    expect(WordTypesCacheKeys.rows(filter, 'occurrences', 1))
      .toBe(WordTypesCacheKeys.rows({ ...filter, search: null }, 'occurrences', 1));
    expect(WordTypesCacheKeys.table(filter, 'roots', 'occurrences', 1))
      .toBe(WordTypesCacheKeys.table({ ...filter, search: null }, 'roots', 'occurrences', 1));
  });

  it('isolates a searched read from an unsearched one on both keys', () => {
    expect(WordTypesCacheKeys.rows(searched, 'occurrences', 1))
      .not.toBe(WordTypesCacheKeys.rows(filter, 'occurrences', 1));
    expect(WordTypesCacheKeys.table(searched, 'roots', 'occurrences', 1))
      .not.toBe(WordTypesCacheKeys.table(filter, 'roots', 'occurrences', 1));
  });

  it('separates two distinct searches', () => {
    expect(WordTypesCacheKeys.rows(searched, 'occurrences', 1))
      .not.toBe(WordTypesCacheKeys.rows({ ...filter, search: 'جذر' }, 'occurrences', 1));
  });
});

describe('WordTypesCacheKeys presence-flag isolation (Feature 026)', () => {
  it('keeps the pre-feature key when every flag is absent', () => {
    expect(WordTypesCacheKeys.rows({ ...filter, hasRoot: null, hasStem: null, hasLemma: null }, 'occurrences', 1))
      .toBe(WordTypesCacheKeys.rows(filter, 'occurrences', 1));
  });

  it('isolates flagged reads and distinguishes every flag value', () => {
    const base = WordTypesCacheKeys.rows(filter, 'occurrences', 1);
    const keys = [
      base,
      WordTypesCacheKeys.rows({ ...filter, hasRoot: true }, 'occurrences', 1),
      WordTypesCacheKeys.rows({ ...filter, hasRoot: false }, 'occurrences', 1),
      WordTypesCacheKeys.rows({ ...filter, hasStem: true }, 'occurrences', 1),
    ];
    expect(new Set(keys).size).toBe(keys.length);

    expect(WordTypesCacheKeys.table({ ...filter, hasLemma: false }, 'roots', 'occurrences', 1))
      .not.toBe(WordTypesCacheKeys.table(filter, 'roots', 'occurrences', 1));
  });
});

describe('WordTypesCacheKeys grouped detail', () => {
  it('keeps summary, words, ayahs, and surahs pairwise unique for one identity and scope', () => {
    const keys = [
      WordTypesCacheKeys.groupedSummary(groupedRequest),
      WordTypesCacheKeys.groupedWords(groupedRequest, 1),
      WordTypesCacheKeys.groupedAyahs(groupedRequest, 1),
      WordTypesCacheKeys.groupedSurahs(groupedRequest),
    ];

    expect(new Set(keys).size).toBe(keys.length);
  });

  describe.each([
    ['summary', (request: WordTypeGroupedRequestParams) => WordTypesCacheKeys.groupedSummary(request)],
    ['surahs', (request: WordTypeGroupedRequestParams) => WordTypesCacheKeys.groupedSurahs(request)],
    ['words', (request: WordTypeGroupedRequestParams) => WordTypesCacheKeys.groupedWords(request, 1)],
    ['ayahs', (request: WordTypeGroupedRequestParams) => WordTypesCacheKeys.groupedAyahs(request, 1)],
  ] as const)('grouped %s key isolation', (_view, keyForRequest) => {
    it('keeps all identity and scope variants pairwise unique', () => {
      const keys = groupedIdentityScopeVariants.map((request) => keyForRequest(request));

      expect(new Set(keys).size).toBe(keys.length);
    });
  });

  it.each([
    ['words', WordTypesCacheKeys.groupedWords],
    ['ayahs', WordTypesCacheKeys.groupedAyahs],
  ] as const)('isolates grouped %s pages', (_view, keyForPage) => {
    expect(keyForPage(groupedRequest, 1)).not.toBe(keyForPage(groupedRequest, 2));
  });

});
