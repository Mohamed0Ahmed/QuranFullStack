import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  PagedResultDto,
  StemAyahMatchDto,
  StemLemmasDto,
  StemListItemDto,
  StemMissingSurahsDto,
  StemSort,
  StemSummaryDto,
  StemSurahsDto,
  StemWordItemDto,
  StemWordView,
} from '../models/stems.models';

/**
 * Typed HTTP client for the Stems Explorer (Feature 016). Endpoints live under
 * `/api/words/stems`. Returns the shared `ApiResponse<T>` envelope. Endpoints
 * are bound by the backend story phases; this service is consumed by the stem
 * facades from US2 onward.
 */
@Injectable({ providedIn: 'root' })
export class StemsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getStemsList(
    search: string,
    sort: StemSort,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<StemListItemDto>>> {
    let params = new HttpParams()
      .set('sort', sort)
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim().length > 0) {
      params = params.set('search', search.trim());
    }

    return this.http.get<ApiResponse<PagedResultDto<StemListItemDto>>>(
      `${this.baseUrl}/api/words/stems`,
      { params },
    );
  }

  getStemSummary(id: number): Observable<ApiResponse<StemSummaryDto>> {
    return this.http.get<ApiResponse<StemSummaryDto>>(
      `${this.baseUrl}/api/words/stems/${id}`,
    );
  }

  getStemWords(
    id: number,
    wordView: StemWordView,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<StemWordItemDto>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResultDto<StemWordItemDto>>>(
      `${this.baseUrl}/api/words/stems/${id}/words/${encodeURIComponent(wordView)}`,
      { params },
    );
  }

  getStemAyahMatches(
    id: number,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<StemAyahMatchDto>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResultDto<StemAyahMatchDto>>>(
      `${this.baseUrl}/api/words/stems/${id}/ayahs`,
      { params },
    );
  }

  getStemMentionedSurahs(id: number): Observable<ApiResponse<StemSurahsDto>> {
    return this.http.get<ApiResponse<StemSurahsDto>>(
      `${this.baseUrl}/api/words/stems/${id}/surahs`,
    );
  }

  getStemMissingSurahs(id: number): Observable<ApiResponse<StemMissingSurahsDto>> {
    return this.http.get<ApiResponse<StemMissingSurahsDto>>(
      `${this.baseUrl}/api/words/stems/${id}/missing-surahs`,
    );
  }

  getStemLemmas(id: number): Observable<ApiResponse<StemLemmasDto>> {
    return this.http.get<ApiResponse<StemLemmasDto>>(
      `${this.baseUrl}/api/words/stems/${id}/lemmas`,
    );
  }
}
