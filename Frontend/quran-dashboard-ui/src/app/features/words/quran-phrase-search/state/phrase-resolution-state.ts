import { PhraseContextBranchesResponse } from '../../../../core/api/generated/models/phrase-context-branches-response';
import { PhraseQueryResolutionResponseApiResponse } from '../../../../core/api/generated/models/phrase-query-resolution-response-api-response';
import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';

import { PhraseResolutionViewState } from '../models/phrase-query.models';
import { PhraseTextMode } from '../models/phrase-repetitions.models';
import { phraseEnvelopeFailure } from './phrase-request-failure';

const UNRESOLVED_MESSAGE = 'لم تُطابق العبارة هوية كلمات قرآنية في الفهرس الحالي.';
const AMBIGUOUS_MESSAGE = 'للعبارة أكثر من قراءة مطابقة. اختر تسلسل الكلمات المقصود.';

export interface MappedPhraseResolution {
  readonly state: PhraseResolutionViewState;
  readonly activeBuildId: string | null;
  readonly autoCandidate: PhraseResolutionCandidateDto | null;
}

export function mapPhraseResolution(
  query: string,
  mode: PhraseTextMode,
  response: PhraseQueryResolutionResponseApiResponse,
): MappedPhraseResolution {
  if (!response.isSuccess || !response.data) {
    const failure = phraseEnvelopeFailure(response.errors, response.message);
    return {
      state: {
        rawQuery: query,
        mode,
        status: failure.status,
        candidates: [],
        selectedResolutionRef: null,
        message: failure.message,
      },
      activeBuildId: null,
      autoCandidate: null,
    };
  }
  const resolvedCandidate =
    response.data.status === 'resolved' && response.data.candidates.length === 1
      ? response.data.candidates[0]
      : null;
  return {
    state: {
      rawQuery: query,
      mode,
      status:
        resolvedCandidate !== null
          ? 'resolved'
          : response.data.status === 'ambiguous'
            ? 'ambiguous'
            : 'unresolved',
      candidates: response.data.candidates,
      selectedResolutionRef: resolvedCandidate?.resolutionRef ?? null,
      message:
        resolvedCandidate !== null
          ? ''
          : response.data.status === 'ambiguous'
            ? AMBIGUOUS_MESSAGE
            : UNRESOLVED_MESSAGE,
    },
    activeBuildId: response.data.activeBuildId,
    autoCandidate: resolvedCandidate,
  };
}

export function phraseResolutionFromBranches(
  rawQuery: string,
  mode: PhraseTextMode,
  response: PhraseContextBranchesResponse,
): PhraseResolutionViewState {
  const candidate = phraseCandidateFromBranches(response);
  return {
    rawQuery,
    mode,
    status: 'resolved',
    candidates: [candidate],
    selectedResolutionRef: candidate.resolutionRef,
    message: '',
  };
}

function phraseCandidateFromBranches(
  response: PhraseContextBranchesResponse,
): PhraseResolutionCandidateDto {
  return {
    displayText: response.query.tokens.map((token) => token.textUthmani).join(' '),
    resolutionRef: response.query.resolutionRef,
    tokens: response.query.tokens,
    wordCount: response.query.tokens.length,
  };
}
