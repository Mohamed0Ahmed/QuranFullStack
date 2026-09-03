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

export type AccessUserLifecycleAction = 'accept' | 'disable' | 'reactivate';

export type AccessUserWorkflowAction = AccessUserLifecycleAction | 'permissions';

export type AccessLifecycleTone = AccessUserStatus | 'unknown';

export function accessLifecycleTone(status: string): AccessLifecycleTone {
  return status === 'pending' || status === 'active' || status === 'disabled' ? status : 'unknown';
}
