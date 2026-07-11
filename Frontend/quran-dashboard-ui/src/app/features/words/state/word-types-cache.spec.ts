import { describe, expect, it } from 'vitest';

import { WordTypesCacheFilter, WordTypesCacheKeys } from './word-types-cache';

const filter: WordTypesCacheFilter = { type: 'noun', childCode: 'PN', case: 'all', tense: 'all', voice: 'all' };

describe('WordTypesCacheKeys.table', () => {
  it('differs by tableView for the same filter/sort/page', () => {
    const words = WordTypesCacheKeys.table(filter, 'words', 'occurrences', 1);
    const roots = WordTypesCacheKeys.table(filter, 'roots', 'occurrences', 1);
    const stems = WordTypesCacheKeys.table(filter, 'stems', 'occurrences', 1);
    const lemmas = WordTypesCacheKeys.table(filter, 'lemmas', 'occurrences', 1);

    const keys = new Set([words, roots, stems, lemmas]);
    expect(keys.size).toBe(4);
  });

  it('stays stable for the same filter/tableView/sort/page', () => {
    const a = WordTypesCacheKeys.table(filter, 'roots', 'occurrences', 1);
    const b = WordTypesCacheKeys.table(filter, 'roots', 'occurrences', 1);

    expect(a).toBe(b);
  });

  it('is distinct from the plain rows(...) key for the same filter/sort/page', () => {
    const rowsKey = WordTypesCacheKeys.rows(filter, 'occurrences', 1);
    const tableKey = WordTypesCacheKeys.table(filter, 'words', 'occurrences', 1);

    expect(tableKey).not.toBe(rowsKey);
  });
});
