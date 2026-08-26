import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';
import { PhraseSimilarityGroupDto } from '../../../../core/api/generated/models/phrase-similarity-group-dto';
import { PhraseSimilarityMatchDto } from '../../../../core/api/generated/models/phrase-similarity-match-dto';

import { PhraseLoadStatus, PhraseTextMode } from './phrase-repetitions.models';
import { PhraseResolutionStatus } from './phrase-query.models';

export type PhraseSimilaritySource = 'manual' | 'global';
export type PhraseSimilaritySort = 'relevance';

export interface PhraseSimilarityUrlState {
  readonly build: string | null;
  readonly source: PhraseSimilaritySource;
  readonly q: string;
  readonly resolution: string | null;
  readonly mode: PhraseTextMode;
  readonly length: number;
  readonly min: number;
  readonly sort: PhraseSimilaritySort;
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
  readonly groups: readonly PhraseSimilarityGroupDto[];
  readonly matches: readonly PhraseSimilarityMatchDto[];
  readonly totalCount: number;
  readonly selectedAnchor: PhraseSimilarityGroupDto | null;
  readonly errorMessage: string;
  readonly notice: string;
  readonly sessionOnly: boolean;
}

export const DEFAULT_PHRASE_SIMILARITY_URL_STATE: PhraseSimilarityUrlState = {
  build: null,
  source: 'global',
  q: '',
  resolution: null,
  mode: 'simple',
  length: 4,
  min: 50,
  sort: 'relevance',
  page: 1,
};

export const PHRASE_SIMILARITY_PAGE_SIZE = 25;
