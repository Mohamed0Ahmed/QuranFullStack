import { Injectable } from '@angular/core';

import { PhraseContextUrlState } from '../models/phrase-context.models';
import { PhraseContextFocusTarget } from '../models/phrase-context.models';
import { PhraseSimilarityUrlState } from '../models/phrase-similarity.models';

interface StoredPhraseLongState {
  readonly context?: PhraseContextUrlState;
  readonly similarity?: PhraseSimilarityUrlState;
  readonly focusTarget?: PhraseContextFocusTarget;
  readonly contextParents?: PhraseContextParentMaps;
}

interface PhraseContextParentMaps {
  readonly previous?: Readonly<Record<string, string | null>>;
  readonly following?: Readonly<Record<string, string | null>>;
}

export interface PhraseContextParentLookup {
  readonly known: boolean;
  readonly parent: string | null;
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
    return stored && sameSimilarityBase(stored, base) ? stored : null;
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

  saveContextParent(
    side: 'previous' | 'following',
    child: string,
    parent: string | null,
  ): void {
    const parents = this.state.contextParents ?? {};
    this.state = {
      ...this.state,
      contextParents: {
        ...parents,
        [side]: { ...(parents[side] ?? {}), [child]: parent },
      },
    };
    this.persist();
  }

  saveContextParents(
    side: 'previous' | 'following',
    entries: readonly { readonly child: string; readonly parent: string | null }[],
  ): void {
    if (entries.length === 0) {
      return;
    }
    const parents = this.state.contextParents ?? {};
    const sideParents = { ...(parents[side] ?? {}) };
    entries.forEach((entry) => {
      sideParents[entry.child] = entry.parent;
    });
    this.state = {
      ...this.state,
      contextParents: { ...parents, [side]: sideParents },
    };
    this.persist();
  }

  restoreContextParent(
    side: 'previous' | 'following',
    child: string | null,
  ): PhraseContextParentLookup {
    if (child === null) {
      return { known: false, parent: null };
    }
    const parents = this.state.contextParents?.[side];
    return parents && Object.prototype.hasOwnProperty.call(parents, child)
      ? { known: true, parent: parents[child] ?? null }
      : { known: false, parent: null };
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
    stored.source === base.source &&
    (base.q === '' || stored.q === base.q) &&
    stored.mode === base.mode
  );
}
