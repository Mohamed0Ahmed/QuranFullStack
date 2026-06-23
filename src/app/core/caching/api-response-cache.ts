import { Observable, finalize, shareReplay, tap } from 'rxjs';

import { ApiResponse } from '../data-access/api-response.model';

const DEFAULT_MAX_ENTRIES = 48;

export class ApiResponseCache {
  private readonly maxEntries = DEFAULT_MAX_ENTRIES;
  private readonly cache = new Map<string, ApiResponse<unknown>>();
  private readonly inFlight = new Map<string, Observable<ApiResponse<unknown>>>();

  getOrLoad<T>(key: string, loader: () => Observable<ApiResponse<T>>): Observable<ApiResponse<T>> {
    const cached = this.cache.get(key);
    if (cached) {
      this.refreshRecency(key, cached);
      return new Observable((subscriber) => {
        subscriber.next(cached as ApiResponse<T>);
        subscriber.complete();
      });
    }

    const pending = this.inFlight.get(key);
    if (pending) {
      return pending as Observable<ApiResponse<T>>;
    }

    const request$ = loader().pipe(
      tap((response) => {
        if (response.isSuccess && response.data != null) {
          this.store(key, response);
        }
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
      finalize(() => this.inFlight.delete(key)),
    );

    this.inFlight.set(key, request$ as Observable<ApiResponse<unknown>>);
    return request$;
  }

  // Returns cached data synchronously; in-flight-only requests count as a miss.
  peek<T>(key: string): T | null {
    const cached = this.cache.get(key);
    if (cached?.isSuccess && cached.data != null) {
      this.refreshRecency(key, cached);
      return cached.data as T;
    }

    return null;
  }

  prefetch<T>(key: string, loader: () => Observable<ApiResponse<T>>): void {
    if (this.cache.has(key) || this.inFlight.has(key)) {
      return;
    }

    this.getOrLoad(key, loader).subscribe({ error: () => undefined });
  }

  store<T>(key: string, response: ApiResponse<T>): void {
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

    this.cache.set(key, response as ApiResponse<unknown>);
  }

  private refreshRecency(key: string, response: ApiResponse<unknown>): void {
    this.cache.delete(key);
    this.cache.set(key, response);
  }
}
