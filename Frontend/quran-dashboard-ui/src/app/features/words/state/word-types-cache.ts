import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { WordTypeGroupedRequestParams } from '../models/word-types-detail.models';
import {
  WordTypeCase,
  WordTypeSort,
  WordTypeTableView,
  WordTypeTense,
  WordTypeVoice,
} from '../models/word-types.models';

export const WordTypesCacheKeys = {
  tree: 'wordtypes:tree',
  rows(filter: WordTypesCacheFilter, sort: WordTypeSort, page: number): string {
    return `wordtypes:rows:${filter.type}:${filter.childCode ?? 'all'}:${filter.case}:${filter.tense}:${filter.voice}${searchSegment(filter.search)}:sort:${sort}:p${page}`;
  },
  table(filter: WordTypesCacheFilter, tableView: WordTypeTableView, sort: WordTypeSort, page: number): string {
    return `wordtypes:table:${filter.type}:${filter.childCode ?? 'all'}:${filter.case}:${filter.tense}:${filter.voice}${searchSegment(filter.search)}:view:${tableView}:sort:${sort}:p${page}`;
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
  groupedSummary(request: WordTypeGroupedRequestParams): string {
    return groupedDetailKey(request, 'summary');
  },
  groupedWords(request: WordTypeGroupedRequestParams, page: number): string {
    return `${groupedDetailKey(request, 'words')}:p${page}`;
  },
  groupedAyahs(request: WordTypeGroupedRequestParams, page: number): string {
    return `${groupedDetailKey(request, 'ayahs')}:p${page}`;
  },
  groupedSurahs(request: WordTypeGroupedRequestParams): string {
    return groupedDetailKey(request, 'surahs');
  },
} as const;

export interface WordTypesCacheFilter {
  type: string;
  childCode: string | null;
  case: WordTypeCase;
  tense: WordTypeTense;
  voice: WordTypeVoice;
  search: string | null;
}

// Empty/absent search adds nothing, keeping the pre-feature key byte-identical (warm entries stay
// valid); a non-empty term is encoded into its own segment so searched reads never cross-serve.
function searchSegment(search: string | null): string {
  return search ? `:q:${encodeURIComponent(search)}` : '';
}

export interface WordTypesCacheIdentity {
  tashkeelWordId: number;
  contextCode: string;
  case: WordTypeCase;
  tense: WordTypeTense;
  voice: WordTypeVoice;
}

function groupedDetailKey(
  request: WordTypeGroupedRequestParams,
  view: 'summary' | 'words' | 'ayahs' | 'surahs',
): string {
  return `wordtypes:grouped:${request.kind}:${request.dimensionId}:${request.type}:${request.childCode ?? 'all'}:${request.case}:${request.tense}:${request.voice}:view:${view}`;
}

@Injectable({ providedIn: 'root' })
export class WordTypesCache extends ApiResponseCache {}
