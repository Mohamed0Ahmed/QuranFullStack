import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { StemSort, StemWordView } from '../models/stems.models';

export const StemsCacheKeys = {
  list(search: string, sort: StemSort, page: number): string {
    return `stems:list:${sort}:${search}:p${page}`;
  },

  summary(stemId: number): string {
    return `stems:${stemId}:summary`;
  },

  words(stemId: number, wordView: StemWordView, page: number): string {
    return `stems:${stemId}:words:${wordView}:p${page}`;
  },

  ayahs(stemId: number, page: number): string {
    return `stems:${stemId}:ayahs:p${page}`;
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
