import { WordTypeDetailFrame } from '../../../core/navigation/detail-overlay/detail-overlay.models';
import { WordAnalysisViewModel } from '../models/mushaf.models';

/**
 * Word Type identity adapter (Feature 029, Change B — locked decision §5.7).
 *
 * The Mushaf morphology DTO has no prebuilt Word Type identity, so the frame is
 * derived frontend-only: the backend matches verbs by tense context and
 * non-verbs by head POS. The optional filters stay `all` so the link opens the
 * complete existing Word Type row, never a silently narrowed view of the one
 * clicked occurrence. When the unique tashkeel ID or the required context code
 * cannot be formed, the caller must leave the label non-interactive — never
 * guess and never derive the context from the localized label.
 */
const UNSPECIFIED_VERB_TENSE_CONTEXT = 'unspecified';

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
