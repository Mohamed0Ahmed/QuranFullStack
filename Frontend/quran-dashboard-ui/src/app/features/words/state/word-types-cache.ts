import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { WordTypeCase, WordTypeSort, WordTypeTableView, WordTypeTense, WordTypeVoice } from '../models/word-types.models';

export const WordTypesCacheKeys = {
  tree: 'wordtypes:tree',
  rows(filter: WordTypesCacheFilter, sort: WordTypeSort, page: number): string {
    return `wordtypes:rows:${filter.type}:${filter.childCode ?? 'all'}:${filter.case}:${filter.tense}:${filter.voice}:sort:${sort}:p${page}`;
  },
  table(filter: WordTypesCacheFilter, tableView: WordTypeTableView, sort: WordTypeSort, page: number): string {
    return `wordtypes:table:${filter.type}:${filter.childCode ?? 'all'}:${filter.case}:${filter.tense}:${filter.voice}:view:${tableView}:sort:${sort}:p${page}`;
  },
  summary(identity: WordTypesCacheIdentity): string {
    return `wordtypes:summary:${identity.tashkeelWordId}:${identity.contextCode}:${identity.case}:${identity.tense}:${identity.voice}`;
  },
  ayahs(identity: WordTypesCacheIdentity, page: number): string {
    return `wordtypes:ayahs:${identity.tashkeelWordId}:${identity.contextCode}:${identity.case}:${identity.tense}:${identity.voice}:p${page}`;
  },
  surahs(identity: WordTypesCacheIdentity): string {
    return `wordtypes:surahs:${identity.tashkeelWordId}:${identity.contextCode}:${identity.case}:${identity.tense}:${identity.voice}`;
  },
} as const;

export interface WordTypesCacheFilter {
  type: string;
  childCode: string | null;
  case: WordTypeCase;
  tense: WordTypeTense;
  voice: WordTypeVoice;
}

export interface WordTypesCacheIdentity {
  tashkeelWordId: number;
  contextCode: string;
  case: WordTypeCase;
  tense: WordTypeTense;
  voice: WordTypeVoice;
}

@Injectable({ providedIn: 'root' })
export class WordTypesCache extends ApiResponseCache {}
