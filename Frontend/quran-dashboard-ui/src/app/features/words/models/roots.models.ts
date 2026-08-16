/**
 * The Roots sort allowlist, split by column class because that decides the natural direction a
 * bare token means (counts descend, text ascends). `mushaf-order` is the default and is not a
 * column — see `explorer-sort.ts` for the token grammar.
 */
type RootCountSortColumn =
  | 'occurrences'
  | 'ayahs'
  | 'surahs'
  | 'simple'
  | 'tashkeel'
  | 'lemmas'
  | 'stems';
type RootTextSortColumn = 'alpha';
export type RootSortColumnKey = RootCountSortColumn | RootTextSortColumn;

export type RootSort =
  | MushafOrderSort
  | CanonicalSortTokens<RootCountSortColumn, 'desc'>
  | CanonicalSortTokens<RootTextSortColumn, 'asc'>;

export type RootWordView = 'simple' | 'tashkeel';

export type RootSurahView = 'mentioned' | 'missing';

export type RootView = 'words' | 'ayahs' | 'surahs' | 'lemmas' | 'stems';

import type {
  MissingSurahItemDto,
  RootAyahMatchDto,
  RootAyahWordDto,
  RootLemmaItemDto,
  RootLemmasResponse as RootLemmasDto,
  RootListItemDto,
  RootMissingSurahsResponse as RootMissingSurahsDto,
  RootStemItemDto,
  RootStemsResponse as RootStemsDto,
  RootSummaryDto,
  RootSurahItemDto,
  RootSurahsResponse as RootSurahsDto,
  RootWordItemDto as RootWordItemWireDto,
} from '../../../core/api/generated/models';
import type { PagedResultDto } from '../../../core/data-access/paged-result.model';
import type { RangeFilters, RangeMetric } from '../state/words-range-filters';
import {
  CanonicalSortTokens,
  ExplorerSortColumn,
  MUSHAF_ORDER_SORT,
  MushafOrderSort,
  canonicalSortTokens,
  canonicalizeSortToken,
} from './explorer-sort';
import { SURAHS_RANGE_THRESHOLD } from './words-filter-presets';
import { WORDS_SHARED_COUNT_COLUMNS, WORDS_SHARED_HEADERS } from './words-shared.labels';

export type {
  MissingSurahItemDto,
  PagedResultDto,
  RootAyahMatchDto,
  RootAyahWordDto,
  RootLemmaItemDto,
  RootLemmasDto,
  RootListItemDto,
  RootMissingSurahsDto,
  RootStemItemDto,
  RootStemsDto,
  RootSummaryDto,
  RootSurahItemDto,
  RootSurahsDto,
};

export interface RootWordItemDto extends Omit<RootWordItemWireDto, 'kind'> {
  kind: RootWordView;
}

export type LoadStatus = 'idle' | 'loading' | 'success' | 'empty' | 'error' | 'notFound';

export interface RootsListState {
  status: LoadStatus;
  items: readonly RootListItemViewModel[];
  page: number;
  pageSize: number;
  totalCount: number;
  search: string;
  sort: RootSort;
  errorMessage: string;
}

export interface RootsPanelState {
  selectedRootId: number | null;
  summary: RootSummaryDto | null;
  view: RootView;
  wordView: RootWordView;
  surahView: RootSurahView;
  ayahTypeCode: string | null;
  detailPage: number;
  ayahs: PagedResultDto<RootAyahMatchDto> | null;
  words: PagedResultDto<RootWordItemDto> | null;
  mentionedSurahs: RootSurahsDto | null;
  missingSurahs: RootMissingSurahsDto | null;
  lemmas: RootLemmasDto | null;
  stems: RootStemsDto | null;
  status: LoadStatus;
  errorMessage: string;
}

export interface RootListItemViewModel extends RootListItemDto {
  displayText: string;
}

export const ROOTS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  root: 'root',
  view: 'view',
  column: 'column',
  wordView: 'wordView',
  surahView: 'surahView',
  detailPage: 'detailPage',
  typeCode: 'typeCode',
} as const;

export const ROOTS_RANGE_METRICS: readonly RangeMetric[] = [
  { key: 'occurrences', urlKey: 'occ', apiKey: 'occ', family: 'occurrences', labelAr: WORDS_SHARED_COUNT_COLUMNS.occurrences },
  { key: 'ayahs', urlKey: 'ayahs', apiKey: 'ayahs', family: 'ayahsSurahs', labelAr: WORDS_SHARED_COUNT_COLUMNS.ayahs },
  { key: 'surahs', urlKey: 'surahs', apiKey: 'surahs', family: 'ayahsSurahs', labelAr: WORDS_SHARED_COUNT_COLUMNS.surahs, threshold: SURAHS_RANGE_THRESHOLD },
  { key: 'simpleWords', urlKey: 'simple', apiKey: 'simpleWords', family: 'subCount', labelAr: WORDS_SHARED_COUNT_COLUMNS.simpleWords },
  { key: 'tashkeelWords', urlKey: 'tashkeel', apiKey: 'tashkeelWords', family: 'subCount', labelAr: WORDS_SHARED_COUNT_COLUMNS.tashkeelWords },
  { key: 'lemmas', urlKey: 'lemmas', apiKey: 'lemmas', family: 'subCount', labelAr: WORDS_SHARED_COUNT_COLUMNS.lemmas },
  { key: 'stems', urlKey: 'stems', apiKey: 'stems', family: 'subCount', labelAr: WORDS_SHARED_COUNT_COLUMNS.stems },
];

export const ROOTS_SELECTION_QUERY_KEYS: readonly string[] = [
  ROOTS_QUERY_KEYS.root,
  ROOTS_QUERY_KEYS.view,
  ROOTS_QUERY_KEYS.column,
  ROOTS_QUERY_KEYS.wordView,
  ROOTS_QUERY_KEYS.surahView,
  ROOTS_QUERY_KEYS.detailPage,
  ROOTS_QUERY_KEYS.typeCode,
] as const;

export const DEFAULT_ROOT_SORT: RootSort = 'mushaf-order';
export const DEFAULT_ROOT_VIEW: RootView = 'words';
export const DEFAULT_ROOT_WORD_VIEW: RootWordView = 'simple';
export const DEFAULT_ROOT_SURAHS_VIEW: RootSurahView = 'mentioned';
export const DEFAULT_ROOTS_LIST_PAGE = 1;
export const ROOTS_LIST_PAGE_SIZE = 1000;
export const DEFAULT_ROOT_DETAIL_PAGE = 1;
export const ROOT_DETAIL_PAGE_SIZE = 100;
export const TOTAL_SURAHS = 114;

/**
 * The sortable Roots columns, in table-header order. Every one is a value the backend already has
 * on the summary row at the sort point. The related-entity text columns are deliberately absent —
 * they render as plain headers.
 */
export const ROOT_SORT_COLUMNS = {
  alpha: { key: 'alpha', natural: 'asc', label: WORDS_SHARED_HEADERS.root },
  occurrences: { key: 'occurrences', natural: 'desc', label: WORDS_SHARED_HEADERS.occurrences },
  ayahs: { key: 'ayahs', natural: 'desc', label: WORDS_SHARED_HEADERS.ayahs },
  surahs: { key: 'surahs', natural: 'desc', label: WORDS_SHARED_HEADERS.surahs },
  simple: { key: 'simple', natural: 'desc', label: WORDS_SHARED_HEADERS.simpleWords },
  tashkeel: { key: 'tashkeel', natural: 'desc', label: WORDS_SHARED_HEADERS.tashkeelWords },
  lemmas: { key: 'lemmas', natural: 'desc', label: WORDS_SHARED_HEADERS.lemmas },
  stems: { key: 'stems', natural: 'desc', label: WORDS_SHARED_HEADERS.stems },
} as const satisfies Record<RootSortColumnKey, ExplorerSortColumn<RootSortColumnKey>>;

export const ROOT_SORT_COLUMN_LIST: readonly ExplorerSortColumn<RootSortColumnKey>[] =
  Object.values(ROOT_SORT_COLUMNS);

export const ROOT_SORT_KEYS: readonly RootSort[] = [
  MUSHAF_ORDER_SORT,
  ...canonicalSortTokens(ROOT_SORT_COLUMN_LIST),
] as readonly RootSort[];
export const ROOT_WORD_VIEW_KEYS = ['simple', 'tashkeel'] as const satisfies readonly RootWordView[];
export const ROOT_SURAHS_VIEW_KEYS = ['mentioned', 'missing'] as const satisfies readonly RootSurahView[];
export const ROOT_VIEW_KEYS = ['words', 'ayahs', 'surahs', 'lemmas', 'stems'] as const satisfies readonly RootView[];
export const PAGINATED_ROOT_VIEWS: readonly RootView[] = ['ayahs', 'words'];

/**
 * True only for a CANONICAL token. Canonicalization is idempotent, so a value that survives it
 * unchanged was already canonical — which rejects the legacy alias spellings (`occurrences-desc`)
 * that `normalizeRootSort` collapses instead.
 */
export function isRootSort(value: unknown): value is RootSort {
  return (
    typeof value === 'string' && canonicalizeSortToken(value, ROOT_SORT_COLUMN_LIST) === value
  );
}

/**
 * The URL/DOM entry point: canonicalizes aliases in, and fails closed to the default on anything
 * unknown, so the frontend never emits a token the backend would 400 on.
 */
export function normalizeRootSort(value: string | null | undefined): RootSort {
  const canonical = canonicalizeSortToken(value, ROOT_SORT_COLUMN_LIST);
  return canonical !== null && isRootSort(canonical) ? canonical : DEFAULT_ROOT_SORT;
}

export function isRootWordView(value: unknown): value is RootWordView {
  return (ROOT_WORD_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isRootSurahView(value: unknown): value is RootSurahView {
  return (ROOT_SURAHS_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isRootView(value: unknown): value is RootView {
  return (ROOT_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isPaginatedRootView(view: RootView): boolean {
  return (PAGINATED_ROOT_VIEWS as readonly string[]).includes(view);
}

export interface ParsedRootsQuery {
  search: string;
  sort: RootSort;
  page: number;
  ranges: RangeFilters;
  rootId: number | null;
  view: RootView;
  column: string | null;
  wordView: RootWordView;
  surahView: RootSurahView;
  detailPage: number;
  typeCode: string | null;
}
