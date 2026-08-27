import { Injectable, signal } from '@angular/core';
import { Observable, map, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { PhraseSimilarityAyahDto } from '../../../../core/api/generated/models/phrase-similarity-ayah-dto';
import { PhraseSimilarityPhraseDto } from '../../../../core/api/generated/models/phrase-similarity-phrase-dto';
import { PhraseLoadStatus } from '../models/phrase-repetitions.models';
import { PhraseSimilarityUrlState } from '../models/phrase-similarity.models';
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
  readonly status = signal<PhraseLoadStatus>('idle');
  readonly ayahs = signal<readonly PhraseSimilarityAyahDto[]>([]);
  readonly totalAyahCount = signal(0);
  readonly totalOccurrenceCount = signal(0);
  readonly queryPhrase = signal<PhraseSimilarityPhraseDto | null>(null);

  clear(): void {
    this.ayahs.set([]);
    this.totalAyahCount.set(0);
    this.totalOccurrenceCount.set(0);
    this.queryPhrase.set(null);
  }

  load(
    route: PhraseSimilarityUrlState,
    request: Observable<PhraseSimilarityLoadResult>,
    hooks: PhraseSimilarityResultHooks,
  ): Observable<void> {
    this.status.set(this.ayahs().length ? 'refreshing' : 'loading');
    const routeKey = phraseSimilarityStateKey(route);
    return request.pipe(
      tap((result) => {
        if (routeKey !== phraseSimilarityStateKey(hooks.currentRoute())) {
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
        this.ayahs.set(result.ayahs);
        this.totalAyahCount.set(result.totalAyahCount);
        this.totalOccurrenceCount.set(result.totalOccurrenceCount);
        this.queryPhrase.set(result.queryPhrase);
        this.status.set(result.totalAyahCount === 0 ? 'empty' : 'success');
      }),
      catchError((error: unknown) => {
        if (routeKey !== phraseSimilarityStateKey(hooks.currentRoute())) {
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
}
