import { PhraseContextBranchesResponse } from '../../../../core/api/generated/models/phrase-context-branches-response';
import { PhraseContextOccurrenceDto } from '../../../../core/api/generated/models/phrase-context-occurrence-dto';
import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';

import { PhraseLoadStatus, PhraseTextMode } from './phrase-repetitions.models';
import { PhraseResolutionViewState } from './phrase-query.models';

export interface PhraseContextUrlState {
  readonly build: string | null;
  readonly mode: PhraseTextMode;
  readonly q: string;
  readonly resolution: string | null;
  readonly before: string | null;
  readonly after: string | null;
  readonly contextsPage: number;
}

export interface ParsedPhraseContextUrlState {
  readonly state: PhraseContextUrlState;
  readonly invalid: boolean;
}

export interface PhraseContextState {
  readonly route: PhraseContextUrlState;
  readonly routeInvalid: boolean;
  readonly workspaceDraftFresh: boolean;
  readonly mode: PhraseTextMode;
  readonly capabilitiesStatus: PhraseLoadStatus;
  readonly capabilities: PhraseSearchCapabilitiesResponse | null;
  readonly resolution: PhraseResolutionViewState;
  readonly branchesStatus: PhraseLoadStatus;
  readonly branches: PhraseContextBranchesResponse | null;
  readonly previousOptions: PhraseContextBranchesResponse['previous']['options'];
  readonly followingOptions: PhraseContextBranchesResponse['following']['options'];
  readonly resultsStatus: PhraseLoadStatus;
  readonly occurrences: readonly PhraseContextOccurrenceDto[];
  readonly resultsPage: number;
  readonly resultsPageSize: number;
  readonly occurrencesTotalCount: number;
  readonly errorMessage: string;
  readonly notice: string;
  readonly sessionOnly: boolean;
  readonly focusTarget: PhraseContextFocusTarget | null;
}

export type PhraseContextFocusTarget =
  | 'previous'
  | 'following'
  | 'previous-more'
  | 'following-more';

export const DEFAULT_PHRASE_CONTEXT_URL_STATE: PhraseContextUrlState = {
  build: null,
  mode: 'simple',
  q: '',
  resolution: null,
  before: null,
  after: null,
  contextsPage: 1,
};

export const PHRASE_CONTEXT_BRANCH_PAGE_SIZE = 25;
export const PHRASE_CONTEXT_RESULT_PAGE_SIZE = 200;
