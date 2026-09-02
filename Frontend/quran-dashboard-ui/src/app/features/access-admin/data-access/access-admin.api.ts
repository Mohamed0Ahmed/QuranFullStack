import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AcceptAccessUserBody } from '../../../core/api/generated/models/accept-access-user-body';
import { AccessUserDetail } from '../../../core/api/generated/models/access-user-detail';
import { AccessUserPermissions } from '../../../core/api/generated/models/access-user-permissions';
import { AccessUserSummaryPagedResult } from '../../../core/api/generated/models/access-user-summary-paged-result';
import { DisableAccessUserBody } from '../../../core/api/generated/models/disable-access-user-body';
import { PermissionCatalogueResponse } from '../../../core/api/generated/models/permission-catalogue-response';
import { ReactivateAccessUserBody } from '../../../core/api/generated/models/reactivate-access-user-body';
import { ReplaceUserPermissionsBody } from '../../../core/api/generated/models/replace-user-permissions-body';
import { AccessUserListQuery } from '../models/access-admin.models';

@Injectable({ providedIn: 'root' })
export class AccessAdminApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/access`;

  listUsers(query: AccessUserListQuery): Observable<ApiResponse<AccessUserSummaryPagedResult>> {
    return this.http.get<ApiResponse<AccessUserSummaryPagedResult>>(`${this.baseUrl}/users`, {
      params: userListParams(query),
    });
  }

  getUser(userId: number): Observable<ApiResponse<AccessUserDetail>> {
    return this.http.get<ApiResponse<AccessUserDetail>>(`${this.baseUrl}/users/${userId}`);
  }

  acceptUser(
    userId: number,
    body: AcceptAccessUserBody,
  ): Observable<ApiResponse<AccessUserDetail>> {
    return this.http.post<ApiResponse<AccessUserDetail>>(
      `${this.baseUrl}/users/${userId}/accept`,
      body,
    );
  }

  disableUser(
    userId: number,
    body: DisableAccessUserBody,
  ): Observable<ApiResponse<AccessUserDetail>> {
    return this.http.post<ApiResponse<AccessUserDetail>>(
      `${this.baseUrl}/users/${userId}/disable`,
      body,
    );
  }

  reactivateUser(
    userId: number,
    body: ReactivateAccessUserBody,
  ): Observable<ApiResponse<AccessUserDetail>> {
    return this.http.post<ApiResponse<AccessUserDetail>>(
      `${this.baseUrl}/users/${userId}/reactivate`,
      body,
    );
  }

  getPermissionCatalogue(): Observable<ApiResponse<PermissionCatalogueResponse>> {
    return this.http.get<ApiResponse<PermissionCatalogueResponse>>(`${this.baseUrl}/permissions`);
  }

  replacePermissions(
    userId: number,
    body: ReplaceUserPermissionsBody,
  ): Observable<ApiResponse<AccessUserPermissions>> {
    return this.http.put<ApiResponse<AccessUserPermissions>>(
      `${this.baseUrl}/users/${userId}/permissions`,
      body,
    );
  }
}

function userListParams(query: AccessUserListQuery): HttpParams {
  let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
  if (query.status) {
    params = params.set('status', query.status);
  }
  if (query.isOwner !== undefined) {
    params = params.set('isOwner', query.isOwner);
  }
  const search = query.search?.trim();
  if (search) {
    params = params.set('search', search);
  }
  return params;
}
