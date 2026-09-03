import { ParamMap } from '@angular/router';

import { uniqueWordsRoutePath } from '../../../core/navigation/route-paths';
import { parseRangeFilters } from './words-range-filters';
import { parsePosCodeParam } from './words-association-filters';
import { parseWordsPositiveSafeInteger } from './words-route-integer';

import {
  DEFAULT_AYAH_PAGE,
  DEFAULT_LIST_PAGE,
  MODAL_QUERY_KEYS,
  ParsedUniqueWordsQuery,
  UNIQUE_WORDS_QUERY_KEYS,
  UNIQUE_WORDS_RANGE_METRICS,
  UniqueWordKind,
  UniqueWordSort,
  WordDrilldownView,
  normalizeUniqueWordSort,
  isWordDrilldownView,
} from '../models/unique-words.models';

export function parseUniqueWordsQueryParams(queryParams: ParamMap): ParsedUniqueWordsQuery {
  // Canonicalizes legacy aliases in (occurrences-desc → occurrences) and fails closed to the
  // default on anything unknown, so one ordering can never be cached under two tokens.
  const sort: UniqueWordSort = normalizeUniqueWordSort(
    queryParams.get(UNIQUE_WORDS_QUERY_KEYS.sort),
  );

  const page =
    parseWordsPositiveSafeInteger(queryParams.get(UNIQUE_WORDS_QUERY_KEYS.page)) ??
    DEFAULT_LIST_PAGE;

  const wordRaw = queryParams.get(UNIQUE_WORDS_QUERY_KEYS.word);
  const wordId = wordRaw === null ? null : parseWordsPositiveSafeInteger(wordRaw);

  const viewRaw = wordId !== null ? queryParams.get(UNIQUE_WORDS_QUERY_KEYS.view) : null;
  const view: WordDrilldownView | null =
    viewRaw !== null && isWordDrilldownView(viewRaw) ? viewRaw : null;

  const ayahPage =
    view === 'ayahs'
      ? (parseWordsPositiveSafeInteger(queryParams.get(UNIQUE_WORDS_QUERY_KEYS.ayahPage)) ??
        DEFAULT_AYAH_PAGE)
      : null;

  const typeCode =
    view === 'ayahs' ? parsePosCodeParam(queryParams.get(UNIQUE_WORDS_QUERY_KEYS.typeCode)) : null;

  return {
    search: queryParams.get(UNIQUE_WORDS_QUERY_KEYS.search) ?? '',
    sort,
    page,
    ranges: parseRangeFilters(queryParams, UNIQUE_WORDS_RANGE_METRICS),
    association: {
      primaryType: parsePosCodeParam(queryParams.get(UNIQUE_WORDS_QUERY_KEYS.primaryType)),
      rootId: parseWordsPositiveSafeInteger(queryParams.get(UNIQUE_WORDS_QUERY_KEYS.rootId)),
    },
    wordId,
    view,
    ayahPage,
    typeCode,
  };
}

export function buildUniqueWordsQueryParams(
  changes: Partial<{
    search: string | null;
    sort: UniqueWordSort | null;
    page: number | null;
    primaryType: string | null;
    rootId: number | null;
    wordId: number | null;
    view: WordDrilldownView | null;
    ayahPage: number | null;
    typeCode: string | null;
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
  if (changes.primaryType !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.primaryType] = changes.primaryType ?? null;
  }
  if (changes.rootId !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.rootId] =
      changes.rootId === null ? null : String(changes.rootId);
  }
  if (changes.wordId !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.word] = changes.wordId === null ? null : String(changes.wordId);
  }
  if (changes.view !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.view] = changes.view ?? null;
  }
  if (changes.ayahPage !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.ayahPage] =
      changes.ayahPage === null ? null : String(changes.ayahPage);
  }
  if (changes.typeCode !== undefined) {
    params[UNIQUE_WORDS_QUERY_KEYS.typeCode] = parsePosCodeParam(changes.typeCode);
  }

  return params;
}

export function buildModalCloseQueryParams(): Record<string, null> {
  return Object.fromEntries(MODAL_QUERY_KEYS.map((key) => [key, null] as const));
}

export interface UniqueWordsDeepLinkOptions {
  search?: string;
  sort?: UniqueWordSort;
  page?: number;
  wordId?: number;
  view?: WordDrilldownView;
  ayahPage?: number;
  typeCode?: string;
}

export interface UniqueWordsDeepLinkTarget {
  path: string;
  queryParams: Record<string, string | null>;
}

export function buildUniqueWordsDeepLink(
  kind: UniqueWordKind,
  options: UniqueWordsDeepLinkOptions = {},
): UniqueWordsDeepLinkTarget {
  const queryParams: Partial<{
    search: string | null;
    sort: UniqueWordSort | null;
    page: number | null;
    wordId: number | null;
    view: WordDrilldownView | null;
    ayahPage: number | null;
    typeCode: string | null;
  }> = {};

  if (options.search !== undefined) {
    queryParams.search = options.search;
  }
  if (options.sort !== undefined) {
    queryParams.sort = options.sort;
  }
  if (options.page !== undefined) {
    queryParams.page = options.page;
  }
  if (options.wordId !== undefined) {
    queryParams.wordId = options.wordId;
  }
  if (options.view !== undefined) {
    queryParams.view = options.view;
  }
  if (options.ayahPage !== undefined) {
    queryParams.ayahPage = options.ayahPage;
  }
  if (options.typeCode !== undefined) {
    queryParams.typeCode = options.typeCode;
  }

  return {
    path: uniqueWordsRoutePath(kind),
    queryParams: buildUniqueWordsQueryParams(queryParams),
  };
}
