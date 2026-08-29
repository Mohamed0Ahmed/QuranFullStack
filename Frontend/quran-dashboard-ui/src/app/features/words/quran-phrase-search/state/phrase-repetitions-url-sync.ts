import { ParamMap, Params } from '@angular/router';

import {
  DEFAULT_PHRASE_REPETITIONS_URL_STATE,
  ParsedPhraseRepetitionsUrlState,
  PhraseRepetitionSort,
  PhraseRepetitionsUrlState,
  PhraseTextMode,
} from '../models/phrase-repetitions.models';

export const PHRASE_REPETITIONS_URL_KEYS = {
  build: 'build',
  mode: 'mode',
  length: 'length',
  query: 'q',
  sort: 'sort',
  page: 'page',
  phrase: 'phrase',
  occPage: 'occPage',
} as const;

const VALID_MODES: ReadonlySet<string> = new Set<PhraseTextMode>(['simple', 'tashkil']);
const VALID_SORTS: ReadonlySet<string> = new Set<PhraseRepetitionSort>([
  'occurrences',
  'occurrences-asc',
  'mushaf-order',
]);
const BUILD_ID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const BASE36_PATTERN = /^[0-9a-z]+$/i;

export function parsePhraseRepetitionsUrlState(
  params: ParamMap,
): ParsedPhraseRepetitionsUrlState {
  const buildResult = parseBuild(params.get(PHRASE_REPETITIONS_URL_KEYS.build));
  const modeResult = parseMode(params.get(PHRASE_REPETITIONS_URL_KEYS.mode));
  const lengthResult = parsePositiveInteger(
    params.get(PHRASE_REPETITIONS_URL_KEYS.length),
    DEFAULT_PHRASE_REPETITIONS_URL_STATE.length,
  );
  const query = params.get(PHRASE_REPETITIONS_URL_KEYS.query)?.trim() ?? '';
  const sortResult = parseSort(params.get(PHRASE_REPETITIONS_URL_KEYS.sort));
  const pageResult = parsePositiveInteger(
    params.get(PHRASE_REPETITIONS_URL_KEYS.page),
    DEFAULT_PHRASE_REPETITIONS_URL_STATE.page,
  );
  const phraseResult = parseVariantReference(params.get(PHRASE_REPETITIONS_URL_KEYS.phrase));
  const occurrencePageResult = parsePositiveInteger(
    params.get(PHRASE_REPETITIONS_URL_KEYS.occPage),
    DEFAULT_PHRASE_REPETITIONS_URL_STATE.occPage,
  );

  const state: PhraseRepetitionsUrlState = {
    build: buildResult.value,
    mode: modeResult.value,
    length: lengthResult.value,
    query,
    sort: sortResult.value,
    page: pageResult.value,
    phrase: phraseResult.value,
    occPage: occurrencePageResult.value,
  };

  const inconsistentSelection = state.phrase !== null && state.build === null;
  const orphanOccurrencePage = state.phrase === null && state.occPage !== 1;

  return {
    state,
    invalid:
      buildResult.invalid ||
      modeResult.invalid ||
      lengthResult.invalid ||
      sortResult.invalid ||
      pageResult.invalid ||
      phraseResult.invalid ||
      occurrencePageResult.invalid ||
      inconsistentSelection ||
      orphanOccurrencePage,
  };
}

export function serializePhraseRepetitionsUrlState(state: PhraseRepetitionsUrlState): Params {
  return {
    [PHRASE_REPETITIONS_URL_KEYS.build]: state.build,
    [PHRASE_REPETITIONS_URL_KEYS.mode]: state.mode,
    [PHRASE_REPETITIONS_URL_KEYS.length]: String(state.length),
    [PHRASE_REPETITIONS_URL_KEYS.query]: state.query || null,
    [PHRASE_REPETITIONS_URL_KEYS.sort]: state.sort,
    [PHRASE_REPETITIONS_URL_KEYS.page]: String(state.page),
    [PHRASE_REPETITIONS_URL_KEYS.phrase]:
      state.phrase === null ? null : state.phrase.toString(36),
    [PHRASE_REPETITIONS_URL_KEYS.occPage]:
      state.phrase === null ? null : String(state.occPage),
  };
}

export function phraseRepetitionsUrlStateKey(parsed: ParsedPhraseRepetitionsUrlState): string {
  return [parsed.invalid, phraseRepetitionsRouteStateKey(parsed.state)].join('|');
}

export function phraseRepetitionsRouteStateKey(state: PhraseRepetitionsUrlState): string {
  return [
    state.build,
    state.mode,
    state.length,
    state.query,
    state.sort,
    state.page,
    state.phrase,
    state.occPage,
  ].join('|');
}

function parseBuild(value: string | null): { value: string | null; invalid: boolean } {
  if (value === null || value === '') {
    return { value: null, invalid: false };
  }
  return BUILD_ID_PATTERN.test(value)
    ? { value: value.toLowerCase(), invalid: false }
    : { value: null, invalid: true };
}

function parseMode(value: string | null): { value: PhraseTextMode; invalid: boolean } {
  if (value === null || value === '') {
    return { value: DEFAULT_PHRASE_REPETITIONS_URL_STATE.mode, invalid: false };
  }
  return VALID_MODES.has(value)
    ? { value: value as PhraseTextMode, invalid: false }
    : { value: DEFAULT_PHRASE_REPETITIONS_URL_STATE.mode, invalid: true };
}

function parseSort(value: string | null): { value: PhraseRepetitionSort; invalid: boolean } {
  if (value === null || value === '') {
    return { value: DEFAULT_PHRASE_REPETITIONS_URL_STATE.sort, invalid: false };
  }
  return VALID_SORTS.has(value)
    ? { value: value as PhraseRepetitionSort, invalid: false }
    : { value: DEFAULT_PHRASE_REPETITIONS_URL_STATE.sort, invalid: true };
}

function parsePositiveInteger(
  value: string | null,
  fallback: number,
): { value: number; invalid: boolean } {
  if (value === null || value === '') {
    return { value: fallback, invalid: false };
  }
  if (!/^\d+$/.test(value)) {
    return { value: fallback, invalid: true };
  }
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0
    ? { value: parsed, invalid: false }
    : { value: fallback, invalid: true };
}

function parseVariantReference(
  value: string | null,
): { value: number | null; invalid: boolean } {
  if (value === null || value === '') {
    return { value: null, invalid: false };
  }
  if (!BASE36_PATTERN.test(value)) {
    return { value: null, invalid: true };
  }
  const parsed = Number.parseInt(value, 36);
  return Number.isSafeInteger(parsed) && parsed > 0
    ? { value: parsed, invalid: false }
    : { value: null, invalid: true };
}
