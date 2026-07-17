import { afterEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { WordsExplainerPreference } from './words-explainer-preference';

const STORAGE_KEY = 'qd-words-explainer';

/** A fresh service instance so each test exercises the constructor-time (synchronous) restore. */
function freshService(): WordsExplainerPreference {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({});
  return TestBed.inject(WordsExplainerPreference);
}

describe('WordsExplainerPreference', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('defaults every hero to expanded when nothing is stored', () => {
    localStorage.clear();
    const pref = freshService();

    expect(pref.isExpanded('unique')).toBe(true);
    expect(pref.isExpanded('word-types')).toBe(true);
  });

  it('persists a collapse and reflects it', () => {
    const pref = freshService();

    pref.setExpanded('roots', false);

    expect(pref.isExpanded('roots')).toBe(false);
    expect(localStorage.getItem(STORAGE_KEY)).toContain('roots');
  });

  it('restores a stored collapsed state on the FIRST read (synchronous, before any paint)', () => {
    localStorage.setItem(STORAGE_KEY, 'roots,stems');

    const pref = freshService();

    // No detectChanges / tick / effect: construction alone already knows the stored state.
    expect(pref.isExpanded('roots')).toBe(false);
    expect(pref.isExpanded('stems')).toBe(false);
    expect(pref.isExpanded('unique')).toBe(true);
  });

  it('isolates keys — collapsing one page never collapses another', () => {
    const pref = freshService();

    pref.setExpanded('roots', false);

    expect(pref.isExpanded('roots')).toBe(false);
    expect(pref.isExpanded('lemmas')).toBe(true);
    expect(pref.isExpanded('word-types')).toBe(true);
  });

  it('re-expanding removes the key from storage', () => {
    localStorage.setItem(STORAGE_KEY, 'roots');
    const pref = freshService();

    pref.setExpanded('roots', true);

    expect(pref.isExpanded('roots')).toBe(true);
    expect(localStorage.getItem(STORAGE_KEY) ?? '').not.toContain('roots');
  });

  // A single real boundary (localStorage) is the only sanctioned mock here (plan §8). Both failure
  // directions are data-driven, not copy-pasted specs.
  const failures: ReadonlyArray<{ readonly op: 'getItem' | 'setItem' }> = [
    { op: 'getItem' },
    { op: 'setItem' },
  ];
  for (const { op } of failures) {
    it(`falls back safely when localStorage.${op} throws (default stays expanded, no crash)`, () => {
      vi.spyOn(Storage.prototype, op).mockImplementation(() => {
        throw new Error('storage denied');
      });

      const pref = freshService();

      expect(pref.isExpanded('roots')).toBe(true);
      expect(() => pref.setExpanded('roots', false)).not.toThrow();
      // The in-memory set still drives the current session even though the write was swallowed.
      expect(pref.isExpanded('roots')).toBe(false);
    });
  }
});
