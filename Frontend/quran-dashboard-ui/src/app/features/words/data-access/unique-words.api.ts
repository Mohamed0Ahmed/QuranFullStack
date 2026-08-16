import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  EMPTY_UNIQUE_WORDS_ASSOCIATION,
  PagedResultDto,
  UNIQUE_WORDS_RANGE_METRICS,
  UniqueWordAyahMatchDto,
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSort,
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
  UniqueWordsAssociation,
} from '../models/unique-words.models';
import { EMPTY_RANGE_FILTERS, RangeFilters, appendRangeApiParams } from '../state/words-range-filters';
import { appendAssociationParam } from '../state/words-association-filters';

@Injectable({ providedIn: 'root' })
export class UniqueWordsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getList(
    kind: UniqueWordKind,
    search: string,
    sort: UniqueWordSort,
    page: number,
    pageSize: number,
    ranges: RangeFilters = EMPTY_RANGE_FILTERS,
    association: UniqueWordsAssociation = EMPTY_UNIQUE_WORDS_ASSOCIATION,
  ): Observable<ApiResponse<PagedResultDto<UniqueWordListItemDto>>> {
    let params = new HttpParams()
      .set('sort', sort)
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim().length > 0) {
      params = params.set('search', search.trim());
    }

    params = appendRangeApiParams(params, ranges, UNIQUE_WORDS_RANGE_METRICS);
    params = appendAssociationParam(params, 'primaryType', association.primaryType);
    params = appendAssociationParam(params, 'rootId', association.rootId);

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

  getSummary(
    kind: UniqueWordKind,
    id: number,
  ): Observable<ApiResponse<UniqueWordSummaryDto>> {
    return this.http.get<ApiResponse<UniqueWordSummaryDto>>(
      `${this.baseUrl}/api/words/unique/${encodeURIComponent(kind)}/${id}`,
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
    typeCode: string | null = null,
  ): Observable<ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (typeCode !== null && typeCode.trim().length > 0) {
      params = params.set('typeCode', typeCode.trim());
    }
    return this.http.get<ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>>>(
      `${this.baseUrl}/api/words/unique/${encodeURIComponent(kind)}/${id}/ayahs`,
      { params },
    );
  }
}
