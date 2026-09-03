import { ParamMap } from '@angular/router';

import { rootsRoutePath } from '../../../core/navigation/route-paths';
import { parseRangeFilters } from './words-range-filters';
import { parsePosCodeParam } from './words-association-filters';
import { parseWordsPositiveSafeInteger } from './words-route-integer';

import {
  DEFAULT_ROOT_DETAIL_PAGE,
  DEFAULT_ROOT_SURAHS_VIEW,
  DEFAULT_ROOT_VIEW,
  DEFAULT_ROOT_WORD_VIEW,
  DEFAULT_ROOTS_LIST_PAGE,
  ParsedRootsQuery,
  ROOTS_QUERY_KEYS,
  ROOTS_RANGE_METRICS,
  ROOTS_SELECTION_QUERY_KEYS,
  RootSort,
  RootSurahView,
  RootView,
  RootWordView,
  isPaginatedRootView,
  isRootSurahView,
  isRootView,
  isRootWordView,
  normalizeRootSort,
} from '../models/roots.models';

export function parseRootsQueryParams(queryParams: ParamMap): ParsedRootsQuery {
  // Canonicalizes legacy aliases in (occurrences-desc → occurrences) and fails closed to the
  // default on anything unknown, so one ordering can never be cached under two tokens.
  const sort: RootSort = normalizeRootSort(queryParams.get(ROOTS_QUERY_KEYS.sort));

  const page =
    parseWordsPositiveSafeInteger(queryParams.get(ROOTS_QUERY_KEYS.page)) ??
    DEFAULT_ROOTS_LIST_PAGE;

  const rootRaw = queryParams.get(ROOTS_QUERY_KEYS.root);
  const rootId = rootRaw === null ? null : parseWordsPositiveSafeInteger(rootRaw);

  const viewRaw = rootId !== null ? queryParams.get(ROOTS_QUERY_KEYS.view) : null;
  const view: RootView = viewRaw !== null && isRootView(viewRaw) ? viewRaw : DEFAULT_ROOT_VIEW;

  const wordViewRaw = view === 'words' ? queryParams.get(ROOTS_QUERY_KEYS.wordView) : null;
  const wordView: RootWordView =
    wordViewRaw !== null && isRootWordView(wordViewRaw) ? wordViewRaw : DEFAULT_ROOT_WORD_VIEW;

  const surahViewRaw = view === 'surahs' ? queryParams.get(ROOTS_QUERY_KEYS.surahView) : null;
  const surahView: RootSurahView =
    surahViewRaw !== null && isRootSurahView(surahViewRaw)
      ? surahViewRaw
      : DEFAULT_ROOT_SURAHS_VIEW;

  const detailPage = isPaginatedRootView(view)
    ? (parseWordsPositiveSafeInteger(queryParams.get(ROOTS_QUERY_KEYS.detailPage)) ??
      DEFAULT_ROOT_DETAIL_PAGE)
    : DEFAULT_ROOT_DETAIL_PAGE;

  const typeCode =
    view === 'ayahs' || view === 'words'
      ? parsePosCodeParam(queryParams.get(ROOTS_QUERY_KEYS.typeCode))
      : null;

  return {
    search: queryParams.get(ROOTS_QUERY_KEYS.search) ?? '',
    sort,
    page,
    ranges: parseRangeFilters(queryParams, ROOTS_RANGE_METRICS),
    rootId,
    view,
    column: queryParams.get(ROOTS_QUERY_KEYS.column),
    wordView,
    surahView,
    detailPage,
    typeCode,
  };
}

export type RootsQueryChange = Partial<{
  search: string | null;
  sort: RootSort | null;
  page: number | null;
  rootId: number | null;
  view: RootView | null;
  column: string | null;
  wordView: RootWordView | null;
  surahView: RootSurahView | null;
  detailPage: number | null;
  typeCode: string | null;
}>;

export function buildRootsQueryParams(changes: RootsQueryChange): Record<string, string | null> {
  const params: Record<string, string | null> = {};

  if (changes.search !== undefined) {
    params[ROOTS_QUERY_KEYS.search] = changes.search ?? null;
  }
  if (changes.sort !== undefined) {
    params[ROOTS_QUERY_KEYS.sort] = changes.sort ?? null;
  }
  if (changes.page !== undefined) {
    params[ROOTS_QUERY_KEYS.page] = changes.page === null ? null : String(changes.page);
  }
  if (changes.rootId !== undefined) {
    params[ROOTS_QUERY_KEYS.root] = changes.rootId === null ? null : String(changes.rootId);
  }
  if (changes.view !== undefined) {
    params[ROOTS_QUERY_KEYS.view] = changes.view ?? null;
  }
  if (changes.column !== undefined) {
    params[ROOTS_QUERY_KEYS.column] = changes.column ?? null;
  }
  if (changes.wordView !== undefined) {
    params[ROOTS_QUERY_KEYS.wordView] = changes.wordView ?? null;
  }
  if (changes.surahView !== undefined) {
    params[ROOTS_QUERY_KEYS.surahView] = changes.surahView ?? null;
  }
  if (changes.detailPage !== undefined) {
    params[ROOTS_QUERY_KEYS.detailPage] =
      changes.detailPage === null ? null : String(changes.detailPage);
  }
  if (changes.typeCode !== undefined) {
    params[ROOTS_QUERY_KEYS.typeCode] = parsePosCodeParam(changes.typeCode);
  }

  return params;
}

export function buildClearSelectionQueryParams(): Record<string, null> {
  return Object.fromEntries(ROOTS_SELECTION_QUERY_KEYS.map((key) => [key, null] as const));
}

export interface RootsDeepLinkTarget {
  path: string;
  queryParams: Record<string, string | null>;
}

export function buildRootsDeepLink(options: RootsQueryChange = {}): RootsDeepLinkTarget {
  return {
    path: rootsRoutePath(),
    queryParams: buildRootsQueryParams(options),
  };
}
