import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseQueryResolutionResponseApiResponse } from '../../../../core/api/generated/models/phrase-query-resolution-response-api-response';
import { PhraseSearchCapabilitiesResponseApiResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response-api-response';
import { environment } from '../../../../../environments/environment';
import { PhraseTextMode } from '../models/phrase-repetitions.models';

@Injectable({ providedIn: 'root' })
export class PhraseResolutionApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/quran/phrase-search`;

  getCapabilities(): Observable<PhraseSearchCapabilitiesResponseApiResponse> {
    return this.http.get<PhraseSearchCapabilitiesResponseApiResponse>(
      `${this.baseUrl}/capabilities`,
    );
  }

  resolve(
    mode: PhraseTextMode,
    encodedQuery: string,
  ): Observable<PhraseQueryResolutionResponseApiResponse> {
    const params = new HttpParams().set('mode', mode).set('q64', encodedQuery);
    return this.http.get<PhraseQueryResolutionResponseApiResponse>(
      `${this.baseUrl}/query-resolutions`,
      { params },
    );
  }
}
