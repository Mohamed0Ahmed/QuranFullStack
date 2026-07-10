import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from './api-response.model';
import { AppInfo, HealthStatus } from './system.models';

class UserFacingApiError extends Error {}

@Injectable({ providedIn: 'root' })
export class SystemApi {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  getDashboardInfo(): Observable<AppInfo> {
    const fallbackMessage = 'تعذر تحميل بيانات التطبيق. حاول مرة أخرى.';

    return this.http
      .get<ApiResponse<AppInfo>>(`${this.baseUrl}/api/dashboard/info`)
      .pipe(
        map((response) => {
          if (response.isSuccess && response.data) {
            return response.data;
          }
          throw new UserFacingApiError(response.message ?? fallbackMessage);
        }),
        catchError((error) =>
          throwError(() => new Error(this.resolveErrorMessage(error, fallbackMessage)))
        )
      );
  }

  getHealth(): Observable<HealthStatus> {
    const fallbackMessage = 'تعذر الاتصال بالخادم. حاول مرة أخرى.';

    return this.http
      .get<ApiResponse<HealthStatus>>(`${this.baseUrl}/api/health`)
      .pipe(
        map((response) => {
          if (response.isSuccess && response.data) {
            return response.data;
          }
          throw new UserFacingApiError(response.message ?? fallbackMessage);
        }),
        catchError((error) =>
          throwError(() => new Error(this.resolveErrorMessage(error, fallbackMessage)))
        )
      );
  }

  private resolveErrorMessage(error: unknown, fallbackMessage: string): string {
    if (error instanceof UserFacingApiError) {
      return error.message;
    }

    if (error instanceof HttpErrorResponse) {
      return this.readApiMessage(error.error) ?? fallbackMessage;
    }

    return fallbackMessage;
  }

  private readApiMessage(errorBody: unknown): string | null {
    if (typeof errorBody !== 'object' || errorBody === null) {
      return null;
    }

    const body = errorBody as Partial<ApiResponse<unknown>>;
    return typeof body.message === 'string' && body.message.trim().length > 0
      ? body.message
      : null;
  }
}
