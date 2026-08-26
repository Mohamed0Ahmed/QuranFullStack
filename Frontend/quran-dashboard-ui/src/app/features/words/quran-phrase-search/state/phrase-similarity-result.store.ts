import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { PhraseSimilarityGroupDto } from '../../../../core/api/generated/models/phrase-similarity-group-dto';
import { PhraseSimilarityMatchDto } from '../../../../core/api/generated/models/phrase-similarity-match-dto';
import { PhraseLoadStatus } from '../models/phrase-repetitions.models';
import { PhraseSimilarityUrlState } from '../models/phrase-similarity.models';
import { PhraseActionRequestGate } from './phrase-action-request-gate';
import { phraseRequestFailure } from './phrase-request-failure';
import { PhraseSimilarityLoadResult } from './phrase-similarity-results.loader';
import { phraseSimilarityStateKey } from './phrase-similarity-url-sync';

export interface PhraseSimilarityResultHooks {
  readonly currentRoute: () => PhraseSimilarityUrlState;
  readonly acceptBuild: (activeBuildId: string) => boolean;
  readonly resetBuild: (activeBuildId: string | null) => void;
  readonly navigate: (state: PhraseSimilarityUrlState, replaceUrl: boolean) => void;
  readonly setError: (message: string) => void;
}

@Injectable()
export class PhraseSimilarityResultStore {
  private readonly gate = inject(PhraseActionRequestGate);
  readonly status = signal<PhraseLoadStatus>('idle');
  readonly groups = signal<readonly PhraseSimilarityGroupDto[]>([]);
  readonly matches = signal<readonly PhraseSimilarityMatchDto[]>([]);
  readonly totalCount = signal(0);
  readonly selectedAnchor = signal<PhraseSimilarityGroupDto | null>(null);

  selectAnchor(group: PhraseSimilarityGroupDto): void {
    this.selectedAnchor.set(group);
    this.groups.set([]);
  }

  clearAnchor(): void {
    this.selectedAnchor.set(null);
    this.matches.set([]);
  }

  clear(): void {
    this.selectedAnchor.set(null);
    this.groups.set([]);
    this.matches.set([]);
    this.totalCount.set(0);
  }

  load(
    route: PhraseSimilarityUrlState,
    request: Observable<PhraseSimilarityLoadResult>,
    expectedAnchorVariantId: number | null,
    hooks: PhraseSimilarityResultHooks,
    actionEpoch?: number,
  ): Observable<void> {
    this.status.set(this.groups().length ? 'refreshing' : 'loading');
    const routeKey = phraseSimilarityStateKey(route);
    return request.pipe(
      tap((result) => {
        if (!this.isCurrent(routeKey, expectedAnchorVariantId, hooks, actionEpoch)) {
          return;
        }
        if (result.kind === 'failure') {
          if (result.failure.status === 'stale') {
            hooks.resetBuild(null);
          } else {
            this.status.set(result.failure.status);
            hooks.setError(result.failure.message);
          }
          return;
        }
        if (!hooks.acceptBuild(result.activeBuildId)) {
          return;
        }
        if (route.page > result.lastPage) {
          hooks.navigate({ ...route, page: result.lastPage }, true);
          return;
        }
        this.groups.set(result.groups);
        this.matches.set(result.matches);
        this.totalCount.set(result.totalCount);
        this.status.set(result.totalCount === 0 ? 'empty' : 'success');
      }),
      catchError((error: unknown) => {
        if (!this.isCurrent(routeKey, expectedAnchorVariantId, hooks, actionEpoch)) {
          return of(undefined);
        }
        const failure = phraseRequestFailure(error);
        if (failure.status === 'stale') {
          hooks.resetBuild(null);
        } else {
          this.status.set(failure.status);
          hooks.setError(failure.message);
        }
        return of(undefined);
      }),
      map(() => undefined),
    );
  }

  loadAction(
    route: PhraseSimilarityUrlState,
    request: Observable<PhraseSimilarityLoadResult>,
    expectedAnchorVariantId: number | null,
    hooks: PhraseSimilarityResultHooks,
  ): void {
    const epoch = this.gate.begin();
    const subscription = this.load(
      route,
      request,
      expectedAnchorVariantId,
      hooks,
      epoch,
    ).subscribe();
    this.gate.track(epoch, subscription);
  }

  private isCurrent(
    routeKey: string,
    expectedAnchorVariantId: number | null,
    hooks: PhraseSimilarityResultHooks,
    actionEpoch?: number,
  ): boolean {
    return (
      routeKey === phraseSimilarityStateKey(hooks.currentRoute()) &&
      (this.selectedAnchor()?.anchor.variantId ?? null) === expectedAnchorVariantId &&
      (actionEpoch === undefined || this.gate.isCurrent(actionEpoch))
    );
  }
}
