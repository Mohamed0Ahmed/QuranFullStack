import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseContextUrlState } from '../models/phrase-context.models';
import { PhraseTextMode } from '../models/phrase-repetitions.models';
import { PhraseActionRequestGate } from './phrase-action-request-gate';
import { PhraseContextResolutionStore } from './phrase-context-resolution.store';
import { phraseContextStateKey } from './phrase-context-url-sync';
import { phraseRequestFailure } from './phrase-request-failure';
import {
  PhraseResolutionRequestIdentity,
  createPhraseResolutionRequestIdentity,
  isPhraseResolutionRequestCurrent,
  normalizePhraseResolutionRequestDraft,
} from './phrase-resolution-request-identity';
import { MappedPhraseResolution } from './phrase-resolution-state';

export interface PhraseContextQueryHooks {
  readonly currentRoute: () => PhraseContextUrlState;
  readonly isCommittedWorkspaceCurrent: () => boolean;
  readonly reloadCurrentRoute: () => void;
  readonly clearWorkspace: () => void;
  readonly acceptBuild: (activeBuildId: string) => boolean;
  readonly selectCandidate: (candidate: PhraseResolutionCandidateDto) => void;
  readonly navigate: (state: PhraseContextUrlState) => void;
}

@Injectable()
export class PhraseContextQueryCoordinator {
  private readonly gate = inject(PhraseActionRequestGate);
  private readonly resolution = inject(PhraseContextResolutionStore);

  invalidate(): void {
    this.gate.invalidate('query');
  }

  submit(hooks: PhraseContextQueryHooks): void {
    const submittedMode = this.resolution.mode();
    const submittedDraft = normalizePhraseResolutionRequestDraft(
      this.resolution.state().rawQuery,
    );
    const route = hooks.currentRoute();
    if (
      route.resolution &&
      route.mode === submittedMode &&
      normalizePhraseResolutionRequestDraft(route.q) === submittedDraft
    ) {
      if (!hooks.isCommittedWorkspaceCurrent()) {
        hooks.reloadCurrentRoute();
      }
      return;
    }
    hooks.clearWorkspace();
    const identity = this.beginIdentity(submittedDraft, submittedMode, route);
    this.run(identity, hooks, false);
  }

  resolveRestored(
    route: PhraseContextUrlState,
    hooks: PhraseContextQueryHooks,
  ): Observable<void> {
    this.resolution.restoreIdle(route.q, route.mode);
    const identity = this.beginIdentity(route.q, route.mode, route);
    this.run(identity, hooks, true);
    return of(undefined);
  }

  retry(hooks: PhraseContextQueryHooks): void {
    const request = this.resolution.prepareRetry();
    if (!request) {
      return;
    }
    const route = hooks.currentRoute();
    const restoredRequest =
      route.resolution === null &&
      route.mode === request.mode &&
      normalizePhraseResolutionRequestDraft(route.q) ===
        normalizePhraseResolutionRequestDraft(request.rawQuery);
    const identity = this.beginIdentity(request.rawQuery, request.mode, route);
    this.run(identity, hooks, restoredRequest);
  }

  private run(
    identity: PhraseResolutionRequestIdentity,
    hooks: PhraseContextQueryHooks,
    restoredRequest: boolean,
  ): void {
    const subscription = this.resolution
      .resolve()
      .pipe(
        tap((mapped) => {
          if (restoredRequest) {
            this.acceptRestored(mapped, identity, hooks);
          } else {
            this.acceptManual(mapped, identity, hooks);
          }
        }),
        catchError((error: unknown) => this.fail(error, identity, hooks)),
      )
      .subscribe();
    this.gate.track('query', identity.epoch, subscription);
  }

  private acceptManual(
    mapped: MappedPhraseResolution | null,
    identity: PhraseResolutionRequestIdentity,
    hooks: PhraseContextQueryHooks,
  ): void {
    if (!mapped || !this.isCurrent(identity, hooks)) {
      return;
    }
    this.resolution.accept(mapped);
    if (mapped.activeBuildId && !hooks.acceptBuild(mapped.activeBuildId)) {
      return;
    }
    if (mapped.autoCandidate) {
      hooks.selectCandidate(mapped.autoCandidate);
      return;
    }
    hooks.navigate({
      ...hooks.currentRoute(),
      mode: identity.mode,
      q: identity.normalizedDraft,
      resolution: null,
      before: null,
      after: null,
      previousAlternatives: null,
      followingAlternatives: null,
      contextsPage: 1,
    });
  }

  private acceptRestored(
    mapped: MappedPhraseResolution | null,
    identity: PhraseResolutionRequestIdentity,
    hooks: PhraseContextQueryHooks,
  ): void {
    if (!mapped || !this.isCurrent(identity, hooks)) {
      return;
    }
    this.resolution.accept(mapped);
    if (mapped.activeBuildId && !hooks.acceptBuild(mapped.activeBuildId)) {
      return;
    }
    if (mapped.autoCandidate) {
      hooks.selectCandidate(mapped.autoCandidate);
    }
  }

  private fail(
    error: unknown,
    identity: PhraseResolutionRequestIdentity,
    hooks: PhraseContextQueryHooks,
  ): Observable<undefined> {
    if (this.isCurrent(identity, hooks)) {
      const failure = phraseRequestFailure(error);
      this.resolution.fail(failure.status, failure.message);
    }
    return of(undefined);
  }

  private beginIdentity(
    draft: string,
    mode: PhraseTextMode,
    route: PhraseContextUrlState,
  ): PhraseResolutionRequestIdentity {
    const epoch = this.gate.begin('query');
    return createPhraseResolutionRequestIdentity(epoch, {
      draft,
      mode,
      routeKey: phraseContextStateKey(route),
    });
  }

  private isCurrent(
    identity: PhraseResolutionRequestIdentity,
    hooks: PhraseContextQueryHooks,
  ): boolean {
    const resolution = this.resolution.state();
    return isPhraseResolutionRequestCurrent(
      identity,
      this.gate.isCurrent('query', identity.epoch),
      {
        draft: resolution.rawQuery,
        mode: this.resolution.mode(),
        routeKey: phraseContextStateKey(hooks.currentRoute()),
      },
    );
  }
}
