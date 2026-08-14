import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingResolvedSourcePageDto } from '../../../core/api/generated/models/linking-resolved-source-page-dto';
import { LinkingResolvedSourcePageDtoApiResponse } from '../../../core/api/generated/models/linking-resolved-source-page-dto-api-response';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingDataStaleError } from '../models/linking-revision.models';
import { LinkingSourcePageRequest } from '../models/linking-page.models';
import { toLinkingSourceDescriptorBody } from '../utils/linking-source-descriptor-body';

export class LinkingSourceViewStaleError extends Error {}

@Injectable({ providedIn: 'root' })
export class LinkingSourcePagesApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/api/linking/sources/resolve-page`;

  load(request: LinkingSourcePageRequest): Observable<LinkingResolvedSourcePageDto> {
    return this.http
      .post<LinkingResolvedSourcePageDtoApiResponse>(this.url, {
        descriptor: toLinkingSourceDescriptorBody(request.source),
        expectedLinkingDataRevision: request.expectedLinkingDataRevision,
        expectedSourceViewIdentity: request.expectedSourceViewIdentity,
        page: request.page,
        pageSize: request.pageSize,
        view: request.view,
      })
      .pipe(
        map((response) => requireData(response, 'تعذر تحميل صفحة المصدر.')),
        catchError((error: unknown) => throwError(() => toPageError(error))),
      );
  }
}

function requireData(
  response: LinkingResolvedSourcePageDtoApiResponse,
  fallback: string,
): LinkingResolvedSourcePageDto {
  if (!response.isSuccess || response.data === null) {
    throw new Error(response.message ?? fallback);
  }
  return response.data;
}

function toPageError(error: unknown): Error {
  if (!(error instanceof HttpErrorResponse)) {
    return error instanceof Error ? error : new Error('تعذر تحميل صفحة المصدر.');
  }
  const response = error.error as ApiResponse<{ code?: string }> | null;
  if (error.status === 409 && response?.data?.code === 'LINKING_DATA_STALE') {
    return new LinkingDataStaleError(response.message ?? 'تغيّرت بيانات الربط؛ أعد التحميل.');
  }
  if (error.status === 409 && response?.data?.code === 'SOURCE_VIEW_STALE') {
    return new LinkingSourceViewStaleError(response.message ?? 'تغيّر عرض المصدر؛ أعد التحميل.');
  }
  return new Error(response?.message ?? 'تعذر تحميل صفحة المصدر.');
}
