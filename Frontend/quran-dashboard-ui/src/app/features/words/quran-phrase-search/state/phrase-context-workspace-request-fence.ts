import { Injectable, inject } from '@angular/core';

import { PhraseContextUrlState } from '../models/phrase-context.models';
import {
  phraseContextBranchStateKey,
  phraseContextStateKey,
} from './phrase-context-url-sync';
import { PhraseActionRequestGate } from './phrase-action-request-gate';
import { isPhraseContextDraftFresh } from './phrase-context-draft-freshness';
import { PhraseContextRequestStatusStore } from './phrase-context-request-status.store';
import { PhraseContextResolutionStore } from './phrase-context-resolution.store';
import { PhraseContextSelectionStore } from './phrase-context-selection.store';

@Injectable()
export class PhraseContextWorkspaceRequestFence {
  private readonly gate = inject(PhraseActionRequestGate);
  private readonly status = inject(PhraseContextRequestStatusStore);
  private readonly resolution = inject(PhraseContextResolutionStore);
  private readonly selection = inject(PhraseContextSelectionStore);

  invalidate(): void {
    this.gate.invalidate('workspace');
  }

  markRefreshing(): void {
    this.status.branches.set('refreshing');
    this.status.results.set('refreshing');
  }

  isWorkspaceFresh(route: PhraseContextUrlState): boolean {
    return isPhraseContextDraftFresh(route, this.resolution.state()) &&
      this.matchesLoadedWorkspace(route);
  }

  isCommittedWorkspaceCurrent(route: PhraseContextUrlState): boolean {
    return route.resolution !== null &&
      this.selection.branches() !== null &&
      this.isWorkspaceFresh(route);
  }

  clearMismatchedPendingWorkspace(
    previousRoute: PhraseContextUrlState,
    route: PhraseContextUrlState,
  ): void {
    if (
      phraseContextBranchStateKey(previousRoute) === phraseContextBranchStateKey(route) ||
      isPhraseContextDraftFresh(route, this.resolution.state())
    ) {
      return;
    }
    this.selection.clearWorkspace();
    this.status.branches.set('idle');
    this.status.results.set('idle');
  }

  begin(route: PhraseContextUrlState): number | null {
    if (!route.resolution) {
      return null;
    }
    if (!isPhraseContextDraftFresh(route, this.resolution.state())) {
      this.settle();
      return null;
    }
    const existingBranches = this.selection.branches();
    if (existingBranches) {
      this.markRefreshing();
    } else {
      this.resolution.markLoading(route.q, route.mode, route.resolution);
      this.selection.clearWorkspace();
      this.status.branches.set('loading');
      this.status.results.set('loading');
    }
    return this.gate.begin('workspace', () => this.settle());
  }

  isCurrent(epoch: number, routeKey: string, currentRoute: PhraseContextUrlState): boolean {
    return this.gate.isCurrent('workspace', epoch) &&
      routeKey === phraseContextStateKey(currentRoute);
  }

  private settle(): void {
    const hasWorkspace = this.selection.branches() !== null;
    this.status.branches.set(hasWorkspace ? 'success' : 'idle');
    this.status.results.set(
      hasWorkspace ? (this.selection.occurrencesTotalCount() === 0 ? 'empty' : 'success') : 'idle',
    );
  }

  private matchesLoadedWorkspace(route: PhraseContextUrlState): boolean {
    if (route.resolution === null || this.selection.branches() === null) {
      return true;
    }
    return this.selection.activeBranchStateKey() === phraseContextBranchStateKey(route);
  }
}
