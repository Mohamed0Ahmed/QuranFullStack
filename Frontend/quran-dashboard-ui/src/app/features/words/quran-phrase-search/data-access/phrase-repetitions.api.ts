import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseOccurrencePageResponseApiResponse } from '../../../../core/api/generated/models/phrase-occurrence-page-response-api-response';
import { PhraseRepetitionsPageResponseApiResponse } from '../../../../core/api/generated/models/phrase-repetitions-page-response-api-response';
import { PhraseSearchCapabilitiesResponseApiResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response-api-response';
import { environment } from '../../../../../environments/environment';
import {
  PhraseRepetitionSort,
  PhraseTextMode,
} from '../models/phrase-repetitions.models';
import { PhraseSearchCache, phraseSearchCacheKey } from '../state/phrase-search-cache';
import { phraseSearchConditionalHeaders } from './phrase-search-conditional-request';

@Injectable()
export class PhraseRepetitionsApi {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(PhraseSearchCache);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/quran/phrase-search`;

  getCapabilities(): Observable<PhraseSearchCapabilitiesResponseApiResponse> {
    return this.cache.capabilities();
  }

  getRepetitions(
    mode: PhraseTextMode,
    length: number,
    encodedQuery: string | null,
    sort: PhraseRepetitionSort,
    page: number,
    pageSize: number,
  ): Observable<PhraseRepetitionsPageResponseApiResponse> {
    const params = new HttpParams()
      .set('mode', mode)
      .set('length', length)
      .set('sort', sort)
      .set('page', page)
      .set('pageSize', pageSize);
    const requestParams = encodedQuery === null ? params : params.set('q64', encodedQuery);

    return this.cache.buildScoped(
      phraseSearchCacheKey('repetitions', mode, length, encodedQuery, sort, page, pageSize),
      (etag) =>
        this.http.get<PhraseRepetitionsPageResponseApiResponse>(
          `${this.baseUrl}/repetitions`,
          {
            headers: phraseSearchConditionalHeaders(etag),
            observe: 'response',
            params: requestParams,
          },
        ),
    );
  }

  getOccurrences(
    buildId: string,
    variantId: number,
    page: number,
    pageSize: number,
  ): Observable<PhraseOccurrencePageResponseApiResponse> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.cache.buildScoped(
      phraseSearchCacheKey('repetition-occurrences', buildId, variantId, page, pageSize),
      (etag) =>
        this.http.get<PhraseOccurrencePageResponseApiResponse>(
          `${this.baseUrl}/repetitions/${encodeURIComponent(buildId)}/${variantId}/occurrences`,
          {
            headers: phraseSearchConditionalHeaders(etag),
            observe: 'response',
            params,
          },
        ),
    );
  }
}
