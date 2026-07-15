import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { StemSort, StemWordView } from '../models/stems.models';

export const StemsCacheKeys = {
  // rangesKey is '' for an unfiltered read, keeping the pre-feature cache key byte-identical (US5).
  list(search: string, sort: StemSort, page: number, rangesKey = '', associationKey = ''): string {
    let key = `stems:list:${sort}:${search}:p${page}`;
    if (rangesKey.length > 0) {
      key += `:${rangesKey}`;
    }
    if (associationKey.length > 0) {
      key += `:assoc(${associationKey})`;
    }
    return key;
  },

  summary(stemId: number): string {
    return `stems:${stemId}:summary`;
  },

  words(stemId: number, wordView: StemWordView, page: number): string {
    return `stems:${stemId}:words:${wordView}:p${page}`;
  },

  ayahs(stemId: number, page: number, pageSize: number, typeCode: string | null): string {
    const normalizedTypeCode = typeCode && typeCode.trim().length > 0 ? typeCode.trim() : 'all';
    return `stems:${stemId}:ayahs:${normalizedTypeCode}:p${page}:s${pageSize}`;
  },

  surahs(stemId: number): string {
    return `stems:${stemId}:surahs`;
  },

  missing(stemId: number): string {
    return `stems:${stemId}:missing`;
  },

  lemmas(stemId: number): string {
    return `stems:${stemId}:lemmas`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class StemsCache extends ApiResponseCache {}
