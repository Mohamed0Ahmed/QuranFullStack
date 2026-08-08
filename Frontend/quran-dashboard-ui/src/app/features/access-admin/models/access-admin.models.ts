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

export interface AccessUserIdentity {
  readonly displayName: string | null;
  readonly email: string;
}

export function accessUserNameLabel(user: AccessUserIdentity): string {
  return user.displayName?.trim() || user.email;
}

export interface AccessPermissionDiff {
  readonly granted: readonly PermissionCode[];
  readonly revoked: readonly PermissionCode[];
}

export function hasPermissionChanges(diff: AccessPermissionDiff): boolean {
  return diff.granted.length > 0 || diff.revoked.length > 0;
}

export type AccessUserLifecycleAction = 'accept' | 'disable' | 'reactivate';

export type AccessUserWorkflowAction = AccessUserLifecycleAction | 'permissions';

export interface AccessUserPermissionTarget {
  readonly isOwner: boolean;
  readonly status: string;
}

export function canSelectUserPermissions(user: AccessUserPermissionTarget | null): boolean {
  return user !== null && !user.isOwner && (user.status === 'pending' || user.status === 'active');
}

export function canReplaceUserPermissions(
  user: AccessUserPermissionTarget | null,
  canAssignPermissions: boolean,
): boolean {
  return canSelectUserPermissions(user) && user?.status === 'active' && canAssignPermissions;
}

export function acceptGrantsPermissions(
  canAssignPermissions: boolean,
  diff: AccessPermissionDiff,
): boolean {
  return canAssignPermissions && diff.granted.length > 0;
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
