import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseSearchSimilarityLinkingSelectionBody } from '../../../../core/api/generated/models/phrase-search-similarity-linking-selection-body';
import { PhraseSimilarityLinkingSelectionResponseApiResponse } from '../../../../core/api/generated/models/phrase-similarity-linking-selection-response-api-response';
import { PhraseSimilaritySearchResponseApiResponse } from '../../../../core/api/generated/models/phrase-similarity-search-response-api-response';
import { environment } from '../../../../../environments/environment';
import { PhraseSimilarityResultSort } from '../models/phrase-similarity.models';
import { PhraseSearchCache, phraseSearchCacheKey } from '../state/phrase-search-cache';
import { phraseSearchConditionalHeaders } from './phrase-search-conditional-request';

@Injectable()
export class PhraseSimilarityApi {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(PhraseSearchCache);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/quran/phrase-search`;

  search(
    resolutionRef: string,
    minimumMatchedWords: number,
    sort: PhraseSimilarityResultSort,
    page: number,
    pageSize: number,
  ): Observable<PhraseSimilaritySearchResponseApiResponse> {
    const params = new HttpParams()
      .set('resolutionRef', resolutionRef)
      .set('minimumMatchedWords', minimumMatchedWords)
      .set('sort', sort)
      .set('page', page)
      .set('pageSize', pageSize);
    return this.cache.buildScoped(
      phraseSearchCacheKey(
        'similarity-search',
        resolutionRef,
        minimumMatchedWords,
        sort,
        page,
        pageSize,
      ),
      (etag) =>
        this.http.get<PhraseSimilaritySearchResponseApiResponse>(
          `${this.baseUrl}/similarities/search`,
          {
            headers: phraseSearchConditionalHeaders(etag),
            observe: 'response',
            params,
          },
        ),
    );
  }

  resolveLinkingSelection(
    request: PhraseSearchSimilarityLinkingSelectionBody,
  ): Observable<PhraseSimilarityLinkingSelectionResponseApiResponse> {
    return this.http.post<PhraseSimilarityLinkingSelectionResponseApiResponse>(
      `${this.baseUrl}/similarities/linking-selection`,
      request,
    );
  }
}
