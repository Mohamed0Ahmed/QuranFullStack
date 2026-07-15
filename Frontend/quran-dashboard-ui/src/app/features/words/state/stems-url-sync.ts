import { ParamMap } from '@angular/router';

import { stemsRoutePath } from '../../../core/navigation/route-paths';
import { parseRangeFilters } from './words-range-filters';
import { parsePositiveIntParam } from './words-association-filters';

import {
  DEFAULT_STEM_DETAIL_PAGE,
  DEFAULT_STEM_SORT,
  DEFAULT_STEM_SURAHS_VIEW,
  DEFAULT_STEM_VIEW,
  DEFAULT_STEM_WORD_VIEW,
  DEFAULT_STEMS_LIST_PAGE,
  STEMS_QUERY_KEYS,
  STEMS_RANGE_METRICS,
  STEMS_SELECTION_QUERY_KEYS,
  StemSort,
  StemSurahView,
  StemView,
  StemWordView,
  ParsedStemsQuery,
  isPaginatedStemView,
  isStemSort,
  isStemSurahView,
  isStemView,
  isStemWordView,
} from '../models/stems.models';

export function parseStemsQueryParams(queryParams: ParamMap): ParsedStemsQuery {
  const sortRaw = queryParams.get(STEMS_QUERY_KEYS.sort);
  const sort: StemSort =
    sortRaw !== null && isStemSort(sortRaw) ? sortRaw : DEFAULT_STEM_SORT;

  const page = parsePositiveInt(queryParams.get(STEMS_QUERY_KEYS.page)) ?? DEFAULT_STEMS_LIST_PAGE;

  const stemRaw = queryParams.get(STEMS_QUERY_KEYS.stem);
  const stemId = stemRaw === null ? null : parsePositiveInt(stemRaw);

  const viewRaw = stemId !== null ? queryParams.get(STEMS_QUERY_KEYS.view) : null;
  const view: StemView =
    viewRaw !== null && isStemView(viewRaw) ? viewRaw : DEFAULT_STEM_VIEW;

  const wordViewRaw = view === 'words' ? queryParams.get(STEMS_QUERY_KEYS.wordView) : null;
  const wordView: StemWordView =
    wordViewRaw !== null && isStemWordView(wordViewRaw) ? wordViewRaw : DEFAULT_STEM_WORD_VIEW;

  const surahViewRaw = view === 'surahs' ? queryParams.get(STEMS_QUERY_KEYS.surahView) : null;
  const surahView: StemSurahView =
    surahViewRaw !== null && isStemSurahView(surahViewRaw) ? surahViewRaw : DEFAULT_STEM_SURAHS_VIEW;

  const detailPage = isPaginatedStemView(view)
    ? parsePositiveInt(queryParams.get(STEMS_QUERY_KEYS.detailPage)) ?? DEFAULT_STEM_DETAIL_PAGE
    : DEFAULT_STEM_DETAIL_PAGE;

  const typeCodeRaw = view === 'ayahs' ? queryParams.get(STEMS_QUERY_KEYS.typeCode) : null;
  const typeCode = normalizeOptionalText(typeCodeRaw);

  return {
    search: queryParams.get(STEMS_QUERY_KEYS.search) ?? '',
    sort,
    page,
    ranges: parseRangeFilters(queryParams, STEMS_RANGE_METRICS),
    association: {
      rootId: parsePositiveIntParam(queryParams.get(STEMS_QUERY_KEYS.rootId)),
      lemmaId: parsePositiveIntParam(queryParams.get(STEMS_QUERY_KEYS.lemmaId)),
    },
    stemId,
    view,
    column: queryParams.get(STEMS_QUERY_KEYS.column),
    wordView,
    surahView,
    detailPage,
    typeCode,
  };
}

export type StemsQueryChange = Partial<{
  search: string | null;
  sort: StemSort | null;
  page: number | null;
  rootId: number | null;
  lemmaId: number | null;
  stemId: number | null;
  view: StemView | null;
  column: string | null;
  wordView: StemWordView | null;
  surahView: StemSurahView | null;
  detailPage: number | null;
  typeCode: string | null;
}>;

export function buildStemsQueryParams(changes: StemsQueryChange): Record<string, string | null> {
  const params: Record<string, string | null> = {};

  if (changes.search !== undefined) {
    params[STEMS_QUERY_KEYS.search] = changes.search ?? null;
  }
  if (changes.sort !== undefined) {
    params[STEMS_QUERY_KEYS.sort] = changes.sort ?? null;
  }
  if (changes.page !== undefined) {
    params[STEMS_QUERY_KEYS.page] = changes.page === null ? null : String(changes.page);
  }
  if (changes.rootId !== undefined) {
    params[STEMS_QUERY_KEYS.rootId] = changes.rootId === null ? null : String(changes.rootId);
  }
  if (changes.lemmaId !== undefined) {
    params[STEMS_QUERY_KEYS.lemmaId] = changes.lemmaId === null ? null : String(changes.lemmaId);
  }
  if (changes.stemId !== undefined) {
    params[STEMS_QUERY_KEYS.stem] = changes.stemId === null ? null : String(changes.stemId);
  }
  if (changes.view !== undefined) {
    params[STEMS_QUERY_KEYS.view] = changes.view ?? null;
  }
  if (changes.column !== undefined) {
    params[STEMS_QUERY_KEYS.column] = changes.column ?? null;
  }
  if (changes.wordView !== undefined) {
    params[STEMS_QUERY_KEYS.wordView] = changes.wordView ?? null;
  }
  if (changes.surahView !== undefined) {
    params[STEMS_QUERY_KEYS.surahView] = changes.surahView ?? null;
  }
  if (changes.detailPage !== undefined) {
    params[STEMS_QUERY_KEYS.detailPage] =
      changes.detailPage === null ? null : String(changes.detailPage);
  }
  if (changes.typeCode !== undefined) {
    params[STEMS_QUERY_KEYS.typeCode] = normalizeOptionalText(changes.typeCode);
  }

  return params;
}

export function buildClearSelectionQueryParams(): Record<string, null> {
  return Object.fromEntries(STEMS_SELECTION_QUERY_KEYS.map((key) => [key, null] as const));
}

export interface StemsDeepLinkTarget {
  path: string;
  queryParams: Record<string, string | null>;
}

export function buildStemsDeepLink(options: StemsQueryChange = {}): StemsDeepLinkTarget {
  return {
    path: stemsRoutePath(),
    queryParams: buildStemsQueryParams(options),
  };
}

function parsePositiveInt(value: string | null): number | null {
  if (value === null || !/^[1-9]\d*$/.test(value)) {
    return null;
  }
  const parsed = Number.parseInt(value, 10);
  return parsed;
}

function normalizeOptionalText(value: string | null): string | null {
  if (value === null) {
    return null;
  }

  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}
