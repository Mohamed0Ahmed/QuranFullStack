import { describe, expect, it } from 'vitest';

import {
  ExplorerSortColumn,
  MUSHAF_ORDER_SORT,
  ariaSortOf,
  canonicalSortTokens,
  canonicalizeSortToken,
  explorerSortActionAria,
  explorerSortOptions,
  nextSortToken,
  sortDirectionOf,
  sortGlyphOf,
  sortQueryValue,
  sortTokenFor,
} from './explorer-sort';

// A count column (natural desc) and a text column (natural asc) — the two classes the grammar has.
const OCCURRENCES: ExplorerSortColumn = { key: 'occurrences', natural: 'desc', label: 'المواضع' };
const ALPHA: ExplorerSortColumn = { key: 'alpha', natural: 'asc', label: 'الجذر' };
const COLUMNS: readonly ExplorerSortColumn[] = [ALPHA, OCCURRENCES];

describe('canonicalizeSortToken', () => {
  it('keeps every canonical token verbatim', () => {
    for (const token of ['occurrences', 'occurrences-asc', 'alpha', 'alpha-desc', MUSHAF_ORDER_SORT]) {
      expect(canonicalizeSortToken(token, COLUMNS)).toBe(token);
    }
  });

  it('canonicalizes the legacy natural-direction aliases OUT to the bare token', () => {
    // The whole point: one ordering must have exactly one spelling in the URL and the cache key.
    expect(canonicalizeSortToken('occurrences-desc', COLUMNS)).toBe('occurrences');
    expect(canonicalizeSortToken('alpha-asc', COLUMNS)).toBe('alpha');
  });

  it('rejects an unlisted column, a bare suffix, and an empty/absent value', () => {
    expect(canonicalizeSortToken('relevance', COLUMNS)).toBeNull();
    expect(canonicalizeSortToken('relevance-asc', COLUMNS)).toBeNull();
    expect(canonicalizeSortToken('-asc', COLUMNS)).toBeNull();
    expect(canonicalizeSortToken('', COLUMNS)).toBeNull();
    expect(canonicalizeSortToken(null, COLUMNS)).toBeNull();
    expect(canonicalizeSortToken(undefined, COLUMNS)).toBeNull();
  });

  it('rejects a direction suffix on mushaf-order, which is ascending-only and bare-only', () => {
    // The backend 400s on these; the frontend must never emit them.
    expect(canonicalizeSortToken('mushaf-order-asc', COLUMNS)).toBeNull();
    expect(canonicalizeSortToken('mushaf-order-desc', COLUMNS)).toBeNull();
  });

  it('matches exactly — case and surrounding space are not folded (fail closed)', () => {
    expect(canonicalizeSortToken('ALPHA', COLUMNS)).toBeNull();
    expect(canonicalizeSortToken('occurrences ', COLUMNS)).toBeNull();
  });

  it('is idempotent, which is what the per-explorer canonical-token guards rely on', () => {
    for (const token of ['occurrences-desc', 'alpha-asc', 'occurrences-asc', MUSHAF_ORDER_SORT]) {
      const once = canonicalizeSortToken(token, COLUMNS);
      expect(canonicalizeSortToken(once, COLUMNS)).toBe(once);
    }
  });
});

describe('sortTokenFor / sortDirectionOf', () => {
  it('serializes the natural direction bare and the opposite one suffixed', () => {
    expect(sortTokenFor(OCCURRENCES, 'desc')).toBe('occurrences');
    expect(sortTokenFor(OCCURRENCES, 'asc')).toBe('occurrences-asc');
    expect(sortTokenFor(ALPHA, 'asc')).toBe('alpha');
    expect(sortTokenFor(ALPHA, 'desc')).toBe('alpha-desc');
  });

  it('reports the direction a token gives a column, or null when it orders another one', () => {
    expect(sortDirectionOf('occurrences', OCCURRENCES)).toBe('desc');
    expect(sortDirectionOf('occurrences-asc', OCCURRENCES)).toBe('asc');
    expect(sortDirectionOf('alpha', OCCURRENCES)).toBeNull();
    expect(sortDirectionOf(MUSHAF_ORDER_SORT, OCCURRENCES)).toBeNull();
  });
});

describe('nextSortToken (the 3-state header cycle)', () => {
  it('walks natural → opposite → release for a count column', () => {
    expect(nextSortToken(MUSHAF_ORDER_SORT, OCCURRENCES)).toBe('occurrences');
    expect(nextSortToken('occurrences', OCCURRENCES)).toBe('occurrences-asc');
    expect(nextSortToken('occurrences-asc', OCCURRENCES)).toBeNull();
  });

  it('walks natural → opposite → release for a text column', () => {
    expect(nextSortToken(MUSHAF_ORDER_SORT, ALPHA)).toBe('alpha');
    expect(nextSortToken('alpha', ALPHA)).toBe('alpha-desc');
    expect(nextSortToken('alpha-desc', ALPHA)).toBeNull();
  });

  it('starts a fresh cycle at the natural direction when another column is active', () => {
    expect(nextSortToken('alpha-desc', OCCURRENCES)).toBe('occurrences');
  });
});

describe('ariaSortOf / sortGlyphOf', () => {
  it('maps the active direction to the aria-sort value and the glyph', () => {
    expect(ariaSortOf('occurrences', OCCURRENCES)).toBe('descending');
    expect(ariaSortOf('occurrences-asc', OCCURRENCES)).toBe('ascending');
    expect(sortGlyphOf('occurrences', OCCURRENCES)).toBe('▼');
    expect(sortGlyphOf('occurrences-asc', OCCURRENCES)).toBe('▲');
  });

  it('reports nothing for an inactive column, so neither attribute nor glyph renders', () => {
    expect(ariaSortOf('alpha', OCCURRENCES)).toBeNull();
    expect(sortGlyphOf('alpha', OCCURRENCES)).toBeNull();
  });
});

describe('explorerSortActionAria', () => {
  it('names the column and the state the NEXT click moves to, across the whole cycle', () => {
    expect(explorerSortActionAria(MUSHAF_ORDER_SORT, OCCURRENCES)).toBe('ترتيب حسب المواضع تنازليًا');
    expect(explorerSortActionAria('occurrences', OCCURRENCES)).toBe('ترتيب حسب المواضع تصاعديًا');
    expect(explorerSortActionAria('occurrences-asc', OCCURRENCES)).toBe('إلغاء الترتيب حسب المواضع');
  });
});

describe('canonicalSortTokens / explorerSortOptions', () => {
  it('lists both canonical tokens per column, natural direction first', () => {
    expect(canonicalSortTokens(COLUMNS)).toEqual([
      'alpha',
      'alpha-desc',
      'occurrences',
      'occurrences-asc',
    ]);
  });

  it('offers mushaf-order plus every column in both directions, with Arabic direction labels', () => {
    expect(explorerSortOptions(COLUMNS, 'ترتيب المصحف')).toEqual([
      { value: MUSHAF_ORDER_SORT, label: 'ترتيب المصحف' },
      { value: 'alpha', label: 'الجذر: تصاعدي' },
      { value: 'alpha-desc', label: 'الجذر: تنازلي' },
      { value: 'occurrences', label: 'المواضع: تنازلي' },
      { value: 'occurrences-asc', label: 'المواضع: تصاعدي' },
    ]);
  });
});

describe('sortQueryValue', () => {
  it('drops the param for the default ordering and keeps every other token', () => {
    expect(sortQueryValue('mushaf-order', 'mushaf-order')).toBeNull();
    expect(sortQueryValue('occurrences', 'mushaf-order')).toBe('occurrences');
    // Word Types: its default is occurrences, so THAT is the one that goes param-free.
    expect(sortQueryValue('occurrences', 'occurrences')).toBeNull();
    expect(sortQueryValue('mushaf-order', 'occurrences')).toBe('mushaf-order');
  });
});
