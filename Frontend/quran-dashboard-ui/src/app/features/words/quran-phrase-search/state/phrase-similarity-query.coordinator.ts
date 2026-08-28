import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { PhraseQueryResolutionResponseApiResponse } from '../../../../core/api/generated/models/phrase-query-resolution-response-api-response';
import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseResolutionApi } from '../data-access/phrase-resolution.api';
import { PhraseTextMode } from '../models/phrase-repetitions.models';
import { PhraseSimilarityUrlState } from '../models/phrase-similarity.models';
import { PhraseActionRequestGate } from './phrase-action-request-gate';
import { encodePhraseQuery, phraseQueryByteLength } from './phrase-query-encoding';
import { phraseRequestFailure } from './phrase-request-failure';
import {
  PhraseResolutionRequestIdentity,
  createPhraseResolutionRequestIdentity,
  isPhraseResolutionRequestCurrent,
  normalizePhraseResolutionRequestDraft,
} from './phrase-resolution-request-identity';
import { mapPhraseResolution } from './phrase-resolution-state';
import { PhraseSimilarityResolutionStore } from './phrase-similarity-resolution.store';
import { percentForMaximumDifferences } from './phrase-similarity-threshold';
import { phraseSimilarityStateKey } from './phrase-similarity-url-sync';

const INVALID_QUERY_MESSAGE = 'اكتب عبارة من كلمتين على الأقل.';

export interface PhraseSimilarityQueryHooks {
  readonly currentRoute: () => PhraseSimilarityUrlState;
  readonly currentMode: () => PhraseTextMode;
  readonly activeBuildId: () => string | null;
  readonly clearResults: () => void;
  readonly setResultsIdle: () => void;
  readonly setError: (message: string) => void;
  readonly ensureBuild: (activeBuildId: string) => boolean;
  readonly navigate: (state: PhraseSimilarityUrlState) => void;
}

@Injectable()
export class PhraseSimilarityQueryCoordinator {
  private readonly api = inject(PhraseResolutionApi);
  private readonly gate = inject(PhraseActionRequestGate);
  private readonly resolution = inject(PhraseSimilarityResolutionStore);

  invalidate(): void {
    this.gate.invalidate('query');
  }

  setDraft(query: string, hooks: PhraseSimilarityQueryHooks): void {
    if (!this.resolution.setDraft(query)) {
      return;
    }
    this.gate.invalidate('query');
    hooks.setError('');
  }

  submit(route: PhraseSimilarityUrlState, hooks: PhraseSimilarityQueryHooks): void {
    const query = normalizePhraseResolutionRequestDraft(this.resolution.draft());
    hooks.clearResults();
    hooks.setResultsIdle();
    if (!query || phraseQueryByteLength(query) > 4096) {
      this.resolution.fail('invalid');
      hooks.setError(INVALID_QUERY_MESSAGE);
      return;
    }

    this.resolution.start();
    hooks.setError('');
    const identity = this.beginIdentity(
      query,
      route.mode,
      phraseSimilarityStateKey(hooks.currentRoute()),
    );
    const subscription = this.api
      .resolve(route.mode, encodePhraseQuery(query))
      .pipe(
        tap((response) => {
          if (this.isCurrent(identity, hooks)) {
            this.acceptResolution(query, route.mode, response, hooks);
          }
        }),
        catchError((error: unknown) => {
          if (this.isCurrent(identity, hooks)) {
            const failure = phraseRequestFailure(error);
            this.resolution.fail(failure.status);
            hooks.setError(failure.message);
          }
          return of(undefined);
        }),
      )
      .subscribe();
    this.gate.track('query', identity.epoch, subscription);
  }

  selectCandidate(
    candidate: PhraseResolutionCandidateDto,
    route: PhraseSimilarityUrlState,
    hooks: PhraseSimilarityQueryHooks,
  ): void {
    if (candidate.wordCount < 2) {
      this.resolution.select(candidate);
      this.resolution.fail('invalid');
      hooks.setError(INVALID_QUERY_MESSAGE);
      return;
    }
    this.resolution.select(candidate);
    hooks.navigate({
      ...route,
      build: hooks.activeBuildId() ?? route.build,
      q: this.resolution.draft(),
      resolution: candidate.resolutionRef,
      length: candidate.wordCount,
      min: percentForMaximumDifferences(candidate.wordCount, 1),
      page: 1,
    });
  }

  resolveRestored(
    route: PhraseSimilarityUrlState,
    hooks: PhraseSimilarityQueryHooks,
  ): Observable<void> {
    const routeKey = phraseSimilarityStateKey(route);
    this.resolution.restoreDraft(route.q);
    this.resolution.start();
    const identity = this.beginIdentity(route.q, route.mode, routeKey);
    const subscription = this.api
      .resolve(route.mode, encodePhraseQuery(route.q))
      .pipe(
        tap((response) => {
          if (this.isCurrent(identity, hooks)) {
            this.acceptResolution(route.q, route.mode, response, hooks);
          }
        }),
        catchError((error: unknown) => {
          if (this.isCurrent(identity, hooks)) {
            const failure = phraseRequestFailure(error);
            this.resolution.fail(failure.status);
            hooks.setError(failure.message);
          }
          return of(undefined);
        }),
      )
      .subscribe();
    this.gate.track('query', identity.epoch, subscription);
    return of(undefined);
  }

  private beginIdentity(
    draft: string,
    mode: PhraseTextMode,
    routeKey: string,
  ): PhraseResolutionRequestIdentity {
    const epoch = this.gate.begin('query');
    return createPhraseResolutionRequestIdentity(epoch, { draft, mode, routeKey });
  }

  private isCurrent(
    identity: PhraseResolutionRequestIdentity,
    hooks: PhraseSimilarityQueryHooks,
  ): boolean {
    return isPhraseResolutionRequestCurrent(
      identity,
      this.gate.isCurrent('query', identity.epoch),
      {
        draft: this.resolution.draft(),
        mode: hooks.currentMode(),
        routeKey: phraseSimilarityStateKey(hooks.currentRoute()),
      },
    );
  }

  private acceptResolution(
    query: string,
    mode: PhraseTextMode,
    response: PhraseQueryResolutionResponseApiResponse,
    hooks: PhraseSimilarityQueryHooks,
  ): void {
    const mapped = mapPhraseResolution(query, mode, response);
    this.resolution.accept(mapped);
    hooks.setError(mapped.state.message);
    if (mapped.activeBuildId && !hooks.ensureBuild(mapped.activeBuildId)) {
      return;
    }
    this.resolution.restoreDraft(query);
    if (mapped.autoCandidate) {
      this.selectCandidate(
        mapped.autoCandidate,
        { ...hooks.currentRoute(), mode },
        hooks,
      );
    } else if (
      hooks.currentRoute().q !== query ||
      hooks.currentRoute().mode !== mode
    ) {
      hooks.navigate({
        ...hooks.currentRoute(),
        mode,
        q: query,
        resolution: null,
        page: 1,
      });
    }
  }
}
