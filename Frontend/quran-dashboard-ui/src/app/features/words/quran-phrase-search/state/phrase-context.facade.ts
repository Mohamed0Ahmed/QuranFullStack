import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, Observable, Subscription, map, of, switchMap } from 'rxjs';
import { catchError, distinctUntilChanged, tap } from 'rxjs/operators';

import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';
import { PhraseResolutionApi } from '../data-access/phrase-resolution.api';
import {
  DEFAULT_PHRASE_CONTEXT_URL_STATE,
  ParsedPhraseContextUrlState,
  PhraseContextState,
  PhraseContextUrlState,
} from '../models/phrase-context.models';
import { PHRASE_INDEX_UNAVAILABLE_MESSAGE } from '../models/phrase-query.models';
import { PhraseTextMode } from '../models/phrase-repetitions.models';
import {
  parsePhraseContextUrlState,
  contextResultsPageOnlyChanged,
  phraseContextBranchStateKey,
  phraseContextStateKey,
} from './phrase-context-url-sync';
import { PhraseContextSelectionStore } from './phrase-context-selection.store';
import { contextResultsRedirectPage } from './phrase-context-results-paging';
import { PhraseActionRequestGate } from './phrase-action-request-gate';
import {
  PhraseContextActionCoordinator,
  PhraseContextActionHooks,
} from './phrase-context-action.coordinator';
import {
  PhraseContextQueryCoordinator,
  PhraseContextQueryHooks,
} from './phrase-context-query.coordinator';
import {
  PhraseContextRequestStatusStore,
  PhraseContextRequestTarget,
} from './phrase-context-request-status.store';
import { PhraseRouteNavigationCoordinator } from './phrase-route-navigation.coordinator';
import { PhraseNoticeStore } from './phrase-notice.store';
import { phraseEnvelopeFailure, phraseRequestFailure } from './phrase-request-failure';
import { normalizePhraseResolutionRequestDraft } from './phrase-resolution-request-identity';
import { PhraseContextResolutionStore } from './phrase-context-resolution.store';
import {
  PhraseContextLoadResult,
  PhraseContextWorkspaceLoader,
} from './phrase-context-workspace.loader';
import { PhraseContextWorkspaceRequestFence } from './phrase-context-workspace-request-fence';

const INVALID_ROUTE_MESSAGE = 'رابط البحث السياقي غير صالح أو يحتوي على مراجع منتهية.';

@Injectable()
export class PhraseContextFacade {
  private readonly workspaceLoader = inject(PhraseContextWorkspaceLoader);
  private readonly workspaceRequests = inject(PhraseContextWorkspaceRequestFence);
  private readonly resolutionApi = inject(PhraseResolutionApi);
  private readonly resolutionFlow = inject(PhraseContextResolutionStore);
  private readonly selection = inject(PhraseContextSelectionStore);
  private readonly requestStatus = inject(PhraseContextRequestStatusStore);
  private readonly routeCoordinator = inject(PhraseRouteNavigationCoordinator);
  private readonly actionGate = inject(PhraseActionRequestGate);
  private readonly notice = inject(PhraseNoticeStore);
  private readonly actions = inject(PhraseContextActionCoordinator);
  private readonly query = inject(PhraseContextQueryCoordinator);
  private readonly _route = signal(DEFAULT_PHRASE_CONTEXT_URL_STATE);
  private readonly _routeInvalid = signal(false);
  private readonly _capabilities = signal<PhraseSearchCapabilitiesResponse | null>(null);
  private readonly _workspaceDraftFresh = computed(() =>
    this.workspaceRequests.isWorkspaceFresh(this._route()),
  );
  private route?: ActivatedRoute;
  private routeSub?: Subscription;
  private draftPending = false;
  private readonly actionHooks: PhraseContextActionHooks = {
    currentRoute: () => this._route(),
    acceptBuild: (activeBuildId) => this.ensureBuild(activeBuildId),
    resetBuild: () => this.resetForBuildChange(),
    navigate: (state, replaceUrl) => this.navigate(state, replaceUrl),
  };
  private readonly queryHooks: PhraseContextQueryHooks = {
    currentRoute: () => this._route(),
    isCommittedWorkspaceCurrent: () =>
      this.workspaceRequests.isCommittedWorkspaceCurrent(this._route()),
    reloadCurrentRoute: () => this.reloadCurrentRoute(),
    clearWorkspace: () => this.clearWorkspaceForNewSubmission(),
    acceptBuild: (activeBuildId) => this.ensureBuild(activeBuildId),
    selectCandidate: (candidate) => this.selectCandidate(candidate),
    navigate: (state) => this.navigate(state),
  };
  readonly state = computed<PhraseContextState>(() => ({
    route: this._route(),
    routeInvalid: this._routeInvalid(),
    workspaceDraftFresh: this._workspaceDraftFresh(),
    mode: this.resolutionFlow.mode(),
    capabilitiesStatus: this.requestStatus.capabilities(),
    capabilities: this._capabilities(),
    resolution: this.resolutionFlow.state(),
    branchesStatus: this.requestStatus.branches(),
    branches: this.selection.branches(),
    previousOptions: this.selection.previousOptions(),
    followingOptions: this.selection.followingOptions(),
    resultsStatus: this.requestStatus.results(),
    occurrences: this.selection.occurrences(),
    resultsPage: this.selection.resultsPage(),
    resultsPageSize: this.selection.resultsPageSize(),
    occurrencesTotalCount: this.selection.occurrencesTotalCount(),
    errorMessage: this.requestStatus.errorMessage(),
    notice: this.notice.message(),
    sessionOnly: this.notice.sessionOnly(),
    focusTarget: this.selection.focusTarget(),
  }));

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();
    this.route = route;
    this.routeCoordinator.bind(route);
    this.routeSub = route.queryParamMap
      .pipe(
        map(parsePhraseContextUrlState),
        map((parsed) => {
          const restored = this.routeCoordinator.restoreContext(parsed);
          this.notice.applyNavigation(restored.outcome);
          return restored.parsed;
        }),
        distinctUntilChanged(
          (a, b) => a.invalid === b.invalid && phraseContextStateKey(a.state) === phraseContextStateKey(b.state),
        ),
        tap((parsed) => this.actions.cancelForRoute(this._route(), parsed)),
        switchMap((parsed) => this.runRoute(parsed)),
      )
      .subscribe();
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.actionGate.invalidate();
    this.routeSub = undefined;
    this.route = undefined;
    this.routeCoordinator.unbind();
  }

  setDraft(rawQuery: string): void {
    if (rawQuery === this.resolutionFlow.state().rawQuery) {
      return;
    }
    this.query.invalidate();
    this.workspaceRequests.invalidate();
    this.resolutionFlow.setDraft(rawQuery);
    this.updateDraftPending();
  }

  setMode(mode: PhraseTextMode): void {
    if (this.resolutionFlow.setMode(mode)) {
      this.query.invalidate();
      this.workspaceRequests.invalidate();
      this.updateDraftPending();
    }
  }

  submitQuery(): void {
    this.query.submit(this.queryHooks);
  }

  selectCandidate(candidate: PhraseResolutionCandidateDto): void {
    const build = this._capabilities()?.activeBuildId ?? this._route().build;
    this.selection.clearAll();
    this.navigate({
      build,
      mode: this.resolutionFlow.mode(),
      q: this.resolutionFlow.state().rawQuery,
      resolution: candidate.resolutionRef,
      before: null,
      after: null,
      previousAlternatives: null,
      followingAlternatives: null,
      contextsPage: 1,
    });
  }

  selectPrevious(selectionRef: string): void {
    if (!this.hasFreshCommittedWorkspace() || this._route().previousAlternatives !== null) {
      return;
    }
    this.selection.requestFocus('previous');
    this.workspaceRequests.markRefreshing();
    this.navigate({ ...this._route(), before: selectionRef, contextsPage: 1 });
  }

  selectFollowing(selectionRef: string): void {
    if (!this.hasFreshCommittedWorkspace() || this._route().followingAlternatives !== null) {
      return;
    }
    this.selection.requestFocus('following');
    this.workspaceRequests.markRefreshing();
    this.navigate({ ...this._route(), after: selectionRef, contextsPage: 1 });
  }

  selectPreviousPath(selectionRef: string | null): void {
    if (!this.hasFreshCommittedWorkspace() || this._route().previousAlternatives !== null) {
      return;
    }
    this.selection.requestFocus('previous');
    this.workspaceRequests.markRefreshing();
    this.navigate({ ...this._route(), before: selectionRef, contextsPage: 1 });
  }

  selectFollowingPath(selectionRef: string | null): void {
    if (!this.hasFreshCommittedWorkspace() || this._route().followingAlternatives !== null) {
      return;
    }
    this.selection.requestFocus('following');
    this.workspaceRequests.markRefreshing();
    this.navigate({ ...this._route(), after: selectionRef, contextsPage: 1 });
  }

  togglePreviousAlternative(alternativeRef: string | null): void {
    if (this.hasFreshCommittedWorkspace()) {
      this.actions.updateAlternativeGroup(
        this._route(),
        'previous',
        alternativeRef,
        this.actionHooks,
      );
    }
  }

  toggleFollowingAlternative(alternativeRef: string | null): void {
    if (this.hasFreshCommittedWorkspace()) {
      this.actions.updateAlternativeGroup(
        this._route(),
        'following',
        alternativeRef,
        this.actionHooks,
      );
    }
  }

  clearPreviousAlternatives(): void {
    this.togglePreviousAlternative(null);
  }

  clearFollowingAlternatives(): void {
    this.toggleFollowingAlternative(null);
  }

  loadMorePrevious(): void {
    if (!this.hasFreshCommittedWorkspace()) {
      return;
    }
    const branches = this.selection.branches();
    if (!branches?.previous.nextCursor) {
      return;
    }
    this.selection.requestFocus('previous-more');
    this.actions.loadBranchPage(
      this._route(),
      'previous',
      branches.previous.nextCursor,
      this.actionHooks,
    );
  }

  loadMoreFollowing(): void {
    if (!this.hasFreshCommittedWorkspace()) {
      return;
    }
    const branches = this.selection.branches();
    if (!branches?.following.nextCursor) {
      return;
    }
    this.selection.requestFocus('following-more');
    this.actions.loadBranchPage(
      this._route(),
      'following',
      branches.following.nextCursor,
      this.actionHooks,
    );
  }

  changeResultsPage(page: number): void {
    const route = this._route();
    if (!this.hasFreshCommittedWorkspace() || page < 1 || page === route.contextsPage) {
      return;
    }
    this.navigate({ ...route, contextsPage: page });
  }

  clearFocusTarget(): void {
    this.selection.clearFocusTarget();
  }

  retryRoute(): void {
    if (
      this.requestStatus.capabilities() === 'error' ||
      this.requestStatus.capabilities() === 'unavailable'
    ) {
      this._capabilities.set(null);
    }
    this.reloadCurrentRoute();
  }

  retryResolution(): void {
    this.query.retry(this.queryHooks);
  }

  resetInvalidState(): void {
    this.actionGate.invalidate();
    this.selection.clearAll();
    this.resolutionFlow.reset('', DEFAULT_PHRASE_CONTEXT_URL_STATE.mode);
    this.requestStatus.branches.set('idle');
    this.requestStatus.results.set('idle');
    this.requestStatus.errorMessage.set('');
    this._routeInvalid.set(false);
    this.navigate({
      ...DEFAULT_PHRASE_CONTEXT_URL_STATE,
      build: this._capabilities()?.activeBuildId ?? null,
    }, true);
  }

  dismissNotice(): void {
    this.notice.dismiss();
  }
  private runRoute(parsed: ParsedPhraseContextUrlState): Observable<void> {
    const previousRoute = this._route();
    const resultsPageChanged =
      !parsed.invalid &&
      contextResultsPageOnlyChanged(previousRoute, parsed.state) &&
      this.workspaceRequests.isCommittedWorkspaceCurrent(parsed.state);
    this._route.set(parsed.state);
    this.syncDraftForRoute(previousRoute, parsed.state);
    this.workspaceRequests.clearMismatchedPendingWorkspace(previousRoute, parsed.state);
    this._routeInvalid.set(parsed.invalid);
    this.requestStatus.errorMessage.set('');
    if (parsed.invalid) {
      this.requestStatus.branches.set('invalid');
      this.requestStatus.results.set('invalid');
      this.requestStatus.errorMessage.set(INVALID_ROUTE_MESSAGE);
      return EMPTY;
    }
    if (resultsPageChanged) {
      this.actions.loadResultsPage(parsed.state, this.actionHooks);
      return of(undefined);
    }
    const capabilities = this._capabilities();
    if (capabilities) {
      return this.runWithCapabilities(parsed.state);
    }
    this.requestStatus.capabilities.set('loading');
    const routeKey = phraseContextStateKey(parsed.state);
    return this.resolutionApi.getCapabilities().pipe(
      switchMap((response) => {
        if (routeKey !== phraseContextStateKey(this._route())) {
          return EMPTY;
        }
        if (!response.isSuccess || !response.data) {
          const failure = phraseEnvelopeFailure(response.errors, response.message);
          this.requestStatus.fail('capabilities', failure.status, failure.message);
          return EMPTY;
        }
        this._capabilities.set(response.data);
        this.requestStatus.capabilities.set('success');
        return this.runWithCapabilities(parsed.state);
      }),
      catchError((error: unknown) => this.applyRouteError(error, 'capabilities', routeKey)),
    );
  }

  private runWithCapabilities(route: PhraseContextUrlState): Observable<void> {
    const capabilities = this._capabilities();
    if (!capabilities?.exactReady) {
      this.requestStatus.capabilities.set('unavailable');
      this.requestStatus.errorMessage.set(PHRASE_INDEX_UNAVAILABLE_MESSAGE);
      return EMPTY;
    }
    if (route.build === null) {
      this.navigate({ ...route, build: capabilities.activeBuildId }, true);
      return EMPTY;
    }
    if (!sameBuild(route.build, capabilities.activeBuildId)) {
      this.resetForBuildChange();
      return EMPTY;
    }
    if (!route.resolution) {
      if (!this._workspaceDraftFresh()) {
        return of(undefined);
      }
      this.selection.clearAll();
      this.requestStatus.branches.set('idle');
      this.requestStatus.results.set('idle');
      if (!route.q) {
        this.resolutionFlow.restoreIdle('', route.mode);
        return of(undefined);
      }
      const resolution = this.resolutionFlow.state();
      if (
        resolution.status !== 'idle' &&
        resolution.status !== 'loading'
      ) {
        return of(undefined);
      }
      return this.query.resolveRestored(route, this.queryHooks);
    }
    const routeKey = phraseContextStateKey(route);
    const workspaceEpoch = this.workspaceRequests.begin(route);
    if (workspaceEpoch === null) {
      return EMPTY;
    }
    return this.workspaceLoader.loadWorkspace(route).pipe(
      tap((result) => {
        if (
          !this.workspaceRequests.isCurrent(workspaceEpoch, routeKey, this._route()) ||
          !this.acceptLoadResult(result, 'workspace') ||
          result.kind !== 'workspace' ||
          !this.workspaceRequests.isCurrent(workspaceEpoch, routeKey, this._route())
        ) {
          return;
        }
        const redirectPage = contextResultsRedirectPage(
          route.contextsPage,
          result.results.pageSize,
          result.results.totalCount,
        );
        if (redirectPage !== null) {
          this.navigate({ ...route, contextsPage: redirectPage }, true);
          return;
        }
        this.selection.replaceBranches(result.branches, phraseContextBranchStateKey(route));
        this.selection.replaceResults(result.results);
        this.resolutionFlow.restoreFromBranches(route.q, result.branches);
        this.requestStatus.branches.set('success');
        this.requestStatus.results.set(
          result.results.totalCount === 0 ? 'empty' : 'success',
        );
      }),
      catchError((error: unknown) =>
        this.workspaceRequests.isCurrent(workspaceEpoch, routeKey, this._route())
          ? this.applyRouteError(error, 'workspace', routeKey)
          : of(undefined),
      ),
      map(() => undefined),
    );
  }

  private acceptLoadResult(
    result: PhraseContextLoadResult,
    target: PhraseContextRequestTarget,
  ): boolean {
    if (result.kind === 'failure') {
      if (result.failure.status === 'stale') {
        this.resetForBuildChange();
      } else if (target === 'workspace' && result.failure.status === 'invalid') {
        this.rejectWorkspaceReference(result.failure.message);
      } else {
        this.requestStatus.fail(target, result.failure.status, result.failure.message);
      }
      return false;
    }
    if (!this.ensureBuild(result.activeBuildId)) {
      return false;
    }
    return true;
  }

  private applyRouteError(
    error: unknown,
    target: PhraseContextRequestTarget,
    routeKey: string,
  ): Observable<void> {
    if (routeKey !== phraseContextStateKey(this._route())) {
      return of(undefined);
    }
    const failure = phraseRequestFailure(error);
    if (failure.status === 'stale') {
      this.resetForBuildChange();
    } else if (target === 'workspace' && failure.status === 'invalid') {
      this.rejectWorkspaceReference(failure.message);
    } else {
      this.requestStatus.fail(target, failure.status, failure.message);
    }
    return of(undefined);
  }

  private ensureBuild(activeBuildId: string): boolean {
    const expected = this._route().build ?? this._capabilities()?.activeBuildId ?? null;
    if (expected && sameBuild(expected, activeBuildId)) {
      return true;
    }
    this.resetForBuildChange();
    return false;
  }

  private resetForBuildChange(): void {
    const mode = this.resolutionFlow.mode();
    const query = this.resolutionFlow.state().rawQuery || this._route().q;
    this.actionGate.invalidate();
    this._capabilities.set(null);
    this.requestStatus.capabilities.set('idle');
    this.selection.clearAll();
    this.resolutionFlow.reset(query, mode);
    this.routeCoordinator.clearBuildScopedState();
    this.notice.indexChanged();
    this.requestStatus.branches.set('stale');
    this.requestStatus.results.set('stale');
    this.navigate(
      {
        ...DEFAULT_PHRASE_CONTEXT_URL_STATE,
        mode,
        q: query,
      },
      true,
    );
  }

  private navigate(state: PhraseContextUrlState, replaceUrl = false): void {
    const outcome = this.routeCoordinator.navigateContext(
      state,
      this._route(),
      replaceUrl,
      () => {
        const epoch = this.actionGate.begin('route');
        const subscription = this.runRoute({ state, invalid: false }).subscribe();
        this.actionGate.track('route', epoch, subscription);
      },
    );
    this.notice.applyNavigation(outcome);
  }

  private clearWorkspaceForNewSubmission(): void {
    this.actionGate.invalidate();
    this.selection.clearAll();
    this.requestStatus.branches.set('idle');
    this.requestStatus.results.set('idle');
    this.requestStatus.errorMessage.set('');
  }

  private reloadCurrentRoute(): void {
    const epoch = this.actionGate.begin('route');
    const subscription = this.runRoute({ state: this._route(), invalid: false }).subscribe();
    this.actionGate.track('route', epoch, subscription);
  }

  private syncDraftForRoute(
    previousRoute: PhraseContextUrlState,
    route: PhraseContextUrlState,
  ): void {
    if (this._workspaceDraftFresh()) {
      this.draftPending = false;
      return;
    }
    const routeQueryChanged =
      normalizePhraseResolutionRequestDraft(previousRoute.q) !==
        normalizePhraseResolutionRequestDraft(route.q) ||
      previousRoute.mode !== route.mode;
    if (routeQueryChanged || !this.draftPending) {
      this.resolutionFlow.restoreIdle(route.q, route.mode);
      this.draftPending = false;
    }
  }

  private updateDraftPending(): void {
    this.draftPending = !this._workspaceDraftFresh();
  }

  private hasFreshCommittedWorkspace(): boolean {
    return this.workspaceRequests.isCommittedWorkspaceCurrent(this._route());
  }

  private rejectWorkspaceReference(message: string): void {
    this.actionGate.invalidate();
    this.selection.clearWorkspace();
    this.resolutionFlow.fail('invalid', message);
    this.requestStatus.fail('workspace', 'invalid', message);
    this._routeInvalid.set(true);
  }
}

function sameBuild(expected: string, actual: string): boolean {
  return expected.toLowerCase() === actual.toLowerCase();
}
