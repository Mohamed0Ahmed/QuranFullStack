import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  PagedResultDto,
  RootAyahMatchDto,
  RootLemmasDto,
  RootListItemDto,
  RootMissingSurahsDto,
  RootSort,
  RootStemsDto,
  RootSummaryDto,
  RootSurahsDto,
  RootWordItemDto,
  RootWordView,
} from '../models/roots.models';

@Injectable({ providedIn: 'root' })
export class RootsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getRootsList(
    search: string,
    sort: RootSort,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<RootListItemDto>>> {
    let params = new HttpParams()
      .set('sort', sort)
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim().length > 0) {
      params = params.set('search', search.trim());
    }

    return this.http.get<ApiResponse<PagedResultDto<RootListItemDto>>>(
      `${this.baseUrl}/api/words/roots`,
      { params },
    );
  }

  getRootSummary(id: number): Observable<ApiResponse<RootSummaryDto>> {
    return this.http.get<ApiResponse<RootSummaryDto>>(
      `${this.baseUrl}/api/words/roots/${id}`,
    );
  }

  getRootWords(
    id: number,
    wordView: RootWordView,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<RootWordItemDto>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResultDto<RootWordItemDto>>>(
      `${this.baseUrl}/api/words/roots/${id}/words/${encodeURIComponent(wordView)}`,
      { params },
    );
  }

  getRootAyahMatches(
    id: number,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<RootAyahMatchDto>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResultDto<RootAyahMatchDto>>>(
      `${this.baseUrl}/api/words/roots/${id}/ayahs`,
      { params },
    );
  }

  getRootMentionedSurahs(id: number): Observable<ApiResponse<RootSurahsDto>> {
    return this.http.get<ApiResponse<RootSurahsDto>>(
      `${this.baseUrl}/api/words/roots/${id}/surahs`,
    );
  }

  getRootMissingSurahs(id: number): Observable<ApiResponse<RootMissingSurahsDto>> {
    return this.http.get<ApiResponse<RootMissingSurahsDto>>(
      `${this.baseUrl}/api/words/roots/${id}/missing-surahs`,
    );
  }

  getRootLemmas(id: number): Observable<ApiResponse<RootLemmasDto>> {
    return this.http.get<ApiResponse<RootLemmasDto>>(
      `${this.baseUrl}/api/words/roots/${id}/lemmas`,
    );
  }

  getRootStems(id: number): Observable<ApiResponse<RootStemsDto>> {
    return this.http.get<ApiResponse<RootStemsDto>>(
      `${this.baseUrl}/api/words/roots/${id}/stems`,
    );
  }
}
