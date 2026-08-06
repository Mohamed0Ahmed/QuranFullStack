import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AcceptAccessUserBody } from '../../../core/api/generated/models/accept-access-user-body';
import { AccessAuditEventPage } from '../../../core/api/generated/models/access-audit-event-page';
import { AccessUserDetail } from '../../../core/api/generated/models/access-user-detail';
import { AccessUserPermissions } from '../../../core/api/generated/models/access-user-permissions';
import { AccessUserSummaryPagedResult } from '../../../core/api/generated/models/access-user-summary-paged-result';
import { ConfirmLogtoSubjectRelinkBody } from '../../../core/api/generated/models/confirm-logto-subject-relink-body';
import { DisableAccessUserBody } from '../../../core/api/generated/models/disable-access-user-body';
import { LogtoSubjectRelinkPreview } from '../../../core/api/generated/models/logto-subject-relink-preview';
import { OwnerReconciliationStatus } from '../../../core/api/generated/models/owner-reconciliation-status';
import { PermissionCatalogueItem } from '../../../core/api/generated/models/permission-catalogue-item';
import { PreviewLogtoSubjectRelinkBody } from '../../../core/api/generated/models/preview-logto-subject-relink-body';
import { ReactivateAccessUserBody } from '../../../core/api/generated/models/reactivate-access-user-body';
import { ReplaceUserPermissionsBody } from '../../../core/api/generated/models/replace-user-permissions-body';
import {
  AccessAuditQuery,
  AccessRelinkConfirmRequest,
  AccessRelinkPreviewRequest,
  AccessUserListQuery,
} from '../models/access-admin.models';

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

  acceptUser(userId: number, body: AcceptAccessUserBody): Observable<ApiResponse<AccessUserDetail>> {
    return this.http.post<ApiResponse<AccessUserDetail>>(`${this.baseUrl}/users/${userId}/accept`, body);
  }

  disableUser(userId: number, body: DisableAccessUserBody): Observable<ApiResponse<AccessUserDetail>> {
    return this.http.post<ApiResponse<AccessUserDetail>>(`${this.baseUrl}/users/${userId}/disable`, body);
  }

  reactivateUser(userId: number, body: ReactivateAccessUserBody): Observable<ApiResponse<AccessUserDetail>> {
    return this.http.post<ApiResponse<AccessUserDetail>>(`${this.baseUrl}/users/${userId}/reactivate`, body);
  }

  getPermissionCatalogue(): Observable<ApiResponse<PermissionCatalogueItem[]>> {
    return this.http.get<ApiResponse<PermissionCatalogueItem[]>>(`${this.baseUrl}/permissions`);
  }

  getUserPermissions(userId: number): Observable<ApiResponse<AccessUserPermissions>> {
    return this.http.get<ApiResponse<AccessUserPermissions>>(`${this.baseUrl}/users/${userId}/permissions`);
  }

  replacePermissions(
    userId: number,
    body: ReplaceUserPermissionsBody,
  ): Observable<ApiResponse<AccessUserPermissions>> {
    return this.http.put<ApiResponse<AccessUserPermissions>>(`${this.baseUrl}/users/${userId}/permissions`, body);
  }

  listAuditEvents(query: AccessAuditQuery): Observable<ApiResponse<AccessAuditEventPage>> {
    return this.http.get<ApiResponse<AccessAuditEventPage>>(`${this.baseUrl}/audit-events`, {
      params: auditParams(query),
    });
  }

  previewRelink(
    userId: number,
    body: AccessRelinkPreviewRequest,
  ): Observable<ApiResponse<LogtoSubjectRelinkPreview>> {
    return this.http.post<ApiResponse<LogtoSubjectRelinkPreview>>(
      `${this.baseUrl}/users/${userId}/logto-sub/relink/preview`,
      body satisfies PreviewLogtoSubjectRelinkBody,
    );
  }

  confirmRelink(
    userId: number,
    body: AccessRelinkConfirmRequest,
  ): Observable<ApiResponse<AccessUserDetail>> {
    return this.http.post<ApiResponse<AccessUserDetail>>(
      `${this.baseUrl}/users/${userId}/logto-sub/relink/confirm`,
      body satisfies ConfirmLogtoSubjectRelinkBody,
    );
  }

  getOwnerReconciliationStatus(): Observable<ApiResponse<OwnerReconciliationStatus>> {
    return this.http.get<ApiResponse<OwnerReconciliationStatus>>(
      `${this.baseUrl}/owner-reconciliation/status`,
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

function auditParams(query: AccessAuditQuery): HttpParams {
  let params = new HttpParams().set('pageSize', query.pageSize);
  for (const [key, value] of Object.entries(query)) {
    if (key !== 'pageSize' && value !== undefined && value !== null && value !== '') {
      params = params.set(key, value);
    }
  }
  return params;
}
