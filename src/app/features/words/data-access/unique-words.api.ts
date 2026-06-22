import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  PagedResultDto,
  UniqueWordAyahMatchDto,
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSort,
  UniqueWordSurahsDto,
} from '../models/unique-words.models';

/**
 * Data-access service for the Unique Words explorer. Returns the existing
 * `ApiResponse<T>` envelope without unwrapping it — the facade owns state
 * mapping and error handling. Child components never call this directly.
 */
@Injectable({ providedIn: 'root' })
export class UniqueWordsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  /**
   * Loads one bounded page of unique words for the given mode.
   *
   * @param kind Stable mode route key (`tashkeel` or `simple`).
   * @param search Arabic contains query; omitted from the request when blank.
   * @param sort Sort option (`mushaf-order` | `occurrences` | `alpha`).
   * @param page 1-based page number.
   * @param pageSize Bounded page size.
   */
  getList(
    kind: UniqueWordKind,
    search: string,
    sort: UniqueWordSort,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<UniqueWordListItemDto>>> {
    let params = new HttpParams()
      .set('sort', sort)
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim().length > 0) {
      params = params.set('search', search.trim());
    }

    return this.http.get<ApiResponse<PagedResultDto<UniqueWordListItemDto>>>(
      `${this.baseUrl}/api/words/unique/${encodeURIComponent(kind)}`,
      { params },
    );
  }

  getMentionedSurahs(
    kind: UniqueWordKind,
    id: number,
  ): Observable<ApiResponse<UniqueWordSurahsDto>> {
    return this.http.get<ApiResponse<UniqueWordSurahsDto>>(
      `${this.baseUrl}/api/words/unique/${encodeURIComponent(kind)}/${id}/surahs`,
    );
  }

  getMissingSurahs(
    kind: UniqueWordKind,
    id: number,
  ): Observable<ApiResponse<UniqueWordMissingSurahsDto>> {
    return this.http.get<ApiResponse<UniqueWordMissingSurahsDto>>(
      `${this.baseUrl}/api/words/unique/${encodeURIComponent(kind)}/${id}/missing-surahs`,
    );
  }

  getAyahMatches(
    kind: UniqueWordKind,
    id: number,
    page: number,
    pageSize: number,
  ): Observable<ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>>>(
      `${this.baseUrl}/api/words/unique/${encodeURIComponent(kind)}/${id}/ayahs`,
      { params },
    );
  }
}
