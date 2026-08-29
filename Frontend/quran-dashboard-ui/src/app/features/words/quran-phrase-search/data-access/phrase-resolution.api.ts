import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseQueryResolutionResponseApiResponse } from '../../../../core/api/generated/models/phrase-query-resolution-response-api-response';
import { PhraseSearchCapabilitiesResponseApiResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response-api-response';
import { environment } from '../../../../../environments/environment';
import { PhraseTextMode } from '../models/phrase-repetitions.models';
import { PhraseSearchCache, phraseSearchCacheKey } from '../state/phrase-search-cache';
import { phraseSearchConditionalHeaders } from './phrase-search-conditional-request';

@Injectable()
export class PhraseResolutionApi {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(PhraseSearchCache);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/quran/phrase-search`;

  getCapabilities(): Observable<PhraseSearchCapabilitiesResponseApiResponse> {
    return this.cache.capabilities();
  }

  resolve(
    mode: PhraseTextMode,
    encodedQuery: string,
  ): Observable<PhraseQueryResolutionResponseApiResponse> {
    const params = new HttpParams().set('mode', mode).set('q64', encodedQuery);
    return this.cache.buildScoped(
      phraseSearchCacheKey('resolution', mode, encodedQuery),
      (etag) =>
        this.http.get<PhraseQueryResolutionResponseApiResponse>(
          `${this.baseUrl}/query-resolutions`,
          {
            headers: phraseSearchConditionalHeaders(etag),
            observe: 'response',
            params,
          },
        ),
    );
  }
}
