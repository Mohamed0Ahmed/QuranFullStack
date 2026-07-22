import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { PermissionAdminView, PermissionMutationRequest } from '../models/permission.models';

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
