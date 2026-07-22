export type WordTypeMainType = 'noun' | 'verb' | 'particle' | 'inl';
export type WordTypeCase = 'all' | 'nominative' | 'accusative' | 'genitive' | 'null';
export type WordTypeTense = 'all' | 'past' | 'present' | 'imperative';
export type WordTypeVoice = 'all' | 'active' | 'passive';
type WordTypeCountSortColumn = 'occurrences' | 'ayahs' | 'surahs';
type WordTypeTextSortColumn = 'alpha';
export type WordTypeSortColumnKey = WordTypeCountSortColumn | WordTypeTextSortColumn;

export type WordTypeSort =
  | MushafOrderSort
  | CanonicalSortTokens<WordTypeCountSortColumn, 'desc'>
  | CanonicalSortTokens<WordTypeTextSortColumn, 'asc'>;

export type WordTypeTableView = 'words' | 'roots' | 'stems' | 'lemmas';
export type WordTypeDetailView = 'words' | 'ayahs' | 'surahs';
export type WordTypePresenceDimension = 'root' | 'stem' | 'lemma';
export type WordTypesLoadStatus = 'idle' | 'loading' | 'selectPrompt' | 'success' | 'empty' | 'error' | 'notFound';

import type {
  AyahWordForHighlightDto,
  WordTypeAyahMatchDto,
  WordTypeChildNodeDto,
  WordTypeFilterOptionDto,
  WordTypeLabelDto,
  WordTypeMissingSurahDto,
  WordTypeScopeCountsDto,
  WordTypeSecondaryFilterDto as WordTypeSecondaryFilterWireDto,
  WordTypeSurahOccurrenceDto,
  WordTypeSurahsResponse as WordTypeSurahsResponseDto,
  WordTypeTreeNodeDto as WordTypeTreeNodeWireDto,
  LemmaTableRowDto as LemmaTableRowWireDto,
  RootTableRowDto as RootTableRowWireDto,
  StemTableRowDto as StemTableRowWireDto,
  WordTableRowDto as WordTableRowWireDto,
} from '../../../core/api/generated/models';
import type { PagedResultDto } from '../../../core/data-access/paged-result.model';
import {
  CanonicalSortTokens,
  ExplorerSortColumn,
  MUSHAF_ORDER_SORT,
  MushafOrderSort,
  canonicalSortTokens,
  canonicalizeSortToken,
} from './explorer-sort';
import { WORDS_SHARED_HEADERS } from './words-shared.labels';

export type {
  AyahWordForHighlightDto,
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeChildNodeDto,
  WordTypeFilterOptionDto,
  WordTypeLabelDto,
  WordTypeMissingSurahDto,
  WordTypeScopeCountsDto,
  WordTypeSurahOccurrenceDto,
  WordTypeSurahsResponseDto,
};

export interface WordTypeSecondaryFilterDto extends Omit<WordTypeSecondaryFilterWireDto, 'kind'> {
  kind: 'case' | 'tense+voice' | 'none';
}

export interface WordTypeTreeNodeDto extends Omit<WordTypeTreeNodeWireDto, 'code' | 'secondaryFilter'> {
  code: WordTypeMainType;
  secondaryFilter: WordTypeSecondaryFilterDto;
}

export interface WordTypeTreeDto { mainTypes: WordTypeTreeNodeDto[]; }

export interface WordTypeRowIdentity {
  tashkeelWordId: number;
  contextCode: string;
  case: WordTypeCase;
  tense: WordTypeTense;
  voice: WordTypeVoice;
}

export interface WordTypeRowDto extends WordTypeRowIdentity {
  displayText: string;
  typeCode: string;
  typeLabel: WordTypeLabelDto;
  broadLabel: WordTypeLabelDto;
  caseOrFeature: string | null;
  rootText: string | null;
  lemmaText: string | null;
  stemText: string | null;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
}

export type WordTypeSummaryDto = WordTypeRowDto;

export interface WordTableRowDto extends Omit<WordTableRowWireDto, 'kind' | 'case' | 'tense' | 'voice'> {
  kind: 'word';
  case: WordTypeCase | null;
  tense: WordTypeTense | null;
  voice: WordTypeVoice | null;
}

export interface RootTableRowDto extends Omit<RootTableRowWireDto, 'kind'> {
  kind: 'root';
}

export interface StemTableRowDto extends Omit<StemTableRowWireDto, 'kind'> {
  kind: 'stem';
}

export interface LemmaTableRowDto extends Omit<LemmaTableRowWireDto, 'kind'> {
  kind: 'lemma';
}

export type WordTypeTableRowDto = WordTableRowDto | RootTableRowDto | StemTableRowDto | LemmaTableRowDto;

export type WordTypeGroupedTableRowDto = RootTableRowDto | StemTableRowDto | LemmaTableRowDto;

export function groupedTableRowId(row: WordTypeGroupedTableRowDto): number {
  switch (row.kind) {
    case 'root': return row.rootId;
    case 'stem': return row.stemId;
    case 'lemma': return row.lemmaId;
  }
}

export interface ParsedWordTypesQuery extends WordTypeRowIdentity {
  type: WordTypeMainType;
  childCode: string | null;
  tableView: WordTypeTableView;
  search: string | null;
  hasRoot: boolean | null;
  hasStem: boolean | null;
  hasLemma: boolean | null;
  sort: WordTypeSort;
  page: number;
  word: number | null;
  root: number | null;
  stem: number | null;
  lemma: number | null;
  detailType: WordTypeMainType | null;
  detailChildCode: string | null;
  detailCase: WordTypeCase | null;
  detailTense: WordTypeTense | null;
  detailVoice: WordTypeVoice | null;
  view: WordTypeDetailView;
  detailPage: number;
  location: string | null;
  column: string | null;
}

export interface WordTypesListState {
  status: WordTypesLoadStatus;
  tree: WordTypeTreeDto | null;
  rows: PagedResultDto<WordTypeTableRowDto> | null;
  query: ParsedWordTypesQuery;
  errorMessage: string;
}

export type WordTypeScopeCountsStatus = 'idle' | 'loading' | 'success' | 'error';

export interface WordTypesScopeCountsState {
  status: WordTypeScopeCountsStatus;
  counts: WordTypeScopeCountsDto | null;
}

export const WORD_TYPES_QUERY_KEYS = {
  type: 'type',
  childCode: 'childCode',
  tableView: 'tableView',
  case: 'case',
  tense: 'tense',
  voice: 'voice',
  search: 'search',
  hasRoot: 'hasRoot',
  hasStem: 'hasStem',
  hasLemma: 'hasLemma',
  sort: 'sort',
  page: 'page',
  word: 'word',
  contextCode: 'contextCode',
  root: 'root',
  stem: 'stem',
  lemma: 'lemma',
  detailType: 'detailType',
  detailChildCode: 'detailChildCode',
  detailCase: 'detailCase',
  detailTense: 'detailTense',
  detailVoice: 'detailVoice',
  view: 'view',
  detailPage: 'detailPage',
  location: 'location',
  column: 'column',
} as const;

export const WORD_TYPES_SELECTION_QUERY_KEYS: readonly string[] = [
  WORD_TYPES_QUERY_KEYS.word,
  WORD_TYPES_QUERY_KEYS.contextCode,
  WORD_TYPES_QUERY_KEYS.root,
  WORD_TYPES_QUERY_KEYS.stem,
  WORD_TYPES_QUERY_KEYS.lemma,
  WORD_TYPES_QUERY_KEYS.detailType,
  WORD_TYPES_QUERY_KEYS.detailChildCode,
  WORD_TYPES_QUERY_KEYS.detailCase,
  WORD_TYPES_QUERY_KEYS.detailTense,
  WORD_TYPES_QUERY_KEYS.detailVoice,
  WORD_TYPES_QUERY_KEYS.view,
  WORD_TYPES_QUERY_KEYS.detailPage,
  WORD_TYPES_QUERY_KEYS.location,
  WORD_TYPES_QUERY_KEYS.column,
];

export const WORD_TYPE_MAIN_TYPES = ['noun', 'verb', 'particle', 'inl'] as const satisfies readonly WordTypeMainType[];
export const WORD_TYPE_CASES = ['all', 'nominative', 'accusative', 'genitive', 'null'] as const satisfies readonly WordTypeCase[];
export const WORD_TYPE_TENSES = ['all', 'past', 'present', 'imperative'] as const satisfies readonly WordTypeTense[];
export const WORD_TYPE_VOICES = ['all', 'active', 'passive'] as const satisfies readonly WordTypeVoice[];
export const WORD_TYPE_SORT_COLUMNS = {
  alpha: { key: 'alpha', natural: 'asc', label: 'أبجدي' },
  occurrences: { key: 'occurrences', natural: 'desc', label: WORDS_SHARED_HEADERS.occurrences },
  ayahs: { key: 'ayahs', natural: 'desc', label: WORDS_SHARED_HEADERS.ayahs },
  surahs: { key: 'surahs', natural: 'desc', label: WORDS_SHARED_HEADERS.surahs },
} as const satisfies Record<WordTypeSortColumnKey, ExplorerSortColumn<WordTypeSortColumnKey>>;

export const WORD_TYPE_SORT_COLUMN_LIST: readonly ExplorerSortColumn<WordTypeSortColumnKey>[] =
  Object.values(WORD_TYPE_SORT_COLUMNS);

export const WORD_TYPE_SORTS: readonly WordTypeSort[] = [
  MUSHAF_ORDER_SORT,
  ...canonicalSortTokens(WORD_TYPE_SORT_COLUMN_LIST),
] as readonly WordTypeSort[];
export const WORD_TYPE_TABLE_VIEWS = ['words', 'roots', 'stems', 'lemmas'] as const satisfies readonly WordTypeTableView[];
export const WORD_TYPE_DETAIL_VIEW_KEYS = ['ayahs', 'surahs'] as const satisfies readonly WordTypeDetailView[];
export const WORD_TYPE_DETAIL_VIEWS = ['words', 'ayahs', 'surahs'] as const satisfies readonly WordTypeDetailView[];

export const DEFAULT_WORD_TYPE: WordTypeMainType = 'noun';
export const DEFAULT_WORD_TYPE_CASE: WordTypeCase = 'all';
export const DEFAULT_WORD_TYPE_TENSE: WordTypeTense = 'all';
export const DEFAULT_WORD_TYPE_VOICE: WordTypeVoice = 'all';
export const DEFAULT_WORD_TYPE_SORT: WordTypeSort = 'occurrences';
export const DEFAULT_WORD_TYPE_TABLE_VIEW: WordTypeTableView = 'words';
export const DEFAULT_WORD_TYPES_PAGE = 1;
export const WORD_TYPES_PAGE_SIZE = 1000;
export const DEFAULT_WORD_TYPES_DETAIL_VIEW: WordTypeDetailView = 'ayahs';
export const DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW: WordTypeDetailView = 'words';
export const DEFAULT_WORD_TYPES_DETAIL_PAGE = 1;
export const WORD_TYPES_DETAIL_PAGE_SIZE = 100;

export function normalizeWordTableRow(row: WordTableRowDto): WordTypeRowDto {
  const { kind: _kind, case: caseValue, tense, voice, ...word } = row;
  return {
    ...word,
    case: caseValue ?? DEFAULT_WORD_TYPE_CASE,
    tense: tense ?? DEFAULT_WORD_TYPE_TENSE,
    voice: voice ?? DEFAULT_WORD_TYPE_VOICE,
  };
}

export function isWordTypeMainType(value: unknown): value is WordTypeMainType {
  return (WORD_TYPE_MAIN_TYPES as readonly string[]).includes(value as string);
}

export function isWordTypeCase(value: unknown): value is WordTypeCase {
  return (WORD_TYPE_CASES as readonly string[]).includes(value as string);
}

export function isWordTypeTense(value: unknown): value is WordTypeTense {
  return (WORD_TYPE_TENSES as readonly string[]).includes(value as string);
}

export function isWordTypeVoice(value: unknown): value is WordTypeVoice {
  return (WORD_TYPE_VOICES as readonly string[]).includes(value as string);
}

export function isWordTypeSort(value: unknown): value is WordTypeSort {
  return (
    typeof value === 'string' && canonicalizeSortToken(value, WORD_TYPE_SORT_COLUMN_LIST) === value
  );
}

export function normalizeWordTypeSort(value: string | null | undefined): WordTypeSort {
  const canonical = canonicalizeSortToken(value, WORD_TYPE_SORT_COLUMN_LIST);
  return canonical !== null && isWordTypeSort(canonical) ? canonical : DEFAULT_WORD_TYPE_SORT;
}

export function isWordTypeTableView(value: unknown): value is WordTypeTableView {
  return (WORD_TYPE_TABLE_VIEWS as readonly string[]).includes(value as string);
}

export function isWordTypeDetailView(value: unknown): value is WordTypeDetailView {
  return (WORD_TYPE_DETAIL_VIEWS as readonly string[]).includes(value as string);
}
