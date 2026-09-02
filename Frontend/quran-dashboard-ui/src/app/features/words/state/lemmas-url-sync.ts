import { ParamMap } from '@angular/router';

import { lemmasRoutePath } from '../../../core/navigation/route-paths';
import { parseRangeFilters } from './words-range-filters';
import { parseWordsPositiveSafeInteger } from './words-route-integer';

import {
  DEFAULT_LEMMA_DETAIL_PAGE,
  DEFAULT_LEMMA_SURAHS_VIEW,
  DEFAULT_LEMMA_VIEW,
  DEFAULT_LEMMA_WORD_VIEW,
  DEFAULT_LEMMAS_LIST_PAGE,
  LEMMAS_QUERY_KEYS,
  LEMMAS_RANGE_METRICS,
  LEMMAS_SELECTION_QUERY_KEYS,
  LemmaSort,
  LemmaSurahView,
  LemmaView,
  LemmaWordView,
  ParsedLemmasQuery,
  isPaginatedLemmaView,
  normalizeLemmaSort,
  isLemmaSurahView,
  isLemmaView,
  isLemmaWordView,
} from '../models/lemmas.models';

export function parseLemmasQueryParams(queryParams: ParamMap): ParsedLemmasQuery {
  // Canonicalizes legacy aliases in (occurrences-desc → occurrences) and fails closed to the
  // default on anything unknown, so one ordering can never be cached under two tokens.
  const sort: LemmaSort = normalizeLemmaSort(queryParams.get(LEMMAS_QUERY_KEYS.sort));

  const page =
    parseWordsPositiveSafeInteger(queryParams.get(LEMMAS_QUERY_KEYS.page)) ??
    DEFAULT_LEMMAS_LIST_PAGE;

  const lemmaRaw = queryParams.get(LEMMAS_QUERY_KEYS.lemma);
  const lemmaId = lemmaRaw === null ? null : parseWordsPositiveSafeInteger(lemmaRaw);

  const viewRaw = lemmaId !== null ? queryParams.get(LEMMAS_QUERY_KEYS.view) : null;
  const view: LemmaView = viewRaw !== null && isLemmaView(viewRaw) ? viewRaw : DEFAULT_LEMMA_VIEW;

  const wordViewRaw = view === 'words' ? queryParams.get(LEMMAS_QUERY_KEYS.wordView) : null;
  const wordView: LemmaWordView =
    wordViewRaw !== null && isLemmaWordView(wordViewRaw) ? wordViewRaw : DEFAULT_LEMMA_WORD_VIEW;

  const surahViewRaw = view === 'surahs' ? queryParams.get(LEMMAS_QUERY_KEYS.surahView) : null;
  const surahView: LemmaSurahView =
    surahViewRaw !== null && isLemmaSurahView(surahViewRaw)
      ? surahViewRaw
      : DEFAULT_LEMMA_SURAHS_VIEW;

  const detailPage = isPaginatedLemmaView(view)
    ? (parseWordsPositiveSafeInteger(queryParams.get(LEMMAS_QUERY_KEYS.detailPage)) ??
      DEFAULT_LEMMA_DETAIL_PAGE)
    : DEFAULT_LEMMA_DETAIL_PAGE;

  const typeCodeRaw =
    view === 'ayahs' || view === 'words' ? queryParams.get(LEMMAS_QUERY_KEYS.typeCode) : null;
  const typeCode = normalizeOptionalText(typeCodeRaw);

  return {
    search: queryParams.get(LEMMAS_QUERY_KEYS.search) ?? '',
    sort,
    page,
    ranges: parseRangeFilters(queryParams, LEMMAS_RANGE_METRICS),
    association: {
      rootId: parseWordsPositiveSafeInteger(queryParams.get(LEMMAS_QUERY_KEYS.rootId)),
    },
    lemmaId,
    view,
    column: queryParams.get(LEMMAS_QUERY_KEYS.column),
    wordView,
    surahView,
    detailPage,
    typeCode,
  };
}

export type LemmasQueryChange = Partial<{
  search: string | null;
  sort: LemmaSort | null;
  page: number | null;
  rootId: number | null;
  lemmaId: number | null;
  view: LemmaView | null;
  column: string | null;
  wordView: LemmaWordView | null;
  surahView: LemmaSurahView | null;
  detailPage: number | null;
  typeCode: string | null;
}>;

export function buildLemmasQueryParams(changes: LemmasQueryChange): Record<string, string | null> {
  const params: Record<string, string | null> = {};

  if (changes.search !== undefined) {
    params[LEMMAS_QUERY_KEYS.search] = changes.search ?? null;
  }
  if (changes.sort !== undefined) {
    params[LEMMAS_QUERY_KEYS.sort] = changes.sort ?? null;
  }
  if (changes.page !== undefined) {
    params[LEMMAS_QUERY_KEYS.page] = changes.page === null ? null : String(changes.page);
  }
  if (changes.rootId !== undefined) {
    params[LEMMAS_QUERY_KEYS.rootId] = changes.rootId === null ? null : String(changes.rootId);
  }
  if (changes.lemmaId !== undefined) {
    params[LEMMAS_QUERY_KEYS.lemma] = changes.lemmaId === null ? null : String(changes.lemmaId);
  }
  if (changes.view !== undefined) {
    params[LEMMAS_QUERY_KEYS.view] = changes.view ?? null;
  }
  if (changes.column !== undefined) {
    params[LEMMAS_QUERY_KEYS.column] = changes.column ?? null;
  }
  if (changes.wordView !== undefined) {
    params[LEMMAS_QUERY_KEYS.wordView] = changes.wordView ?? null;
  }
  if (changes.surahView !== undefined) {
    params[LEMMAS_QUERY_KEYS.surahView] = changes.surahView ?? null;
  }
  if (changes.detailPage !== undefined) {
    params[LEMMAS_QUERY_KEYS.detailPage] =
      changes.detailPage === null ? null : String(changes.detailPage);
  }
  if (changes.typeCode !== undefined) {
    params[LEMMAS_QUERY_KEYS.typeCode] = normalizeOptionalText(changes.typeCode);
  }

  return params;
}

export function buildClearSelectionQueryParams(): Record<string, null> {
  return Object.fromEntries(LEMMAS_SELECTION_QUERY_KEYS.map((key) => [key, null] as const));
}

export interface LemmasDeepLinkTarget {
  path: string;
  queryParams: Record<string, string | null>;
}

export function buildLemmasDeepLink(options: LemmasQueryChange = {}): LemmasDeepLinkTarget {
  return {
    path: lemmasRoutePath(),
    queryParams: buildLemmasQueryParams(options),
  };
}

function normalizeOptionalText(value: string | null): string | null {
  if (value === null) {
    return null;
  }

  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}
