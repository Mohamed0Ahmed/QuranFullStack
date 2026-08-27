import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';
import { PhraseSimilarityAyahDto } from '../../../../core/api/generated/models/phrase-similarity-ayah-dto';
import { PhraseSimilarityPhraseDto } from '../../../../core/api/generated/models/phrase-similarity-phrase-dto';

import { PhraseLoadStatus, PhraseTextMode } from './phrase-repetitions.models';
import { PhraseResolutionStatus } from './phrase-query.models';

export interface PhraseSimilarityUrlState {
  readonly build: string | null;
  readonly q: string;
  readonly resolution: string | null;
  readonly mode: PhraseTextMode;
  readonly length: number;
  readonly min: number;
  readonly page: number;
}

export interface ParsedPhraseSimilarityUrlState {
  readonly state: PhraseSimilarityUrlState;
  readonly invalid: boolean;
}

export interface PhraseSimilarityState {
  readonly route: PhraseSimilarityUrlState;
  readonly routeInvalid: boolean;
  readonly capabilitiesStatus: PhraseLoadStatus;
  readonly capabilities: PhraseSearchCapabilitiesResponse | null;
  readonly resolutionStatus: PhraseResolutionStatus;
  readonly candidates: readonly PhraseResolutionCandidateDto[];
  readonly resultsStatus: PhraseLoadStatus;
  readonly ayahs: readonly PhraseSimilarityAyahDto[];
  readonly totalAyahCount: number;
  readonly totalOccurrenceCount: number;
  readonly queryPhrase: PhraseSimilarityPhraseDto | null;
  readonly errorMessage: string;
  readonly notice: string;
  readonly sessionOnly: boolean;
}

export const DEFAULT_PHRASE_SIMILARITY_URL_STATE: PhraseSimilarityUrlState = {
  build: null,
  q: '',
  resolution: null,
  mode: 'simple',
  length: 4,
  min: 60,
  page: 1,
};

export const PHRASE_SIMILARITY_AYAH_PAGE_SIZE = 100;
