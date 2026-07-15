export type LemmaSort = 'mushaf-order' | 'occurrences' | 'alpha';

export type LemmaWordView = 'simple' | 'tashkeel';

export type LemmaSurahView = 'mentioned' | 'missing';

export type LemmaView = 'words' | 'ayahs' | 'surahs' | 'stems';

import type {
  LemmaAyahMatchDto,
  LemmaAyahWordDto,
  LemmaListItemDto,
  LemmaMissingSurahsResponse as LemmaMissingSurahsDto,
  LemmaStemItemDto,
  LemmaStemsResponse as LemmaStemsDto,
  LemmaSummaryDto,
  LemmaSurahItemDto,
  LemmaSurahsResponse as LemmaSurahsDto,
  LemmaWordItemDto,
  MissingSurahItemDto,
  TypeSummaryDto,
} from '../../../core/api/generated/models';
import type { PagedResultDto } from '../../../core/data-access/paged-result.model';
import type { RangeFilters, RangeMetric } from '../state/words-range-filters';
import { WORDS_SHARED_COUNT_COLUMNS } from './words-shared.labels';

export type {
  LemmaAyahMatchDto,
  LemmaAyahWordDto,
  LemmaListItemDto,
  LemmaMissingSurahsDto,
  LemmaStemItemDto,
  LemmaStemsDto,
  LemmaSummaryDto,
  LemmaSurahItemDto,
  LemmaSurahsDto,
  LemmaWordItemDto,
  MissingSurahItemDto,
  PagedResultDto,
  TypeSummaryDto,
};

export type LoadStatus = 'idle' | 'loading' | 'success' | 'empty' | 'error' | 'notFound';

export interface LemmasListState {
  status: LoadStatus;
  items: readonly LemmaListItemViewModel[];
  page: number;
  pageSize: number;
  totalCount: number;
  search: string;
  sort: LemmaSort;
  errorMessage: string;
}

export interface LemmasPanelState {
  selectedLemmaId: number | null;
  summary: LemmaSummaryDto | null;
  view: LemmaView;
  wordView: LemmaWordView;
  surahView: LemmaSurahView;
  ayahTypeCode: string | null;
  detailPage: number;
  ayahs: PagedResultDto<LemmaAyahMatchDto> | null;
  words: PagedResultDto<LemmaWordItemDto> | null;
  mentionedSurahs: LemmaSurahsDto | null;
  missingSurahs: LemmaMissingSurahsDto | null;
  stems: LemmaStemsDto | null;
  status: LoadStatus;
  errorMessage: string;
}

export interface LemmaListItemViewModel extends LemmaListItemDto {
  displayText: string;
}

export const LEMMAS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  rootId: 'rootId',
  lemma: 'lemma',
  view: 'view',
  column: 'column',
  wordView: 'wordView',
  surahView: 'surahView',
  detailPage: 'detailPage',
  typeCode: 'typeCode',
} as const;

// Association filter (Feature 026, US7): owned root (real FK belonging). Fails closed in the URL.
export interface LemmasAssociation {
  readonly rootId: number | null;
}

export const EMPTY_LEMMAS_ASSOCIATION: LemmasAssociation = { rootId: null };

export function isLemmasAssociationActive(association: LemmasAssociation): boolean {
  return association.rootId !== null;
}

// Count-range filter metrics (Feature 026, US5) — the six count columns the Lemmas list shows.
export const LEMMAS_RANGE_METRICS: readonly RangeMetric[] = [
  { key: 'occurrences', urlKey: 'occ', apiKey: 'occ', family: 'occurrences', labelAr: WORDS_SHARED_COUNT_COLUMNS.occurrences },
  { key: 'ayahs', urlKey: 'ayahs', apiKey: 'ayahs', family: 'ayahsSurahs', labelAr: WORDS_SHARED_COUNT_COLUMNS.ayahs },
  { key: 'surahs', urlKey: 'surahs', apiKey: 'surahs', family: 'ayahsSurahs', labelAr: WORDS_SHARED_COUNT_COLUMNS.surahs },
  { key: 'simpleWords', urlKey: 'simple', apiKey: 'simpleWords', family: 'subCount', labelAr: WORDS_SHARED_COUNT_COLUMNS.simpleWords },
  { key: 'tashkeelWords', urlKey: 'tashkeel', apiKey: 'tashkeelWords', family: 'subCount', labelAr: WORDS_SHARED_COUNT_COLUMNS.tashkeelWords },
  { key: 'stems', urlKey: 'stems', apiKey: 'stems', family: 'subCount', labelAr: WORDS_SHARED_COUNT_COLUMNS.stems },
];

export const LEMMAS_SELECTION_QUERY_KEYS: readonly string[] = [
  LEMMAS_QUERY_KEYS.lemma,
  LEMMAS_QUERY_KEYS.view,
  LEMMAS_QUERY_KEYS.column,
  LEMMAS_QUERY_KEYS.wordView,
  LEMMAS_QUERY_KEYS.surahView,
  LEMMAS_QUERY_KEYS.detailPage,
  LEMMAS_QUERY_KEYS.typeCode,
] as const;

export const DEFAULT_LEMMA_SORT: LemmaSort = 'mushaf-order';
export const DEFAULT_LEMMA_VIEW: LemmaView = 'words';
export const DEFAULT_LEMMA_WORD_VIEW: LemmaWordView = 'simple';
export const DEFAULT_LEMMA_SURAHS_VIEW: LemmaSurahView = 'mentioned';
export const DEFAULT_LEMMAS_LIST_PAGE = 1;
export const LEMMAS_LIST_PAGE_SIZE = 1000;
export const DEFAULT_LEMMA_DETAIL_PAGE = 1;
export const LEMMA_DETAIL_PAGE_SIZE = 100;
export const TOTAL_SURAHS = 114;

export const LEMMA_SORT_KEYS = ['mushaf-order', 'occurrences', 'alpha'] as const satisfies readonly LemmaSort[];
export const LEMMA_WORD_VIEW_KEYS = ['simple', 'tashkeel'] as const satisfies readonly LemmaWordView[];
export const LEMMA_SURAHS_VIEW_KEYS = ['mentioned', 'missing'] as const satisfies readonly LemmaSurahView[];
export const LEMMA_VIEW_KEYS = ['words', 'ayahs', 'surahs', 'stems'] as const satisfies readonly LemmaView[];
export const PAGINATED_LEMMA_VIEWS: readonly LemmaView[] = ['ayahs', 'words'];

export function isLemmaSort(value: unknown): value is LemmaSort {
  return (LEMMA_SORT_KEYS as readonly string[]).includes(value as string);
}

export function isLemmaWordView(value: unknown): value is LemmaWordView {
  return (LEMMA_WORD_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isLemmaSurahView(value: unknown): value is LemmaSurahView {
  return (LEMMA_SURAHS_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isLemmaView(value: unknown): value is LemmaView {
  return (LEMMA_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isPaginatedLemmaView(view: LemmaView): boolean {
  return (PAGINATED_LEMMA_VIEWS as readonly string[]).includes(view);
}

export interface ParsedLemmasQuery {
  search: string;
  sort: LemmaSort;
  page: number;
  ranges: RangeFilters;
  association: LemmasAssociation;
  lemmaId: number | null;
  view: LemmaView;
  column: string | null;
  wordView: LemmaWordView;
  surahView: LemmaSurahView;
  detailPage: number;
  typeCode: string | null;
}
