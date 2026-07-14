import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { UniqueWordKind, UniqueWordSort } from '../models/unique-words.models';

export const UniqueWordsCacheKeys = {
  // rangesKey is '' for an unfiltered read, keeping the pre-feature cache key byte-identical (US5).
  list(mode: UniqueWordKind, sort: UniqueWordSort, search: string, page: number, rangesKey = ''): string {
    const base = `words:list:${mode}:${sort}:${search}:p${page}`;
    return rangesKey.length > 0 ? `${base}:${rangesKey}` : base;
  },

  summary(mode: UniqueWordKind, wordId: number): string {
    return `words:${mode}:${wordId}:summary`;
  },

  surahs(mode: UniqueWordKind, wordId: number): string {
    return `words:${mode}:${wordId}:surahs`;
  },

  missing(mode: UniqueWordKind, wordId: number): string {
    return `words:${mode}:${wordId}:missing`;
  },

  ayahs(mode: UniqueWordKind, wordId: number, page: number): string {
    return `words:${mode}:${wordId}:ayahs:p${page}`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class UniqueWordsCache extends ApiResponseCache {}
