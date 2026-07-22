// Generic optimistic-concurrency primitives. They mirror the shape of a server-side revision/generation
// check (an expected token vs the current one) but carry NO domain knowledge — no entity, endpoint, or
// code string — so any feature can reuse them. The revision type is caller-chosen (a number, an ETag, a
// composite key).

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
    // Stable name marker so callers (e.g. AsyncAction) can recognize a conflict without importing this
    // class — keeps lower layers decoupled from this shared primitive.
    this.name = 'ConcurrencyConflictError';
  }
}

export function isConcurrencyConflict(error: unknown): error is ConcurrencyConflictError {
  return error instanceof ConcurrencyConflictError;
}

// Applies a change only when the caller's expected revision still matches the current one; otherwise it
// reports a conflict and never runs `apply`. This is the client-side counterpart to a fail-before-mutation
// concurrency guard.
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
