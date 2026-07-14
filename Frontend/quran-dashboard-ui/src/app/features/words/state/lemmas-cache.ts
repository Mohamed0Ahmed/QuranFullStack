import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { LemmaSort, LemmaWordView } from '../models/lemmas.models';

export const LemmasCacheKeys = {
  // rangesKey is '' for an unfiltered read, keeping the pre-feature cache key byte-identical (US5).
  list(search: string, sort: LemmaSort, page: number, rangesKey = ''): string {
    const base = `lemmas:list:${sort}:${search}:p${page}`;
    return rangesKey.length > 0 ? `${base}:${rangesKey}` : base;
  },

  summary(lemmaId: number): string {
    return `lemmas:${lemmaId}:summary`;
  },

  words(lemmaId: number, wordView: LemmaWordView, page: number): string {
    return `lemmas:${lemmaId}:words:${wordView}:p${page}`;
  },

  ayahs(lemmaId: number, page: number, pageSize: number, typeCode: string | null): string {
    const normalizedTypeCode = typeCode && typeCode.trim().length > 0 ? typeCode.trim() : 'all';
    return `lemmas:${lemmaId}:ayahs:${normalizedTypeCode}:p${page}:s${pageSize}`;
  },

  surahs(lemmaId: number): string {
    return `lemmas:${lemmaId}:surahs`;
  },

  missing(lemmaId: number): string {
    return `lemmas:${lemmaId}:missing`;
  },

  stems(lemmaId: number): string {
    return `lemmas:${lemmaId}:stems`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class LemmasCache extends ApiResponseCache {}
