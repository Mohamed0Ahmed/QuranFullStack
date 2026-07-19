import { HttpParams } from '@angular/common/http';

// Fail-closed: every URL key is optional and additive, and a malformed value is treated as absent, so
// a pre-feature URL (without any association key) parses byte-identically to today.

export interface AssociationOption {
  readonly id: string | number;
  readonly label: string;
  readonly sublabel?: string;
}

// success with an empty options array is a genuine zero-match; error is a backend failure
// (isSuccess === false) or a transport error. The two must stay distinguishable so the picker shows an
// error hint instead of silently looking empty (M32/M43/M74).
export type AssociationOptionsResult =
  | { readonly status: 'success'; readonly options: readonly AssociationOption[] }
  | { readonly status: 'error' };

export function parsePositiveIntParam(value: string | null): number | null {
  if (value === null || !/^[1-9]\d*$/.test(value)) {
    return null;
  }
  return Number.parseInt(value, 10);
}

// Case is preserved: the backend compares against the catalogue code verbatim. The regex only checks
// shape (a short alphanumeric code) — the picker emits real codes from the word-types tree, and an
// unknown-but-well-formed code is rejected server-side (400).
export function parsePosCodeParam(value: string | null): string | null {
  if (value === null) {
    return null;
  }
  const trimmed = value.trim();
  return /^[A-Za-z][A-Za-z0-9]{0,11}$/.test(trimmed) ? trimmed : null;
}

// Empty ⇒ '' so an unfiltered read keeps its pre-feature cache key byte-identical; entries are given
// in a fixed order by the caller.
export function serializeAssociationKey(
  entries: readonly (readonly [string, string | number | null])[],
): string {
  return entries
    .filter(([, value]) => value !== null && value !== '')
    .map(([key, value]) => `${key}=${value}`)
    .join(',');
}

// Appends only when set — absent ⇒ pre-feature request byte-identical.
export function appendAssociationParam(
  params: HttpParams,
  key: string,
  value: string | number | null,
): HttpParams {
  return value === null || value === '' ? params : params.set(key, value);
}
