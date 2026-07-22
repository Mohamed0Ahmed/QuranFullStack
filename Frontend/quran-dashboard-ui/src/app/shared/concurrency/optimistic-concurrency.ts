export type Revision = string | number;

export interface Revised<TRevision = Revision> {
  readonly revision: TRevision;
}

export type Reconciliation<T, TRevision = Revision> =
  | { readonly kind: 'applied'; readonly value: T; readonly revision: TRevision }
  | { readonly kind: 'conflict'; readonly expected: TRevision; readonly actual: TRevision };

export class ConcurrencyConflictError<TRevision = Revision> extends Error {
  constructor(
    readonly expected: TRevision,
    readonly actual: TRevision,
  ) {
    super(`Concurrency conflict: expected revision ${String(expected)} but found ${String(actual)}.`);
    // Stable name marker so callers can recognize a conflict without importing this class.
    this.name = 'ConcurrencyConflictError';
  }
}

export function isConcurrencyConflict(error: unknown): error is ConcurrencyConflictError {
  return error instanceof ConcurrencyConflictError;
}

export function reconcile<T, TRevision = Revision>(
  expected: TRevision,
  current: Revised<TRevision>,
  apply: () => { value: T; revision: TRevision },
  equals: (a: TRevision, b: TRevision) => boolean = Object.is,
): Reconciliation<T, TRevision> {
  if (!equals(expected, current.revision)) {
    return { kind: 'conflict', expected, actual: current.revision };
  }
  const applied = apply();
  return { kind: 'applied', value: applied.value, revision: applied.revision };
}
