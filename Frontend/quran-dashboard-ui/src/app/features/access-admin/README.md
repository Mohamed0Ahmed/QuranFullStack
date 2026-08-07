# Access administration

Owner-only security-administration feature at `/settings/access`. It is intentionally absent from
the navbar and is reached through the guarded route only.

## What it does

- Lists and filters local access users, then shows an individual account's status and version.
- Lets an active Owner accept Pending users, disable Active non-Owners, and reactivate Disabled
  non-Owners. Disable explains that it removes every direct grant; reactivate begins with none.
- Presents server-catalogued permissions by group. Select-all is group-local: a partial group is
  indeterminate, selecting it adds that group's individual `PermissionCode` values, and clearing it
  removes only that group. Every individual value can still be unchecked. Requests contain the
  flattened known codes only, never a group or select-all sentinel; a later code is not silently
  granted to existing selections.
- Requires an inline reason/diff confirmation for grant/status changes. Every changed permission
  is shown by its Arabic label and stable code before confirmation. There is no feature modal or
  backdrop.
- Uses relink preview followed by a separate explicit confirmation. The UI submits a new subject
  plus masked verification evidence, clears that evidence after preview/cancellation/completion,
  and has no email-only relink path.
- Displays basic keyset-paginated audit history with actor type and actor ID attribution, plus
  target, actor, action, and permission filters, and owner-reconciliation status as read-only
  data.

## Boundaries

- `data-access/access-admin.api.ts` is the typed Phase 6 HTTP boundary.
- `state/access-admin.facade.ts` owns API orchestration and refreshes the selected target after a
  `409`; it never retries a mutation. `401` and `403` go through the shared write-auth coordinator.
- `components/` renders feature UI and emits interactions. It does not call HTTP directly.
- Owner membership, role editing, group grants, and navbar changes are out of scope.

## Testing

`npm run test:feature:access-admin` is this feature's primary test lane. The repository-wide gate
checker requires this lane to be configured whenever specs in this folder change.
