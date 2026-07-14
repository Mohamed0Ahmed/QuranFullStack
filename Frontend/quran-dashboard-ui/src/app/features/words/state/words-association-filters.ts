import { HttpParams } from '@angular/common/http';

/**
 * Shared parsing/serialization for the Words association filters (Feature 026, US7): Unique Words by
 * primary word type (POS code) and primary root; Lemmas by owned root; Stems by primary root/lemma.
 * Every URL key is optional, additive, and parsed FAIL-CLOSED (malformed ⇒ treated as absent) so a
 * pre-feature URL (without any of these keys) parses byte-identically to today.
 */

/** One selectable association-picker option. Id is a POS code (string) or a dimension id (number). */
export interface AssociationOption {
  readonly id: string | number;
  readonly label: string;
  readonly sublabel?: string;
}

/** Fail-closed positive-integer id parse (roots/lemmas). Non-numeric / zero / negative ⇒ null. */
export function parsePositiveIntParam(value: string | null): number | null {
  if (value === null || !/^[1-9]\d*$/.test(value)) {
    return null;
  }
  return Number.parseInt(value, 10);
}

/**
 * Fail-closed POS-code parse (Unique Words primaryType). Keeps a plausibly well-formed catalogue token
 * (a short alphanumeric code such as N / V / PN / INL / ADJ); anything else ⇒ null. Case is preserved
 * because the backend compares against the catalogue code verbatim; the picker only ever emits real
 * codes from the word-types tree, and an unknown-but-well-formed code is rejected server-side (400).
 */
export function parsePosCodeParam(value: string | null): string | null {
  if (value === null) {
    return null;
  }
  const trimmed = value.trim();
  return /^[A-Za-z][A-Za-z0-9]{0,11}$/.test(trimmed) ? trimmed : null;
}

/**
 * Deterministic cache-key fragment for the active association values. Empty ⇒ '' so an unfiltered read
 * keeps its pre-feature cache key byte-identical. Entries are given in a fixed order by the caller.
 */
export function serializeAssociationKey(
  entries: readonly (readonly [string, string | number | null])[],
): string {
  return entries
    .filter(([, value]) => value !== null && value !== '')
    .map(([key, value]) => `${key}=${value}`)
    .join(',');
}

/** Appends a single association param only when set (absent ⇒ pre-feature request byte-identical). */
export function appendAssociationParam(
  params: HttpParams,
  key: string,
  value: string | number | null,
): HttpParams {
  return value === null || value === '' ? params : params.set(key, value);
}
