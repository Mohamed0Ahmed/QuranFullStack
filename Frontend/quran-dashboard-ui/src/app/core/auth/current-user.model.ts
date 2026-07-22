export interface CurrentUser {
  sub: string;
  email: string;
  displayName: string | null;
  status: 'pending' | 'active' | 'disabled';
  roleId: number | null;
  roleName: string | null;
  permissions: string[];
}

// Mirrors backend Domain/Access/RoleNames.cs.
export type RoleName = 'Owner' | 'Admin' | 'Editor';

export type CurrentUserDto = CurrentUser;
