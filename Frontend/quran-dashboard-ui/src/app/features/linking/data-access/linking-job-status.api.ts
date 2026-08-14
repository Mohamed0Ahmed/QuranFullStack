import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingConfirmationJobStatusDtoApiResponse } from '../../../core/api/generated/models/linking-confirmation-job-status-dto-api-response';
import { LinkingDurableConfirmationOutcomeDtoApiResponse } from '../../../core/api/generated/models/linking-durable-confirmation-outcome-dto-api-response';
import {
  LinkingConfirmationJobStatus,
  LinkingDurableConfirmationOutcome,
} from '../models/linking-execution.models';

@Injectable({ providedIn: 'root' })
export class LinkingJobStatusApi {
  private readonly http = inject(HttpClient);
  private readonly jobsUrl = `${environment.apiBaseUrl}/api/linking/confirmation-jobs`;
  private readonly outcomesUrl = `${environment.apiBaseUrl}/api/linking/confirmation-outcomes`;

  get(jobId: string): Observable<LinkingConfirmationJobStatus> {
    return this.http
      .get<LinkingConfirmationJobStatusDtoApiResponse>(`${this.jobsUrl}/${jobId}`)
      .pipe(map((response) => requireData(response, 'تعذر تحميل حالة تنفيذ الربط.')));
  }

  cancel(jobId: string): Observable<LinkingConfirmationJobStatus> {
    return this.http
      .delete<LinkingConfirmationJobStatusDtoApiResponse>(`${this.jobsUrl}/${jobId}`)
      .pipe(map((response) => requireData(response, 'تعذر إلغاء تنفيذ الربط.')));
  }

  getOutcome(idempotencyKey: string): Observable<LinkingDurableConfirmationOutcome> {
    return this.http
      .get<LinkingDurableConfirmationOutcomeDtoApiResponse>(`${this.outcomesUrl}/${idempotencyKey}`)
      .pipe(map((response) => requireData(response, 'تعذر استعادة نتيجة الربط.')));
  }
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
