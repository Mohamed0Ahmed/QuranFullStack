export type WordTypeMainType = 'noun' | 'verb' | 'particle' | 'inl';
export type WordTypeCase = 'all' | 'nominative' | 'accusative' | 'genitive' | 'null';
export type WordTypeTense = 'all' | 'past' | 'present' | 'imperative';
export type WordTypeVoice = 'all' | 'active' | 'passive';
export type WordTypeSort = 'occurrences' | 'ayahs' | 'surahs' | 'mushaf-order' | 'alpha';
export type WordTypeTableView = 'words' | 'roots' | 'stems' | 'lemmas';
export type WordTypeDetailView = 'words' | 'ayahs' | 'surahs';
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

// Wire row returned by GET .../word-types/table (kind:"word"). The backend leaves inactive
// grammatical filters null, while the URL/detail identity represents them canonically as "all".
export interface WordTableRowDto {
  kind: 'word';
  tashkeelWordId: number;
  contextCode: string;
  case: WordTypeCase | null;
  tense: WordTypeTense | null;
  voice: WordTypeVoice | null;
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

export interface RootTableRowDto {
  kind: 'root';
  rootId: number;
  displayText: string;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
}

export interface StemTableRowDto {
  kind: 'stem';
  stemId: number;
  displayText: string;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
}

export interface LemmaTableRowDto {
  kind: 'lemma';
  lemmaId: number;
  displayText: string;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
}

export type WordTypeTableRowDto = WordTableRowDto | RootTableRowDto | StemTableRowDto | LemmaTableRowDto;

// The grouped table rows (everything but the word row), keyed by their numeric dimension identity.
export type WordTypeGroupedTableRowDto = RootTableRowDto | StemTableRowDto | LemmaTableRowDto;

export function groupedTableRowId(row: WordTypeGroupedTableRowDto): number {
  switch (row.kind) {
    case 'root': return row.rootId;
    case 'stem': return row.stemId;
    case 'lemma': return row.lemmaId;
  }
}

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
  tableView: WordTypeTableView;
  sort: WordTypeSort;
  page: number;
  word: number | null;
  root: number | null;
  stem: number | null;
  lemma: number | null;
  view: WordTypeDetailView;
  detailPage: number;
  location: string | null;
  column: string | null;
}

export interface WordTypesListState {
  status: WordTypesLoadStatus;
  tree: WordTypeTreeDto | null;
  // Preserve every discriminated /table variant (word rows and grouped root/stem/lemma rows) in
  // feature state; the table component branches on `tableView` to render each kind.
  rows: PagedResultDto<WordTypeTableRowDto> | null;
  query: ParsedWordTypesQuery;
  errorMessage: string;
}

export const WORD_TYPES_QUERY_KEYS = {
  type: 'type',
  childCode: 'childCode',
  tableView: 'tableView',
  case: 'case',
  tense: 'tense',
  voice: 'voice',
  sort: 'sort',
  page: 'page',
  word: 'word',
  contextCode: 'contextCode',
  root: 'root',
  stem: 'stem',
  lemma: 'lemma',
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
export const WORD_TYPES_PAGE_SIZE = 25;
export const DEFAULT_WORD_TYPES_DETAIL_VIEW: WordTypeDetailView = 'ayahs';
export const DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW: WordTypeDetailView = 'words';
export const DEFAULT_WORD_TYPES_DETAIL_PAGE = 1;
export const WORD_TYPES_DETAIL_PAGE_SIZE = 25;

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
  return (WORD_TYPE_SORTS as readonly string[]).includes(value as string);
}

export function isWordTypeTableView(value: unknown): value is WordTypeTableView {
  return (WORD_TYPE_TABLE_VIEWS as readonly string[]).includes(value as string);
}

export function isWordTypeDetailView(value: unknown): value is WordTypeDetailView {
  return (WORD_TYPE_DETAIL_VIEWS as readonly string[]).includes(value as string);
}
