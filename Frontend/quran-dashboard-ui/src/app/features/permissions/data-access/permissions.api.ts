import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { PermissionAdminView, PermissionMutationRequest } from '../models/permission.models';

// Thin API boundary for the Owner-only permission-administration surface. Types the calls and returns the
// raw ApiResponse envelope for the facade to unwrap. The secure-url + auth interceptors attach the bearer
// token because the URL is under environment.apiBaseUrl.
@Injectable({ providedIn: 'root' })
export class PermissionsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list(): Observable<ApiResponse<PermissionAdminView>> {
    return this.http.get<ApiResponse<PermissionAdminView>>(`${this.baseUrl}/api/security/permissions`);
  }

  grant(request: PermissionMutationRequest): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>(`${this.baseUrl}/api/security/permissions/grant`, request);
  }

  revoke(request: PermissionMutationRequest): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>(`${this.baseUrl}/api/security/permissions/revoke`, request);
  }
}
