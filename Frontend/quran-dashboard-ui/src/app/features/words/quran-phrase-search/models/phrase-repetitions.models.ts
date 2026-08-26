import { PhraseOccurrencePageResponse } from '../../../../core/api/generated/models/phrase-occurrence-page-response';
import { PhraseRepetitionsPageResponse } from '../../../../core/api/generated/models/phrase-repetitions-page-response';
import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';

export type PhraseTextMode = 'simple' | 'tashkil';
export type PhraseRepetitionSort = 'occurrences' | 'occurrences-asc' | 'mushaf-order';

export type PhraseLoadStatus =
  | 'idle'
  | 'loading'
  | 'refreshing'
  | 'success'
  | 'empty'
  | 'invalid'
  | 'error'
  | 'stale'
  | 'unavailable';

export interface PhraseRepetitionsUrlState {
  readonly build: string | null;
  readonly mode: PhraseTextMode;
  readonly length: number;
  readonly sort: PhraseRepetitionSort;
  readonly page: number;
  readonly phrase: number | null;
  readonly occPage: number;
}

export interface ParsedPhraseRepetitionsUrlState {
  readonly state: PhraseRepetitionsUrlState;
  readonly invalid: boolean;
}

export interface PhraseRepetitionsState {
  readonly route: PhraseRepetitionsUrlState;
  readonly routeInvalid: boolean;
  readonly capabilitiesStatus: PhraseLoadStatus;
  readonly capabilities: PhraseSearchCapabilitiesResponse | null;
  readonly listStatus: PhraseLoadStatus;
  readonly list: PhraseRepetitionsPageResponse | null;
  readonly occurrencesStatus: PhraseLoadStatus;
  readonly occurrences: PhraseOccurrencePageResponse | null;
  readonly errorMessage: string;
  readonly occurrencesErrorMessage: string;
  readonly indexNotice: string;
}

export const DEFAULT_PHRASE_REPETITIONS_URL_STATE: PhraseRepetitionsUrlState = {
  build: null,
  mode: 'simple',
  length: 2,
  sort: 'occurrences',
  page: 1,
  phrase: null,
  occPage: 1,
};

export const PHRASE_REPETITIONS_PAGE_SIZE = 1000;
export const PHRASE_OCCURRENCES_PAGE_SIZE = 25;

export const PHRASE_REPETITION_SORT_OPTIONS: readonly {
  readonly value: PhraseRepetitionSort;
  readonly label: string;
}[] = [
  { value: 'occurrences', label: 'الأكثر تكرارًا' },
  { value: 'occurrences-asc', label: 'الأقل تكرارًا' },
  { value: 'mushaf-order', label: 'ترتيب أول موضع' },
];

export const PHRASE_TEXT_MODE_LABELS: Readonly<Record<PhraseTextMode, string>> = {
  simple: 'بدون تشكيل',
  tashkil: 'بالتشكيل',
};

export function isPhraseTextMode(value: string | null | undefined): value is PhraseTextMode {
  return value === 'simple' || value === 'tashkil';
}

export function isPhraseRepetitionSort(
  value: string | null | undefined,
): value is PhraseRepetitionSort {
  return value === 'occurrences' || value === 'occurrences-asc' || value === 'mushaf-order';
}
