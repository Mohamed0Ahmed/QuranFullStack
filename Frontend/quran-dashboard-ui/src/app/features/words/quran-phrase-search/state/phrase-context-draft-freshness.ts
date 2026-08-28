import { PhraseContextUrlState } from '../models/phrase-context.models';
import { PhraseResolutionViewState } from '../models/phrase-query.models';
import { normalizePhraseResolutionRequestDraft } from './phrase-resolution-request-identity';

export function isPhraseContextDraftFresh(
  route: PhraseContextUrlState,
  resolution: PhraseResolutionViewState,
): boolean {
  return resolution.mode === route.mode &&
    normalizePhraseResolutionRequestDraft(resolution.rawQuery) ===
      normalizePhraseResolutionRequestDraft(route.q);
}
