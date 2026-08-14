import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingConfirmationSubmissionResponseApiResponse } from '../../../core/api/generated/models/linking-confirmation-submission-response-api-response';
import { LinkingConfirmationSubmission } from '../models/linking-execution.models';

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
      );
  }
}
