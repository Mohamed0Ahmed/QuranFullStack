/**
 * The authenticated user's local account, as returned by `GET /api/access/me`.
 *
 * This is the exact backend contract (camelCase; `status` is lowercased server-side),
 * so the stored view model and the wire DTO are the same shape — no UI narrowing is
 * needed. `roleId` / `roleName` are `null` and `status` is `'pending'` until the account
 * holds a role: the bootstrapped Owner comes back `active` with `roleName === 'Owner'`,
 * while everyone else stays `pending` / `null` until the Owner assigns a role. The
 * reusable `roleGuard` reads `status` + `roleName` (Feature 033, Phase 2).
 */
export interface CurrentUser {
  sub: string;
  email: string;
  displayName: string | null;
  status: 'pending' | 'active' | 'disabled';
  roleId: number | null;
  roleName: string | null;
}

/**
 * The role names the app can guard on. Mirrors the backend `RoleNames` constants
 * (`Backend/.../Domain/Access/RoleNames.cs`). `CurrentUser.roleName` stays the wider
 * `string | null` (the raw wire value); this narrower union types the `roleGuard`
 * argument so a typo can't silently deny everyone.
 */
export type RoleName = 'Owner' | 'Admin' | 'Editor';

/** Wire DTO alias — identical to {@link CurrentUser}; used at the API boundary. */
export type CurrentUserDto = CurrentUser;
