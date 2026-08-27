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
import {
  PHRASE_INDEX_UNAVAILABLE_MESSAGE,
} from '../models/phrase-query.models';
import { PhraseTextMode } from '../models/phrase-repetitions.models';
import {
  parsePhraseContextUrlState,
  contextResultsPageOnlyChanged,
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
  PhraseContextRequestStatusStore,
  PhraseContextRequestTarget,
} from './phrase-context-request-status.store';
import { PhraseRouteNavigationCoordinator } from './phrase-route-navigation.coordinator';
import { PhraseNoticeStore } from './phrase-notice.store';
import { phraseEnvelopeFailure, phraseRequestFailure } from './phrase-request-failure';
import { PhraseContextResolutionStore } from './phrase-context-resolution.store';
import {
  PhraseContextLoadResult,
  PhraseContextWorkspaceLoader,
} from './phrase-context-workspace.loader';

const INVALID_ROUTE_MESSAGE = 'رابط البحث السياقي غير صالح أو يحتوي على مراجع منتهية.';

@Injectable()
export class PhraseContextFacade {
  private readonly workspaceLoader = inject(PhraseContextWorkspaceLoader);
  private readonly resolutionApi = inject(PhraseResolutionApi);
  private readonly resolutionFlow = inject(PhraseContextResolutionStore);
  private readonly selection = inject(PhraseContextSelectionStore);
  private readonly requestStatus = inject(PhraseContextRequestStatusStore);
  private readonly routeCoordinator = inject(PhraseRouteNavigationCoordinator);
  private readonly actionGate = inject(PhraseActionRequestGate);
  private readonly notice = inject(PhraseNoticeStore);
  private readonly actions = inject(PhraseContextActionCoordinator);
  private readonly _route = signal(DEFAULT_PHRASE_CONTEXT_URL_STATE);
  private readonly _routeInvalid = signal(false);
  private readonly _capabilities = signal<PhraseSearchCapabilitiesResponse | null>(null);
  private route?: ActivatedRoute;
  private routeSub?: Subscription;
  private readonly actionHooks: PhraseContextActionHooks = {
    currentRoute: () => this._route(),
    acceptBuild: (activeBuildId) => this.ensureBuild(activeBuildId),
    resetBuild: (activeBuildId) => this.resetForBuildChange(activeBuildId),
    navigate: (state, replaceUrl) => this.navigate(state, replaceUrl),
  };
  readonly state = computed<PhraseContextState>(() => ({
    route: this._route(),
    routeInvalid: this._routeInvalid(),
    mode: this.resolutionFlow.mode(),
    capabilitiesStatus: this.requestStatus.capabilities(),
    capabilities: this._capabilities(),
    resolution: this.resolutionFlow.state(),
    branchesStatus: this.requestStatus.branches(),
    branches: this.selection.branches(),
    previousOptions: this.selection.previousOptions(),
    followingOptions: this.selection.followingOptions(),
    groupsStatus: this.requestStatus.groups(),
    groups: this.selection.groups(),
    groupsTotalCount: this.selection.groupsTotalCount(),
    groupsNextCursor: this.selection.groupsNextCursor(),
    resultsStatus: this.requestStatus.results(),
    occurrencesStatus: this.requestStatus.occurrences(),
    occurrences: this.selection.occurrences(),
    resultsPage: this.selection.resultsPage(),
    resultsPageSize: this.selection.resultsPageSize(),
    occurrencesTotalCount: this.selection.occurrencesTotalCount(),
    occurrencesNextCursor: this.selection.occurrencesNextCursor(),
    selectedContextRef: this.selection.selectedContextRef(),
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
        tap(() => this.actionGate.invalidate()),
        map(parsePhraseContextUrlState),
        map((parsed) => {
          const restored = this.routeCoordinator.restoreContext(parsed);
          this.notice.applyNavigation(restored.outcome);
          return restored.parsed;
        }),
        distinctUntilChanged(
          (a, b) => a.invalid === b.invalid && phraseContextStateKey(a.state) === phraseContextStateKey(b.state),
        ),
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
    this.resolutionFlow.setDraft(rawQuery);
  }

  setMode(mode: PhraseTextMode): void {
    if (!this.resolutionFlow.setMode(mode)) {
      return;
    }
    this.selection.clearAll();
    this.navigate({
      ...this._route(),
      mode,
      resolution: null,
      before: null,
      after: null,
      contextsPage: 1,
    });
  }

  submitQuery(): void {
    this.clearWorkspaceForNewSubmission();
    const epoch = this.actionGate.begin();
    const submittedMode = this.resolutionFlow.mode();
    const submittedQuery = this.resolutionFlow.state().rawQuery.trim();
    const subscription = this.resolutionFlow
      .resolve()
      .pipe(
        tap((mapped) => {
          if (
            !this.actionGate.isCurrent(epoch) ||
            this.resolutionFlow.mode() !== submittedMode ||
            this.resolutionFlow.state().rawQuery.trim() !== submittedQuery ||
            !mapped
          ) {
            return;
          }
          this.resolutionFlow.accept(mapped);
          if (mapped.activeBuildId && !this.ensureBuild(mapped.activeBuildId)) {
            return;
          }
          if (mapped.autoCandidate) {
            this.selectCandidate(mapped.autoCandidate);
          } else {
            this.navigate({
              ...this._route(),
              mode: submittedMode,
              q: submittedQuery,
              resolution: null,
              before: null,
              after: null,
              contextsPage: 1,
            });
          }
        }),
        catchError((error: unknown) => {
          if (!this.actionGate.isCurrent(epoch)) {
            return of(undefined);
          }
          const failure = phraseRequestFailure(error);
          this.resolutionFlow.fail(failure.status, failure.message);
          return of(undefined);
        }),
      )
      .subscribe();
    this.actionGate.track(epoch, subscription);
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
      contextsPage: 1,
    });
  }

  selectPrevious(selectionRef: string): void {
    this.selection.requestFocus('previous');
    this.startWorkspaceRefresh();
    this.navigate({ ...this._route(), before: selectionRef, contextsPage: 1 });
  }

  selectFollowing(selectionRef: string): void {
    this.selection.requestFocus('following');
    this.startWorkspaceRefresh();
    this.navigate({ ...this._route(), after: selectionRef, contextsPage: 1 });
  }

  selectPreviousPath(selectionRef: string | null): void {
    this.selection.requestFocus('previous');
    this.startWorkspaceRefresh();
    this.navigate({ ...this._route(), before: selectionRef, contextsPage: 1 });
  }

  selectFollowingPath(selectionRef: string | null): void {
    this.selection.requestFocus('following');
    this.startWorkspaceRefresh();
    this.navigate({ ...this._route(), after: selectionRef, contextsPage: 1 });
  }

  loadMorePrevious(): void {
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
    if (page < 1 || page === route.contextsPage) {
      return;
    }
    this.navigate({ ...route, contextsPage: page });
  }

  loadMoreGroups(): void {
    const cursor = this.selection.groupsNextCursor();
    const route = this._route();
    if (!cursor || !route.resolution) {
      return;
    }
    this.actions.loadMoreGroups(route, cursor, this.actionHooks);
  }

  selectContext(contextRef: string): void {
    this.selection.selectContext(contextRef);
    this.actions.loadOccurrences(this._route(), contextRef, null, false, this.actionHooks);
  }

  loadMoreOccurrences(): void {
    const contextRef = this.selection.selectedContextRef();
    const cursor = this.selection.occurrencesNextCursor();
    if (contextRef && cursor) {
      this.actions.loadOccurrences(this._route(), contextRef, cursor, true, this.actionHooks);
    }
  }

  clearSelectedContext(): void {
    this.actionGate.invalidate();
    this.selection.clearOccurrences();
    this.requestStatus.occurrences.set('idle');
  }

  clearFocusTarget(): void {
    this.selection.clearFocusTarget();
  }

  retry(): void {
    if (
      this.requestStatus.capabilities() === 'error' ||
      this.requestStatus.capabilities() === 'unavailable'
    ) {
      this._capabilities.set(null);
    }
    const epoch = this.actionGate.begin();
    const subscription = this.runRoute({ state: this._route(), invalid: false }).subscribe();
    this.actionGate.track(epoch, subscription);
  }

  resetInvalidState(): void {
    this.navigate({
      ...DEFAULT_PHRASE_CONTEXT_URL_STATE,
      build: this._capabilities()?.activeBuildId ?? null,
    }, true);
  }

  dismissNotice(): void {
    this.notice.dismiss();
  }
  private runRoute(parsed: ParsedPhraseContextUrlState): Observable<void> {
    const resultsPageChanged =
      !parsed.invalid &&
      contextResultsPageOnlyChanged(this._route(), parsed.state) &&
      this.selection.branches() !== null;
    this._route.set(parsed.state);
    this._routeInvalid.set(parsed.invalid);
    this.requestStatus.errorMessage.set('');
    if (parsed.invalid) {
      this.requestStatus.branches.set('invalid');
      this.requestStatus.groups.set('invalid');
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
      this.resetForBuildChange(capabilities.activeBuildId);
      return EMPTY;
    }
    if (!route.resolution) {
      this.selection.clearAll();
      this.requestStatus.branches.set('idle');
      this.requestStatus.groups.set('idle');
      this.requestStatus.results.set('idle');
      this.requestStatus.occurrences.set('idle');
      if (!route.q) {
        this.resolutionFlow.restoreIdle('', route.mode);
        return of(undefined);
      }
      const resolution = this.resolutionFlow.state();
      if (
        resolution.rawQuery === route.q &&
        resolution.mode === route.mode &&
        resolution.status !== 'idle' &&
        resolution.status !== 'loading'
      ) {
        return of(undefined);
      }
      return this.resolveRestoredQuery(route);
    }
    if (this.selection.branches()) {
      this.startWorkspaceRefresh();
    } else {
      this.resolutionFlow.markLoading(route.q, route.resolution);
      this.selection.clearWorkspace();
      this.requestStatus.branches.set('loading');
      this.requestStatus.results.set('loading');
    }
    const routeKey = phraseContextStateKey(route);
    return this.workspaceLoader.loadWorkspace(route).pipe(
      tap((result) => {
        if (
          routeKey !== phraseContextStateKey(this._route()) ||
          !this.acceptLoadResult(result, 'workspace') ||
          result.kind !== 'workspace' ||
          routeKey !== phraseContextStateKey(this._route())
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
        this.selection.replaceBranches(result.branches);
        this.selection.replaceResults(result.results);
        this.resolutionFlow.restoreFromBranches(
          this.resolutionFlow.state().rawQuery,
          result.branches,
        );
        this.requestStatus.branches.set('success');
        this.requestStatus.results.set(
          result.results.totalCount === 0 ? 'empty' : 'success',
        );
      }),
      catchError((error: unknown) => this.applyRouteError(error, 'workspace', routeKey)),
      map(() => undefined),
    );
  }

  private resolveRestoredQuery(route: PhraseContextUrlState): Observable<void> {
    this.resolutionFlow.restoreIdle(route.q, route.mode);
    const routeKey = phraseContextStateKey(route);
    return this.resolutionFlow.resolve().pipe(
      tap((mapped) => {
        if (!mapped || routeKey !== phraseContextStateKey(this._route())) {
          return;
        }
        this.resolutionFlow.accept(mapped);
        if (mapped.activeBuildId && !this.ensureBuild(mapped.activeBuildId)) {
          return;
        }
        if (mapped.autoCandidate) {
          this.selectCandidate(mapped.autoCandidate);
        }
      }),
      catchError((error: unknown) => {
        if (routeKey !== phraseContextStateKey(this._route())) {
          return of(undefined);
        }
        const failure = phraseRequestFailure(error);
        this.resolutionFlow.fail(failure.status, failure.message);
        return of(undefined);
      }),
      map(() => undefined),
    );
  }

  private acceptLoadResult(
    result: PhraseContextLoadResult,
    target: PhraseContextRequestTarget,
  ): boolean {
    if (result.kind === 'failure') {
      if (result.failure.status === 'stale') {
        this.resetForBuildChange(null);
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
      this.resetForBuildChange(null);
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
    this.resetForBuildChange(activeBuildId);
    return false;
  }

  private startWorkspaceRefresh(): void {
    this.requestStatus.branches.set('refreshing');
    this.requestStatus.results.set('refreshing');
  }

  private resetForBuildChange(activeBuildId: string | null): void {
    this.actionGate.invalidate();
    this.selection.clearAll();
    this.routeCoordinator.clearBuildScopedState();
    this.notice.indexChanged();
    this.requestStatus.branches.set('stale');
    this.requestStatus.groups.set('stale');
    this.requestStatus.results.set('stale');
    this.requestStatus.occurrences.set('stale');
    this.navigate(
      {
        ...DEFAULT_PHRASE_CONTEXT_URL_STATE,
        build: activeBuildId,
        mode: this.resolutionFlow.mode(),
        q: this._route().q || this.resolutionFlow.state().rawQuery,
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
        const epoch = this.actionGate.begin();
        const subscription = this.runRoute({ state, invalid: false }).subscribe();
        this.actionGate.track(epoch, subscription);
      },
    );
    this.notice.applyNavigation(outcome);
  }

  private clearWorkspaceForNewSubmission(): void {
    this.selection.clearAll();
    this.requestStatus.branches.set('idle');
    this.requestStatus.groups.set('idle');
    this.requestStatus.results.set('idle');
    this.requestStatus.occurrences.set('idle');
    this.requestStatus.errorMessage.set('');
  }
}

function sameBuild(expected: string, actual: string): boolean {
  return expected.toLowerCase() === actual.toLowerCase();
}
