import { Injectable } from '@angular/core';

import { PhraseContextUrlState } from '../models/phrase-context.models';
import { PhraseContextFocusTarget } from '../models/phrase-context.models';
import {
  isPhraseSimilarityResultSort,
  PhraseSimilarityUrlState,
} from '../models/phrase-similarity.models';

interface StoredPhraseLongState {
  readonly context?: PhraseContextUrlState;
  readonly similarity?: PhraseSimilarityUrlState;
  readonly focusTarget?: PhraseContextFocusTarget;
}

const STORAGE_KEY = 'qd.phrase-search.long-state.v1';

@Injectable()
export class PhraseLongStateSessionStore {
  private state = this.read();

  saveContext(state: PhraseContextUrlState): void {
    this.state = { ...this.state, context: state };
    this.persist();
  }

  restoreContext(base: PhraseContextUrlState): PhraseContextUrlState | null {
    const stored = this.state.context;
    return stored && sameContextBase(stored, base) ? stored : null;
  }

  clearContext(): void {
    this.state = { ...this.state, context: undefined };
    this.persist();
  }

  saveSimilarity(state: PhraseSimilarityUrlState): void {
    this.state = { ...this.state, similarity: state };
    this.persist();
  }

  restoreSimilarity(base: PhraseSimilarityUrlState): PhraseSimilarityUrlState | null {
    const stored = this.state.similarity;
    if (!stored || !sameSimilarityBase(stored, base)) {
      return null;
    }
    return {
      ...stored,
      sort: isPhraseSimilarityResultSort(stored.sort) ? stored.sort : base.sort,
    };
  }

  clearSimilarity(): void {
    this.state = { ...this.state, similarity: undefined };
    this.persist();
  }

  clearBuildScopedState(): void {
    this.state = {};
    this.persist();
  }

  saveFocusTarget(target: PhraseContextFocusTarget): void {
    this.state = { ...this.state, focusTarget: target };
    this.persist();
  }

  restoreFocusTarget(): PhraseContextFocusTarget | null {
    return this.state.focusTarget ?? null;
  }

  clearFocusTarget(): void {
    this.state = { ...this.state, focusTarget: undefined };
    this.persist();
  }

  private read(): StoredPhraseLongState {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as StoredPhraseLongState) : {};
    } catch {
      return {};
    }
  }

  private persist(): void {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(this.state));
    } catch {
      return;
    }
  }
}

function sameContextBase(stored: PhraseContextUrlState, base: PhraseContextUrlState): boolean {
  return (
    stored.build === base.build &&
    stored.mode === base.mode &&
    (base.q === '' || stored.q === base.q)
  );
}

function sameSimilarityBase(
  stored: PhraseSimilarityUrlState,
  base: PhraseSimilarityUrlState,
): boolean {
  return (
    stored.build === base.build &&
    (base.q === '' || stored.q === base.q) &&
    stored.mode === base.mode
  );
}
