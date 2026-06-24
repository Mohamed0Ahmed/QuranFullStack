export type UniqueWordKind = 'tashkeel' | 'simple';

export type UniqueWordSort = 'mushaf-order' | 'occurrences' | 'alpha';

export type WordDrilldownView = 'surahs' | 'missing' | 'ayahs';

export interface PagedResultDto<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

export interface UniqueWordListItemDto {
  id: number;
  kind: UniqueWordKind;
  displayTextUthmani: string;
  textUthmani?: string;
  textUthmaniSimple?: string;
  textImlaeiSimple?: string;
  wordKeyImlaeiSimple?: string | null;
  qpcGlyph?: string | null;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
  missingSurahsCount: number;
  firstVerseKey: string;
  firstLocation: string;
}

export interface UniqueWordSummaryDto {
  id: number;
  kind: UniqueWordKind;
  displayTextUthmani: string;
  textUthmani?: string;
  textUthmaniSimple?: string;
  textImlaeiSimple?: string;
  wordKeyImlaeiSimple?: string | null;
  qpcGlyph?: string | null;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
  missingSurahsCount: number;
  firstVerseKey: string;
  firstLocation: string;
}

export interface UniqueWordSurahItemDto {
  surahNumber: number;
  nameArabic: string;
  occurrencesInSurah: number;
}

export interface UniqueWordSurahsDto {
  id: number;
  kind: UniqueWordKind;
  displayTextUthmani: string;
  surahsCount: number;
  surahs: UniqueWordSurahItemDto[];
}

export interface MissingSurahItemDto {
  surahNumber: number;
  nameArabic: string;
}

export interface UniqueWordMissingSurahsDto {
  id: number;
  kind: UniqueWordKind;
  displayTextUthmani: string;
  missingSurahsCount: number;
  surahs: MissingSurahItemDto[];
}

export interface AyahWordForHighlightDto {
  quranWordId: number;
  wordNumber: number;
  textUthmani: string;
  isAyahMarker: boolean;
}

export interface AyahMatchDto {
  ayahId: number;
  verseKey: string;
  surahNumber: number;
  surahNameArabic: string;
  ayahNumber: number;
  pageNumber: number;
  matchedQuranWordIds: number[];
  words: AyahWordForHighlightDto[];
}

export interface UniqueWordAyahMatchDto extends AyahMatchDto {}

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

export interface UniqueWordListItemViewModel extends UniqueWordListItemDto {
  displayText: string;
}
