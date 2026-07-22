import { afterEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { WordsExplainerPreference } from './words-explainer-preference';

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

  it('persists a collapse and reflects it after the service is recreated', () => {
    const pref = freshService();

    pref.setExpanded('roots', false);
    expect(pref.isExpanded('roots')).toBe(false);

    const reloaded = freshService();
    expect(reloaded.isExpanded('roots')).toBe(false);
  });

  it('restores a persisted collapsed state on the FIRST read (synchronous, before any paint)', () => {
    const seed = freshService();
    seed.setExpanded('roots', false);
    seed.setExpanded('stems', false);

    const pref = freshService();

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

  it('re-expanding clears the persisted collapse (a fresh instance defaults back to expanded)', () => {
    const seed = freshService();
    seed.setExpanded('roots', false);

    const pref = freshService();
    expect(pref.isExpanded('roots')).toBe(false);

    pref.setExpanded('roots', true);

    const reloaded = freshService();
    expect(reloaded.isExpanded('roots')).toBe(true);
  });

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
      expect(pref.isExpanded('roots')).toBe(false);
    });
  }
});
