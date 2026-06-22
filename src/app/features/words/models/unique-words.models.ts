/**
 * Unique Words feature models. Backend JSON shapes are mirrored here as
 * frontend DTOs. `ApiResponse<T>` unwrapping and state mapping live in the
 * facade; components consume page-ready state from the facade and do not
 * unwrap `ApiResponse<T>` directly (frontend API integration contract).
 */

/** The two Unique Words explorer modes. Stable route keys, not translated labels. */
export type UniqueWordKind = 'tashkeel' | 'simple';

/** Unique Words list sort options. */
export type UniqueWordSort = 'mushaf-order' | 'occurrences' | 'alpha';

/** Modal drill-down views for a selected unique word. */
export type WordDrilldownView = 'surahs' | 'missing' | 'ayahs';

/** Minimal reusable paged result contract mirroring the backend shape. */
export interface PagedResultDto<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

/** One item in the Unique Words list. `displayTextUthmani` is the primary label. */
export interface UniqueWordListItemDto {
  id: number;
  kind: UniqueWordKind;
  displayTextUthmani: string;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
  /** Derived as 114 - surahsCount. */
  missingSurahsCount: number;
  firstVerseKey: string;
  firstLocation: string;
}

/** Summary of a selected unique word, used to restore modal state. */
export interface UniqueWordSummaryDto {
  id: number;
  kind: UniqueWordKind;
  displayTextUthmani: string;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
  missingSurahsCount: number;
  firstVerseKey: string;
  firstLocation: string;
}

/** A surah where the selected unique word is mentioned, with per-surah count. */
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

/** A surah where the selected unique word does NOT appear. */
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

/** A single Quran word inside an ayah, for display and ID-based highlighting. */
export interface AyahWordForHighlightDto {
  quranWordId: number;
  wordNumber: number;
  textUthmani: string;
  isAyahMarker: boolean;
}

/** One ayah with exact matched word IDs for the selected unique word. */
export interface UniqueWordAyahMatchDto {
  ayahId: number;
  verseKey: string;
  surahNumber: number;
  surahNameArabic: string;
  ayahNumber: number;
  /** Exact quran_words.id values; only these are highlighted. */
  matchedQuranWordIds: number[];
  words: AyahWordForHighlightDto[];
}

/** Combined view-state status for facade-driven regions. */
export type LoadStatus = 'idle' | 'loading' | 'success' | 'empty' | 'error' | 'notFound';

/** List explorer state consumed by the page component. */
export interface UniqueWordsListState {
  status: LoadStatus;
  items: UniqueWordListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  mode: UniqueWordKind;
  search: string;
  sort: UniqueWordSort;
  errorMessage: string;
}

/** Selected-word modal drill-down state consumed by the page/modal. */
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

/** Stable route-key constants; labels live in `unique-words.labels.ts`. */
export const UNIQUE_WORD_KIND_KEYS = ['tashkeel', 'simple'] as const satisfies readonly UniqueWordKind[];
export const UNIQUE_WORD_SORT_KEYS = ['mushaf-order', 'occurrences', 'alpha'] as const satisfies readonly UniqueWordSort[];
export const WORD_DRILLDOWN_VIEW_KEYS = ['surahs', 'missing', 'ayahs'] as const satisfies readonly WordDrilldownView[];

export const DEFAULT_UNIQUE_WORD_KIND: UniqueWordKind = 'tashkeel';
export const DEFAULT_UNIQUE_WORD_SORT: UniqueWordSort = 'mushaf-order';
export const DEFAULT_LIST_PAGE = 1;
export const DEFAULT_LIST_PAGE_SIZE = 50;
export const DEFAULT_AYAH_PAGE = 1;
export const DEFAULT_AYAH_PAGE_SIZE = 20;
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

/**
 * Stable query-param keys for the explorer. List state (`search`/`sort`/`page`)
 * and modal state (`word`/`view`/`ap`) live together on the mode route. Closing
 * the modal clears only the modal keys.
 */
export const UNIQUE_WORDS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  word: 'word',
  view: 'view',
  ayahPage: 'ap',
} as const;

/** Modal-only query-param keys; cleared on modal close while list state is kept. */
export const MODAL_QUERY_KEYS: readonly string[] = [
  UNIQUE_WORDS_QUERY_KEYS.word,
  UNIQUE_WORDS_QUERY_KEYS.view,
  UNIQUE_WORDS_QUERY_KEYS.ayahPage,
] as const;

/**
 * Parsed explorer query state. Modal fields are `null` when absent (vs. list
 * fields which always resolve to their documented defaults).
 */
export interface ParsedUniqueWordsQuery {
  search: string;
  sort: UniqueWordSort;
  page: number;
  /** Stable unique-word ID when a modal is restored, otherwise `null`. */
  wordId: number | null;
  /** Active modal view when a modal is restored, otherwise `null`. */
  view: WordDrilldownView | null;
  /** Ayah-match page when `view === 'ayahs'`, otherwise `null`/default. */
  ayahPage: number | null;
}
