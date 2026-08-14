import { Injectable } from '@angular/core';

import { Observable, finalize, of, shareReplay, tap } from 'rxjs';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingResolvedSourceDto } from '../../../core/api/generated/models/linking-resolved-source-dto';

// ApiResponseCache's default of 48 complete sources would hold tens of MB in the heap (research R19).
const COMPLETE_SOURCES_HELD_IN_HEAP = 6;

export const LinkingSourceCacheKeys = {
  source(sourceIdentity: string, linkingDataRevision: number): string {
    return `linking:source:${linkingDataRevision}:${sourceIdentity}`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class LinkingSourceCache {
  private readonly cachedSources = new Map<string, ApiResponse<LinkingResolvedSourceDto>>();
  private readonly revisionBySource = new Map<string, number>();
  private readonly sourceLoads = new Map<string, SourceLoad>();
  private generation = 0;

  getOrLoadSource(
    sourceIdentity: string,
    loader: () => Observable<ApiResponse<LinkingResolvedSourceDto>>,
  ): Observable<ApiResponse<LinkingResolvedSourceDto>> {
    const revision = this.revisionBySource.get(sourceIdentity);
    if (revision !== undefined) {
      const cacheKey = LinkingSourceCacheKeys.source(sourceIdentity, revision);
      const cached = this.cachedSources.get(cacheKey);
      if (cached !== undefined) {
        this.cachedSources.delete(cacheKey);
        this.cachedSources.set(cacheKey, cached);
        return of(cached);
      }
    }
    const pending = this.sourceLoads.get(sourceIdentity);
    if (pending !== undefined) {
      return pending.observable;
    }
    const generation = this.generation;
    const request = loader().pipe(
      tap((response) => {
        if (generation === this.generation && response.isSuccess && response.data) {
          this.revisionBySource.set(sourceIdentity, response.data.linkingDataRevision);
          this.store(
            LinkingSourceCacheKeys.source(sourceIdentity, response.data.linkingDataRevision),
            response,
          );
        }
      }),
      finalize(() => {
        if (this.sourceLoads.get(sourceIdentity)?.generation === generation) {
          this.sourceLoads.delete(sourceIdentity);
        }
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );
    this.sourceLoads.set(sourceIdentity, { generation, observable: request });
    return request;
  }

  evictResolvedSources(): void {
    this.generation += 1;
    this.cachedSources.clear();
    this.revisionBySource.clear();
    this.sourceLoads.clear();
  }

  private store(key: string, response: ApiResponse<LinkingResolvedSourceDto>): void {
    this.cachedSources.delete(key);
    if (this.cachedSources.size >= COMPLETE_SOURCES_HELD_IN_HEAP) {
      const oldest = this.cachedSources.keys().next().value;
      if (oldest !== undefined) {
        this.cachedSources.delete(oldest);
      }
    }
    this.cachedSources.set(key, response);
  }
}

interface SourceLoad {
  generation: number;
  observable: Observable<ApiResponse<LinkingResolvedSourceDto>>;
}
