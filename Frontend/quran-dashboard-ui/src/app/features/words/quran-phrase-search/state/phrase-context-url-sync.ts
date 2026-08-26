import { ParamMap, Params } from '@angular/router';

import {
  DEFAULT_PHRASE_CONTEXT_URL_STATE,
  ParsedPhraseContextUrlState,
  PhraseContextUrlState,
} from '../models/phrase-context.models';
import { isPhraseTextMode } from '../models/phrase-repetitions.models';

const BUILD_ID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const OPAQUE_REF_PATTERN = /^[A-Za-z0-9_-]+$/;

export function parsePhraseContextUrlState(params: ParamMap): ParsedPhraseContextUrlState {
  const build = parseBuild(params.get('build'));
  const modeValue = params.get('mode');
  const mode = isPhraseTextMode(modeValue)
    ? { value: modeValue, invalid: false }
    : modeValue
      ? { value: DEFAULT_PHRASE_CONTEXT_URL_STATE.mode, invalid: true }
      : { value: DEFAULT_PHRASE_CONTEXT_URL_STATE.mode, invalid: false };
  const resolution = parseReference(params.get('resolution'));
  const before = parseReference(params.get('before'));
  const after = parseReference(params.get('after'));
  const contextsPage = parsePositiveInteger(params.get('contextsPage'), 1);
  const q = params.get('q') ?? '';
  const state: PhraseContextUrlState = {
    build: build.value,
    mode: mode.value,
    q,
    resolution: resolution.value,
    before: before.value,
    after: after.value,
    contextsPage: contextsPage.value,
  };
  const orphanPath = (state.before !== null || state.after !== null) && state.resolution === null;
  const orphanPage = state.contextsPage !== 1 && state.resolution === null;
  return {
    state,
    invalid:
      build.invalid ||
      mode.invalid ||
      resolution.invalid ||
      before.invalid ||
      after.invalid ||
      contextsPage.invalid ||
      orphanPath ||
      orphanPage,
  };
}

export function serializePhraseContextUrlState(state: PhraseContextUrlState): Params {
  return {
    build: state.build,
    mode: state.mode,
    q: state.q || null,
    resolution: state.resolution,
    before: state.before,
    after: state.after,
    contextsPage: state.contextsPage === 1 ? null : String(state.contextsPage),
  };
}

export function safePhraseContextUrlState(
  state: PhraseContextUrlState,
  basePath = '/dashboard/words/phrases/context',
): PhraseContextUrlState {
  const safeWithQuery: PhraseContextUrlState = {
    ...DEFAULT_PHRASE_CONTEXT_URL_STATE,
    build: state.build,
    mode: state.mode,
    q: state.q,
  };
  return phraseUrlLength(
    basePath,
    serializePhraseContextUrlState(safeWithQuery),
  ) <= 1800
    ? safeWithQuery
    : { ...safeWithQuery, q: '' };
}

export function phraseContextStateKey(state: PhraseContextUrlState): string {
  return [
    state.build,
    state.mode,
    state.q,
    state.resolution,
    state.before,
    state.after,
    state.contextsPage,
  ].join('|');
}

export function contextPageOnlyChanged(
  current: PhraseContextUrlState,
  next: PhraseContextUrlState,
): boolean {
  return (
    current.contextsPage !== next.contextsPage &&
    current.build === next.build &&
    current.mode === next.mode &&
    current.q === next.q &&
    current.resolution === next.resolution &&
    current.before === next.before &&
    current.after === next.after
  );
}

export function phraseUrlLength(path: string, params: Params): number {
  const query = new URLSearchParams(
    Object.entries(params)
      .filter((entry): entry is [string, string] => entry[1] !== null && entry[1] !== undefined)
      .map(([key, value]) => [key, String(value)]),
  ).toString();
  return query ? `${path}?${query}`.length : path.length;
}

function parseBuild(value: string | null): { value: string | null; invalid: boolean } {
  if (!value) {
    return { value: null, invalid: false };
  }
  return BUILD_ID_PATTERN.test(value)
    ? { value: value.toLowerCase(), invalid: false }
    : { value: null, invalid: true };
}

function parseReference(value: string | null): { value: string | null; invalid: boolean } {
  if (!value) {
    return { value: null, invalid: false };
  }
  return OPAQUE_REF_PATTERN.test(value)
    ? { value, invalid: false }
    : { value: null, invalid: true };
}

function parsePositiveInteger(
  value: string | null,
  fallback: number,
): { value: number; invalid: boolean } {
  if (!value) {
    return { value: fallback, invalid: false };
  }
  const parsed = /^\d+$/.test(value) ? Number(value) : Number.NaN;
  return Number.isSafeInteger(parsed) && parsed > 0
    ? { value: parsed, invalid: false }
    : { value: fallback, invalid: true };
}
