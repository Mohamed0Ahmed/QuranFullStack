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
    const isRetainedRelation = closed && kind === 'relations';
    const isTargetedInclusions = kind === 'inclusions';
    if ((!isRetainedRelation && !isTargetedInclusions) || subjectDoorId === null) {
      return null;
    }
    return { kind: isRetainedRelation ? 'relations' : 'inclusions', closed, subjectDoorId };
  }

  if (!isAbwabModalKind(body)) {
    return null;
  }
  if (body === 'inclusions') {
    return null;
  }
  if (door === null && isDoorDependentAbwabModalKind(body)) {
    return null;
  }
  return { kind: body, closed, subjectDoorId: null };
}

export function serializeAbwabModal(modal: AbwabModalState): string {
  const subject = modal.subjectDoorId === null ? '' : `-${modal.subjectDoorId}`;
  const closed = modal.closed ? MODAL_CLOSED_SUFFIX : '';
  return `${modal.kind}${subject}${closed}`;
}

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
