import { PhraseTextMode } from '../models/phrase-repetitions.models';

export interface PhraseResolutionRequestIdentity {
  readonly epoch: number;
  readonly normalizedDraft: string;
  readonly mode: PhraseTextMode;
  readonly routeKey: string;
}

export interface PhraseResolutionRequestSnapshot {
  readonly draft: string;
  readonly mode: PhraseTextMode;
  readonly routeKey: string;
}

export function createPhraseResolutionRequestIdentity(
  epoch: number,
  snapshot: PhraseResolutionRequestSnapshot,
): PhraseResolutionRequestIdentity {
  return {
    epoch,
    normalizedDraft: normalizePhraseResolutionRequestDraft(snapshot.draft),
    mode: snapshot.mode,
    routeKey: snapshot.routeKey,
  };
}

export function isPhraseResolutionRequestCurrent(
  identity: PhraseResolutionRequestIdentity,
  currentEpoch: boolean,
  snapshot: PhraseResolutionRequestSnapshot,
): boolean {
  return currentEpoch &&
    identity.normalizedDraft === normalizePhraseResolutionRequestDraft(snapshot.draft) &&
    identity.mode === snapshot.mode &&
    identity.routeKey === snapshot.routeKey;
}

export function normalizePhraseResolutionRequestDraft(rawQuery: string): string {
  return rawQuery.trim().normalize('NFC');
}
