import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, Observable, Subscription, map, of, switchMap } from 'rxjs';
import { catchError, distinctUntilChanged, tap } from 'rxjs/operators';

import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseSimilarityGroupDto } from '../../../../core/api/generated/models/phrase-similarity-group-dto';
import { PhraseResolutionApi } from '../data-access/phrase-resolution.api';
import {
  PHRASE_INDEX_UNAVAILABLE_MESSAGE,
} from '../models/phrase-query.models';
import { PhraseLoadStatus, PhraseTextMode } from '../models/phrase-repetitions.models';
import {
  DEFAULT_PHRASE_SIMILARITY_URL_STATE,
  PHRASE_SIMILARITY_PAGE_SIZE,
  ParsedPhraseSimilarityUrlState,
  PhraseSimilaritySource,
  PhraseSimilarityState,
  PhraseSimilarityUrlState,
} from '../models/phrase-similarity.models';
import { PhraseRouteNavigationCoordinator } from './phrase-route-navigation.coordinator';
import { PhraseActionRequestGate } from './phrase-action-request-gate';
import { PhraseNoticeStore } from './phrase-notice.store';
import { encodePhraseQuery, phraseQueryByteLength } from './phrase-query-encoding';
import { phraseEnvelopeFailure, phraseRequestFailure } from './phrase-request-failure';
import { mapPhraseResolution } from './phrase-resolution-state';
import { PhraseSimilarityResolutionStore } from './phrase-similarity-resolution.store';
import {
  PhraseSimilarityResultsLoader,
} from './phrase-similarity-results.loader';
import {
  PhraseSimilarityResultHooks,
  PhraseSimilarityResultStore,
} from './phrase-similarity-result.store';
import {
  parsePhraseSimilarityUrlState,
  phraseSimilarityStateKey,
} from './phrase-similarity-url-sync';
import {
  maximumDifferenceCount,
  minimumMatchedWords,
  percentForMaximumDifferences,
} from './phrase-similarity-threshold';
import {
  supportedGlobalSimilarityOptions,
  supportsSimilarityRoute,
} from './phrase-similarity-route-options';

const INVALID_ROUTE_MESSAGE = 'رابط المتشابهات غير صالح أو يحتوي على خيارات غير متاحة.';
const INVALID_QUERY_MESSAGE = 'اكتب عبارة من كلمتين على الأقل ولا تتجاوز 4 كيلوبايت.';

@Injectable()
export class PhraseSimilarityFacade {
  private readonly resultsLoader = inject(PhraseSimilarityResultsLoader);
  private readonly resolutionApi = inject(PhraseResolutionApi);
  private readonly routeCoordinator = inject(PhraseRouteNavigationCoordinator);
  private readonly actionGate = inject(PhraseActionRequestGate);
  private readonly notice = inject(PhraseNoticeStore);
  private readonly results = inject(PhraseSimilarityResultStore);
  private readonly resolution = inject(PhraseSimilarityResolutionStore);

  private readonly _route = signal(DEFAULT_PHRASE_SIMILARITY_URL_STATE);
  private readonly _routeInvalid = signal(false);
  private readonly _capabilitiesStatus = signal<PhraseLoadStatus>('idle');
  private readonly _capabilities = signal<
    import('../../../../core/api/generated/models/phrase-search-capabilities-response').PhraseSearchCapabilitiesResponse | null
  >(null);
  private readonly _errorMessage = signal('');

  private route?: ActivatedRoute;
  private routeSub?: Subscription;
  private readonly resultHooks: PhraseSimilarityResultHooks = {
    currentRoute: () => this._route(),
    acceptBuild: (activeBuildId) => this.ensureBuild(activeBuildId),
    resetBuild: (activeBuildId) => this.resetForBuildChange(activeBuildId),
    navigate: (state, replaceUrl) => this.navigate(state, replaceUrl),
    setError: (message) => this._errorMessage.set(message),
  };

  readonly state = computed<PhraseSimilarityState>(() => ({
    route: this._route(),
    routeInvalid: this._routeInvalid(),
    capabilitiesStatus: this._capabilitiesStatus(),
    capabilities: this._capabilities(),
    resolutionStatus: this.resolution.status(),
    candidates: this.resolution.candidates(),
    resultsStatus: this.results.status(),
    groups: this.results.groups(),
    matches: this.results.matches(),
    totalCount: this.results.totalCount(),
    selectedAnchor: this.results.selectedAnchor(),
    errorMessage: this._errorMessage(),
    notice: this.notice.message(),
    sessionOnly: this.notice.sessionOnly(),
  }));
  readonly draft = this.resolution.draft.asReadonly();

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();
    this.route = route;
    this.routeCoordinator.bind(route);
    this.routeSub = route.queryParamMap
      .pipe(
        tap(() => this.actionGate.invalidate()),
        map(parsePhraseSimilarityUrlState),
        map((parsed) => {
          const restored = this.routeCoordinator.restoreSimilarity(parsed);
          this.notice.applyNavigation(restored.outcome);
          return restored.parsed;
        }),
        distinctUntilChanged(
          (a, b) => a.invalid === b.invalid && phraseSimilarityStateKey(a.state) === phraseSimilarityStateKey(b.state),
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

  setDraft(query: string): void {
    if (!this.resolution.setDraft(query)) {
      return;
    }
    this.actionGate.invalidate();
    this._errorMessage.set('');
    this.clearResultData();
    this.results.status.set('idle');
  }

  setSource(source: PhraseSimilaritySource): void {
    if (source === this._route().source) {
      return;
    }
    this.cancelAndClearResults();
    const global = supportedGlobalSimilarityOptions(
      this._capabilities(),
      this._route().mode,
      this._route().length,
      this._route().min,
    );
    this.navigate({
      ...this._route(),
      source,
      q: source === 'manual' ? this.resolution.draft() : '',
      resolution: null,
      length: source === 'global' ? global.length : this._route().length,
      min: source === 'global' ? global.minimum : this._route().min,
      page: 1,
    });
  }

  setMode(mode: PhraseTextMode): void {
    if (mode === this._route().mode) {
      return;
    }
    this.cancelAndClearResults();
    this.resolution.reset();
    this.navigate({ ...this._route(), mode, resolution: null, page: 1 });
  }

  setLength(length: number): void {
    if (length === this._route().length) {
      return;
    }
    this.cancelAndClearResults();
    this.navigate({ ...this._route(), length, page: 1 });
  }

  setMinimumPercent(minimum: number): void {
    if (minimum === this._route().min) {
      return;
    }
    this.cancelAndClearResults();
    this.navigate({ ...this._route(), min: minimum, page: 1 });
  }

  setMaximumDifferences(maximumDifferences: number): void {
    this.setMinimumPercent(
      percentForMaximumDifferences(this._route().length, maximumDifferences),
    );
  }

  setPage(page: number): void {
    if (page !== this._route().page) {
      this.navigate({ ...this._route(), page });
    }
  }

  submitQuery(): void {
    const query = this.resolution.draft().trim();
    this.cancelAndClearResults();
    this.results.status.set('idle');
    if (!query || phraseQueryByteLength(query) > 4096) {
      this.resolution.fail('invalid');
      this._errorMessage.set(INVALID_QUERY_MESSAGE);
      return;
    }
    this.resolution.start();
    this._errorMessage.set('');
    const mode = this._route().mode;
    const epoch = this.actionGate.begin();
    const subscription = this.resolutionApi
      .resolve(mode, encodePhraseQuery(query))
      .pipe(
        tap((response) => {
          if (
            this.actionGate.isCurrent(epoch) &&
            this.resolution.draft().trim() === query &&
            this._route().mode === mode
          ) {
            this.acceptResolution(query, mode, response);
          }
        }),
        catchError((error: unknown) => {
          if (!this.actionGate.isCurrent(epoch)) {
            return of(undefined);
          }
          const failure = phraseRequestFailure(error);
          this.resolution.fail(failure.status);
          this._errorMessage.set(failure.message);
          return of(undefined);
        }),
      )
      .subscribe();
    this.actionGate.track(epoch, subscription);
  }

  selectCandidate(candidate: PhraseResolutionCandidateDto): void {
    if (candidate.wordCount < 2) {
      this.resolution.select(candidate);
      this.resolution.fail('invalid');
      this._errorMessage.set(INVALID_QUERY_MESSAGE);
      return;
    }
    this.resolution.select(candidate);
    this.navigate({
      ...this._route(),
      build: this._capabilities()?.activeBuildId ?? this._route().build,
      source: 'manual',
      q: this.resolution.draft(),
      resolution: candidate.resolutionRef,
      length: candidate.wordCount,
      page: 1,
    });
  }

  selectAnchor(group: PhraseSimilarityGroupDto): void {
    this.results.selectAnchor(group);
    const wasFirstPage = this._route().page === 1;
    this.navigate({ ...this._route(), page: 1 }, true);
    if (wasFirstPage) {
      this.loadAnchorMatches(group, 1);
    }
  }

  clearAnchor(): void {
    this.results.clearAnchor();
    const wasFirstPage = this._route().page === 1;
    this.navigate({ ...this._route(), page: 1 });
    if (wasFirstPage) {
      const route = { ...this._route(), page: 1 };
      this.results.loadAction(
        route,
        this.resultsLoader.loadGroups(route),
        null,
        this.resultHooks,
      );
    }
  }

  retry(): void {
    if (
      this._capabilitiesStatus() === 'error' ||
      this._capabilitiesStatus() === 'unavailable'
    ) {
      this._capabilities.set(null);
    }
    const epoch = this.actionGate.begin();
    const subscription = this.runRoute({ state: this._route(), invalid: false }).subscribe();
    this.actionGate.track(epoch, subscription);
  }

  resetInvalidState(): void {
    this.navigate({
      ...DEFAULT_PHRASE_SIMILARITY_URL_STATE,
      build: this._capabilities()?.activeBuildId ?? null,
    }, true);
  }

  dismissNotice(): void {
    this.notice.dismiss();
  }

  minimumMatchedWords(): number {
    return minimumMatchedWords(this._route().length, this._route().min);
  }

  maximumDifferences(): number {
    return maximumDifferenceCount(this._route().length, this._route().min);
  }

  private runRoute(parsed: ParsedPhraseSimilarityUrlState): Observable<void> {
    this._route.set(parsed.state);
    this._routeInvalid.set(parsed.invalid);
    this.resolution.restoreDraft(parsed.state.q);
    this._errorMessage.set('');
    if (parsed.invalid) {
      this.clearResultData();
      this.results.status.set('invalid');
      this._errorMessage.set(INVALID_ROUTE_MESSAGE);
      return EMPTY;
    }
    const capabilities = this._capabilities();
    if (capabilities) {
      return this.runWithCapabilities(parsed.state);
    }
    this._capabilitiesStatus.set('loading');
    const routeKey = phraseSimilarityStateKey(parsed.state);
    return this.resolutionApi.getCapabilities().pipe(
      switchMap((response) => {
        if (routeKey !== phraseSimilarityStateKey(this._route())) {
          return EMPTY;
        }
        if (!response.isSuccess || !response.data) {
          const failure = phraseEnvelopeFailure(response.errors, response.message);
          this._capabilitiesStatus.set(failure.status);
          this._errorMessage.set(failure.message);
          return EMPTY;
        }
        this._capabilities.set(response.data);
        this._capabilitiesStatus.set('success');
        return this.runWithCapabilities(parsed.state);
      }),
      catchError((error: unknown) => this.applyRouteError(error, routeKey)),
    );
  }

  private runWithCapabilities(route: PhraseSimilarityUrlState): Observable<void> {
    const capabilities = this._capabilities();
    if (!capabilities?.similarityReady) {
      this.clearResultData();
      this._capabilitiesStatus.set('unavailable');
      this._errorMessage.set(PHRASE_INDEX_UNAVAILABLE_MESSAGE);
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
    if (!supportsSimilarityRoute(this._capabilities(), route)) {
      this._routeInvalid.set(true);
      this.results.status.set('invalid');
      this._errorMessage.set(INVALID_ROUTE_MESSAGE);
      return EMPTY;
    }
    if (route.source === 'manual') {
      if (!route.resolution) {
        this.results.status.set('idle');
        this.clearResultData();
        if (!route.q) {
          this.resolution.reset();
          return of(undefined);
        }
        if (
          this.resolution.draft() === route.q &&
          this.resolution.status() !== 'idle' &&
          this.resolution.status() !== 'loading'
        ) {
          return of(undefined);
        }
        return this.resolveRestoredQuery(route);
      }
      this.resolution.fail('resolved');
      return this.results.load(
        route,
        this.resultsLoader.loadManual(route),
        null,
        this.resultHooks,
      );
    }
    const anchor = this.results.selectedAnchor();
    return anchor
      ? this.results.load(
          route,
          this.resultsLoader.loadMatches(route, anchor),
          anchor.anchor.variantId,
          this.resultHooks,
        )
      : this.results.load(
          route,
          this.resultsLoader.loadGroups(route),
          null,
          this.resultHooks,
        );
  }

  private loadAnchorMatches(group: PhraseSimilarityGroupDto, page: number): void {
    const route = { ...this._route(), page };
    this.results.loadAction(
      route,
      this.resultsLoader.loadMatches(route, group),
      group.anchor.variantId,
      this.resultHooks,
    );
  }

  private acceptResolution(
    query: string,
    mode: PhraseTextMode,
    response: import('../../../../core/api/generated/models/phrase-query-resolution-response-api-response').PhraseQueryResolutionResponseApiResponse,
  ): void {
    const mapped = mapPhraseResolution(query, mode, response);
    this.resolution.accept(mapped);
    this._errorMessage.set(mapped.state.message);
    if (mapped.activeBuildId && !this.ensureBuild(mapped.activeBuildId)) {
      return;
    }
    this.resolution.restoreDraft(query);
    if (mapped.autoCandidate) {
      this.selectCandidate(mapped.autoCandidate);
    } else if (this._route().source === 'manual' && this._route().q !== query) {
      this.navigate({
        ...this._route(),
        q: query,
        resolution: null,
        page: 1,
      });
    }
  }

  private resolveRestoredQuery(route: PhraseSimilarityUrlState): Observable<void> {
    const routeKey = phraseSimilarityStateKey(route);
    this.resolution.restoreDraft(route.q);
    this.resolution.start();
    return this.resolutionApi
      .resolve(route.mode, encodePhraseQuery(route.q))
      .pipe(
        tap((response) => {
          if (routeKey === phraseSimilarityStateKey(this._route())) {
            this.acceptResolution(route.q, route.mode, response);
          }
        }),
        catchError((error: unknown) => {
          if (routeKey !== phraseSimilarityStateKey(this._route())) {
            return of(undefined);
          }
          const failure = phraseRequestFailure(error);
          this.resolution.fail(failure.status);
          this._errorMessage.set(failure.message);
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private cancelAndClearResults(): void {
    this.actionGate.invalidate();
    this.clearResultData();
  }

  private clearResultData(): void {
    this.results.clear();
  }

  private applyRouteError(error: unknown, routeKey: string): Observable<void> {
    if (routeKey !== phraseSimilarityStateKey(this._route())) {
      return of(undefined);
    }
    const failure = phraseRequestFailure(error);
    if (failure.status === 'stale') {
      this.resetForBuildChange(null);
    } else {
      this.results.status.set(failure.status);
      this._errorMessage.set(failure.message);
    }
    return of(undefined);
  }

  private ensureBuild(activeBuildId: string): boolean {
    const expected = this._route().build ?? this._capabilities()?.activeBuildId;
    if (expected && sameBuild(expected, activeBuildId)) {
      return true;
    }
    this.resetForBuildChange(activeBuildId);
    return false;
  }

  private resetForBuildChange(activeBuildId: string | null): void {
    this.actionGate.invalidate();
    this.clearResultData();
    this.resolution.reset('stale');
    this.routeCoordinator.clearBuildScopedState();
    this.notice.indexChanged();
    this.results.status.set('stale');
    this.navigate(
      {
        ...DEFAULT_PHRASE_SIMILARITY_URL_STATE,
        build: activeBuildId,
        source: this._route().source,
        q: this.resolution.draft(),
        mode: this._route().mode,
      },
      true,
    );
  }

  private navigate(state: PhraseSimilarityUrlState, replaceUrl = false): void {
    const outcome = this.routeCoordinator.navigateSimilarity(
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
}

function sameBuild(expected: string, actual: string): boolean {
  return expected.toLowerCase() === actual.toLowerCase();
}
