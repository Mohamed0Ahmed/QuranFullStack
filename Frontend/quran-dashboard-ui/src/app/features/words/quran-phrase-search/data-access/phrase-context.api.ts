import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseContextBranchesResponseApiResponse } from '../../../../core/api/generated/models/phrase-context-branches-response-api-response';
import { PhraseContextGroupsResponseApiResponse } from '../../../../core/api/generated/models/phrase-context-groups-response-api-response';
import { PhraseContextOccurrencesResponseApiResponse } from '../../../../core/api/generated/models/phrase-context-occurrences-response-api-response';
import { PhraseContextResultsResponseApiResponse } from '../../../../core/api/generated/models/phrase-context-results-response-api-response';
import { environment } from '../../../../../environments/environment';
import { PhraseSearchCache, phraseSearchCacheKey } from '../state/phrase-search-cache';

@Injectable({ providedIn: 'root' })
export class PhraseContextApi {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(PhraseSearchCache);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/quran/phrase-search/contexts`;

  getBranches(
    resolutionRef: string,
    previousRef: string | null,
    followingRef: string | null,
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
    params = setOptional(params, 'previousCursor', previousCursor);
    params = setOptional(params, 'followingCursor', followingCursor);
    return this.cache.buildScoped(
      phraseSearchCacheKey(
        'context-branches',
        resolutionRef,
        previousRef,
        followingRef,
        previousCursor,
        followingCursor,
        pageSize,
      ),
      () =>
        this.http.get<PhraseContextBranchesResponseApiResponse>(
          `${this.baseUrl}/branches`,
          { params },
        ),
    );
  }

  getGroups(
    resolutionRef: string,
    previousRef: string | null,
    followingRef: string | null,
    cursor: string | null,
    pageSize: number,
  ): Observable<PhraseContextGroupsResponseApiResponse> {
    let params = new HttpParams().set('resolutionRef', resolutionRef).set('pageSize', pageSize);
    params = setOptional(params, 'previousRef', previousRef);
    params = setOptional(params, 'followingRef', followingRef);
    params = setOptional(params, 'cursor', cursor);
    return this.cache.buildScoped(
      phraseSearchCacheKey(
        'context-groups',
        resolutionRef,
        previousRef,
        followingRef,
        cursor,
        pageSize,
      ),
      () =>
        this.http.get<PhraseContextGroupsResponseApiResponse>(`${this.baseUrl}/groups`, {
          params,
        }),
    );
  }

  getResults(
    resolutionRef: string,
    previousRef: string | null,
    followingRef: string | null,
    page: number,
    pageSize: number,
  ): Observable<PhraseContextResultsResponseApiResponse> {
    let params = new HttpParams()
      .set('resolutionRef', resolutionRef)
      .set('page', page)
      .set('pageSize', pageSize);
    params = setOptional(params, 'previousRef', previousRef);
    params = setOptional(params, 'followingRef', followingRef);
    return this.cache.buildScoped(
      phraseSearchCacheKey(
        'context-results',
        resolutionRef,
        previousRef,
        followingRef,
        page,
        pageSize,
      ),
      () =>
        this.http.get<PhraseContextResultsResponseApiResponse>(`${this.baseUrl}/results`, {
          params,
        }),
    );
  }

  getOccurrences(
    contextRef: string,
    cursor: string | null,
    pageSize: number,
  ): Observable<PhraseContextOccurrencesResponseApiResponse> {
    let params = new HttpParams().set('contextRef', contextRef).set('pageSize', pageSize);
    params = setOptional(params, 'cursor', cursor);
    return this.cache.buildScoped(
      phraseSearchCacheKey('context-occurrences', contextRef, cursor, pageSize),
      () =>
        this.http.get<PhraseContextOccurrencesResponseApiResponse>(
          `${this.baseUrl}/occurrences`,
          { params },
        ),
    );
  }
}

function setOptional(params: HttpParams, key: string, value: string | null): HttpParams {
  return value ? params.set(key, value) : params;
}
