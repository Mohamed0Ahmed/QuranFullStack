import { ParamMap } from '@angular/router';

import {
  ABWAB_QUERY_DEFAULTS,
  ABWAB_QUERY_KEYS,
  AbwabQueryState,
  AbwabView,
  isAbwabView,
  isPositiveId,
} from '../models/abwab.models';

function parsePositiveId(raw: string | null): number | null {
  if (raw === null) {
    return null;
  }
  const value = Number(raw);
  return isPositiveId(value) ? value : null;
}

/** Parses the six locked query keys (plan-slice-b.md §4.4), fail-closed to the defaults. */
export function parseAbwabQueryParams(queryParams: ParamMap): AbwabQueryState {
  const viewRaw = queryParams.get(ABWAB_QUERY_KEYS.view);

  return {
    section: parsePositiveId(queryParams.get(ABWAB_QUERY_KEYS.section)),
    view: viewRaw !== null && isAbwabView(viewRaw) ? viewRaw : ABWAB_QUERY_DEFAULTS.view,
    archive: queryParams.get(ABWAB_QUERY_KEYS.archive) === '1',
    door: parsePositiveId(queryParams.get(ABWAB_QUERY_KEYS.door)),
    card: parsePositiveId(queryParams.get(ABWAB_QUERY_KEYS.card)),
    q: queryParams.get(ABWAB_QUERY_KEYS.q) ?? ABWAB_QUERY_DEFAULTS.q,
  };
}

export type AbwabQueryChange = Partial<{
  section: number | null;
  view: AbwabView;
  archive: boolean;
  door: number | null;
  card: number | null;
  q: string;
}>;

/**
 * Builds a `Router.navigate` query-param patch. Switching `section`, or turning
 * `archive` on, invalidates `door`/`card` (plan-slice-b.md §4.4) — a selection is not
 * meaningful across scopes. Turning `archive` off restores neither. An explicit
 * `door`/`card` in the same change overrides the invalidation clear.
 */
export function buildAbwabQueryParams(changes: AbwabQueryChange): Record<string, string | null> {
  const params: Record<string, string | null> = {};

  if (changes.section !== undefined) {
    params[ABWAB_QUERY_KEYS.section] = changes.section === null ? null : String(changes.section);
  }
  if (changes.view !== undefined) {
    params[ABWAB_QUERY_KEYS.view] = changes.view;
  }
  if (changes.archive !== undefined) {
    params[ABWAB_QUERY_KEYS.archive] = changes.archive ? '1' : null;
  }
  if (changes.q !== undefined) {
    params[ABWAB_QUERY_KEYS.q] = changes.q === '' ? null : changes.q;
  }

  const invalidatesSelection = changes.section !== undefined || changes.archive === true;
  if (invalidatesSelection) {
    params[ABWAB_QUERY_KEYS.door] = null;
    params[ABWAB_QUERY_KEYS.card] = null;
  }

  if (changes.door !== undefined) {
    params[ABWAB_QUERY_KEYS.door] = changes.door === null ? null : String(changes.door);
  }
  if (changes.card !== undefined) {
    params[ABWAB_QUERY_KEYS.card] = changes.card === null ? null : String(changes.card);
  }

  return params;
}
