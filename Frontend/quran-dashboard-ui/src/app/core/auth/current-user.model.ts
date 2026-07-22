// The exact `GET /api/access/me` backend contract, so the stored view model and the wire DTO are
// the same shape — no UI narrowing. `roleId`/`roleName` are null and `status` is `pending` until
// the account holds a role; the bootstrapped Owner comes back `active` with `roleName === 'Owner'`.
export interface CurrentUser {
  sub: string;
  email: string;
  displayName: string | null;
  status: 'pending' | 'active' | 'disabled';
  roleId: number | null;
  roleName: string | null;
  // Effective permission codes projected by `/me` (US5). Used ONLY for non-authoritative UI hiding;
  // the backend policy is the authority. Wire is `string[]` (tolerant of catalogue growth); the
  // permission guard/helper narrow to the known `PermissionCode` union.
  permissions: string[];
}

// Role names the app can guard on. Mirrors the backend `RoleNames` constants
// (Backend/.../Domain/Access/RoleNames.cs). `CurrentUser.roleName` stays the wider raw `string |
// null`; this narrower union types the `roleGuard` argument so a typo can't silently deny everyone.
export type RoleName = 'Owner' | 'Admin' | 'Editor';

// Wire DTO alias — identical to `CurrentUser`; named for use at the API boundary.
export type CurrentUserDto = CurrentUser;
