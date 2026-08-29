import { Injectable, inject } from '@angular/core';
import { of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import {
  ParsedPhraseContextUrlState,
  PhraseContextUrlState,
} from '../models/phrase-context.models';
import { PhraseLoadStatus } from '../models/phrase-repetitions.models';
import {
  PhraseActionRequestGate,
  PhraseActionRequestTarget,
} from './phrase-action-request-gate';
import {
  PhraseContextRequestStatusStore,
  PhraseContextRequestTarget,
} from './phrase-context-request-status.store';
import { PhraseContextSelectionStore } from './phrase-context-selection.store';
import { contextResultsRedirectPage } from './phrase-context-results-paging';
import {
  phraseContextBranchStateKey,
  phraseContextStateKey,
} from './phrase-context-url-sync';
import {
  PhraseContextLoadResult,
  PhraseContextWorkspaceLoader,
} from './phrase-context-workspace.loader';
import { phraseRequestFailure } from './phrase-request-failure';

type PhraseContextActionTarget = Extract<
  PhraseActionRequestTarget,
  'branches' | 'results'
>;

export interface PhraseContextActionHooks {
  readonly currentRoute: () => PhraseContextUrlState;
  readonly acceptBuild: (activeBuildId: string) => boolean;
  readonly resetBuild: () => void;
  readonly navigate: (state: PhraseContextUrlState, replaceUrl: boolean) => void;
}

@Injectable()
export class PhraseContextActionCoordinator {
  private readonly loader = inject(PhraseContextWorkspaceLoader);
  private readonly selection = inject(PhraseContextSelectionStore);
  private readonly status = inject(PhraseContextRequestStatusStore);
  private readonly gate = inject(PhraseActionRequestGate);

  cancelForRoute(
    previous: PhraseContextUrlState,
    parsed: ParsedPhraseContextUrlState,
  ): void {
    this.gate.invalidate('query');
    this.gate.invalidate('route');
    this.gate.invalidate('workspace');
    if (
      parsed.invalid ||
      phraseContextBranchStateKey(previous) !== phraseContextBranchStateKey(parsed.state)
    ) {
      this.gate.invalidate('branches');
      this.gate.invalidate('results');
      return;
    }
    if (phraseContextStateKey(previous) !== phraseContextStateKey(parsed.state)) {
      this.gate.invalidate('results');
    }
  }

  loadResultsPage(route: PhraseContextUrlState, hooks: PhraseContextActionHooks): void {
    const target = 'results';
    const routeKey = phraseContextStateKey(route);
    const epoch = this.startRequest(target, 'refreshing');
    const subscription = this.loader
      .loadResultsPage(route)
      .pipe(
        tap((result) => {
          if (
            !this.isCurrent(target, epoch, routeKey, hooks) ||
            !this.accept(result, target, hooks) ||
            result.kind !== 'results'
          ) {
            return;
          }
          const redirectPage = contextResultsRedirectPage(
            route.contextsPage,
            result.results.pageSize,
            result.results.totalCount,
          );
          if (redirectPage !== null) {
            hooks.navigate({ ...route, contextsPage: redirectPage }, true);
            return;
          }
          this.selection.replaceResults(result.results);
          this.status.results.set(result.results.totalCount === 0 ? 'empty' : 'success');
        }),
        catchError((error: unknown) => this.fail(error, target, epoch, routeKey, hooks)),
      )
      .subscribe();
    this.gate.track(target, epoch, subscription);
  }

  updateAlternativeGroup(
    route: PhraseContextUrlState,
    side: 'previous' | 'following',
    alternativeRef: string | null,
    hooks: PhraseContextActionHooks,
  ): void {
    const currentRef = side === 'previous'
      ? route.previousAlternatives
      : route.followingAlternatives;
    if (currentRef === alternativeRef) {
      return;
    }
    this.selection.requestFocus(`${side}-alternative`);
    this.status.branches.set('refreshing');
    this.status.results.set('refreshing');
    hooks.navigate({
      ...route,
      previousAlternatives: side === 'previous' ? alternativeRef : route.previousAlternatives,
      followingAlternatives: side === 'following' ? alternativeRef : route.followingAlternatives,
      contextsPage: 1,
    }, false);
  }

  loadBranchPage(
    route: PhraseContextUrlState,
    side: 'previous' | 'following',
    cursor: string,
    hooks: PhraseContextActionHooks,
  ): void {
    const target = 'branches';
    const routeKey = phraseContextBranchStateKey(route);
    const epoch = this.startRequest(target, 'refreshing');
    const subscription = this.loader
      .loadBranchPage(route, side === 'previous' ? cursor : null, side === 'following' ? cursor : null)
      .pipe(
        tap((result) => {
          const activeCursor =
            side === 'previous'
              ? this.selection.branches()?.previous.nextCursor
              : this.selection.branches()?.following.nextCursor;
          if (
            !this.isCurrent(target, epoch, routeKey, hooks) ||
            activeCursor !== cursor ||
            !this.accept(result, target, hooks) ||
            result.kind !== 'branches'
          ) {
            return;
          }
          if (side === 'previous') {
            this.selection.appendPrevious(result.branches, phraseContextBranchStateKey(route));
          } else {
            this.selection.appendFollowing(result.branches, phraseContextBranchStateKey(route));
          }
          this.status.branches.set('success');
        }),
        catchError((error: unknown) => this.fail(error, target, epoch, routeKey, hooks)),
      )
      .subscribe();
    this.gate.track(target, epoch, subscription);
  }

  private accept(
    result: PhraseContextLoadResult,
    target: PhraseContextRequestTarget,
    hooks: PhraseContextActionHooks,
  ): boolean {
    if (result.kind === 'failure') {
      if (result.failure.status === 'stale') {
        hooks.resetBuild();
      } else {
        this.status.fail(target, result.failure.status, result.failure.message);
      }
      return false;
    }
    return hooks.acceptBuild(result.activeBuildId);
  }

  private fail(
    error: unknown,
    target: PhraseContextActionTarget,
    epoch: number,
    routeKey: string,
    hooks: PhraseContextActionHooks,
  ) {
    if (!this.isCurrent(target, epoch, routeKey, hooks)) {
      return of(undefined);
    }
    const failure = phraseRequestFailure(error);
    if (failure.status === 'stale') {
      hooks.resetBuild();
    } else {
      this.status.fail(target, failure.status, failure.message);
    }
    return of(undefined);
  }

  private isCurrent(
    target: PhraseContextActionTarget,
    epoch: number,
    routeKey: string,
    hooks: PhraseContextActionHooks,
  ): boolean {
    return (
      this.gate.isCurrent(target, epoch) &&
      routeKey === this.routeKey(target, hooks.currentRoute())
    );
  }

  private startRequest(
    target: PhraseContextActionTarget,
    busyStatus: Extract<PhraseLoadStatus, 'loading' | 'refreshing'>,
  ): number {
    let invalidatedStatus: PhraseLoadStatus = 'idle';
    const epoch = this.gate.begin(target, () => this.status.set(target, invalidatedStatus));
    invalidatedStatus = settledStatus(this.currentStatus(target));
    this.status.set(target, busyStatus);
    return epoch;
  }

  private currentStatus(target: PhraseContextActionTarget): PhraseLoadStatus {
    if (target === 'results') {
      return this.status.results();
    }
    return this.status.branches();
  }

  private routeKey(
    target: PhraseContextActionTarget,
    route: PhraseContextUrlState,
  ): string {
    return target === 'results'
      ? phraseContextStateKey(route)
      : phraseContextBranchStateKey(route);
  }
}

function settledStatus(status: PhraseLoadStatus): PhraseLoadStatus {
  return status === 'loading' || status === 'refreshing' ? 'idle' : status;
}
