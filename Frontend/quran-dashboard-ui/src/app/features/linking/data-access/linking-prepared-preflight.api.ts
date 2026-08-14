import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';

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
import { LinkingLifecycleError } from '../models/linking-revision.models';
import { ApiResponse } from '../../../core/data-access/api-response.model';

@Injectable({ providedIn: 'root' })
export class LinkingPreparedPreflightApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/linking/preflights`;

  create(request: LinkingPreparedPreflightRequest): Observable<LinkingPreparedPreflightStatus> {
    return this.http
      .post<LinkingPreparedPreflightStatusDtoApiResponse>(this.baseUrl, request)
      .pipe(map(requireStatus), catchError(rethrowLifecycle));
  }

  get(preflightId: string): Observable<LinkingPreparedPreflightStatus> {
    return this.http
      .get<LinkingPreparedPreflightStatusDtoApiResponse>(`${this.baseUrl}/${preflightId}`)
      .pipe(map(requireStatus), catchError(rethrowLifecycle));
  }

  cancel(preflightId: string): Observable<LinkingPreparedPreflightStatus> {
    return this.http
      .delete<LinkingPreparedPreflightStatusDtoApiResponse>(`${this.baseUrl}/${preflightId}`)
      .pipe(map(requireStatus), catchError(rethrowLifecycle));
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
      .pipe(
        map((response) => requireData(response, 'تعذر تحميل تفاصيل المراجعة.')),
        catchError(rethrowLifecycle),
      );
  }
}

function rethrowLifecycle(error: unknown): Observable<never> {
  if (error instanceof HttpErrorResponse) {
    const response = error.error as ApiResponse<{ code?: string }> | null;
    const code = response?.data?.code;
    if (typeof code === 'string') {
      return throwError(() => new LinkingLifecycleError(code, response?.message ?? 'تعارضت حالة عملية الربط.'));
    }
    return throwError(() => new Error(response?.message ?? 'تعذر تحميل عملية الربط.'));
  }
  return throwError(() => error instanceof Error ? error : new Error('تعذر تحميل عملية الربط.'));
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
