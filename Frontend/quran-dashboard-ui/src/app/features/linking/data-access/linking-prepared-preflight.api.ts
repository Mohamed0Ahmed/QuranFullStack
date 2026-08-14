import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingPreparedDetailPageDto } from '../../../core/api/generated/models/linking-prepared-detail-page-dto';
import { LinkingPreparedDetailPageDtoApiResponse } from '../../../core/api/generated/models/linking-prepared-detail-page-dto-api-response';
import { LinkingPreparedPreflightStatusDto } from '../../../core/api/generated/models/linking-prepared-preflight-status-dto';
import { LinkingPreparedPreflightStatusDtoApiResponse } from '../../../core/api/generated/models/linking-prepared-preflight-status-dto-api-response';
import {
  LinkingPreparedPreflightRequest,
  LinkingPreparedPreflightStatus,
} from '../models/linking-prepared-preflight.models';
import { LinkingPreparedDetailRequest } from '../models/linking-page.models';

@Injectable({ providedIn: 'root' })
export class LinkingPreparedPreflightApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/linking/preflights`;

  create(request: LinkingPreparedPreflightRequest): Observable<LinkingPreparedPreflightStatus> {
    return this.http
      .post<LinkingPreparedPreflightStatusDtoApiResponse>(this.baseUrl, request)
      .pipe(map(requireStatus));
  }

  get(preflightId: string): Observable<LinkingPreparedPreflightStatus> {
    return this.http
      .get<LinkingPreparedPreflightStatusDtoApiResponse>(`${this.baseUrl}/${preflightId}`)
      .pipe(map(requireStatus));
  }

  cancel(preflightId: string): Observable<LinkingPreparedPreflightStatus> {
    return this.http
      .delete<LinkingPreparedPreflightStatusDtoApiResponse>(`${this.baseUrl}/${preflightId}`)
      .pipe(map(requireStatus));
  }

  loadDetails(request: LinkingPreparedDetailRequest): Observable<LinkingPreparedDetailPageDto> {
    const path =
      request.detailKind === 'merged'
        ? `${this.baseUrl}/${request.preflightId}/merged-ayahs`
        : `${this.baseUrl}/${request.preflightId}/sources/${request.preparedSourceId}/ayahs`;
    const params = new HttpParams()
      .set('filter', request.filter)
      .set('page', request.page)
      .set('pageSize', request.pageSize);
    return this.http
      .get<LinkingPreparedDetailPageDtoApiResponse>(path, { params })
      .pipe(map((response) => requireData(response, 'تعذر تحميل تفاصيل المراجعة.')));
  }
}

function requireStatus(response: LinkingPreparedPreflightStatusDtoApiResponse): LinkingPreparedPreflightStatusDto {
  return requireData(response, 'تعذر تحميل المراجعة المحضّرة.');
}

function requireData<T>(
  response: { isSuccess: boolean; data: T | null; message: string | null },
  fallback: string,
): T {
  if (!response.isSuccess || response.data === null) {
    throw new Error(response.message ?? fallback);
  }
  return response.data;
}
