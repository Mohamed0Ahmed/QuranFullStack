import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../data-access/api-response.model';
import { CurrentUserResponse } from '../api/generated/models/current-user-response';

export const INTERACTIVE_IDENTITY_EVIDENCE_HEADER = 'X-Interactive-Identity-Evidence';

@Injectable({ providedIn: 'root' })
export class AccessApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getMe(): Observable<ApiResponse<CurrentUserResponse>> {
    return this.http.get<ApiResponse<CurrentUserResponse>>(`${this.baseUrl}/api/access/me`);
  }

  createDeviceSession(accessToken: string, identityEvidenceToken: string): Observable<ApiResponse<unknown>> {
    const headers: Record<string, string> = { Authorization: `Bearer ${accessToken}` };
    if (identityEvidenceToken) {
      headers[INTERACTIVE_IDENTITY_EVIDENCE_HEADER] = identityEvidenceToken;
    }

    return this.http.post<ApiResponse<unknown>>(`${this.baseUrl}/api/auth/sessions`, null, { headers });
  }

  revokeCurrentSession(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/auth/sessions/current`);
  }
}
