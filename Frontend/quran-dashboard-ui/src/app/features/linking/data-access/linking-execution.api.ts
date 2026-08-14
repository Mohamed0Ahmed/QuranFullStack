import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingConfirmationSubmissionResponseApiResponse } from '../../../core/api/generated/models/linking-confirmation-submission-response-api-response';
import { LinkingConfirmationSubmission } from '../models/linking-execution.models';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingLifecycleError } from '../models/linking-revision.models';

@Injectable({ providedIn: 'root' })
export class LinkingExecutionApi {
  private readonly http = inject(HttpClient);

  createJob(
    preflightId: string,
    preflightToken: string,
    idempotencyKey: string,
  ): Observable<LinkingConfirmationSubmission> {
    return this.http
      .post<LinkingConfirmationSubmissionResponseApiResponse>(
        `${environment.apiBaseUrl}/api/linking/preflights/${preflightId}/confirmation-jobs`,
        { preflightToken, idempotencyKey },
      )
      .pipe(
        map((response) => {
          if (!response.isSuccess || response.data === null) {
            throw new Error(response.message ?? 'تعذر بدء تنفيذ الربط.');
          }
          return response.data;
        }),
        catchError((error: unknown) => {
          if (error instanceof HttpErrorResponse) {
            const response = error.error as ApiResponse<{ code?: string }> | null;
            const code = response?.data?.code;
            return throwError(() => typeof code === 'string'
              ? new LinkingLifecycleError(code, response?.message ?? 'تعارضت حالة تنفيذ الربط.')
              : new Error(response?.message ?? 'تعذر بدء تنفيذ الربط.'));
          }
          return throwError(() => error instanceof Error ? error : new Error('تعذر بدء تنفيذ الربط.'));
        }),
      );
  }
}
