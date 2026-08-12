import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';

// ApiResponseCache's default of 48 complete sources would hold tens of MB in the heap (research R19).
const COMPLETE_SOURCES_HELD_IN_HEAP = 6;

export const LinkingSourceCacheKeys = {
  source(sourceIdentity: string): string {
    return `linking:source:${sourceIdentity}`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class LinkingSourceCache extends ApiResponseCache {
  protected override readonly maxEntries = COMPLETE_SOURCES_HELD_IN_HEAP;
}
