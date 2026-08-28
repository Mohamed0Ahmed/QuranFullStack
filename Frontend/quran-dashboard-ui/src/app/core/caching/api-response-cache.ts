import { Observable, finalize, shareReplay, tap } from 'rxjs';

import { ApiResponse } from '../data-access/api-response.model';

const DEFAULT_MAX_ENTRIES = 48;

interface CachedApiResponse {
  readonly response: ApiResponse<unknown>;
  readonly expiresAt: number | null;
}

interface InFlightApiResponse {
  readonly generation: number;
  readonly request: Observable<ApiResponse<unknown>>;
}

export class ApiResponseCache {
  protected readonly maxEntries: number = DEFAULT_MAX_ENTRIES;
  private readonly cache = new Map<string, CachedApiResponse>();
  private readonly inFlight = new Map<string, InFlightApiResponse>();
  private generation = 0;

  getOrLoad<T, TResponse extends ApiResponse<T> = ApiResponse<T>>(
    key: string,
    loader: () => Observable<TResponse>,
    maxAgeMs: number | null = null,
  ): Observable<TResponse> {
    const cached = this.read(key);
    if (cached) {
      return new Observable((subscriber) => {
        subscriber.next(cached.response as TResponse);
        subscriber.complete();
      });
    }

    const pending = this.inFlight.get(key);
    if (pending?.generation === this.generation) {
      return pending.request as Observable<TResponse>;
    }

    const requestGeneration = this.generation;
    let request$: Observable<TResponse>;
    request$ = loader().pipe(
      tap((response) => {
        if (
          requestGeneration === this.generation &&
          response.isSuccess &&
          response.data != null
        ) {
          this.store(key, response, maxAgeMs);
        }
      }),
      finalize(() => {
        const current = this.inFlight.get(key);
        if (current?.request === request$) {
          this.inFlight.delete(key);
        }
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    this.inFlight.set(key, {
      generation: requestGeneration,
      request: request$ as Observable<ApiResponse<unknown>>,
    });
    return request$;
  }

  peek<T>(key: string): T | null {
    const cached = this.read(key)?.response;
    if (cached?.isSuccess && cached.data != null) {
      return cached.data as T;
    }

    return null;
  }

  prefetch<T, TResponse extends ApiResponse<T> = ApiResponse<T>>(
    key: string,
    loader: () => Observable<TResponse>,
    maxAgeMs: number | null = null,
  ): void {
    if (this.read(key) || this.inFlight.has(key)) {
      return;
    }

    this.getOrLoad(key, loader, maxAgeMs).subscribe({ error: () => undefined });
  }

  store<T, TResponse extends ApiResponse<T> = ApiResponse<T>>(
    key: string,
    response: TResponse,
    maxAgeMs: number | null = null,
  ): void {
    if (!response.isSuccess || response.data == null) {
      return;
    }

    const wasCached = this.cache.delete(key);

    if (!wasCached && this.cache.size >= this.maxEntries) {
      const oldestKey = this.cache.keys().next().value;
      if (oldestKey !== undefined) {
        this.cache.delete(oldestKey);
      }
    }

    this.cache.set(key, {
      response: response as ApiResponse<unknown>,
      expiresAt: maxAgeMs === null ? null : Date.now() + Math.max(0, maxAgeMs),
    });
  }

  clear(): void {
    this.generation += 1;
    this.cache.clear();
    this.inFlight.clear();
  }

  private read(key: string): CachedApiResponse | null {
    const cached = this.cache.get(key);
    if (!cached) {
      return null;
    }
    if (cached.expiresAt !== null && cached.expiresAt <= Date.now()) {
      this.cache.delete(key);
      return null;
    }
    this.cache.delete(key);
    this.cache.set(key, cached);
    return cached;
  }
}
