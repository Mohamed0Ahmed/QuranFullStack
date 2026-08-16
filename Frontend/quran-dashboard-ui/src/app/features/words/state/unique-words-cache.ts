import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { UniqueWordKind, UniqueWordSort } from '../models/unique-words.models';

export const UniqueWordsCacheKeys = {
  // rangesKey/associationKey are '' for an unfiltered read, keeping the pre-feature cache key
  // byte-identical (US5/US7).
  list(
    mode: UniqueWordKind,
    sort: UniqueWordSort,
    search: string,
    page: number,
    rangesKey = '',
    associationKey = '',
  ): string {
    let key = `words:list:${mode}:${sort}:${search}:p${page}`;
    if (rangesKey.length > 0) {
      key += `:${rangesKey}`;
    }
    if (associationKey.length > 0) {
      key += `:assoc(${associationKey})`;
    }
    return key;
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

  ayahs(mode: UniqueWordKind, wordId: number, page: number, typeCode: string | null): string {
    const normalizedTypeCode = typeCode && typeCode.trim().length > 0 ? typeCode.trim() : 'all';
    return `words:${mode}:${wordId}:ayahs:${normalizedTypeCode}:p${page}`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class UniqueWordsCache extends ApiResponseCache {}
