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
- Fails closed when permission assignment is not available — see *Permission-assignment failure
  model* below.
- Requires an inline reason/diff confirmation for grant/status changes. Every changed permission
  is shown by its Arabic label and stable code before confirmation. There is no feature modal or
  backdrop.
- Uses relink preview followed by a separate explicit confirmation. The UI submits a new subject
  plus masked verification evidence, clears that evidence after preview/cancellation/completion,
  and has no email-only relink path.
- Displays basic keyset-paginated audit history with actor type and actor ID attribution, plus
  target, actor, action, and permission filters, and owner-reconciliation status as read-only
  data.

## Permission-assignment failure model

`GET /api/access/permissions` answers with `{ items, assignmentReady }`. A readable catalogue does
not mean a writable one, so the two failure modes are kept distinct and neither is allowed to change
stored access.

- **The catalogue request fails.** Only the الصلاحيات المباشرة region degrades: it renders an error
  with a retry that re-issues the catalogue request. Identity, status badges, lifecycle actions and
  the relink form keep rendering, because none of them reads the catalogue.
- **The catalogue is served but assignment is not ready.** The editor stays visible and read-only so
  current grants can be inspected, an Arabic notice states that assignment is unavailable and that
  existing access is unchanged, and no permission-save path is offered.

`AccessAdminFacade.canAssignPermissions` is the single gate. It requires `assignmentReady`, no
catalogue error, **and** at least one rendered permission group — an empty catalogue is never
treated as ready, so Save can never be enabled over an empty editor. A failed catalogue load also
resets the readiness flag, so a stale `true` cannot survive a failed refresh or the window while a
retry is in flight.

Two independent guards keep a degraded catalogue from producing an empty replacement set:

- `setSelectedPermissionCodes` filters the draft by `isPermissionCode` alone, never through the
  catalogue, so a catalogue that fails or arrives empty cannot empty a draft already held. The
  editor is a narrower gate on its own side — `AccessPermissionEditorComponent.emitSelection`
  intersects with the groups it rendered — but it only ever reports a selection the operator just
  made in a rendered editor, and a failed or unready catalogue renders no editable one;
- `replaceSelectedPermissions` refuses to submit while assignment is unavailable, and
  `acceptSelectedUser` then sends an explicit empty `permissionCodes` array rather than whatever the
  catalogue projection produced.

Accept-without-permissions, disable and reactivate stay available throughout — none of them can
revoke a grant that assignment readiness was protecting. Readiness is re-read after every mutation
and on retry, so recovery re-enables Save without a page reload. The write path is never relaxed to
compensate: a `400` from the server on an unseeded catalogue is the fail-safe working.

The page waits for the current-user load state before deciding access, so a token renewal shows a
loading state rather than flashing the permission-denied error.

## State regions and announcement

The mutation message is the one `qd-state` on this page rendered **unconditionally**: it sits
directly in the detail card, above the `@if` chain, carrying `[reserve]="true"` and an empty message
whenever there is nothing to say. That shape is what makes it announce. A success or `409` recovery
notice renders `role="status"`, and a live region created together with its text is generally not
read out; because this element already exists and is empty, the later text insertion is what the
screen reader announces. `runMutation` clears the message before every write, so the region is
always empty again before the next text lands. The severity routing survives the shape — `error`
renders the `error` variant (`role="alert"`, which announces on insertion), success and the `409`
notice render the quieter `empty` variant.

Because that region outlives the detail pane's own load/error branches, the message channel carries
mutation outcomes and nothing else. The permission-denied text is rendered from `canAccess()`
directly, one branch higher; `load()` does not also copy it into the message, which would survive an
access state that has since changed.

Every other state on this page sits inside an `@if`/`@else if` branch, where `[reserve]` would
reserve nothing — the flag holds space only in a region that is always rendered. Load and error
transitions therefore still resize the user, detail, and reconciliation panes; closing that needs a
min-block-size on the panes themselves, which is layout work and has not been done.

## Boundaries

- `data-access/access-admin.api.ts` is the typed Phase 6 HTTP boundary.
- `state/access-admin.facade.ts` owns API orchestration and refreshes the selected target after a
  `409`; it never retries a mutation. `401` and `403` go through the shared write-auth coordinator.
  It also owns the tone of every operator-facing message: a completed change reads as success, the
  `409` recovery reads as a notice, and only genuine failures render as errors.
- `components/` renders feature UI and emits interactions. It does not call HTTP directly.
- Owner membership, role editing, group grants, and navbar changes are out of scope.

## Testing

`npm run test:feature:access-admin` is this feature's primary test lane. The repository-wide gate
checker requires this lane to be configured whenever specs in this folder change.
