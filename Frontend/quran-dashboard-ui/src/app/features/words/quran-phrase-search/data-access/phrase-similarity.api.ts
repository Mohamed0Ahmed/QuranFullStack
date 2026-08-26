import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseSimilarityGroupsResponseApiResponse } from '../../../../core/api/generated/models/phrase-similarity-groups-response-api-response';
import { PhraseSimilarityMatchesResponseApiResponse } from '../../../../core/api/generated/models/phrase-similarity-matches-response-api-response';
import { PhraseSimilaritySearchResponseApiResponse } from '../../../../core/api/generated/models/phrase-similarity-search-response-api-response';
import { environment } from '../../../../../environments/environment';
import { PhraseTextMode } from '../models/phrase-repetitions.models';

@Injectable({ providedIn: 'root' })
export class PhraseSimilarityApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/quran/phrase-search`;

  search(
    resolutionRef: string,
    minimumMatchedWords: number,
    page: number,
    pageSize: number,
  ): Observable<PhraseSimilaritySearchResponseApiResponse> {
    const params = new HttpParams()
      .set('resolutionRef', resolutionRef)
      .set('minimumMatchedWords', minimumMatchedWords)
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PhraseSimilaritySearchResponseApiResponse>(
      `${this.baseUrl}/similarities/search`,
      { params },
    );
  }

  getGroups(
    mode: PhraseTextMode,
    length: number,
    threshold: number,
    page: number,
    pageSize: number,
  ): Observable<PhraseSimilarityGroupsResponseApiResponse> {
    const params = new HttpParams()
      .set('mode', mode)
      .set('length', length)
      .set('threshold', threshold)
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PhraseSimilarityGroupsResponseApiResponse>(
      `${this.baseUrl}/similarity-groups`,
      { params },
    );
  }

  getMatches(
    buildId: string,
    variantId: number,
    threshold: number,
    page: number,
    pageSize: number,
  ): Observable<PhraseSimilarityMatchesResponseApiResponse> {
    const params = new HttpParams()
      .set('threshold', threshold)
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PhraseSimilarityMatchesResponseApiResponse>(
      `${this.baseUrl}/similarity-groups/${encodeURIComponent(buildId)}/${variantId}/matches`,
      { params },
    );
  }
}
