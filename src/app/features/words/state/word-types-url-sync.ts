import { ParamMap } from '@angular/router';

import { wordTypesRoutePath } from '../../../core/navigation/route-paths';
import {
  DEFAULT_WORD_TYPE,
  DEFAULT_WORD_TYPE_CASE,
  DEFAULT_WORD_TYPE_SORT,
  DEFAULT_WORD_TYPE_TENSE,
  DEFAULT_WORD_TYPE_VOICE,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPES_PAGE,
  ParsedWordTypesQuery,
  WORD_TYPES_QUERY_KEYS,
  WORD_TYPES_SELECTION_QUERY_KEYS,
  WordTypeCase,
  WordTypeDetailView,
  WordTypeMainType,
  WordTypeSort,
  WordTypeTense,
  WordTypeVoice,
  isWordTypeCase,
  isWordTypeDetailView,
  isWordTypeMainType,
  isWordTypeSort,
  isWordTypeTense,
  isWordTypeVoice,
} from '../models/word-types.models';

export function parseWordTypesQueryParams(queryParams: ParamMap): ParsedWordTypesQuery {
  const typeRaw = queryParams.get(WORD_TYPES_QUERY_KEYS.type);
  const type: WordTypeMainType = typeRaw !== null && isWordTypeMainType(typeRaw) ? typeRaw : DEFAULT_WORD_TYPE;
  const childCode = normalizeChildCode(type, queryParams.get(WORD_TYPES_QUERY_KEYS.childCode));
  const word = parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.word));
  const contextCode = word === null ? '' : normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.contextCode)) ?? '';

  return {
    type,
    childCode,
    case: normalizeCase(type, queryParams.get(WORD_TYPES_QUERY_KEYS.case)),
    tense: normalizeTense(type, queryParams.get(WORD_TYPES_QUERY_KEYS.tense)),
    voice: normalizeVoice(type, queryParams.get(WORD_TYPES_QUERY_KEYS.voice)),
    sort: normalizeSort(queryParams.get(WORD_TYPES_QUERY_KEYS.sort)),
    page: parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.page)) ?? DEFAULT_WORD_TYPES_PAGE,
    word: contextCode.length === 0 ? null : word,
    tashkeelWordId: word ?? 0,
    contextCode,
    view: normalizeView(queryParams.get(WORD_TYPES_QUERY_KEYS.view)),
    detailPage: parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.detailPage)) ?? DEFAULT_WORD_TYPES_DETAIL_PAGE,
    location: normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.location)),
    column: normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.column)),
  };
}

export type WordTypesQueryChange = Partial<{
  type: WordTypeMainType | null;
  childCode: string | null;
  case: WordTypeCase | null;
  tense: WordTypeTense | null;
  voice: WordTypeVoice | null;
  sort: WordTypeSort | null;
  page: number | null;
  word: number | null;
  contextCode: string | null;
  view: WordTypeDetailView | null;
  detailPage: number | null;
  location: string | null;
  column: string | null;
}>;

export function buildWordTypesQueryParams(changes: WordTypesQueryChange): Record<string, string | null> {
  const params: Record<string, string | null> = {};
  for (const [key, value] of Object.entries(changes)) {
    params[WORD_TYPES_QUERY_KEYS[key as keyof typeof WORD_TYPES_QUERY_KEYS]] = value === null ? null : String(value);
  }
  return params;
}

export function clearWordTypesSelection(): Record<string, null> {
  return Object.fromEntries(WORD_TYPES_SELECTION_QUERY_KEYS.map((key) => [key, null] as const));
}

export interface WordTypesDeepLinkTarget {
  path: string;
  queryParams: Record<string, string | null>;
}

export function buildWordTypesDeepLink(options: WordTypesQueryChange = {}): WordTypesDeepLinkTarget {
  return { path: wordTypesRoutePath(), queryParams: buildWordTypesQueryParams(options) };
}

function normalizeChildCode(type: WordTypeMainType, value: string | null): string | null {
  const raw = normalizeOptionalText(value);
  if (raw === null) {
    return null;
  }

  // particle and inl expose no child nodes in v1, so any child code is invalid and dropped.
  // Verb children are the fixed tense set and are validated here. Noun children are catalogue POS
  // codes the parser cannot enumerate, so it passes them through; the backend validates them and
  // rejects an unrecognized noun code with 400 (surfaced as the list error state).
  if (type === 'particle' || type === 'inl') {
    return null;
  }

  if (type === 'verb') {
    return isWordTypeTense(raw) ? raw : null;
  }

  return raw;
}

function normalizeCase(type: WordTypeMainType, value: string | null): WordTypeCase {
  return type === 'noun' && value !== null && isWordTypeCase(value) ? value : DEFAULT_WORD_TYPE_CASE;
}

function normalizeTense(type: WordTypeMainType, value: string | null): WordTypeTense {
  return type === 'verb' && value !== null && isWordTypeTense(value) ? value : DEFAULT_WORD_TYPE_TENSE;
}

function normalizeVoice(type: WordTypeMainType, value: string | null): WordTypeVoice {
  return type === 'verb' && value !== null && isWordTypeVoice(value) ? value : DEFAULT_WORD_TYPE_VOICE;
}

function normalizeSort(value: string | null): WordTypeSort {
  return value !== null && isWordTypeSort(value) ? value : DEFAULT_WORD_TYPE_SORT;
}

function normalizeView(value: string | null): WordTypeDetailView {
  return value !== null && isWordTypeDetailView(value) ? value : DEFAULT_WORD_TYPES_DETAIL_VIEW;
}

function parsePositiveInt(value: string | null): number | null {
  return value !== null && /^[1-9]\d*$/.test(value) ? Number.parseInt(value, 10) : null;
}

function normalizeOptionalText(value: string | null): string | null {
  const trimmed = value?.trim() ?? '';
  return trimmed.length === 0 ? null : trimmed;
}
