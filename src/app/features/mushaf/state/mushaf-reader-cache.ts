import { Injectable } from '@angular/core';
import { Observable, finalize, shareReplay, tap } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AyahStudySourceParams } from '../data-access/mushaf-ayah-study.api';

/** Logical cache keys aligned with data-model §E. */
export const MushafReaderCacheKeys = {
  page(pageNumber: number): string {
    return `mushaf:page:${pageNumber}`;
  },

  ayahStudy(verseKey: string, sources: AyahStudySourceParams): string {
    const taf = sources.tafsirSource ?? 'none';
    const tr = sources.translationSource ?? 'none';
    const i3rab = sources.fullI3rabSource ?? 'none';
    return `mushaf:ayah-study:${verseKey}:taf:${taf}:tr:${tr}:i3rab:${i3rab}`;
  },

  wordAnalysis(wordLocation: string): string {
    return `mushaf:word-analysis:${wordLocation}`;
  },
} as const;

const DEFAULT_MAX_ENTRIES = 48;

/**
 * Bounded in-memory cache for successful Mushaf reader API responses.
 * Deduplicates concurrent identical requests via shared in-flight observables.
 */
@Injectable({ providedIn: 'root' })
export class MushafReaderCache {
  private readonly maxEntries = DEFAULT_MAX_ENTRIES;
  private readonly cache = new Map<string, ApiResponse<unknown>>();
  private readonly inFlight = new Map<string, Observable<ApiResponse<unknown>>>();

  getOrLoad<T>(key: string, loader: () => Observable<ApiResponse<T>>): Observable<ApiResponse<T>> {
    const cached = this.cache.get(key);
    if (cached) {
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

  /** Warms the cache without surfacing errors to the caller. */
  prefetch<T>(key: string, loader: () => Observable<ApiResponse<T>>): void {
    if (this.cache.has(key) || this.inFlight.has(key)) {
      return;
    }

    this.getOrLoad(key, loader).subscribe({ error: () => undefined });
  }

  private store<T>(key: string, response: ApiResponse<T>): void {
    if (this.cache.size >= this.maxEntries && !this.cache.has(key)) {
      const oldestKey = this.cache.keys().next().value;
      if (oldestKey !== undefined) {
        this.cache.delete(oldestKey);
      }
    }

    this.cache.set(key, response as ApiResponse<unknown>);
  }
}
