import { ParamMap } from '@angular/router';

import { wordTypesRoutePath } from '../../../core/navigation/route-paths';
import {
  DEFAULT_WORD_TYPE,
  DEFAULT_WORD_TYPE_CASE,
  DEFAULT_WORD_TYPE_SORT,
  DEFAULT_WORD_TYPE_TABLE_VIEW,
  DEFAULT_WORD_TYPE_TENSE,
  DEFAULT_WORD_TYPE_VOICE,
  DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPES_PAGE,
  ParsedWordTypesQuery,
  WORD_TYPES_QUERY_KEYS,
  WordTypeCase,
  WordTypeDetailView,
  WordTypeMainType,
  WordTypeSort,
  WordTypeTableView,
  WordTypeTense,
  WordTypeVoice,
  isWordTypeCase,
  isWordTypeDetailView,
  isWordTypeMainType,
  isWordTypeSort,
  isWordTypeTableView,
  isWordTypeTense,
  isWordTypeVoice,
} from '../models/word-types.models';

export function parseWordTypesQueryParams(queryParams: ParamMap): ParsedWordTypesQuery {
  const typeRaw = queryParams.get(WORD_TYPES_QUERY_KEYS.type);
  const type: WordTypeMainType = typeRaw !== null && isWordTypeMainType(typeRaw) ? typeRaw : DEFAULT_WORD_TYPE;
  const childCode = normalizeChildCode(type, queryParams.get(WORD_TYPES_QUERY_KEYS.childCode));
  const tableView = normalizeTableView(queryParams.get(WORD_TYPES_QUERY_KEYS.tableView));

  // A URL selection must match its table view. Display text never participates in this identity.
  const word = tableView === 'words' ? parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.word)) : null;
  const contextCode = tableView !== 'words' || word === null
    ? ''
    : normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.contextCode)) ?? '';
  const root = tableView === 'roots' ? parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.root)) : null;
  const stem = tableView === 'stems' ? parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.stem)) : null;
  const lemma = tableView === 'lemmas' ? parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.lemma)) : null;
  const view = normalizeView(tableView, queryParams.get(WORD_TYPES_QUERY_KEYS.view));

  return {
    type,
    childCode,
    tableView,
    case: normalizeCase(type, queryParams.get(WORD_TYPES_QUERY_KEYS.case)),
    tense: normalizeTense(type, queryParams.get(WORD_TYPES_QUERY_KEYS.tense)),
    voice: normalizeVoice(type, queryParams.get(WORD_TYPES_QUERY_KEYS.voice)),
    sort: normalizeSort(queryParams.get(WORD_TYPES_QUERY_KEYS.sort)),
    page: parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.page)) ?? DEFAULT_WORD_TYPES_PAGE,
    word: contextCode.length === 0 ? null : word,
    root,
    stem,
    lemma,
    tashkeelWordId: word ?? 0,
    contextCode,
    view,
    detailPage: view === 'surahs'
      ? DEFAULT_WORD_TYPES_DETAIL_PAGE
      : parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.detailPage)) ?? DEFAULT_WORD_TYPES_DETAIL_PAGE,
    location: tableView === 'words' ? normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.location)) : null,
    column: tableView === 'words' ? normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.column)) : null,
  };
}

export type WordTypesQueryChange = Partial<{
  type: WordTypeMainType | null;
  childCode: string | null;
  tableView: WordTypeTableView | null;
  case: WordTypeCase | null;
  tense: WordTypeTense | null;
  voice: WordTypeVoice | null;
  sort: WordTypeSort | null;
  page: number | null;
  word: number | null;
  contextCode: string | null;
  root: number | null;
  stem: number | null;
  lemma: number | null;
  view: WordTypeDetailView | null;
  detailPage: number | string | null;
  location: string | null;
  column: string | null;
}>;

const WORD_TYPES_QUERY_ORDER = [
  'type',
  'childCode',
  'tableView',
  'case',
  'tense',
  'voice',
  'sort',
  'page',
  'word',
  'contextCode',
  'root',
  'stem',
  'lemma',
  'view',
  'detailPage',
  'location',
  'column',
] as const satisfies readonly (keyof WordTypesQueryChange)[];

export function buildWordTypesQueryParams(changes: WordTypesQueryChange): Record<string, string | null> {
  const params: Record<string, string | null> = {};
  for (const key of WORD_TYPES_QUERY_ORDER) {
    if (!Object.prototype.hasOwnProperty.call(changes, key)) {
      continue;
    }

    const value = changes[key];
    if (value === undefined) {
      continue;
    }

    params[WORD_TYPES_QUERY_KEYS[key]] = value === null ? null : String(value);
  }
  return params;
}

export function clearWordTypesSelection(): Record<string, string | null> {
  return buildWordTypesQueryParams({
    word: null,
    contextCode: null,
    root: null,
    stem: null,
    lemma: null,
    view: null,
    detailPage: null,
    location: null,
    column: null,
  });
}

export function clearSelectionForTableView(target: WordTypeTableView): Record<string, string | null> {
  return buildWordTypesQueryParams({
    ...(target === 'words' ? {} : { word: null, contextCode: null }),
    ...(target === 'roots' ? {} : { root: null }),
    ...(target === 'stems' ? {} : { stem: null }),
    ...(target === 'lemmas' ? {} : { lemma: null }),
    view: null,
    detailPage: null,
    location: null,
    column: null,
  });
}

export function canonicalWordTypesDetailPage(view: WordTypeDetailView, page: number | string): string | null {
  const normalizedPage = typeof page === 'string'
    ? parsePositiveInt(page)
    : Number.isInteger(page) && page > 0 ? page : null;

  return view === 'surahs' || normalizedPage === null || normalizedPage <= DEFAULT_WORD_TYPES_DETAIL_PAGE
    ? null
    : String(normalizedPage);
}

export interface WordTypesDeepLinkTarget {
  path: string;
  queryParams: Record<string, string | null>;
}

export function buildWordTypesDeepLink(options: WordTypesQueryChange = {}): WordTypesDeepLinkTarget {
  const tableView = options.tableView ?? DEFAULT_WORD_TYPE_TABLE_VIEW;
  const view = options.view ?? defaultViewForTableView(tableView);
  const detailPage = options.detailPage === undefined || options.detailPage === null
    ? options.detailPage
    : canonicalWordTypesDetailPage(view, options.detailPage);

  return {
    path: wordTypesRoutePath(),
    queryParams: buildWordTypesQueryParams({ ...options, ...(detailPage === undefined ? {} : { detailPage }) }),
  };
}

function normalizeChildCode(type: WordTypeMainType, value: string | null): string | null {
  const raw = normalizeOptionalText(value);
  if (raw === null) {
    return null;
  }

  // inl is the only leaf with no child dimension. Verb children are the fixed tense set and are
  // validated here. Noun and particle children are catalogue POS codes the parser cannot enumerate,
  // so they pass through; the backend validates them and rejects an unrecognized code with 400.
  if (type === 'inl') {
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

function normalizeTableView(value: string | null): WordTypeTableView {
  return value !== null && isWordTypeTableView(value) ? value : DEFAULT_WORD_TYPE_TABLE_VIEW;
}

function normalizeView(tableView: WordTypeTableView, value: string | null): WordTypeDetailView {
  const normalized = value !== null && isWordTypeDetailView(value) ? value : defaultViewForTableView(tableView);
  return tableView === 'words' && normalized === 'words' ? DEFAULT_WORD_TYPES_DETAIL_VIEW : normalized;
}

function defaultViewForTableView(tableView: WordTypeTableView): WordTypeDetailView {
  return tableView === 'words' ? DEFAULT_WORD_TYPES_DETAIL_VIEW : DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW;
}

function parsePositiveInt(value: string | null): number | null {
  return value !== null && /^[1-9]\d*$/.test(value) ? Number.parseInt(value, 10) : null;
}

function normalizeOptionalText(value: string | null): string | null {
  const trimmed = value?.trim() ?? '';
  return trimmed.length === 0 ? null : trimmed;
}
