import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { RootSort, RootWordView } from '../models/roots.models';

/**
 * Roots Explorer (Feature 015) cache keys over the shared `ApiResponseCache`,
 * mirroring the backend `roots:` keys. Modeled on `UniqueWordsCacheKeys`.
 */
export const RootsCacheKeys = {
  list(search: string, sort: RootSort, page: number): string {
    return `roots:list:${sort}:${search}:p${page}`;
  },

  summary(rootId: number): string {
    return `roots:${rootId}:summary`;
  },

  words(rootId: number, wordView: RootWordView, page: number): string {
    return `roots:${rootId}:words:${wordView}:p${page}`;
  },

  ayahs(rootId: number, page: number): string {
    return `roots:${rootId}:ayahs:p${page}`;
  },

  surahs(rootId: number): string {
    return `roots:${rootId}:surahs`;
  },

  missing(rootId: number): string {
    return `roots:${rootId}:missing`;
  },

  lemmas(rootId: number): string {
    return `roots:${rootId}:lemmas`;
  },

  stems(rootId: number): string {
    return `roots:${rootId}:stems`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class RootsCache extends ApiResponseCache {}
