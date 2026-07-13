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
import {
  WordTypeDetailSelection,
  WordTypeDetailSelectionKind,
} from '../models/word-types-detail.models';

type ParsedDetailScope = Pick<
  ParsedWordTypesQuery,
  'detailType' | 'detailChildCode' | 'detailCase' | 'detailTense' | 'detailVoice'
>;

const EMPTY_DETAIL_SCOPE: ParsedDetailScope = {
  detailType: null,
  detailChildCode: null,
  detailCase: null,
  detailTense: null,
  detailVoice: null,
};

export function parseWordTypesQueryParams(queryParams: ParamMap): ParsedWordTypesQuery {
  const typeRaw = queryParams.get(WORD_TYPES_QUERY_KEYS.type);
  const type: WordTypeMainType = typeRaw !== null && isWordTypeMainType(typeRaw) ? typeRaw : DEFAULT_WORD_TYPE;
  const childCode = normalizeChildCode(type, queryParams.get(WORD_TYPES_QUERY_KEYS.childCode));
  const tableView = normalizeTableView(queryParams.get(WORD_TYPES_QUERY_KEYS.tableView));

  const parsedIdentity = parseDetailIdentity(queryParams);
  const view = normalizeView(parsedIdentity.kind, tableView, queryParams.get(WORD_TYPES_QUERY_KEYS.view));
  const wordRouteState = parsedIdentity.kind === 'word'
    || (parsedIdentity.kind === null && tableView === 'words');
  const detailScope = parseDetailScope(queryParams);

  return {
    type,
    childCode,
    tableView,
    case: normalizeCase(type, queryParams.get(WORD_TYPES_QUERY_KEYS.case)),
    tense: normalizeTense(type, queryParams.get(WORD_TYPES_QUERY_KEYS.tense)),
    voice: normalizeVoice(type, queryParams.get(WORD_TYPES_QUERY_KEYS.voice)),
    sort: normalizeSort(queryParams.get(WORD_TYPES_QUERY_KEYS.sort)),
    page: parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.page)) ?? DEFAULT_WORD_TYPES_PAGE,
    word: parsedIdentity.word,
    root: parsedIdentity.root,
    stem: parsedIdentity.stem,
    lemma: parsedIdentity.lemma,
    ...detailScope,
    tashkeelWordId: parsedIdentity.word ?? 0,
    contextCode: parsedIdentity.contextCode,
    view,
    detailPage: view === 'surahs'
      ? DEFAULT_WORD_TYPES_DETAIL_PAGE
      : parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.detailPage)) ?? DEFAULT_WORD_TYPES_DETAIL_PAGE,
    location: wordRouteState ? normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.location)) : null,
    column: wordRouteState ? normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.column)) : null,
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
  detailType: WordTypeMainType | null;
  detailChildCode: string | null;
  detailCase: WordTypeCase | null;
  detailTense: WordTypeTense | null;
  detailVoice: WordTypeVoice | null;
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
  'detailType',
  'detailChildCode',
  'detailCase',
  'detailTense',
  'detailVoice',
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

export function buildWordTypesDetailScopeQuery(
  selection: Pick<WordTypeDetailSelection, 'scope'>,
): WordTypesQueryChange {
  return {
    detailType: selection.scope.type,
    detailChildCode: selection.scope.childCode,
    detailCase: selection.scope.case,
    detailTense: selection.scope.tense,
    detailVoice: selection.scope.voice,
  };
}

export function clearWordTypesSelection(): Record<string, string | null> {
  return buildWordTypesQueryParams({
    word: null,
    contextCode: null,
    root: null,
    stem: null,
    lemma: null,
    detailType: null,
    detailChildCode: null,
    detailCase: null,
    detailTense: null,
    detailVoice: null,
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

function parseDetailScope(queryParams: ParamMap): ParsedDetailScope {
  const detailCaseRaw = queryParams.get(WORD_TYPES_QUERY_KEYS.detailCase);
  const detailTenseRaw = queryParams.get(WORD_TYPES_QUERY_KEYS.detailTense);
  const detailVoiceRaw = queryParams.get(WORD_TYPES_QUERY_KEYS.detailVoice);
  if (
    detailCaseRaw === null || !isWordTypeCase(detailCaseRaw)
    || detailTenseRaw === null || !isWordTypeTense(detailTenseRaw)
    || detailVoiceRaw === null || !isWordTypeVoice(detailVoiceRaw)
  ) {
    return EMPTY_DETAIL_SCOPE;
  }

  const detailTypeRaw = queryParams.get(WORD_TYPES_QUERY_KEYS.detailType);
  if (detailTypeRaw === null || !isWordTypeMainType(detailTypeRaw)) {
    return EMPTY_DETAIL_SCOPE;
  }

  const detailChildCodeRaw = normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.detailChildCode));
  const detailChildCode = normalizeChildCode(detailTypeRaw, detailChildCodeRaw);
  if (
    (detailTypeRaw === 'inl' && detailChildCodeRaw !== null)
    || (detailTypeRaw !== 'inl' && detailChildCode === null)
    || !hasCompatibleDetailFeatures(detailTypeRaw, detailCaseRaw, detailTenseRaw, detailVoiceRaw)
  ) {
    return EMPTY_DETAIL_SCOPE;
  }

  return {
    detailType: detailTypeRaw,
    detailChildCode,
    detailCase: detailCaseRaw,
    detailTense: detailTenseRaw,
    detailVoice: detailVoiceRaw,
  };
}

function hasCompatibleDetailFeatures(
  type: WordTypeMainType,
  caseValue: WordTypeCase,
  tense: WordTypeTense,
  voice: WordTypeVoice,
): boolean {
  switch (type) {
    case 'noun': return tense === DEFAULT_WORD_TYPE_TENSE && voice === DEFAULT_WORD_TYPE_VOICE;
    case 'verb': return caseValue === DEFAULT_WORD_TYPE_CASE;
    case 'particle':
    case 'inl':
      return caseValue === DEFAULT_WORD_TYPE_CASE
        && tense === DEFAULT_WORD_TYPE_TENSE
        && voice === DEFAULT_WORD_TYPE_VOICE;
  }
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

function normalizeView(
  detailKind: WordTypeDetailSelectionKind | null,
  tableView: WordTypeTableView,
  value: string | null,
): WordTypeDetailView {
  const wordDetail = detailKind === 'word' || (detailKind === null && tableView === 'words');
  const defaultView = wordDetail ? DEFAULT_WORD_TYPES_DETAIL_VIEW : DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW;
  const normalized = value !== null && isWordTypeDetailView(value) ? value : defaultView;
  return wordDetail && normalized === 'words' ? DEFAULT_WORD_TYPES_DETAIL_VIEW : normalized;
}

interface ParsedDetailIdentity {
  readonly kind: WordTypeDetailSelectionKind | null;
  readonly word: number | null;
  readonly contextCode: string;
  readonly root: number | null;
  readonly stem: number | null;
  readonly lemma: number | null;
}

function parseDetailIdentity(queryParams: ParamMap): ParsedDetailIdentity {
  const word = parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.word));
  const contextCode = word === null
    ? ''
    : normalizeOptionalText(queryParams.get(WORD_TYPES_QUERY_KEYS.contextCode)) ?? '';
  const candidates: ReadonlyArray<readonly [WordTypeDetailSelectionKind, number | null]> = [
    ['word', contextCode.length > 0 ? word : null],
    ['root', parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.root))],
    ['stem', parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.stem))],
    ['lemma', parsePositiveInt(queryParams.get(WORD_TYPES_QUERY_KEYS.lemma))],
  ];
  const selected = candidates.filter((candidate) => candidate[1] !== null);
  const kind = selected.length === 1 ? selected[0][0] : null;

  return {
    kind,
    word: kind === 'word' ? word : null,
    contextCode: kind === 'word' ? contextCode : '',
    root: kind === 'root' ? selected[0][1] : null,
    stem: kind === 'stem' ? selected[0][1] : null,
    lemma: kind === 'lemma' ? selected[0][1] : null,
  };
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
