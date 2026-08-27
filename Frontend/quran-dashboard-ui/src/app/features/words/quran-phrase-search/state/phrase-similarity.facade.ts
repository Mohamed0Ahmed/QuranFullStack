import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, Observable, Subscription, map, of, switchMap } from 'rxjs';
import { catchError, distinctUntilChanged, tap } from 'rxjs/operators';

import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseResolutionApi } from '../data-access/phrase-resolution.api';
import { PHRASE_INDEX_UNAVAILABLE_MESSAGE } from '../models/phrase-query.models';
import { PhraseLoadStatus, PhraseTextMode } from '../models/phrase-repetitions.models';
import {
  DEFAULT_PHRASE_SIMILARITY_URL_STATE,
  ParsedPhraseSimilarityUrlState,
  PhraseSimilarityState,
  PhraseSimilarityUrlState,
} from '../models/phrase-similarity.models';
import { PhraseRouteNavigationCoordinator } from './phrase-route-navigation.coordinator';
import { PhraseActionRequestGate } from './phrase-action-request-gate';
import { PhraseNoticeStore } from './phrase-notice.store';
import { phraseEnvelopeFailure, phraseRequestFailure } from './phrase-request-failure';
import { PhraseSimilarityQueryCoordinator, PhraseSimilarityQueryHooks } from './phrase-similarity-query.coordinator';
import { PhraseSimilarityResolutionStore } from './phrase-similarity-resolution.store';
import { PhraseSimilarityResultsLoader } from './phrase-similarity-results.loader';
import { PhraseSimilarityResultHooks, PhraseSimilarityResultStore } from './phrase-similarity-result.store';
import { parsePhraseSimilarityUrlState, phraseSimilarityStateKey } from './phrase-similarity-url-sync';
import { maximumDifferenceCount, minimumMatchedWords, percentForMaximumDifferences } from './phrase-similarity-threshold';
import { supportsSimilarityRoute } from './phrase-similarity-route-options';

const INVALID_ROUTE_MESSAGE = 'رابط المتشابهات غير صالح أو يحتوي على خيارات غير متاحة.';

@Injectable()
export class PhraseSimilarityFacade {
  private readonly resultsLoader = inject(PhraseSimilarityResultsLoader);
  private readonly capabilitiesApi = inject(PhraseResolutionApi);
  private readonly query = inject(PhraseSimilarityQueryCoordinator);
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
  private readonly queryHooks: PhraseSimilarityQueryHooks = {
    currentRoute: () => this._route(),
    activeBuildId: () => this._capabilities()?.activeBuildId ?? null,
    clearResults: () => this.cancelAndClearResults(),
    setResultsIdle: () => this.results.status.set('idle'),
    setError: (message) => this._errorMessage.set(message),
    ensureBuild: (activeBuildId) => this.ensureBuild(activeBuildId),
    navigate: (state) => this.navigate(state),
  };

  readonly state = computed<PhraseSimilarityState>(() => ({
    route: this._route(),
    routeInvalid: this._routeInvalid(),
    capabilitiesStatus: this._capabilitiesStatus(),
    capabilities: this._capabilities(),
    resolutionStatus: this.resolution.status(),
    candidates: this.resolution.candidates(),
    resultsStatus: this.results.status(),
    ayahs: this.results.ayahs(),
    totalAyahCount: this.results.totalAyahCount(),
    totalOccurrenceCount: this.results.totalOccurrenceCount(),
    queryPhrase: this.results.queryPhrase(),
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
    this.query.setDraft(query, this.queryHooks);
  }

  setMode(mode: PhraseTextMode): void {
    if (mode === this._route().mode) {
      return;
    }
    this.cancelAndClearResults();
    this.resolution.reset();
    this.navigate({ ...this._route(), mode, resolution: null, page: 1 });
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
    this.query.submit(this._route(), this.queryHooks);
  }

  selectCandidate(candidate: PhraseResolutionCandidateDto): void {
    this.query.selectCandidate(candidate, this._route(), this.queryHooks);
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
    return this.capabilitiesApi.getCapabilities().pipe(
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
      return this.query.resolveRestored(route, this.queryHooks);
    }
    this.resolution.fail('resolved');
    return this.results.load(
      route,
      this.resultsLoader.load(route),
      this.resultHooks,
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
