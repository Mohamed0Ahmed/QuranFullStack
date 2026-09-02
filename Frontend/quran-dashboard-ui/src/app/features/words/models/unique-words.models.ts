export type UniqueWordKind = 'tashkeel' | 'simple';

/**
 * The Unique Words sort allowlist, split by column class because that decides the natural
 * direction a bare token means (counts descend, text ascends). `mushaf-order` is the default and
 * is not a column; نوع الكلمة/الجذر (page-only enrichments) and لم يذكر فيها (computed post-page,
 * and merely the inverse of السور) are excluded.
 */
type UniqueWordCountSortColumn = 'occurrences' | 'ayahs' | 'surahs';
type UniqueWordTextSortColumn = 'alpha';
export type UniqueWordSortColumnKey = UniqueWordCountSortColumn | UniqueWordTextSortColumn;

export type UniqueWordSort =
  | MushafOrderSort
  | CanonicalSortTokens<UniqueWordCountSortColumn, 'desc'>
  | CanonicalSortTokens<UniqueWordTextSortColumn, 'asc'>;

export type WordDrilldownView = 'surahs' | 'missing' | 'ayahs';

import type {
  AyahWordForHighlightDto,
  MissingSurahItemDto,
  UniqueWordAyahMatchDto,
  UniqueWordListItemDto as UniqueWordListItemWireDto,
  UniqueWordMissingSurahsResponse as UniqueWordMissingSurahsDto,
  UniqueWordSummaryDto as UniqueWordSummaryWireDto,
  UniqueWordSurahItemDto,
  UniqueWordSurahsResponse as UniqueWordSurahsDto,
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
import type { QuranVerseKey, QuranWordLocation } from '../../../shared/quran/quran-location';
import { WORDS_SHARED_COUNT_COLUMNS, WORDS_SHARED_HEADERS } from './words-shared.labels';

export type {
  AyahWordForHighlightDto,
  MissingSurahItemDto,
  PagedResultDto,
  UniqueWordAyahMatchDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSurahItemDto,
  UniqueWordSurahsDto,
};

export interface UniqueWordListItemDto extends Omit<UniqueWordListItemWireDto, 'kind'> {
  kind: UniqueWordKind;
}

export interface UniqueWordSummaryDto extends Omit<UniqueWordSummaryWireDto, 'kind'> {
  kind: UniqueWordKind;
}

export interface AyahMatchDto {
  ayahId: number;
  verseKey: QuranVerseKey;
  surahNameArabic: string;
  ayahNumber: number;
  pageNumber: number;
  matchedQuranWordIds: number[];
  analysisLocation?: QuranWordLocation | null;
  words: AyahWordForHighlightDto[];
}

export type LoadStatus = 'idle' | 'loading' | 'success' | 'empty' | 'error' | 'notFound';

export interface UniqueWordsListState {
  status: LoadStatus;
  items: readonly UniqueWordListItemViewModel[];
  page: number;
  pageSize: number;
  totalCount: number;
  mode: UniqueWordKind;
  search: string;
  sort: UniqueWordSort;
  errorMessage: string;
}

export interface WordDrilldownState {
  isOpen: boolean;
  selectedWordId: number | null;
  view: WordDrilldownView;
  summary: UniqueWordSummaryDto | null;
  surahs: UniqueWordSurahsDto | null;
  missingSurahs: UniqueWordMissingSurahsDto | null;
  ayahs: PagedResultDto<UniqueWordAyahMatchDto> | null;
  ayahPage: number;
  ayahTypeCode: string | null;
  status: LoadStatus;
  errorMessage: string;
}

export const UNIQUE_WORD_KIND_KEYS = ['tashkeel', 'simple'] as const satisfies readonly UniqueWordKind[];
export const UNIQUE_WORD_SORT_COLUMNS = {
  alpha: { key: 'alpha', natural: 'asc', label: WORDS_SHARED_HEADERS.word },
  occurrences: { key: 'occurrences', natural: 'desc', label: WORDS_SHARED_HEADERS.occurrences },
  ayahs: { key: 'ayahs', natural: 'desc', label: WORDS_SHARED_HEADERS.ayahs },
  surahs: { key: 'surahs', natural: 'desc', label: WORDS_SHARED_HEADERS.surahs },
} as const satisfies Record<UniqueWordSortColumnKey, ExplorerSortColumn<UniqueWordSortColumnKey>>;

export const UNIQUE_WORD_SORT_COLUMN_LIST: readonly ExplorerSortColumn<UniqueWordSortColumnKey>[] =
  Object.values(UNIQUE_WORD_SORT_COLUMNS);

export const UNIQUE_WORD_SORT_KEYS: readonly UniqueWordSort[] = [
  MUSHAF_ORDER_SORT,
  ...canonicalSortTokens(UNIQUE_WORD_SORT_COLUMN_LIST),
] as readonly UniqueWordSort[];
export const WORD_DRILLDOWN_VIEW_KEYS = ['surahs', 'missing', 'ayahs'] as const satisfies readonly WordDrilldownView[];

export const DEFAULT_UNIQUE_WORD_KIND: UniqueWordKind = 'tashkeel';
export const DEFAULT_UNIQUE_WORD_SORT: UniqueWordSort = 'mushaf-order';
export const DEFAULT_LIST_PAGE = 1;
export const UNIQUE_WORDS_PAGE_SIZE = 1000;
export const DEFAULT_LIST_PAGE_SIZE = UNIQUE_WORDS_PAGE_SIZE;
export const DEFAULT_AYAH_PAGE = 1;
export const DEFAULT_AYAH_PAGE_SIZE = 100;
export const TOTAL_SURAHS = 114;

export function isUniqueWordKind(value: unknown): value is UniqueWordKind {
  return value === 'tashkeel' || value === 'simple';
}

/** True only for a CANONICAL token — the legacy alias spellings normalize instead (see roots). */
export function isUniqueWordSort(value: unknown): value is UniqueWordSort {
  return (
    typeof value === 'string' &&
    canonicalizeSortToken(value, UNIQUE_WORD_SORT_COLUMN_LIST) === value
  );
}

export function normalizeUniqueWordSort(value: string | null | undefined): UniqueWordSort {
  const canonical = canonicalizeSortToken(value, UNIQUE_WORD_SORT_COLUMN_LIST);
  return canonical !== null && isUniqueWordSort(canonical) ? canonical : DEFAULT_UNIQUE_WORD_SORT;
}

export function isWordDrilldownView(value: unknown): value is WordDrilldownView {
  return value === 'surahs' || value === 'missing' || value === 'ayahs';
}

export const UNIQUE_WORDS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  primaryType: 'primaryType',
  rootId: 'rootId',
  word: 'word',
  view: 'view',
  ayahPage: 'ap',
  typeCode: 'typeCode',
} as const;

// Association filters (Feature 026, US7): primary word type (POS code) and primary root. Both fail
// closed in the URL and are absent by default (pre-feature URLs unchanged).
export interface UniqueWordsAssociation {
  readonly primaryType: string | null;
  readonly rootId: number | null;
}

export const EMPTY_UNIQUE_WORDS_ASSOCIATION: UniqueWordsAssociation = { primaryType: null, rootId: null };

export function isUniqueWordsAssociationActive(association: UniqueWordsAssociation): boolean {
  return association.primaryType !== null || association.rootId !== null;
}

export const UNIQUE_WORDS_RANGE_METRICS: readonly RangeMetric[] = [
  { key: 'occurrences', urlKey: 'occ', apiKey: 'occ', family: 'occurrences', labelAr: WORDS_SHARED_COUNT_COLUMNS.occurrences },
  { key: 'ayahs', urlKey: 'ayahs', apiKey: 'ayahs', family: 'ayahsSurahs', labelAr: WORDS_SHARED_COUNT_COLUMNS.ayahs },
  { key: 'surahs', urlKey: 'surahs', apiKey: 'surahs', family: 'ayahsSurahs', labelAr: WORDS_SHARED_COUNT_COLUMNS.surahs, threshold: SURAHS_RANGE_THRESHOLD },
];

export const MODAL_QUERY_KEYS: readonly string[] = [
  UNIQUE_WORDS_QUERY_KEYS.word,
  UNIQUE_WORDS_QUERY_KEYS.view,
  UNIQUE_WORDS_QUERY_KEYS.ayahPage,
  UNIQUE_WORDS_QUERY_KEYS.typeCode,
] as const;

export interface ParsedUniqueWordsQuery {
  search: string;
  sort: UniqueWordSort;
  page: number;
  ranges: RangeFilters;
  association: UniqueWordsAssociation;
  wordId: number | null;
  view: WordDrilldownView | null;
  ayahPage: number | null;
  typeCode: string | null;
}

export type UniqueWordListItemViewModel = UniqueWordListItemDto;
