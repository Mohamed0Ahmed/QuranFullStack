import { PermissionCode } from '../../../core/auth/permission-code';

export type AccessUserStatus = 'pending' | 'active' | 'disabled';

export interface AccessUserListQuery {
  readonly status?: AccessUserStatus;
  readonly isOwner?: boolean;
  readonly search?: string;
  readonly page: number;
  readonly pageSize: number;
}

export interface AccessUserListFilters {
  readonly status?: AccessUserStatus;
  readonly isOwner?: boolean;
  readonly search?: string;
}

export interface AccessAuditQuery {
  readonly targetUserId?: number;
  readonly actorUserId?: number;
  readonly actionType?: string;
  readonly permissionCode?: PermissionCode;
  readonly fromUtc?: string;
  readonly toUtc?: string;
  readonly cursor?: string;
  readonly pageSize: number;
}

export interface AccessPermissionDiff {
  readonly granted: readonly PermissionCode[];
  readonly revoked: readonly PermissionCode[];
}

export interface AccessRelinkPreviewRequest {
  readonly newSub: string;
  readonly evidenceToken: string;
}

export interface AccessRelinkConfirmRequest extends AccessRelinkPreviewRequest {
  readonly expectedVersion: number;
  readonly oldSub: string;
  readonly reason: string;
  readonly confirmed: true;
}
