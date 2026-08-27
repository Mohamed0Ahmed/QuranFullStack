import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PhraseSimilaritySearchResponseApiResponse } from '../../../../core/api/generated/models/phrase-similarity-search-response-api-response';
import { environment } from '../../../../../environments/environment';
import { PhraseSearchCache, phraseSearchCacheKey } from '../state/phrase-search-cache';

@Injectable({ providedIn: 'root' })
export class PhraseSimilarityApi {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(PhraseSearchCache);
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
    return this.cache.buildScoped(
      phraseSearchCacheKey(
        'similarity-search',
        resolutionRef,
        minimumMatchedWords,
        page,
        pageSize,
      ),
      () =>
        this.http.get<PhraseSimilaritySearchResponseApiResponse>(
          `${this.baseUrl}/similarities/search`,
          { params },
        ),
    );
  }

}
