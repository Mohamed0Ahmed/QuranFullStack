import { WordTypeDetailFrame } from '../../../core/navigation/detail-overlay/detail-overlay.models';
import { WordAnalysisViewModel } from '../models/mushaf.models';

const UNSPECIFIED_VERB_TENSE_CONTEXT = 'unspecified';

// Missing tashkeel id or context code returns null; never guess a Word Type identity the DTO lacks.
export function wordTypeDetailFrameFromAnalysis(analysis: WordAnalysisViewModel): WordTypeDetailFrame | null {
  const tashkeelWordId = analysis.identity?.uniqueTashkeel?.id;

  if (typeof tashkeelWordId !== 'number' || !Number.isInteger(tashkeelWordId) || tashkeelWordId <= 0) {
    return null;
  }

  const morphology = analysis.morphology;
  const contextCode = morphology.isVerb
    ? (morphology.verbTense ?? UNSPECIFIED_VERB_TENSE_CONTEXT)
    : morphology.headPos;

  if (typeof contextCode !== 'string' || contextCode.trim().length === 0) {
    return null;
  }

  return {
    kind: 'wordType',
    tashkeelWordId,
    contextCode,
    case: 'all',
    tense: 'all',
    voice: 'all',
    view: 'ayahs',
    detailPage: 1,
  };
}
