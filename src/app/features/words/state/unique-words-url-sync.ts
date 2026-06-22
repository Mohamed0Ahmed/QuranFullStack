import { ParamMap } from '@angular/router';

import {
  DEFAULT_AYAH_PAGE,
  DEFAULT_LIST_PAGE,
  DEFAULT_UNIQUE_WORD_SORT,
  MODAL_QUERY_KEYS,
  ParsedUniqueWordsQuery,
  UNIQUE_WORDS_QUERY_KEYS,
  UniqueWordSort,
  WordDrilldownView,
  isUniqueWordSort,
  isWordDrilldownView,
} from '../models/unique-words.models';

/**
 * Pure query-param <-> explorer-state helpers for US4 URL restore/share.
 *
 * These functions own the only non-trivial parsing/normalization rules for the
 * explorer query params so the facade can stay thin (see plan.md watch item on
 * `unique-words.facade.ts`). They are framework-light: `ParamMap` is the only
 * Angular type used, and only as an input shape — there are no DI or zone
 * dependencies, so the module is unit-testable in isolation.
 *
 * Parsing rules (see `contracts/frontend-routing-state.md`):
 * - `search` defaults to empty.
 * - `sort` defaults to `mushaf-order`; an unsupported value falls back to the
 *   default rather than failing, because a shared URL with a typo should still
 *   restore the list.
 * - `page` defaults to 1; a non-positive or non-numeric value falls back to 1.
 * - `word` is `null` when absent or non-numeric; a non-positive value is `null`.
 * - `view` is `null` when absent or unsupported; only valid when a `word` is set.
 * - `ap` is `null` when absent; defaults to 1 when the modal ayahs view is open.
 */

/** Parses explorer query params into typed state with documented defaults. */
export function parseUniqueWordsQueryParams(queryParams: ParamMap): ParsedUniqueWordsQuery {
  const sortRaw = queryParams.get(UNIQUE_WORDS_QUERY_KEYS.sort);
  const sort: UniqueWordSort =
    sortRaw !== null && isUniqueWordSort(sortRaw) ? sortRaw : DEFAULT_UNIQUE_WORD_SORT;

  const page = parsePositiveInt(queryParams.get(UNIQUE_WORDS_QUERY_KEYS.page)) ?? DEFAULT_LIST_PAGE;

  const wordRaw = queryParams.get(UNIQUE_WORDS_QUERY_KEYS.word);
  const wordId = wordRaw === null ? null : parsePositiveInt(wordRaw);

  // `view` only makes sense alongside a `word`; ignore it otherwise.
  const viewRaw = wordId !== null ? queryParams.get(UNIQUE_WORDS_QUERY_KEYS.view) : null;
  const view: WordDrilldownView | null =
    viewRaw !== null && isWordDrilldownView(viewRaw) ? viewRaw : null;

  // `ap` only applies to the ayahs view; otherwise leave it null.
  const ayahPage =
    view === 'ayahs'
      ? parsePositiveInt(queryParams.get(UNIQUE_WORDS_QUERY_KEYS.ayahPage)) ?? DEFAULT_AYAH_PAGE
      : null;

  return {
    search: queryParams.get(UNIQUE_WORDS_QUERY_KEYS.search) ?? '',
    sort,
    page,
    wordId,
    view,
    ayahPage,
  };
}

/**
 * Builds query-param changes for `router.navigate([], { queryParams })` from a
 * partial state. Omitted fields are not included so `queryParamsHandling:
 * 'merge'` preserves them. Pass `null` to explicitly remove a param.
 *
 * Field types intentionally include `undefined` because the input is a `Partial`
 * of optional fields; `undefined` values are skipped (treated as "not provided").
 */
export function buildUniqueWordsQueryParams(
  changes: Partial<{
    search: string | null;
    sort: UniqueWordSort | null;
    page: number | null;
    wordId: number | null;
    view: WordDrilldownView | null;
    ayahPage: number | null;
  }>,
): Record<string, string | null> {
  const params: Record<string, string | null> = {};

  if (changes.search !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.search] = changes.search ?? null;
  }
  if (changes.sort !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.sort] = changes.sort ?? null;
  }
  if (changes.page !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.page] = changes.page === null ? null : String(changes.page);
  }
  if (changes.wordId !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.word] =
      changes.wordId === null ? null : String(changes.wordId);
  }
  if (changes.view !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.view] = changes.view ?? null;
  }
  if (changes.ayahPage !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.ayahPage] =
      changes.ayahPage === null ? null : String(changes.ayahPage);
  }

  return params;
}

/**
 * Builds the query-param changes that close the modal while preserving the
 * list context (`search`/`sort`/`page` and the route mode are untouched).
 */
export function buildModalCloseQueryParams(): Record<string, null> {
  return Object.fromEntries(MODAL_QUERY_KEYS.map((key) => [key, null] as const));
}

/** Parses a string to a positive integer, returning `null` when invalid. */
function parsePositiveInt(value: string | null): number | null {
  if (value === null) {
    return null;
  }
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed >= 1 ? parsed : null;
}
