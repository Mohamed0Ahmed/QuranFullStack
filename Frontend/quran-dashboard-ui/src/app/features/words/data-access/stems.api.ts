import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  EMPTY_STEMS_ASSOCIATION,
  PagedResultDto,
  STEMS_RANGE_METRICS,
  StemAyahMatchDto,
  StemLemmasDto,
  StemListItemDto,
  StemMissingSurahsDto,
  StemSort,
  StemSummaryDto,
  StemSurahsDto,
  StemWordItemDto,
  StemWordView,
  StemsAssociation,
} from '../models/stems.models';
import { EMPTY_RANGE_FILTERS, RangeFilters, appendRangeApiParams } from '../state/words-range-filters';
import { appendAssociationParam } from '../state/words-association-filters';

@Injectable({ providedIn: 'root' })
export class StemsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getStemsList(
    search: string,
    sort: StemSort,
    page: number,
    pageSize: number,
    ranges: RangeFilters = EMPTY_RANGE_FILTERS,
    association: StemsAssociation = EMPTY_STEMS_ASSOCIATION,
  ): Observable<ApiResponse<PagedResultDto<StemListItemDto>>> {
    let params = new HttpParams()
      .set('sort', sort)
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim().length > 0) {
      params = params.set('search', search.trim());
    }

    params = appendRangeApiParams(params, ranges, STEMS_RANGE_METRICS);
    params = appendAssociationParam(params, 'rootId', association.rootId);
    params = appendAssociationParam(params, 'lemmaId', association.lemmaId);

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
    typeCode: string | null = null,
  ): Observable<ApiResponse<PagedResultDto<StemWordItemDto>>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (typeCode !== null && typeCode.trim().length > 0) {
      params = params.set('typeCode', typeCode.trim());
    }
    return this.http.get<ApiResponse<PagedResultDto<StemWordItemDto>>>(
      `${this.baseUrl}/api/words/stems/${id}/words/${encodeURIComponent(wordView)}`,
      { params },
    );
  }

  getStemAyahMatches(
    id: number,
    page: number,
    pageSize: number,
    typeCode: string | null = null,
  ): Observable<ApiResponse<PagedResultDto<StemAyahMatchDto>>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (typeCode !== null && typeCode.trim().length > 0) {
      params = params.set('typeCode', typeCode.trim());
    }
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
