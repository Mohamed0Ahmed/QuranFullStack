import { describe, expect, it, vi } from 'vitest';

import {
  ConcurrencyConflictError,
  isConcurrencyConflict,
  reconcile,
} from './optimistic-concurrency';

describe('ConcurrencyConflictError', () => {
  it('carries the expected and actual revisions and a stable name marker', () => {
    const error = new ConcurrencyConflictError(3, 5);

    expect(error.expected).toBe(3);
    expect(error.actual).toBe(5);
    expect(error.name).toBe('ConcurrencyConflictError');
    expect(error).toBeInstanceOf(Error);
  });

  it('isConcurrencyConflict narrows only true conflicts', () => {
    expect(isConcurrencyConflict(new ConcurrencyConflictError(1, 2))).toBe(true);
    expect(isConcurrencyConflict(new Error('other'))).toBe(false);
    expect(isConcurrencyConflict(null)).toBe(false);
  });
});

describe('reconcile', () => {
  it('applies the change when the expected revision matches the current one', () => {
    const apply = vi.fn(() => ({ value: 'next', revision: 2 }));

    const outcome = reconcile(1, { revision: 1 }, apply);

    expect(outcome).toEqual({ kind: 'applied', value: 'next', revision: 2 });
    expect(apply).toHaveBeenCalledTimes(1);
  });

  it('reports a conflict without applying when revisions differ', () => {
    const apply = vi.fn(() => ({ value: 'next', revision: 2 }));

    const outcome = reconcile(1, { revision: 4 }, apply);

    expect(outcome).toEqual({ kind: 'conflict', expected: 1, actual: 4 });
    expect(apply).not.toHaveBeenCalled();
  });

  it('honors a custom equality comparator', () => {
    const apply = vi.fn(() => ({ value: 'ok', revision: 'v2' }));
    const caseInsensitive = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();

    const outcome = reconcile('ETAG', { revision: 'etag' }, apply, caseInsensitive);

    expect(outcome.kind).toBe('applied');
    expect(apply).toHaveBeenCalledTimes(1);
  });
});
