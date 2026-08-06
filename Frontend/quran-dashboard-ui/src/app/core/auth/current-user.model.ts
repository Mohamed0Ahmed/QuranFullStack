import { CurrentUserResponse } from '../api/generated/models/current-user-response';
import { PermissionCode, isPermissionCode } from './permission-code';

export type CurrentUserStatus = 'pending' | 'active' | 'disabled';
export type TransitionalRoleName = 'Owner' | null;

export interface CurrentUser {
  sub: string;
  email: string;
  displayName: string | null;
  status: CurrentUserStatus;
  isOwner: boolean;
  permissions: readonly PermissionCode[];
  roleName: TransitionalRoleName;
}

export type CurrentUserDto = CurrentUserResponse;

const userStatuses = new Set<CurrentUserStatus>(['pending', 'active', 'disabled']);

export function toCurrentUser(response: CurrentUserResponse): CurrentUser | null {
  if (!userStatuses.has(response.status as CurrentUserStatus)) {
    return null;
  }

  if (!response.sub.trim() || !response.email.trim()) {
    return null;
  }

  if (!response.permissions.every(isPermissionCode)) {
    return null;
  }

  const status = response.status as CurrentUserStatus;
  const permissions = status === 'active' ? [...new Set(response.permissions)] : [];

  return {
    sub: response.sub,
    email: response.email,
    displayName: response.displayName,
    status,
    isOwner: response.isOwner,
    permissions,
    roleName: response.roleName === 'Owner' ? 'Owner' : null,
  };
}
