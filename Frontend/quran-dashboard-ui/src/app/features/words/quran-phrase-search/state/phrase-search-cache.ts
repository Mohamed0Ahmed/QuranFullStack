import {
  HttpErrorResponse,
  HttpResponse,
  HttpStatusCode,
} from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Observable,
  catchError,
  finalize,
  map,
  of,
  shareReplay,
  switchMap,
  throwError,
} from 'rxjs';

import { PhraseSearchCapabilitiesResponseApiResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response-api-response';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PhraseSearchBuildAuthorityApi } from '../data-access/phrase-search-build-authority.api';

interface PhraseBuildScopedResponse {
  readonly activeBuildId: string;
}

interface CachedPhraseResponse {
  readonly activeBuildId: string;
  readonly etag: string;
  readonly response: ApiResponse<unknown>;
}

interface InFlightPhraseResponse {
  readonly generation: number;
  readonly request: Observable<ApiResponse<unknown>>;
}

interface PhraseBuildTransition {
  readonly activeBuildId: string;
  readonly sourceGeneration: number;
  readonly targetGeneration: number;
}

class PhraseSearchRequestSupersededError extends Error {}

type PhraseSearchCacheKeyPart = string | number | null;

export function phraseSearchCacheKey(
  ...parts: readonly PhraseSearchCacheKeyPart[]
): string {
  return JSON.stringify(parts.map(encodePhraseSearchCacheKeyPart));
}

function encodePhraseSearchCacheKeyPart(
  part: PhraseSearchCacheKeyPart,
):
  | readonly ['null']
  | readonly ['string', string]
  | readonly ['number', string] {
  if (part === null) {
    return ['null'];
  }
  if (typeof part === 'string') {
    return ['string', part];
  }
  return ['number', encodePhraseSearchCacheKeyNumber(part)];
}

function encodePhraseSearchCacheKeyNumber(part: number): string {
  if (Number.isNaN(part)) {
    return 'NaN';
  }
  if (Object.is(part, -0)) {
    return '-0';
  }
  return String(part);
}

@Injectable()
export class PhraseSearchCache {
  private readonly maxEntries = 16;
  private readonly buildAuthority = inject(PhraseSearchBuildAuthorityApi);
  private readonly cache = new Map<string, CachedPhraseResponse>();
  private readonly inFlight = new Map<string, InFlightPhraseResponse>();
  private activeBuildId: string | null = null;
  private buildGeneration = 0;
  private buildTransition: PhraseBuildTransition | null = null;
  private authorityInFlight: Observable<PhraseSearchCapabilitiesResponseApiResponse> | null = null;

  capabilities(): Observable<PhraseSearchCapabilitiesResponseApiResponse> {
    return this.loadAuthoritativeCapabilities();
  }

  buildScoped<
    T extends PhraseBuildScopedResponse,
    TResponse extends ApiResponse<T>,
  >(
    key: string,
    loader: (etag: string | null) => Observable<HttpResponse<TResponse>>,
  ): Observable<TResponse> {
    const cached = this.read(key);
    const requestKey = phraseSearchCacheKey('phrase-search-request', key);
    const pending = this.inFlight.get(requestKey);
    if (pending?.generation === this.buildGeneration) {
      return pending.request as Observable<TResponse>;
    }

    const requestGeneration = this.buildGeneration;
    let request$: Observable<TResponse>;
    request$ = loader(cached?.etag ?? null).pipe(
      switchMap((response) =>
        this.acceptFreshResponse<T, TResponse>(key, response, requestGeneration),
      ),
      catchError((error: unknown) =>
        this.handleReadError<T, TResponse>(
          key,
          cached,
          error,
          requestGeneration,
        ),
      ),
      finalize(() => {
        const current = this.inFlight.get(requestKey);
        if (current?.request === request$) {
          this.inFlight.delete(requestKey);
        }
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );
    this.inFlight.set(requestKey, {
      generation: requestGeneration,
      request: request$ as Observable<ApiResponse<unknown>>,
    });
    return request$;
  }

  clear(): void {
    this.invalidateBuild();
  }

  private loadAuthoritativeCapabilities(): Observable<PhraseSearchCapabilitiesResponseApiResponse> {
    if (this.authorityInFlight) {
      return this.authorityInFlight;
    }

    const requestGeneration = this.buildGeneration;
    let request$: Observable<PhraseSearchCapabilitiesResponseApiResponse>;
    request$ = this.buildAuthority.getCapabilities().pipe(
      map((response) => {
        this.observeAuthoritativeBuild(response, requestGeneration);
        return response;
      }),
      catchError((error: unknown) =>
        this.handleAuthorityError(error, requestGeneration),
      ),
      finalize(() => {
        if (this.authorityInFlight === request$) {
          this.authorityInFlight = null;
        }
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );
    this.authorityInFlight = request$;
    return request$;
  }

  private observeAuthoritativeBuild(
    response: PhraseSearchCapabilitiesResponseApiResponse,
    requestGeneration: number,
  ): void {
    const buildId = response.isSuccess ? response.data?.activeBuildId : null;
    if (buildId) {
      if (!this.acceptResponseBuild(buildId, requestGeneration)) {
        throw new PhraseSearchRequestSupersededError();
      }
      return;
    }
    if (requestGeneration !== this.buildGeneration) {
      throw new PhraseSearchRequestSupersededError();
    }
  }

  private acceptFreshResponse<
    T extends PhraseBuildScopedResponse,
    TResponse extends ApiResponse<T>,
  >(
    key: string,
    response: HttpResponse<TResponse>,
    requestGeneration: number,
  ): Observable<TResponse> {
    const envelope = response.body;
    if (!envelope) {
      if (requestGeneration !== this.buildGeneration) {
        return this.rejectSupersededRequest();
      }
      return throwError(() => new Error('Phrase search response body is missing.'));
    }
    if (!envelope.isSuccess || !envelope.data) {
      if (requestGeneration !== this.buildGeneration) {
        return this.rejectSupersededRequest();
      }
      return of(envelope);
    }

    const buildId = envelope.data.activeBuildId;
    if (!buildId) {
      if (requestGeneration !== this.buildGeneration) {
        return this.rejectSupersededRequest();
      }
      this.remove(key);
      return throwError(
        () => new Error('Phrase search response build authority is missing.'),
      );
    }

    if (!this.acceptResponseBuild(buildId, requestGeneration)) {
      return this.rejectSupersededRequest();
    }
    const acceptedBuildId = this.activeBuildId ?? buildId;
    const etag = response.headers.get('ETag');
    if (etag) {
      this.store(key, acceptedBuildId, etag, envelope);
    } else {
      this.remove(key);
    }
    return of(envelope);
  }

  private handleReadError<
    T extends PhraseBuildScopedResponse,
    TResponse extends ApiResponse<T>,
  >(
    key: string,
    cached: CachedPhraseResponse | null,
    error: unknown,
    requestGeneration: number,
  ): Observable<TResponse> {
    if (error instanceof PhraseSearchRequestSupersededError) {
      return this.rejectSupersededRequest();
    }
    if (
      cached &&
      error instanceof HttpErrorResponse &&
      error.status === HttpStatusCode.NotModified
    ) {
      if (!this.acceptResponseBuild(cached.activeBuildId, requestGeneration)) {
        return this.rejectSupersededRequest();
      }
      this.store(key, cached.activeBuildId, cached.etag, cached.response);
      return of(cached.response as TResponse);
    }
    if (requestGeneration !== this.buildGeneration) {
      return this.rejectSupersededRequest();
    }

    this.handleConflict(error, requestGeneration);
    return throwError(() => error);
  }

  private read(key: string): CachedPhraseResponse | null {
    if (!this.activeBuildId) {
      return null;
    }
    const cacheKey = this.buildCacheKey(key, this.activeBuildId);
    const cached = this.cache.get(cacheKey);
    if (!cached) {
      return null;
    }
    this.cache.delete(cacheKey);
    this.cache.set(cacheKey, cached);
    return cached;
  }

  private store(
    key: string,
    buildId: string,
    etag: string,
    response: ApiResponse<unknown>,
  ): void {
    const cacheKey = this.buildCacheKey(key, buildId);
    const wasCached = this.cache.delete(cacheKey);
    if (!wasCached && this.cache.size >= this.maxEntries) {
      const oldestKey = this.cache.keys().next().value;
      if (oldestKey !== undefined) {
        this.cache.delete(oldestKey);
      }
    }
    this.cache.set(cacheKey, { activeBuildId: buildId, etag, response });
  }

  private remove(key: string): void {
    if (this.activeBuildId) {
      this.cache.delete(this.buildCacheKey(key, this.activeBuildId));
    }
  }

  private acceptResponseBuild(buildId: string, requestGeneration: number): boolean {
    if (requestGeneration === this.buildGeneration) {
      this.acceptBuild(buildId);
      return true;
    }

    const transition = this.buildTransition;
    return Boolean(
      transition &&
        requestGeneration === transition.sourceGeneration &&
        this.buildGeneration === transition.targetGeneration &&
        sameBuild(transition.activeBuildId, buildId) &&
        this.activeBuildId !== null &&
        sameBuild(this.activeBuildId, buildId),
    );
  }

  private acceptBuild(buildId: string): void {
    if (this.activeBuildId !== null && !sameBuild(this.activeBuildId, buildId)) {
      this.transitionBuild(buildId);
      return;
    }
    this.activeBuildId = buildId;
  }

  private transitionBuild(buildId: string): void {
    const sourceGeneration = this.buildGeneration;
    this.buildGeneration += 1;
    this.activeBuildId = buildId;
    this.buildTransition = {
      activeBuildId: buildId,
      sourceGeneration,
      targetGeneration: this.buildGeneration,
    };
    this.authorityInFlight = null;
    this.cache.clear();
    this.inFlight.clear();
  }

  private invalidateBuild(): void {
    this.buildGeneration += 1;
    this.activeBuildId = null;
    this.buildTransition = null;
    this.authorityInFlight = null;
    this.cache.clear();
    this.inFlight.clear();
  }

  private handleAuthorityError(
    error: unknown,
    requestGeneration: number,
  ): Observable<never> {
    if (
      error instanceof PhraseSearchRequestSupersededError ||
      requestGeneration !== this.buildGeneration
    ) {
      return this.rejectSupersededRequest();
    }
    this.handleConflict(error, requestGeneration);
    return throwError(() => error);
  }

  private rejectSupersededRequest<T>(): Observable<T> {
    return throwError(
      () =>
        new HttpErrorResponse({
          status: HttpStatusCode.Conflict,
          statusText: 'Conflict',
        }),
    );
  }

  private handleConflict(error: unknown, requestGeneration: number): void {
    if (
      requestGeneration === this.buildGeneration &&
      error instanceof HttpErrorResponse &&
      error.status === HttpStatusCode.Conflict
    ) {
      this.clear();
    }
  }

  private buildCacheKey(key: string, buildId: string): string {
    return phraseSearchCacheKey('phrase-search', buildId, key);
  }
}

function sameBuild(expected: string, actual: string): boolean {
  return expected.toLowerCase() === actual.toLowerCase();
}
