/**
 * Roots Explorer (Feature 015) models — DTO interfaces mirroring
 * `specs/015-roots-explorer/contracts/roots-api.md`, plus view models, list and
 * panel state, query-key constants, defaults, and type guards. Modeled on
 * `unique-words.models.ts`. Backend ids are never rendered; they exist for
 * selection, the `root=` URL param, and deep-links only.
 */

import { AyahMatchDto } from './unique-words.models';

export type RootSort = 'mushaf-order' | 'occurrences' | 'alpha';

/** Sub-view of the الكلمات tab: without/with tashkeel. */
export type RootWordView = 'simple' | 'tashkeel';

/** Sub-view of the السور tab: mentioned / missing. */
export type RootSurahView = 'mentioned' | 'missing';

/** Active panel tab. There is intentionally no overview ("نظرة عامة") view. */
export type RootView = 'words' | 'ayahs' | 'surahs' | 'lemmas' | 'stems';

export interface PagedResultDto<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

export interface RootListItemDto {
  /** URL/selection only — never displayed. */
  id: number;
  rootText: string;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
  simpleWordsCount: number;
  tashkeelWordsCount: number;
  lemmasCount: number;
  stemsCount: number;
  firstVerseKey: string;
}

/** Same shape as the list item; used to restore panel state from a URL. */
export interface RootSummaryDto {
  id: number;
  rootText: string;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
  simpleWordsCount: number;
  tashkeelWordsCount: number;
  lemmasCount: number;
  stemsCount: number;
  firstVerseKey: string;
}

export interface RootWordItemDto {
  /** Deep-link target into Feature 014 (simple/tashkeel). Never displayed. */
  uniqueWordId: number;
  kind: RootWordView;
  displayTextUthmani: string;
  occurrencesCount: number;
  firstVerseKey: string;
}

export interface RootAyahMatchDto extends AyahMatchDto {}

export interface RootSurahItemDto {
  surahNumber: number;
  nameArabic: string;
  occurrencesInSurah: number;
}

export interface RootSurahsDto {
  id: number;
  rootText: string;
  surahsCount: number;
  surahs: RootSurahItemDto[];
}

export interface MissingSurahItemDto {
  surahNumber: number;
  nameArabic: string;
}

export interface RootMissingSurahsDto {
  id: number;
  rootText: string;
  missingSurahsCount: number;
  surahs: MissingSurahItemDto[];
}

export interface RootLemmaItemDto {
  /** Retained for future linking; not interactive now. */
  lemmaId: number;
  lemmaText: string;
  occurrencesCount: number;
}

export interface RootLemmasDto {
  id: number;
  rootText: string;
  lemmasCount: number;
  lemmas: RootLemmaItemDto[];
}

export interface RootStemItemDto {
  /** Retained for future linking; not interactive now. */
  stemId: number;
  stemText: string;
  occurrencesCount: number;
}

export interface RootStemsDto {
  id: number;
  rootText: string;
  stemsCount: number;
  stems: RootStemItemDto[];
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
  /** Display label for the root text (same as rootText today; explicit for the view). */
  displayText: string;
}

/** Maps a list row to the summary shape used by the detail panel and URL restore. */
export function toRootSummary(root: RootListItemDto): RootSummaryDto {
  return {
    id: root.id,
    rootText: root.rootText,
    occurrencesCount: root.occurrencesCount,
    ayahsCount: root.ayahsCount,
    surahsCount: root.surahsCount,
    simpleWordsCount: root.simpleWordsCount,
    tashkeelWordsCount: root.tashkeelWordsCount,
    lemmasCount: root.lemmasCount,
    stemsCount: root.stemsCount,
    firstVerseKey: root.firstVerseKey,
  };
}

// --- Query-param keys (stable route keys, not translated labels) ---
export const ROOTS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  root: 'root',
  view: 'view',
  wordView: 'wordView',
  surahView: 'surahView',
  detailPage: 'detailPage',
} as const;

/** Selection/panel params cleared when the selection is cleared. */
export const ROOTS_SELECTION_QUERY_KEYS: readonly string[] = [
  ROOTS_QUERY_KEYS.root,
  ROOTS_QUERY_KEYS.view,
  ROOTS_QUERY_KEYS.wordView,
  ROOTS_QUERY_KEYS.surahView,
  ROOTS_QUERY_KEYS.detailPage,
] as const;

// --- Defaults (mirrors contracts; pageSize/detailPageSize are fixed) ---
export const DEFAULT_ROOT_SORT: RootSort = 'mushaf-order';
export const DEFAULT_ROOT_VIEW: RootView = 'ayahs';
export const DEFAULT_ROOT_WORD_VIEW: RootWordView = 'simple';
export const DEFAULT_ROOT_SURAHS_VIEW: RootSurahView = 'mentioned';
export const DEFAULT_ROOTS_LIST_PAGE = 1;
export const ROOTS_LIST_PAGE_SIZE = 1000;
export const DEFAULT_ROOT_DETAIL_PAGE = 1;
export const ROOT_DETAIL_PAGE_SIZE = 100;
export const TOTAL_SURAHS = 114;

// --- Key sets / type guards ---
export const ROOT_SORT_KEYS = ['mushaf-order', 'occurrences', 'alpha'] as const satisfies readonly RootSort[];
export const ROOT_WORD_VIEW_KEYS = ['simple', 'tashkeel'] as const satisfies readonly RootWordView[];
export const ROOT_SURAHS_VIEW_KEYS = ['mentioned', 'missing'] as const satisfies readonly RootSurahView[];
export const ROOT_VIEW_KEYS = ['words', 'ayahs', 'surahs', 'lemmas', 'stems'] as const satisfies readonly RootView[];
/** Views that carry a paginated detail page. */
export const PAGINATED_ROOT_VIEWS: readonly RootView[] = ['ayahs', 'words'];

export function isRootSort(value: unknown): value is RootSort {
  return (ROOT_SORT_KEYS as readonly string[]).includes(value as string);
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
  rootId: number | null;
  view: RootView;
  wordView: RootWordView;
  surahView: RootSurahView;
  detailPage: number;
}
