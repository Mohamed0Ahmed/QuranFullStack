import { HttpParams } from '@angular/common/http';

export interface AssociationOption {
  readonly id: string | number;
  readonly label: string;
  readonly sublabel?: string;
}

export type AssociationOptionsResult =
  | { readonly status: 'success'; readonly options: readonly AssociationOption[] }
  | { readonly status: 'error' };

export function parsePositiveIntParam(value: string | null): number | null {
  if (value === null || !/^[1-9]\d*$/.test(value)) {
    return null;
  }
  return Number.parseInt(value, 10);
}

// Case preserved — the backend compares the code verbatim; the regex checks shape only.
export function parsePosCodeParam(value: string | null): string | null {
  if (value === null) {
    return null;
  }
  const trimmed = value.trim();
  return /^[A-Za-z][A-Za-z0-9]{0,11}$/.test(trimmed) ? trimmed : null;
}

export function serializeAssociationKey(
  entries: readonly (readonly [string, string | number | null])[],
): string {
  return entries
    .filter(([, value]) => value !== null && value !== '')
    .map(([key, value]) => `${key}=${value}`)
    .join(',');
}

export function appendAssociationParam(
  params: HttpParams,
  key: string,
  value: string | number | null,
): HttpParams {
  return value === null || value === '' ? params : params.set(key, value);
}
