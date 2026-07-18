/**
 * The authenticated user's local account, as returned by `GET /api/access/me`.
 *
 * This is the exact backend contract (camelCase; `status` is lowercased server-side),
 * so the stored view model and the wire DTO are the same shape — no UI narrowing is
 * needed. `roleId` is `null` and `status` is `'pending'` until the Owner activates the
 * account (Phase 2 consumes this to gate the pending-activation flow).
 */
export interface CurrentUser {
  sub: string;
  email: string;
  displayName: string | null;
  status: 'pending' | 'active' | 'disabled';
  roleId: number | null;
}

/** Wire DTO alias — identical to {@link CurrentUser}; used at the API boundary. */
export type CurrentUserDto = CurrentUser;
