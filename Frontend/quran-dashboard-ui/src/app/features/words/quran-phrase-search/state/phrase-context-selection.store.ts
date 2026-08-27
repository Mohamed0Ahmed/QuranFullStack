import { Injectable, computed, inject, signal } from '@angular/core';

import { PhraseContextBranchesResponse } from '../../../../core/api/generated/models/phrase-context-branches-response';
import { PhraseContextGroupsResponse } from '../../../../core/api/generated/models/phrase-context-groups-response';
import { PhraseContextOccurrenceDto } from '../../../../core/api/generated/models/phrase-context-occurrence-dto';
import { PhraseContextOccurrencesResponse } from '../../../../core/api/generated/models/phrase-context-occurrences-response';
import { PhraseContextResultsResponse } from '../../../../core/api/generated/models/phrase-context-results-response';
import { PhraseFullContextGroupDto } from '../../../../core/api/generated/models/phrase-full-context-group-dto';
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
  private activeBranchStateKey: string | null = null;
  private readonly _branches = signal<PhraseContextBranchesResponse | null>(null);
  private readonly _previousOptions = signal<PhraseContextBranchOptions>([]);
  private readonly _followingOptions = signal<PhraseContextBranchOptions>([]);
  private readonly _groups = signal<readonly PhraseFullContextGroupDto[]>([]);
  private readonly _groupsTotalCount = signal(0);
  private readonly _groupsNextCursor = signal<string | null>(null);
  private readonly _selectedContextRef = signal<string | null>(null);
  private readonly _occurrences = signal<readonly PhraseContextOccurrenceDto[]>([]);
  private readonly _resultsPage = signal(1);
  private readonly _resultsPageSize = signal(PHRASE_CONTEXT_RESULT_PAGE_SIZE);
  private readonly _occurrencesTotalCount = signal(0);
  private readonly _occurrencesNextCursor = signal<string | null>(null);
  private readonly _focusTarget = signal<PhraseContextFocusTarget | null>(
    this.longState.restoreFocusTarget(),
  );

  readonly branches = this._branches.asReadonly();
  readonly previousOptions = this._previousOptions.asReadonly();
  readonly followingOptions = this._followingOptions.asReadonly();
  readonly groups = this._groups.asReadonly();
  readonly groupsTotalCount = this._groupsTotalCount.asReadonly();
  readonly groupsNextCursor = this._groupsNextCursor.asReadonly();
  readonly selectedContextRef = this._selectedContextRef.asReadonly();
  readonly occurrences = this._occurrences.asReadonly();
  readonly resultsPage = this._resultsPage.asReadonly();
  readonly resultsPageSize = this._resultsPageSize.asReadonly();
  readonly occurrencesTotalCount = this._occurrencesTotalCount.asReadonly();
  readonly occurrencesNextCursor = this._occurrencesNextCursor.asReadonly();
  readonly focusTarget = this._focusTarget.asReadonly();
  readonly hasSelectedContext = computed(() => this._selectedContextRef() !== null);

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
    this.activeBranchStateKey = stateKey;
    this.showBranchSnapshot(snapshot);
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
    if (this.activeBranchStateKey === stateKey) {
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
    if (this.activeBranchStateKey === stateKey) {
      this.showBranchSnapshot(snapshot);
    }
  }

  replaceGroups(response: PhraseContextGroupsResponse): void {
    this._groups.set(response.items);
    this._groupsTotalCount.set(response.totalCount);
    this._groupsNextCursor.set(response.nextCursor);
    this.clearOccurrences();
  }

  appendGroups(response: PhraseContextGroupsResponse): void {
    this._groups.update((items) => appendUniqueGroups(items, response.items));
    this._groupsTotalCount.set(response.totalCount);
    this._groupsNextCursor.set(response.nextCursor);
  }

  selectContext(contextRef: string): void {
    if (this._selectedContextRef() === contextRef) {
      return;
    }
    this._selectedContextRef.set(contextRef);
    this._occurrences.set([]);
    this._occurrencesTotalCount.set(0);
    this._occurrencesNextCursor.set(null);
  }

  replaceOccurrences(response: PhraseContextOccurrencesResponse): void {
    this._selectedContextRef.set(response.context.contextRef);
    this._occurrences.set(response.items);
    this._occurrencesTotalCount.set(response.totalCount);
    this._occurrencesNextCursor.set(response.nextCursor);
  }

  replaceResults(response: PhraseContextResultsResponse): void {
    this._selectedContextRef.set(null);
    this._occurrences.set(response.items);
    this._resultsPage.set(response.page);
    this._resultsPageSize.set(response.pageSize);
    this._occurrencesTotalCount.set(response.totalCount);
    this._occurrencesNextCursor.set(null);
  }

  appendOccurrences(response: PhraseContextOccurrencesResponse): void {
    this._occurrences.update((items) => appendUniqueOccurrences(items, response.items));
    this._occurrencesTotalCount.set(response.totalCount);
    this._occurrencesNextCursor.set(response.nextCursor);
  }

  clearAll(): void {
    this.branchSnapshots.clear();
    this.clearWorkspace();
  }

  clearWorkspace(): void {
    this.activeBranchStateKey = null;
    this._branches.set(null);
    this._previousOptions.set([]);
    this._followingOptions.set([]);
    this._groups.set([]);
    this._groupsTotalCount.set(0);
    this._groupsNextCursor.set(null);
    this.clearOccurrences();
  }

  clearOccurrences(): void {
    this._selectedContextRef.set(null);
    this._occurrences.set([]);
    this._resultsPage.set(1);
    this._resultsPageSize.set(PHRASE_CONTEXT_RESULT_PAGE_SIZE);
    this._occurrencesTotalCount.set(0);
    this._occurrencesNextCursor.set(null);
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

function appendUniqueGroups(
  current: readonly PhraseFullContextGroupDto[],
  incoming: readonly PhraseFullContextGroupDto[],
): PhraseFullContextGroupDto[] {
  const seen = new Set(current.map((item) => item.contextRef));
  return [...current, ...incoming.filter((item) => !seen.has(item.contextRef))];
}

function appendUniqueOccurrences(
  current: readonly PhraseContextOccurrenceDto[],
  incoming: readonly PhraseContextOccurrenceDto[],
): PhraseContextOccurrenceDto[] {
  const seen = new Set(current.map((item) => item.occurrenceId));
  return [...current, ...incoming.filter((item) => !seen.has(item.occurrenceId))];
}
