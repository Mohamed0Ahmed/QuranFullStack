import { ParamMap, Params } from '@angular/router';

import {
  DEFAULT_PHRASE_SIMILARITY_URL_STATE,
  ParsedPhraseSimilarityUrlState,
  PhraseSimilaritySource,
  PhraseSimilarityUrlState,
} from '../models/phrase-similarity.models';
import { isPhraseTextMode } from '../models/phrase-repetitions.models';

const BUILD_ID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const OPAQUE_REF_PATTERN = /^[A-Za-z0-9_-]+$/;

export function parsePhraseSimilarityUrlState(
  params: ParamMap,
): ParsedPhraseSimilarityUrlState {
  const build = parseBuild(params.get('build'));
  const source = parseSource(params.get('source'));
  const modeValue = params.get('mode');
  const mode = isPhraseTextMode(modeValue)
    ? { value: modeValue, invalid: false }
    : modeValue
      ? { value: DEFAULT_PHRASE_SIMILARITY_URL_STATE.mode, invalid: true }
      : { value: DEFAULT_PHRASE_SIMILARITY_URL_STATE.mode, invalid: false };
  const length = parsePositiveInteger(params.get('length'), 4);
  const minimum = parsePositiveNumber(params.get('min'), 50);
  const page = parsePositiveInteger(params.get('page'), 1);
  const resolution = parseReference(params.get('resolution'));
  const sortValue = params.get('sort');
  const sortInvalid = sortValue !== null && sortValue !== 'relevance';
  const state: PhraseSimilarityUrlState = {
    build: build.value,
    source: source.value,
    q: params.get('q') ?? '',
    resolution: resolution.value,
    mode: mode.value,
    length: length.value,
    min: minimum.value,
    sort: 'relevance',
    page: page.value,
  };
  const manualWithoutQuery = state.source === 'manual' && state.resolution !== null && !state.q;
  const globalWithResolution = state.source === 'global' && state.resolution !== null;
  return {
    state,
    invalid:
      build.invalid ||
      source.invalid ||
      mode.invalid ||
      length.invalid ||
      minimum.invalid ||
      page.invalid ||
      resolution.invalid ||
      sortInvalid ||
      manualWithoutQuery ||
      globalWithResolution,
  };
}

export function serializePhraseSimilarityUrlState(state: PhraseSimilarityUrlState): Params {
  return {
    build: state.build,
    source: state.source,
    q: state.source === 'manual' && state.q ? state.q : null,
    resolution: state.source === 'manual' ? state.resolution : null,
    mode: state.mode,
    length: String(state.length),
    min: String(state.min),
    sort: state.sort,
    page: String(state.page),
  };
}

export function safePhraseSimilarityUrlState(
  state: PhraseSimilarityUrlState,
  basePath = '/dashboard/words/phrases/similarity',
): PhraseSimilarityUrlState {
  const safeWithQuery: PhraseSimilarityUrlState = {
    ...state,
    resolution: null,
    page: 1,
  };
  return phraseSimilarityUrlLength(
    basePath,
    serializePhraseSimilarityUrlState(safeWithQuery),
  ) <= 1800
    ? safeWithQuery
    : { ...safeWithQuery, q: '' };
}

export function phraseSimilarityStateKey(state: PhraseSimilarityUrlState): string {
  return [
    state.build,
    state.source,
    state.q,
    state.resolution,
    state.mode,
    state.length,
    state.min,
    state.sort,
    state.page,
  ].join('|');
}

export function phraseSimilarityUrlLength(path: string, params: Params): number {
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

function parseSource(value: string | null): { value: PhraseSimilaritySource; invalid: boolean } {
  if (!value) {
    return { value: 'global', invalid: false };
  }
  return value === 'manual' || value === 'global'
    ? { value, invalid: false }
    : { value: 'global', invalid: true };
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

function parsePositiveNumber(
  value: string | null,
  fallback: number,
): { value: number; invalid: boolean } {
  if (!value) {
    return { value: fallback, invalid: false };
  }
  const parsed = /^\d+(?:\.\d+)?$/.test(value) ? Number(value) : Number.NaN;
  return Number.isFinite(parsed) && parsed > 0
    ? { value: parsed, invalid: false }
    : { value: fallback, invalid: true };
}
