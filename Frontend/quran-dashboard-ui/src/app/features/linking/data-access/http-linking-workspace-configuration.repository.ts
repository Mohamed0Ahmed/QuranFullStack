import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingWorkspaceDeltaBody } from '../../../core/api/generated/models/linking-workspace-delta-body';
import { LinkingWorkspaceDeltaResponse } from '../../../core/api/generated/models/linking-workspace-delta-response';
import { LinkingWorkspaceDeltaResponseApiResponse } from '../../../core/api/generated/models/linking-workspace-delta-response-api-response';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingDataStaleError } from '../models/linking-revision.models';
import {
  LinkingWorkspaceConfigurationRepository,
  LinkingWorkspaceSourceStaleError,
} from './linking-workspace-configuration.repository';

@Injectable({ providedIn: 'root' })
export class HttpLinkingWorkspaceConfigurationRepository
  implements LinkingWorkspaceConfigurationRepository
{
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/linking/workspace/sources`;

  applyDelta(
    sourceId: number,
    delta: LinkingWorkspaceDeltaBody,
  ): Observable<LinkingWorkspaceDeltaResponse> {
    return this.http
      .patch<LinkingWorkspaceDeltaResponseApiResponse>(
        `${this.baseUrl}/${sourceId}/configuration`,
        delta,
      )
      .pipe(
        map((response) => {
          if (!response.isSuccess || response.data === null) {
            throw new Error(response.message ?? 'تعذر حفظ إعدادات المصدر.');
          }
          return response.data;
        }),
        catchError((error: unknown) => throwError(() => toDeltaError(error))),
      );
  }
}

function toDeltaError(error: unknown): Error {
  if (!(error instanceof HttpErrorResponse)) {
    return error instanceof Error ? error : new Error('تعذر حفظ إعدادات المصدر.');
  }
  const response = error.error as ApiResponse<{ code?: string }> | null;
  if (error.status === 409 && response?.data?.code === 'LINKING_DATA_STALE') {
    return new LinkingDataStaleError(response.message ?? 'تغيّرت بيانات الربط؛ أعد التحميل.');
  }
  if (error.status === 409) {
    return new LinkingWorkspaceSourceStaleError(
      response?.message ?? 'تغيّر المصدر في جلسة أخرى.',
    );
  }
  return new Error(response?.message ?? 'تعذر حفظ إعدادات المصدر.');
}
