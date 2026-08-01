import { describe, expect, it } from 'vitest';

import {
  ABWAB_QUERY_DEFAULTS,
  ABWAB_QUERY_KEYS,
  isAbwabModalKind,
  isAbwabView,
  isDoorDependentAbwabModalKind,
  isPositiveId,
} from './abwab.models';

describe('isAbwabView', () => {
  it('accepts only the two known view modes', () => {
    expect(isAbwabView('tree')).toBe(true);
    expect(isAbwabView('cards')).toBe(true);
  });

  it('rejects anything else, including a plausible near-miss', () => {
    expect(isAbwabView('list')).toBe(false);
    expect(isAbwabView('')).toBe(false);
    expect(isAbwabView(null)).toBe(false);
    expect(isAbwabView(undefined)).toBe(false);
  });
});

describe('isPositiveId', () => {
  it('accepts positive integers only', () => {
    expect(isPositiveId(1)).toBe(true);
    expect(isPositiveId(42)).toBe(true);
  });

  it('rejects zero, negatives, fractions, NaN and non-numbers', () => {
    expect(isPositiveId(0)).toBe(false);
    expect(isPositiveId(-3)).toBe(false);
    expect(isPositiveId(1.5)).toBe(false);
    expect(isPositiveId(Number.NaN)).toBe(false);
    expect(isPositiveId('4')).toBe(false);
    expect(isPositiveId(null)).toBe(false);
    expect(isPositiveId(undefined)).toBe(false);
  });
});

describe('ABWAB_QUERY_KEYS / ABWAB_QUERY_DEFAULTS', () => {
  it('exposes the seven locked query keys as stable strings', () => {
    expect(ABWAB_QUERY_KEYS).toEqual({
      section: 'section',
      view: 'view',
      archive: 'archive',
      door: 'door',
      card: 'card',
      q: 'q',
      modal: 'modal',
    });
  });

  it('fails closed to the documented defaults (plan-slice-b.md §4.4)', () => {
    expect(ABWAB_QUERY_DEFAULTS).toEqual({
      section: null,
      view: 'tree',
      archive: false,
      door: null,
      card: null,
      q: '',
      modal: null,
    });
  });
});

describe('isAbwabModalKind / isDoorDependentAbwabModalKind', () => {
  it('accepts exactly the six restorable kinds', () => {
    for (const kind of ['create', 'child', 'edit', 'move', 'sections', 'relations']) {
      expect(isAbwabModalKind(kind)).toBe(true);
    }
    expect(isAbwabModalKind('banana')).toBe(false);
    expect(isAbwabModalKind('Edit')).toBe(false);
    expect(isAbwabModalKind('')).toBe(false);
    expect(isAbwabModalKind(null)).toBe(false);
  });

  it('marks only the kinds whose subject is door= as door-dependent', () => {
    expect(isDoorDependentAbwabModalKind('child')).toBe(true);
    expect(isDoorDependentAbwabModalKind('edit')).toBe(true);
    expect(isDoorDependentAbwabModalKind('move')).toBe(true);
    expect(isDoorDependentAbwabModalKind('relations')).toBe(true);
    expect(isDoorDependentAbwabModalKind('create')).toBe(false);
    expect(isDoorDependentAbwabModalKind('sections')).toBe(false);
  });
});
