export type WordTypeMainType = 'noun' | 'verb' | 'particle' | 'inl';
export type WordTypeCase = 'all' | 'nominative' | 'accusative' | 'genitive' | 'null';
export type WordTypeTense = 'all' | 'past' | 'present' | 'imperative';
export type WordTypeVoice = 'all' | 'active' | 'passive';
export type WordTypeSort = 'occurrences' | 'ayahs' | 'surahs' | 'mushaf-order' | 'alpha';
export type WordTypeDetailView = 'ayahs' | 'surahs';
export type WordTypesLoadStatus = 'idle' | 'loading' | 'selectPrompt' | 'success' | 'empty' | 'error' | 'notFound';

export interface PagedResultDto<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

export interface WordTypeLabelDto { ar: string; }

export interface WordTypeFilterOptionDto {
  code: string;
  label: WordTypeLabelDto;
}

export interface WordTypeSecondaryFilterDto {
  kind: 'case' | 'tense+voice' | 'none';
  options?: WordTypeFilterOptionDto[];
  voiceOptions?: WordTypeFilterOptionDto[];
}

export interface WordTypeChildNodeDto {
  code: string;
  childCode: string;
  label: WordTypeLabelDto;
  count: number;
}

export interface WordTypeTreeNodeDto {
  code: WordTypeMainType;
  label: WordTypeLabelDto;
  count: number;
  secondaryFilter: WordTypeSecondaryFilterDto;
  children: WordTypeChildNodeDto[];
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

export interface WordTypeAyahMatchDto {
  verseKey: string;
  surahNumber: number;
  ayahNumber: number;
  pageNumber: number;
  matchedWordPositions: number[];
  matchedWordIds: number[];
  words: AyahWordForHighlightDto[];
}

export interface AyahWordForHighlightDto {
  quranWordId: number;
  textUthmani: string;
  isAyahMarker: boolean;
}

export interface WordTypeSurahOccurrenceDto {
  surahNumber: number;
  nameArabic: string;
  occurrencesCount: number;
}

export interface WordTypeMissingSurahDto {
  surahNumber: number;
  nameArabic: string;
}

export interface WordTypeSurahsResponseDto {
  surahs: WordTypeSurahOccurrenceDto[];
  missingSurahs: WordTypeMissingSurahDto[];
}

export interface ParsedWordTypesQuery extends WordTypeRowIdentity {
  type: WordTypeMainType;
  childCode: string | null;
  sort: WordTypeSort;
  page: number;
  word: number | null;
  view: WordTypeDetailView;
  detailPage: number;
  location: string | null;
  column: string | null;
}

export interface WordTypesListState {
  status: WordTypesLoadStatus;
  tree: WordTypeTreeDto | null;
  rows: PagedResultDto<WordTypeRowDto> | null;
  query: ParsedWordTypesQuery;
  errorMessage: string;
}

export interface WordTypesDetailState {
  status: WordTypesLoadStatus;
  selectedRow: WordTypeRowIdentity | null;
  view: WordTypeDetailView;
  detailPage: number;
  location: string | null;
  summary: WordTypeSummaryDto | null;
  ayahs: PagedResultDto<WordTypeAyahMatchDto> | null;
  surahs: WordTypeSurahsResponseDto | null;
  errorMessage: string;
}

export const WORD_TYPES_QUERY_KEYS = {
  type: 'type',
  childCode: 'childCode',
  case: 'case',
  tense: 'tense',
  voice: 'voice',
  sort: 'sort',
  page: 'page',
  word: 'word',
  contextCode: 'contextCode',
  view: 'view',
  detailPage: 'detailPage',
  location: 'location',
  column: 'column',
} as const;

export const WORD_TYPES_SELECTION_QUERY_KEYS: readonly string[] = [
  WORD_TYPES_QUERY_KEYS.word,
  WORD_TYPES_QUERY_KEYS.contextCode,
  WORD_TYPES_QUERY_KEYS.view,
  WORD_TYPES_QUERY_KEYS.detailPage,
  WORD_TYPES_QUERY_KEYS.location,
  WORD_TYPES_QUERY_KEYS.column,
];

export const WORD_TYPE_MAIN_TYPES = ['noun', 'verb', 'particle', 'inl'] as const satisfies readonly WordTypeMainType[];
export const WORD_TYPE_CASES = ['all', 'nominative', 'accusative', 'genitive', 'null'] as const satisfies readonly WordTypeCase[];
export const WORD_TYPE_TENSES = ['all', 'past', 'present', 'imperative'] as const satisfies readonly WordTypeTense[];
export const WORD_TYPE_VOICES = ['all', 'active', 'passive'] as const satisfies readonly WordTypeVoice[];
export const WORD_TYPE_SORTS = ['occurrences', 'ayahs', 'surahs', 'mushaf-order', 'alpha'] as const satisfies readonly WordTypeSort[];
export const WORD_TYPE_DETAIL_VIEW_KEYS = ['ayahs', 'surahs'] as const satisfies readonly WordTypeDetailView[];
export const WORD_TYPE_DETAIL_VIEWS = WORD_TYPE_DETAIL_VIEW_KEYS;

export const DEFAULT_WORD_TYPE: WordTypeMainType = 'noun';
export const DEFAULT_WORD_TYPE_CASE: WordTypeCase = 'all';
export const DEFAULT_WORD_TYPE_TENSE: WordTypeTense = 'all';
export const DEFAULT_WORD_TYPE_VOICE: WordTypeVoice = 'all';
export const DEFAULT_WORD_TYPE_SORT: WordTypeSort = 'occurrences';
export const DEFAULT_WORD_TYPES_PAGE = 1;
export const WORD_TYPES_PAGE_SIZE = 25;
export const DEFAULT_WORD_TYPES_DETAIL_VIEW: WordTypeDetailView = 'ayahs';
export const DEFAULT_WORD_TYPES_DETAIL_PAGE = 1;
export const WORD_TYPES_DETAIL_PAGE_SIZE = 25;

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
  return (WORD_TYPE_SORTS as readonly string[]).includes(value as string);
}

export function isWordTypeDetailView(value: unknown): value is WordTypeDetailView {
  return (WORD_TYPE_DETAIL_VIEWS as readonly string[]).includes(value as string);
}
