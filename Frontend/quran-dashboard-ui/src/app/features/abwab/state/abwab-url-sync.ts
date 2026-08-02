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
 * The seventh key's grammar (plan-slice-e.md §4.2-2/§4.2-3, widened by ux-slice-l):
 *
 *     modal = <kind>                    open; subject is door=
 *     modal = <kind>-closed             retained; subject is door=, so it follows a later selection
 *     modal = relations-<id>-closed     retained; subject is door <id>, regardless of door=
 *
 * The plain forms hold no id of their own, so the door-dependent kinds fail closed when the
 * **same** ParamMap carries no valid `door` — a `modal=edit` without a subject would restore
 * nothing. The id-carrying form is the one exception, and only for a retained `relations`:
 * a reveal points `door=` at the target while the retained overlay still belongs to the
 * source, so the key has to say which. Everything else about it fails closed:
 *
 * - the id must be a positive integer, else the whole key is inert;
 * - an id on the **open** form is invalid — an open modal's subject is always `door=`, and a
 *   diverged subject there is exactly what `canOpen` exists to forbid;
 * - an id on any other kind is invalid — only the relations modal has a reveal.
 */
function parseModal(raw: string | null, door: number | null): AbwabModalState | null {
  if (raw === null) {
    return null;
  }
  const closed = raw.endsWith(MODAL_CLOSED_SUFFIX);
  const body = closed ? raw.slice(0, -MODAL_CLOSED_SUFFIX.length) : raw;

  const idSeparator = body.lastIndexOf('-');
  if (idSeparator > 0) {
    const kind = body.slice(0, idSeparator);
    const subjectDoorId = parsePositiveId(body.slice(idSeparator + 1));
    if (!closed || kind !== 'relations' || subjectDoorId === null) {
      return null;
    }
    // The literal, not `kind`: TS does not narrow a `string` to a literal through the guard
    // above, and widening the field to `string` would let a typo reach the controller.
    return { kind: 'relations', closed, subjectDoorId };
  }

  if (!isAbwabModalKind(body)) {
    return null;
  }
  if (door === null && isDoorDependentAbwabModalKind(body)) {
    return null;
  }
  return { kind: body, closed, subjectDoorId: null };
}

export function serializeAbwabModal(modal: AbwabModalState): string {
  if (!modal.closed) {
    return modal.kind;
  }
  const subject = modal.subjectDoorId === null ? '' : `-${modal.subjectDoorId}`;
  return `${modal.kind}${subject}${MODAL_CLOSED_SUFFIX}`;
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
