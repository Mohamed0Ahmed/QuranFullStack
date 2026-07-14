export type StemSort = 'mushaf-order' | 'occurrences' | 'alpha';

export type StemWordView = 'simple' | 'tashkeel';

export type StemSurahView = 'mentioned' | 'missing';

export type StemView = 'words' | 'ayahs' | 'surahs' | 'lemmas';

import type {
  MissingSurahItemDto,
  StemAyahMatchDto,
  StemAyahWordDto,
  StemLemmaItemDto,
  StemLemmasResponse as StemLemmasDto,
  StemListItemDto,
  StemMissingSurahsResponse as StemMissingSurahsDto,
  StemSummaryDto,
  StemSurahItemDto,
  StemSurahsResponse as StemSurahsDto,
  StemWordItemDto,
  TypeSummaryDto,
} from '../../../core/api/generated/models';
import type { PagedResultDto } from '../../../core/data-access/paged-result.model';

export type {
  MissingSurahItemDto,
  PagedResultDto,
  StemAyahMatchDto,
  StemAyahWordDto,
  StemLemmaItemDto,
  StemLemmasDto,
  StemListItemDto,
  StemMissingSurahsDto,
  StemSummaryDto,
  StemSurahItemDto,
  StemSurahsDto,
  StemWordItemDto,
  TypeSummaryDto,
};

export type LoadStatus = 'idle' | 'loading' | 'success' | 'empty' | 'error' | 'notFound';

export interface StemsListState {
  status: LoadStatus;
  items: readonly StemListItemViewModel[];
  page: number;
  pageSize: number;
  totalCount: number;
  search: string;
  sort: StemSort;
  errorMessage: string;
}

export interface StemsPanelState {
  selectedStemId: number | null;
  summary: StemSummaryDto | null;
  view: StemView;
  wordView: StemWordView;
  surahView: StemSurahView;
  ayahTypeCode: string | null;
  detailPage: number;
  ayahs: PagedResultDto<StemAyahMatchDto> | null;
  words: PagedResultDto<StemWordItemDto> | null;
  mentionedSurahs: StemSurahsDto | null;
  missingSurahs: StemMissingSurahsDto | null;
  lemmas: StemLemmasDto | null;
  status: LoadStatus;
  errorMessage: string;
}

export interface StemListItemViewModel extends StemListItemDto {
  displayText: string;
}

export const STEMS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  stem: 'stem',
  view: 'view',
  column: 'column',
  wordView: 'wordView',
  surahView: 'surahView',
  detailPage: 'detailPage',
  typeCode: 'typeCode',
} as const;

export const STEMS_SELECTION_QUERY_KEYS: readonly string[] = [
  STEMS_QUERY_KEYS.stem,
  STEMS_QUERY_KEYS.view,
  STEMS_QUERY_KEYS.column,
  STEMS_QUERY_KEYS.wordView,
  STEMS_QUERY_KEYS.surahView,
  STEMS_QUERY_KEYS.detailPage,
  STEMS_QUERY_KEYS.typeCode,
] as const;

export const DEFAULT_STEM_SORT: StemSort = 'mushaf-order';
export const DEFAULT_STEM_VIEW: StemView = 'words';
export const DEFAULT_STEM_WORD_VIEW: StemWordView = 'simple';
export const DEFAULT_STEM_SURAHS_VIEW: StemSurahView = 'mentioned';
export const DEFAULT_STEMS_LIST_PAGE = 1;
export const STEMS_LIST_PAGE_SIZE = 1000;
export const DEFAULT_STEM_DETAIL_PAGE = 1;
export const STEM_DETAIL_PAGE_SIZE = 100;
export const TOTAL_SURAHS = 114;

export const STEM_SORT_KEYS = ['mushaf-order', 'occurrences', 'alpha'] as const satisfies readonly StemSort[];
export const STEM_WORD_VIEW_KEYS = ['simple', 'tashkeel'] as const satisfies readonly StemWordView[];
export const STEM_SURAHS_VIEW_KEYS = ['mentioned', 'missing'] as const satisfies readonly StemSurahView[];
export const STEM_VIEW_KEYS = ['words', 'ayahs', 'surahs', 'lemmas'] as const satisfies readonly StemView[];
export const PAGINATED_STEM_VIEWS: readonly StemView[] = ['ayahs', 'words'];

export function isStemSort(value: unknown): value is StemSort {
  return (STEM_SORT_KEYS as readonly string[]).includes(value as string);
}

export function isStemWordView(value: unknown): value is StemWordView {
  return (STEM_WORD_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isStemSurahView(value: unknown): value is StemSurahView {
  return (STEM_SURAHS_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isStemView(value: unknown): value is StemView {
  return (STEM_VIEW_KEYS as readonly string[]).includes(value as string);
}

export function isPaginatedStemView(view: StemView): boolean {
  return (PAGINATED_STEM_VIEWS as readonly string[]).includes(view);
}

export interface ParsedStemsQuery {
  search: string;
  sort: StemSort;
  page: number;
  stemId: number | null;
  view: StemView;
  column: string | null;
  wordView: StemWordView;
  surahView: StemSurahView;
  detailPage: number;
  typeCode: string | null;
}
