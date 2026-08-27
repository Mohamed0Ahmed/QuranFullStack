import { Injectable, inject } from '@angular/core';
import { of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { PhraseContextUrlState } from '../models/phrase-context.models';
import { PhraseActionRequestGate } from './phrase-action-request-gate';
import { PhraseContextRequestStatusStore, PhraseContextRequestTarget } from './phrase-context-request-status.store';
import { PhraseContextSelectionStore } from './phrase-context-selection.store';
import { contextResultsRedirectPage } from './phrase-context-results-paging';
import { phraseContextStateKey } from './phrase-context-url-sync';
import { PhraseContextLoadResult, PhraseContextWorkspaceLoader } from './phrase-context-workspace.loader';
import { phraseRequestFailure } from './phrase-request-failure';

export interface PhraseContextActionHooks {
  readonly currentRoute: () => PhraseContextUrlState;
  readonly acceptBuild: (activeBuildId: string) => boolean;
  readonly resetBuild: (activeBuildId: string | null) => void;
  readonly navigate: (state: PhraseContextUrlState, replaceUrl: boolean) => void;
}

@Injectable()
export class PhraseContextActionCoordinator {
  private readonly loader = inject(PhraseContextWorkspaceLoader);
  private readonly selection = inject(PhraseContextSelectionStore);
  private readonly status = inject(PhraseContextRequestStatusStore);
  private readonly gate = inject(PhraseActionRequestGate);

  loadMoreGroups(
    route: PhraseContextUrlState,
    cursor: string,
    hooks: PhraseContextActionHooks,
  ): void {
    this.status.groups.set('refreshing');
    const routeKey = phraseContextStateKey(route);
    const epoch = this.gate.begin();
    const subscription = this.loader
      .loadGroupsPage(route, cursor)
      .pipe(
        tap((result) => {
          if (
            !this.isCurrent(epoch, routeKey, hooks) ||
            this.selection.groupsNextCursor() !== cursor ||
            !this.accept(result, 'groups', hooks) ||
            result.kind !== 'groups'
          ) {
            return;
          }
          this.selection.appendGroups(result.groups);
          this.status.groups.set('success');
        }),
        catchError((error: unknown) => this.fail(error, 'groups', epoch, routeKey, hooks)),
      )
      .subscribe();
    this.gate.track(epoch, subscription);
  }

  loadResultsPage(route: PhraseContextUrlState, hooks: PhraseContextActionHooks): void {
    this.status.results.set('refreshing');
    const routeKey = phraseContextStateKey(route);
    const epoch = this.gate.begin();
    const subscription = this.loader
      .loadResultsPage(route)
      .pipe(
        tap((result) => {
          if (
            !this.isCurrent(epoch, routeKey, hooks) ||
            !this.accept(result, 'results', hooks) ||
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
        catchError((error: unknown) => this.fail(error, 'results', epoch, routeKey, hooks)),
      )
      .subscribe();
    this.gate.track(epoch, subscription);
  }

  loadBranchPage(
    route: PhraseContextUrlState,
    side: 'previous' | 'following',
    cursor: string,
    hooks: PhraseContextActionHooks,
  ): void {
    this.status.branches.set('refreshing');
    const routeKey = phraseContextStateKey(route);
    const epoch = this.gate.begin();
    const subscription = this.loader
      .loadBranchPage(route, side === 'previous' ? cursor : null, side === 'following' ? cursor : null)
      .pipe(
        tap((result) => {
          const activeCursor =
            side === 'previous'
              ? this.selection.branches()?.previous.nextCursor
              : this.selection.branches()?.following.nextCursor;
          if (
            !this.isCurrent(epoch, routeKey, hooks) ||
            activeCursor !== cursor ||
            !this.accept(result, 'branches', hooks) ||
            result.kind !== 'branches'
          ) {
            return;
          }
          if (side === 'previous') {
            this.selection.appendPrevious(result.branches);
          } else {
            this.selection.appendFollowing(result.branches);
          }
          this.status.branches.set('success');
        }),
        catchError((error: unknown) => this.fail(error, 'branches', epoch, routeKey, hooks)),
      )
      .subscribe();
    this.gate.track(epoch, subscription);
  }

  loadOccurrences(
    route: PhraseContextUrlState,
    contextRef: string,
    cursor: string | null,
    append: boolean,
    hooks: PhraseContextActionHooks,
  ): void {
    this.status.occurrences.set(append ? 'refreshing' : 'loading');
    const routeKey = phraseContextStateKey(route);
    const epoch = this.gate.begin();
    const subscription = this.loader
      .loadOccurrences(contextRef, cursor)
      .pipe(
        tap((result) => {
          if (
            !this.isCurrent(epoch, routeKey, hooks, contextRef) ||
            !this.accept(result, 'occurrences', hooks) ||
            result.kind !== 'occurrences'
          ) {
            return;
          }
          if (append) {
            this.selection.appendOccurrences(result.occurrences);
          } else {
            this.selection.replaceOccurrences(result.occurrences);
          }
          this.status.occurrences.set(result.occurrences.totalCount === 0 ? 'empty' : 'success');
        }),
        catchError((error: unknown) =>
          this.fail(error, 'occurrences', epoch, routeKey, hooks, contextRef),
        ),
      )
      .subscribe();
    this.gate.track(epoch, subscription);
  }

  private accept(
    result: PhraseContextLoadResult,
    target: PhraseContextRequestTarget,
    hooks: PhraseContextActionHooks,
  ): boolean {
    if (result.kind === 'failure') {
      if (result.failure.status === 'stale') {
        hooks.resetBuild(null);
      } else {
        this.status.fail(target, result.failure.status, result.failure.message);
      }
      return false;
    }
    return hooks.acceptBuild(result.activeBuildId);
  }

  private fail(
    error: unknown,
    target: PhraseContextRequestTarget,
    epoch: number,
    routeKey: string,
    hooks: PhraseContextActionHooks,
    contextRef?: string,
  ) {
    if (!this.isCurrent(epoch, routeKey, hooks, contextRef)) {
      return of(undefined);
    }
    const failure = phraseRequestFailure(error);
    if (failure.status === 'stale') {
      hooks.resetBuild(null);
    } else {
      this.status.fail(target, failure.status, failure.message);
    }
    return of(undefined);
  }

  private isCurrent(
    epoch: number,
    routeKey: string,
    hooks: PhraseContextActionHooks,
    contextRef?: string,
  ): boolean {
    return (
      this.gate.isCurrent(epoch) &&
      routeKey === phraseContextStateKey(hooks.currentRoute()) &&
      (contextRef === undefined || this.selection.selectedContextRef() === contextRef)
    );
  }
}
