import { ParamMap } from '@angular/router';

import {
  ABWAB_QUERY_DEFAULTS,
  ABWAB_QUERY_KEYS,
  AbwabModalState,
  AbwabQueryState,
  AbwabView,
  isAbwabModalKind,
  isAbwabView,
  isDoorDependentAbwabModalKind,
  isPositiveId,
} from '../models/abwab.models';

const MODAL_CLOSED_SUFFIX = '-closed';

function parsePositiveId(raw: string | null): number | null {
  if (raw === null) {
    return null;
  }
  const value = Number(raw);
  return isPositiveId(value) ? value : null;
}

/**
 * The seventh key's grammar (plan-slice-e.md §4.2-2/§4.2-3): a kind from the closed set,
 * optionally suffixed `-closed` for the retained-but-restorable state. Door-dependent
 * kinds fail closed when the **same** ParamMap carries no valid `door`, because the key
 * holds no id of its own — a `modal=edit` without a subject would restore nothing.
 */
function parseModal(raw: string | null, door: number | null): AbwabModalState | null {
  if (raw === null) {
    return null;
  }
  const closed = raw.endsWith(MODAL_CLOSED_SUFFIX);
  const kind = closed ? raw.slice(0, -MODAL_CLOSED_SUFFIX.length) : raw;
  if (!isAbwabModalKind(kind)) {
    return null;
  }
  if (door === null && isDoorDependentAbwabModalKind(kind)) {
    return null;
  }
  return { kind, closed };
}

export function serializeAbwabModal(modal: AbwabModalState): string {
  return modal.closed ? `${modal.kind}${MODAL_CLOSED_SUFFIX}` : modal.kind;
}

/** Parses the seven locked query keys (plan-slice-b.md §4.4), fail-closed to the defaults. */
export function parseAbwabQueryParams(queryParams: ParamMap): AbwabQueryState {
  const viewRaw = queryParams.get(ABWAB_QUERY_KEYS.view);
  const door = parsePositiveId(queryParams.get(ABWAB_QUERY_KEYS.door));

  return {
    section: parsePositiveId(queryParams.get(ABWAB_QUERY_KEYS.section)),
    view: viewRaw !== null && isAbwabView(viewRaw) ? viewRaw : ABWAB_QUERY_DEFAULTS.view,
    archive: queryParams.get(ABWAB_QUERY_KEYS.archive) === '1',
    door,
    card: parsePositiveId(queryParams.get(ABWAB_QUERY_KEYS.card)),
    q: queryParams.get(ABWAB_QUERY_KEYS.q) ?? ABWAB_QUERY_DEFAULTS.q,
    modal: parseModal(queryParams.get(ABWAB_QUERY_KEYS.modal), door),
  };
}

export type AbwabQueryChange = Partial<{
  section: number | null;
  view: AbwabView;
  archive: boolean;
  door: number | null;
  card: number | null;
  q: string;
  modal: AbwabModalState | null;
}>;

/**
 * Builds a `Router.navigate` query-param patch. Switching `section`, or turning
 * `archive` on, invalidates `door`/`card`/`modal` (plan-slice-b.md §4.4,
 * plan-slice-e.md §4.2-8) — neither a selection nor an overlay over it is meaningful
 * across scopes, and the rule stays uniform for the door-independent kinds too because a
 * scope switch is a context change. Turning `archive` off restores none of them. An
 * explicit `door`/`card`/`modal` in the same change overrides the invalidation clear.
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
    params[ABWAB_QUERY_KEYS.modal] = null;
  }

  if (changes.door !== undefined) {
    params[ABWAB_QUERY_KEYS.door] = changes.door === null ? null : String(changes.door);
  }
  if (changes.card !== undefined) {
    params[ABWAB_QUERY_KEYS.card] = changes.card === null ? null : String(changes.card);
  }
  if (changes.modal !== undefined) {
    params[ABWAB_QUERY_KEYS.modal] = changes.modal === null ? null : serializeAbwabModal(changes.modal);
  }

  return params;
}
