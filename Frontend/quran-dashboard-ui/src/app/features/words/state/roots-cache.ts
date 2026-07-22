import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { RootSort, RootWordView } from '../models/roots.models';

export const RootsCacheKeys = {
  list(search: string, sort: RootSort, page: number, rangesKey = ''): string {
    const base = `roots:list:${sort}:${search}:p${page}`;
    return rangesKey.length > 0 ? `${base}:${rangesKey}` : base;
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
