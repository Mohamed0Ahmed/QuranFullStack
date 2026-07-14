export type RootSort = 'mushaf-order' | 'occurrences' | 'alpha';

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
  };
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
} as const;

export const ROOTS_SELECTION_QUERY_KEYS: readonly string[] = [
  ROOTS_QUERY_KEYS.root,
  ROOTS_QUERY_KEYS.view,
  ROOTS_QUERY_KEYS.column,
  ROOTS_QUERY_KEYS.wordView,
  ROOTS_QUERY_KEYS.surahView,
  ROOTS_QUERY_KEYS.detailPage,
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

export const ROOT_SORT_KEYS = ['mushaf-order', 'occurrences', 'alpha'] as const satisfies readonly RootSort[];
export const ROOT_WORD_VIEW_KEYS = ['simple', 'tashkeel'] as const satisfies readonly RootWordView[];
export const ROOT_SURAHS_VIEW_KEYS = ['mentioned', 'missing'] as const satisfies readonly RootSurahView[];
export const ROOT_VIEW_KEYS = ['words', 'ayahs', 'surahs', 'lemmas', 'stems'] as const satisfies readonly RootView[];
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
  column: string | null;
  wordView: RootWordView;
  surahView: RootSurahView;
  detailPage: number;
}
