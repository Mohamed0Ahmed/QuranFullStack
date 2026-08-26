import { Injectable, inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { ActivatedRoute, Params, Router } from '@angular/router';

import {
  ParsedPhraseContextUrlState,
  PhraseContextUrlState,
} from '../models/phrase-context.models';
import {
  ParsedPhraseSimilarityUrlState,
  PhraseSimilarityUrlState,
} from '../models/phrase-similarity.models';
import {
  phraseContextStateKey,
  phraseUrlLength,
  safePhraseContextUrlState,
  serializePhraseContextUrlState,
} from './phrase-context-url-sync';
import { PhraseLongStateSessionStore } from './phrase-long-state-session.store';
import {
  phraseSimilarityStateKey,
  phraseSimilarityUrlLength,
  safePhraseSimilarityUrlState,
  serializePhraseSimilarityUrlState,
} from './phrase-similarity-url-sync';

export interface PhraseRestoredRoute<T> {
  readonly parsed: T;
  readonly outcome: PhraseNavigationOutcome;
}

export interface PhraseNavigationOutcome {
  readonly sessionOnly: boolean;
  readonly queryDisposition: 'retained' | 'omitted' | 'absent';
}

const CONTEXT_PATH = '/dashboard/words/phrases/context';
const SIMILARITY_PATH = '/dashboard/words/phrases/similarity';

@Injectable()
export class PhraseRouteNavigationCoordinator {
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly longState = inject(PhraseLongStateSessionStore);
  private route?: ActivatedRoute;

  bind(route: ActivatedRoute): void {
    this.route = route;
  }

  unbind(): void {
    this.route = undefined;
  }

  restoreContext(
    parsed: ParsedPhraseContextUrlState,
  ): PhraseRestoredRoute<ParsedPhraseContextUrlState> {
    if (parsed.invalid) {
      return { parsed, outcome: shareableOutcome() };
    }
    const restored = this.longState.restoreContext(parsed.state);
    return restored && phraseContextStateKey(restored) !== phraseContextStateKey(parsed.state)
      ? {
          parsed: { state: restored, invalid: false },
          outcome: sessionOutcome(restored.q, parsed.state.q),
        }
      : { parsed, outcome: shareableOutcome() };
  }

  restoreSimilarity(
    parsed: ParsedPhraseSimilarityUrlState,
  ): PhraseRestoredRoute<ParsedPhraseSimilarityUrlState> {
    if (parsed.invalid) {
      return { parsed, outcome: shareableOutcome() };
    }
    const restored = this.longState.restoreSimilarity(parsed.state);
    return restored && phraseSimilarityStateKey(restored) !== phraseSimilarityStateKey(parsed.state)
      ? {
          parsed: { state: restored, invalid: false },
          outcome: sessionOutcome(restored.q, parsed.state.q),
        }
      : { parsed, outcome: shareableOutcome() };
  }

  navigateContext(
    state: PhraseContextUrlState,
    current: PhraseContextUrlState,
    replaceUrl: boolean,
    onSkipped: () => void,
  ): PhraseNavigationOutcome {
    const params = serializePhraseContextUrlState(state);
    const basePath = this.absolutePath(CONTEXT_PATH);
    const sessionOnly = phraseUrlLength(basePath, params) > 1800;
    const target = sessionOnly ? safePhraseContextUrlState(state, basePath) : state;
    if (sessionOnly) {
      this.longState.saveContext(state);
    } else {
      this.longState.clearContext();
    }
    this.navigate(
      serializePhraseContextUrlState(target),
      replaceUrl,
      phraseContextStateKey(state) !== phraseContextStateKey(current),
      onSkipped,
    );
    return sessionOnly ? sessionOutcome(state.q, target.q) : shareableOutcome();
  }

  navigateSimilarity(
    state: PhraseSimilarityUrlState,
    current: PhraseSimilarityUrlState,
    replaceUrl: boolean,
    onSkipped: () => void,
  ): PhraseNavigationOutcome {
    const params = serializePhraseSimilarityUrlState(state);
    const basePath = this.absolutePath(SIMILARITY_PATH);
    const sessionOnly = phraseSimilarityUrlLength(basePath, params) > 1800;
    const target = sessionOnly ? safePhraseSimilarityUrlState(state, basePath) : state;
    if (sessionOnly) {
      this.longState.saveSimilarity(state);
    } else {
      this.longState.clearSimilarity();
    }
    this.navigate(
      serializePhraseSimilarityUrlState(target),
      replaceUrl,
      phraseSimilarityStateKey(state) !== phraseSimilarityStateKey(current),
      onSkipped,
    );
    return sessionOnly ? sessionOutcome(state.q, target.q) : shareableOutcome();
  }

  clearBuildScopedState(): void {
    this.longState.clearBuildScopedState();
  }

  private absolutePath(path: string): string {
    return `${this.document.location.origin}${path}`;
  }

  private navigate(
    queryParams: Params,
    replaceUrl: boolean,
    stateChanged: boolean,
    onSkipped: () => void,
  ): void {
    if (!this.route) {
      return;
    }
    void this.router
      .navigate([], { relativeTo: this.route, queryParams, replaceUrl })
      .then((navigated) => {
        if (!navigated && stateChanged) {
          onSkipped();
        }
      });
  }
}

function shareableOutcome(): PhraseNavigationOutcome {
  return { sessionOnly: false, queryDisposition: 'absent' };
}

function sessionOutcome(fullQuery: string, safeQuery: string): PhraseNavigationOutcome {
  return {
    sessionOnly: true,
    queryDisposition: fullQuery ? (safeQuery === fullQuery ? 'retained' : 'omitted') : 'absent',
  };
}
