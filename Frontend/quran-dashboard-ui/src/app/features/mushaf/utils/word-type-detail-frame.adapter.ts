import { WordTypeDetailFrame } from '../../../core/navigation/detail-overlay/detail-overlay.models';
import { WordAnalysisViewModel } from '../models/mushaf.models';

const UNSPECIFIED_VERB_TENSE_CONTEXT = 'unspecified';

// case/tense/voice stay 'all' so the link opens the whole existing Word Type row, never a narrowed
// view of the single clicked occurrence; a missing tashkeel id or context code returns null so the
// caller keeps the label non-interactive rather than guessing (the DTO carries no Word Type identity).
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
