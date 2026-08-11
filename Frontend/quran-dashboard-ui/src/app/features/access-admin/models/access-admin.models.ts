import { AccessUserSummary } from '../../../core/api/generated/models/access-user-summary';
import { PermissionCode } from '../../../core/auth/permission-code';

export type AccessUserStatus = 'pending' | 'active' | 'disabled';

export const ACCESS_AUDIT_ACTION_TYPES = [
  'UserAccepted',
  'UserActivated',
  'UserDisabled',
  'UserReactivated',
  'PermissionGranted',
  'PermissionRevoked',
  'LogtoSubjectRelinked',
  'OwnerGrantedByReconciliation',
  'OwnerRemovedByReconciliation',
  'LegacyRoleRemoved',
] as const;

export type AccessAuditActionType = (typeof ACCESS_AUDIT_ACTION_TYPES)[number];

export function isAccessAuditActionType(value: string): value is AccessAuditActionType {
  return (ACCESS_AUDIT_ACTION_TYPES as readonly string[]).includes(value);
}

export const OWNER_RELINK_REQUIRED_CANDIDATE_STATE = 'Unchanged';

export const ACCESS_USER_PICKER_PAGE_SIZE = 10;

export interface AccessUserSearchState {
  readonly users: readonly AccessUserSummary[];
  readonly error: string | null;
  readonly loading: boolean;
}

export const EMPTY_ACCESS_USER_SEARCH: AccessUserSearchState = {
  users: [],
  error: null,
  loading: false,
};

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

export const ACCESS_ACCOUNT_VARIANTS = [
  'pending-non-owner',
  'active-non-owner',
  'disabled-non-owner',
  'active-owner',
  'pending-owner',
  'disabled-owner',
  'unknown-status',
] as const;

export type AccessAccountVariant = (typeof ACCESS_ACCOUNT_VARIANTS)[number];

export function accessAccountVariant(
  user: AccessUserPermissionTarget | null,
): AccessAccountVariant | null {
  if (user === null) {
    return null;
  }
  const status = user.status;
  if (status !== 'pending' && status !== 'active' && status !== 'disabled') {
    return 'unknown-status';
  }
  return user.isOwner ? `${status}-owner` : `${status}-non-owner`;
}

export function accessLifecycleActionsApply(user: AccessUserPermissionTarget | null): boolean {
  const variant = accessAccountVariant(user);
  return (
    variant === 'pending-non-owner' ||
    variant === 'active-non-owner' ||
    variant === 'disabled-non-owner'
  );
}

export type AccessLifecycleTone = AccessUserStatus | 'unknown';

export function accessLifecycleTone(status: string): AccessLifecycleTone {
  return status === 'pending' || status === 'active' || status === 'disabled' ? status : 'unknown';
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
