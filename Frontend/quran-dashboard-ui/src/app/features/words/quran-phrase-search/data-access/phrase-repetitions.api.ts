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

@Injectable({ providedIn: 'root' })
export class PhraseRepetitionsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/quran/phrase-search`;

  getCapabilities(): Observable<PhraseSearchCapabilitiesResponseApiResponse> {
    return this.http.get<PhraseSearchCapabilitiesResponseApiResponse>(
      `${this.baseUrl}/capabilities`,
    );
  }

  getRepetitions(
    mode: PhraseTextMode,
    length: number,
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

    return this.http.get<PhraseRepetitionsPageResponseApiResponse>(
      `${this.baseUrl}/repetitions`,
      { params },
    );
  }

  getOccurrences(
    buildId: string,
    variantId: number,
    page: number,
    pageSize: number,
  ): Observable<PhraseOccurrencePageResponseApiResponse> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PhraseOccurrencePageResponseApiResponse>(
      `${this.baseUrl}/repetitions/${encodeURIComponent(buildId)}/${variantId}/occurrences`,
      { params },
    );
  }
}
