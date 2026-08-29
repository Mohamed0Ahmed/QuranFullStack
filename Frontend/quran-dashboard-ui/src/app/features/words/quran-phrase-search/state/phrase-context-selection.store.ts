import { Injectable, inject, signal } from '@angular/core';

import { PhraseContextBranchesResponse } from '../../../../core/api/generated/models/phrase-context-branches-response';
import { PhraseContextAyahDto } from '../../../../core/api/generated/models/phrase-context-ayah-dto';
import { PhraseContextResultsResponse } from '../../../../core/api/generated/models/phrase-context-results-response';
import {
  PHRASE_CONTEXT_RESULT_PAGE_SIZE,
  PhraseContextFocusTarget,
} from '../models/phrase-context.models';
import { PhraseLongStateSessionStore } from './phrase-long-state-session.store';

type PhraseContextBranchOptions = PhraseContextBranchesResponse['previous']['options'];

interface PhraseContextBranchSnapshot {
  readonly branches: PhraseContextBranchesResponse;
  readonly previousOptions: PhraseContextBranchOptions;
  readonly followingOptions: PhraseContextBranchOptions;
}

@Injectable()
export class PhraseContextSelectionStore {
  private readonly longState = inject(PhraseLongStateSessionStore);
  private readonly branchSnapshots = new Map<string, PhraseContextBranchSnapshot>();
  private readonly _activeBranchStateKey = signal<string | null>(null);
  private readonly _branches = signal<PhraseContextBranchesResponse | null>(null);
  private readonly _previousOptions = signal<PhraseContextBranchOptions>([]);
  private readonly _followingOptions = signal<PhraseContextBranchOptions>([]);
  private readonly _occurrences = signal<readonly PhraseContextAyahDto[]>([]);
  private readonly _resultsPage = signal(1);
  private readonly _resultsPageSize = signal(PHRASE_CONTEXT_RESULT_PAGE_SIZE);
  private readonly _occurrencesTotalCount = signal(0);
  private readonly _focusTarget = signal<PhraseContextFocusTarget | null>(
    this.longState.restoreFocusTarget(),
  );

  readonly branches = this._branches.asReadonly();
  readonly activeBranchStateKey = this._activeBranchStateKey.asReadonly();
  readonly previousOptions = this._previousOptions.asReadonly();
  readonly followingOptions = this._followingOptions.asReadonly();
  readonly occurrences = this._occurrences.asReadonly();
  readonly resultsPage = this._resultsPage.asReadonly();
  readonly resultsPageSize = this._resultsPageSize.asReadonly();
  readonly occurrencesTotalCount = this._occurrencesTotalCount.asReadonly();
  readonly focusTarget = this._focusTarget.asReadonly();

  requestFocus(target: PhraseContextFocusTarget): void {
    this._focusTarget.set(target);
    this.longState.saveFocusTarget(target);
  }

  clearFocusTarget(): void {
    this._focusTarget.set(null);
    this.longState.clearFocusTarget();
  }

  replaceBranches(response: PhraseContextBranchesResponse, stateKey: string): void {
    const snapshot = this.branchSnapshots.get(stateKey) ?? {
      branches: response,
      previousOptions: response.previous.options,
      followingOptions: response.following.options,
    };
    this.branchSnapshots.set(stateKey, snapshot);
    this.showBranchSnapshot(snapshot);
    this._activeBranchStateKey.set(stateKey);
  }

  appendPrevious(response: PhraseContextBranchesResponse, stateKey: string): void {
    const current = this.branchSnapshots.get(stateKey);
    const snapshot: PhraseContextBranchSnapshot = {
      branches: current
        ? {
            ...response,
            following: current.branches.following,
            followingSelection: current.branches.followingSelection,
          }
        : response,
      previousOptions: appendUniqueOptions(
        current?.previousOptions ?? [],
        response.previous.options,
      ),
      followingOptions: current?.followingOptions ?? response.following.options,
    };
    this.branchSnapshots.set(stateKey, snapshot);
    if (this._activeBranchStateKey() === stateKey) {
      this.showBranchSnapshot(snapshot);
    }
  }

  appendFollowing(response: PhraseContextBranchesResponse, stateKey: string): void {
    const current = this.branchSnapshots.get(stateKey);
    const snapshot: PhraseContextBranchSnapshot = {
      branches: current
        ? {
            ...response,
            previous: current.branches.previous,
            previousSelection: current.branches.previousSelection,
          }
        : response,
      previousOptions: current?.previousOptions ?? response.previous.options,
      followingOptions: appendUniqueOptions(
        current?.followingOptions ?? [],
        response.following.options,
      ),
    };
    this.branchSnapshots.set(stateKey, snapshot);
    if (this._activeBranchStateKey() === stateKey) {
      this.showBranchSnapshot(snapshot);
    }
  }

  replaceResults(response: PhraseContextResultsResponse): void {
    this._occurrences.set(response.items);
    this._resultsPage.set(response.page);
    this._resultsPageSize.set(response.pageSize);
    this._occurrencesTotalCount.set(response.totalCount);
  }

  clearAll(): void {
    this.branchSnapshots.clear();
    this.clearWorkspace();
  }

  clearWorkspace(): void {
    this._branches.set(null);
    this._previousOptions.set([]);
    this._followingOptions.set([]);
    this.clearOccurrences();
    this._activeBranchStateKey.set(null);
  }

  clearOccurrences(): void {
    this._occurrences.set([]);
    this._resultsPage.set(1);
    this._resultsPageSize.set(PHRASE_CONTEXT_RESULT_PAGE_SIZE);
    this._occurrencesTotalCount.set(0);
  }

  private showBranchSnapshot(snapshot: PhraseContextBranchSnapshot): void {
    this._branches.set(snapshot.branches);
    this._previousOptions.set(snapshot.previousOptions);
    this._followingOptions.set(snapshot.followingOptions);
  }
}

function appendUniqueOptions<T extends { readonly selectionRef: string }>(
  current: readonly T[],
  incoming: readonly T[],
): T[] {
  const seen = new Set(current.map((item) => item.selectionRef));
  return [...current, ...incoming.filter((item) => !seen.has(item.selectionRef))];
}
