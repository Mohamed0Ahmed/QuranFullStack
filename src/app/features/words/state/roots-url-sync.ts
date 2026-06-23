import { ParamMap } from '@angular/router';

import { rootsRoutePath } from '../../../core/navigation/route-paths';

import {
  DEFAULT_ROOT_DETAIL_PAGE,
  DEFAULT_ROOT_SORT,
  DEFAULT_ROOT_SURAHS_VIEW,
  DEFAULT_ROOT_VIEW,
  DEFAULT_ROOT_WORD_VIEW,
  DEFAULT_ROOTS_LIST_PAGE,
  ParsedRootsQuery,
  ROOTS_QUERY_KEYS,
  ROOTS_SELECTION_QUERY_KEYS,
  RootSort,
  RootSurahView,
  RootView,
  RootWordView,
  isPaginatedRootView,
  isRootSort,
  isRootSurahView,
  isRootView,
  isRootWordView,
} from '../models/roots.models';

/**
 * Roots Explorer (Feature 015) URL serialization. Rules per
 * `contracts/frontend-routing-state.md`:
 *
 * - `search` defaults to empty.
 * - `sort` defaults to `mushaf-order`; unsupported values fall back to default.
 * - `page` defaults to 1; non-positive/non-numeric → default.
 * - `view` defaults to `ayahs`; ignored unless `root` is a valid positive int.
 *   Sub-views (`wordView`, `surahView`) are ignored unless their parent view is
 *   active; `detailPage` is ignored outside the paginated views (`ayahs`,
 *   `words`). Default values are still emitted when the parent is active but the
 *   param is absent, so downstream consumers always see a complete panel tuple.
 * - Clearing the selection clears `root`/`view`/`wordView`/`surahView`/
 *   `detailPage` and preserves `search`/`sort`/page`.
 */
export function parseRootsQueryParams(queryParams: ParamMap): ParsedRootsQuery {
  const sortRaw = queryParams.get(ROOTS_QUERY_KEYS.sort);
  const sort: RootSort =
    sortRaw !== null && isRootSort(sortRaw) ? sortRaw : DEFAULT_ROOT_SORT;

  const page = parsePositiveInt(queryParams.get(ROOTS_QUERY_KEYS.page)) ?? DEFAULT_ROOTS_LIST_PAGE;

  const rootRaw = queryParams.get(ROOTS_QUERY_KEYS.root);
  const rootId = rootRaw === null ? null : parsePositiveInt(rootRaw);

  // `view` only applies when a root is selected.
  const viewRaw = rootId !== null ? queryParams.get(ROOTS_QUERY_KEYS.view) : null;
  const view: RootView =
    viewRaw !== null && isRootView(viewRaw) ? viewRaw : DEFAULT_ROOT_VIEW;

  // Sub-views are valid only under their parent view; otherwise default.
  const wordViewRaw = view === 'words' ? queryParams.get(ROOTS_QUERY_KEYS.wordView) : null;
  const wordView: RootWordView =
    wordViewRaw !== null && isRootWordView(wordViewRaw) ? wordViewRaw : DEFAULT_ROOT_WORD_VIEW;

  const surahViewRaw = view === 'surahs' ? queryParams.get(ROOTS_QUERY_KEYS.surahView) : null;
  const surahView: RootSurahView =
    surahViewRaw !== null && isRootSurahView(surahViewRaw) ? surahViewRaw : DEFAULT_ROOT_SURAHS_VIEW;

  // `detailPage` only applies to paginated views; otherwise default (1).
  const detailPage = isPaginatedRootView(view)
    ? parsePositiveInt(queryParams.get(ROOTS_QUERY_KEYS.detailPage)) ?? DEFAULT_ROOT_DETAIL_PAGE
    : DEFAULT_ROOT_DETAIL_PAGE;

  return {
    search: queryParams.get(ROOTS_QUERY_KEYS.search) ?? '',
    sort,
    page,
    rootId,
    view,
    wordView,
    surahView,
    detailPage,
  };
}

export type RootsQueryChange = Partial<{
  search: string | null;
  sort: RootSort | null;
  page: number | null;
  rootId: number | null;
  view: RootView | null;
  wordView: RootWordView | null;
  surahView: RootSurahView | null;
  detailPage: number | null;
}>;

/**
 * Builds only the provided fields (undefined is skipped so a merge preserves
 * the rest); `null` explicitly clears the param. Numeric values are stringified.
 */
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

  return params;
}

/**
 * Clears only the selection/panel params, preserving list context
 * (`search`/`sort`/`page`). Used by the clear-selection action.
 */
export function buildClearSelectionQueryParams(): Record<string, null> {
  return Object.fromEntries(ROOTS_SELECTION_QUERY_KEYS.map((key) => [key, null] as const));
}

export interface RootsDeepLinkTarget {
  path: string;
  queryParams: Record<string, string | null>;
}

/**
 * Builds a stable `/dashboard/words/roots` deep link from the provided fields.
 * Omitted fields are left out so merge semantics keep the rest of the URL.
 */
export function buildRootsDeepLink(options: RootsQueryChange = {}): RootsDeepLinkTarget {
  return {
    path: rootsRoutePath(),
    queryParams: buildRootsQueryParams(options),
  };
}

function parsePositiveInt(value: string | null): number | null {
  if (value === null) {
    return null;
  }
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed >= 1 ? parsed : null;
}
