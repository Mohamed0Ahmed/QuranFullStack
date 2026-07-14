export type UniqueWordKind = 'tashkeel' | 'simple';

export type UniqueWordSort = 'mushaf-order' | 'occurrences' | 'alpha';

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
  verseKey: string;
  surahNameArabic: string;
  ayahNumber: number;
  pageNumber: number;
  matchedQuranWordIds: number[];
  analysisLocation?: string | null;
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
  status: LoadStatus;
  errorMessage: string;
}

export const UNIQUE_WORD_KIND_KEYS = ['tashkeel', 'simple'] as const satisfies readonly UniqueWordKind[];
export const UNIQUE_WORD_SORT_KEYS = ['mushaf-order', 'occurrences', 'alpha'] as const satisfies readonly UniqueWordSort[];
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

export function isUniqueWordSort(value: unknown): value is UniqueWordSort {
  return value === 'mushaf-order' || value === 'occurrences' || value === 'alpha';
}

export function isWordDrilldownView(value: unknown): value is WordDrilldownView {
  return value === 'surahs' || value === 'missing' || value === 'ayahs';
}

export const UNIQUE_WORDS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  word: 'word',
  view: 'view',
  ayahPage: 'ap',
} as const;

export const MODAL_QUERY_KEYS: readonly string[] = [
  UNIQUE_WORDS_QUERY_KEYS.word,
  UNIQUE_WORDS_QUERY_KEYS.view,
  UNIQUE_WORDS_QUERY_KEYS.ayahPage,
] as const;

export interface ParsedUniqueWordsQuery {
  search: string;
  sort: UniqueWordSort;
  page: number;
  wordId: number | null;
  view: WordDrilldownView | null;
  ayahPage: number | null;
}

export type UniqueWordListItemViewModel = UniqueWordListItemDto;
