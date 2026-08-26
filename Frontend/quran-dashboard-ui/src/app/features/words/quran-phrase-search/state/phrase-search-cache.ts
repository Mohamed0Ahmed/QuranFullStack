import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { ApiResponseCache } from '../../../../core/caching/api-response-cache';
import { ApiResponse } from '../../../../core/data-access/api-response.model';

interface PhraseBuildScopedResponse {
  readonly activeBuildId: string;
}

const CAPABILITIES_MAX_AGE_MS = 0;

export function phraseSearchCacheKey(
  ...parts: readonly (string | number | null)[]
): string {
  return parts
    .map((part) => encodeURIComponent(part === null ? 'none' : String(part)))
    .join(':');
}

@Injectable({ providedIn: 'root' })
export class PhraseSearchCache extends ApiResponseCache {
  protected override readonly maxEntries = 16;
  private activeBuildId: string | null = null;

  capabilities<
    T extends PhraseBuildScopedResponse,
    TResponse extends ApiResponse<T>,
  >(
    loader: () => Observable<TResponse>,
  ): Observable<TResponse> {
    return this.getOrLoad<T, TResponse>(
      'phrase-search:capabilities',
      () => this.observeBuild(loader()),
      CAPABILITIES_MAX_AGE_MS,
    );
  }

  buildScoped<
    T extends PhraseBuildScopedResponse,
    TResponse extends ApiResponse<T>,
  >(
    key: string,
    loader: () => Observable<TResponse>,
  ): Observable<TResponse> {
    const buildKey = this.activeBuildId ?? 'pending';
    return this.getOrLoad<T, TResponse>(
      `phrase-search:${buildKey}:${key}`,
      loader,
    );
  }

  private observeBuild<
    T extends PhraseBuildScopedResponse,
    TResponse extends ApiResponse<T>,
  >(
    request: Observable<TResponse>,
  ): Observable<TResponse> {
    return request.pipe(
      tap((response) => {
        const buildId = response.isSuccess ? response.data?.activeBuildId : null;
        if (!buildId) {
          return;
        }
        if (this.activeBuildId !== null && this.activeBuildId !== buildId) {
          this.clear();
        }
        this.activeBuildId = buildId;
      }),
    );
  }
}
