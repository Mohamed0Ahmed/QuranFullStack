import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { LemmaSort, LemmaWordView } from '../models/lemmas.models';

export const LemmasCacheKeys = {
  list(search: string, sort: LemmaSort, page: number): string {
    return `lemmas:list:${sort}:${search}:p${page}`;
  },

  summary(lemmaId: number): string {
    return `lemmas:${lemmaId}:summary`;
  },

  words(lemmaId: number, wordView: LemmaWordView, page: number): string {
    return `lemmas:${lemmaId}:words:${wordView}:p${page}`;
  },

  ayahs(lemmaId: number, page: number): string {
    return `lemmas:${lemmaId}:ayahs:p${page}`;
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
