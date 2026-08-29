import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseContextBranchesResponseApiResponse } from '../../../../core/api/generated/models/phrase-context-branches-response-api-response';
import { PhraseContextResultsResponseApiResponse } from '../../../../core/api/generated/models/phrase-context-results-response-api-response';
import { environment } from '../../../../../environments/environment';
import { PhraseSearchCache, phraseSearchCacheKey } from '../state/phrase-search-cache';
import { phraseSearchConditionalHeaders } from './phrase-search-conditional-request';

@Injectable()
export class PhraseContextApi {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(PhraseSearchCache);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/quran/phrase-search/contexts`;

  getBranches(
    resolutionRef: string,
    previousRef: string | null,
    followingRef: string | null,
    previousAlternativesRef: string | null,
    followingAlternativesRef: string | null,
    previousCursor: string | null,
    followingCursor: string | null,
    pageSize: number,
  ): Observable<PhraseContextBranchesResponseApiResponse> {
    let params = new HttpParams()
      .set('resolutionRef', resolutionRef)
      .set('previousPageSize', pageSize)
      .set('followingPageSize', pageSize);
    params = setOptional(params, 'previousRef', previousRef);
    params = setOptional(params, 'followingRef', followingRef);
    params = setOptional(params, 'previousAlternativesRef', previousAlternativesRef);
    params = setOptional(params, 'followingAlternativesRef', followingAlternativesRef);
    params = setOptional(params, 'previousCursor', previousCursor);
    params = setOptional(params, 'followingCursor', followingCursor);
    return this.cache.buildScoped(
      phraseSearchCacheKey(
        'context-branches',
        resolutionRef,
        previousRef,
        followingRef,
        previousAlternativesRef,
        followingAlternativesRef,
        previousCursor,
        followingCursor,
        pageSize,
      ),
      (etag) =>
        this.http.get<PhraseContextBranchesResponseApiResponse>(
          `${this.baseUrl}/branches`,
          {
            headers: phraseSearchConditionalHeaders(etag),
            observe: 'response',
            params,
          },
        ),
    );
  }

  getResults(
    resolutionRef: string,
    previousRef: string | null,
    followingRef: string | null,
    previousAlternativesRef: string | null,
    followingAlternativesRef: string | null,
    page: number,
    pageSize: number,
  ): Observable<PhraseContextResultsResponseApiResponse> {
    let params = new HttpParams()
      .set('resolutionRef', resolutionRef)
      .set('page', page)
      .set('pageSize', pageSize);
    params = setOptional(params, 'previousRef', previousRef);
    params = setOptional(params, 'followingRef', followingRef);
    params = setOptional(params, 'previousAlternativesRef', previousAlternativesRef);
    params = setOptional(params, 'followingAlternativesRef', followingAlternativesRef);
    return this.cache.buildScoped(
      phraseSearchCacheKey(
        'context-results',
        resolutionRef,
        previousRef,
        followingRef,
        previousAlternativesRef,
        followingAlternativesRef,
        page,
        pageSize,
      ),
      (etag) =>
        this.http.get<PhraseContextResultsResponseApiResponse>(`${this.baseUrl}/results`, {
          headers: phraseSearchConditionalHeaders(etag),
          observe: 'response',
          params,
        }),
    );
  }
}

function setOptional(params: HttpParams, key: string, value: string | null): HttpParams {
  return value ? params.set(key, value) : params;
}
