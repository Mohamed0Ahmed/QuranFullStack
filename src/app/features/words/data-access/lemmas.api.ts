import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  LemmaAyahMatchDto,
  LemmaListItemDto,
  LemmaMissingSurahsDto,
  LemmaSort,
  LemmaStemsDto,
  LemmaSummaryDto,
  LemmaSurahsDto,
  LemmaWordItemDto,
  LemmaWordView,
  PagedResultDto,
} from '../models/lemmas.models';

/**
 * Typed HTTP client for the Lemmas Explorer (Feature 016). Endpoints live under
 * `/api/words/lemmas`. Returns the shared `ApiResponse<T>` envelope. Endpoints
 * are bound by the backend story phases; this service is consumed by the lemma
 * facades from US1 onward.
 */
@Injectable({ providedIn: 'root' })
export class LemmasApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getLemmasList(
    search: string,
    sort: LemmaSort,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<LemmaListItemDto>>> {
    let params = new HttpParams()
      .set('sort', sort)
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim().length > 0) {
      params = params.set('search', search.trim());
    }

    return this.http.get<ApiResponse<PagedResultDto<LemmaListItemDto>>>(
      `${this.baseUrl}/api/words/lemmas`,
      { params },
    );
  }

  getLemmaSummary(id: number): Observable<ApiResponse<LemmaSummaryDto>> {
    return this.http.get<ApiResponse<LemmaSummaryDto>>(
      `${this.baseUrl}/api/words/lemmas/${id}`,
    );
  }

  getLemmaWords(
    id: number,
    wordView: LemmaWordView,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<LemmaWordItemDto>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResultDto<LemmaWordItemDto>>>(
      `${this.baseUrl}/api/words/lemmas/${id}/words/${encodeURIComponent(wordView)}`,
      { params },
    );
  }

  getLemmaAyahMatches(
    id: number,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<LemmaAyahMatchDto>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResultDto<LemmaAyahMatchDto>>>(
      `${this.baseUrl}/api/words/lemmas/${id}/ayahs`,
      { params },
    );
  }

  getLemmaMentionedSurahs(id: number): Observable<ApiResponse<LemmaSurahsDto>> {
    return this.http.get<ApiResponse<LemmaSurahsDto>>(
      `${this.baseUrl}/api/words/lemmas/${id}/surahs`,
    );
  }

  getLemmaMissingSurahs(id: number): Observable<ApiResponse<LemmaMissingSurahsDto>> {
    return this.http.get<ApiResponse<LemmaMissingSurahsDto>>(
      `${this.baseUrl}/api/words/lemmas/${id}/missing-surahs`,
    );
  }

  getLemmaStems(id: number): Observable<ApiResponse<LemmaStemsDto>> {
    return this.http.get<ApiResponse<LemmaStemsDto>>(
      `${this.baseUrl}/api/words/lemmas/${id}/stems`,
    );
  }
}
