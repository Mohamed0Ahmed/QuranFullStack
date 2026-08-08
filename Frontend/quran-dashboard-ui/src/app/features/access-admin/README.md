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
- Treats permission editing as a draft with an honest exit — see *Permission draft, revert and
  unsaved-change protection* below.
- Requires an inline reason/diff confirmation for grant/status changes. Every changed permission
  is shown by its Arabic label and stable code before confirmation. There is only one modal in
  the feature: the unsaved-changes confirmation described below.
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
loading state rather than flashing the permission-denied error. It also loads on an `effect` over
`canAccess()` rather than once from a lifecycle hook, so an Owner whose identity resolves after the
page is mounted gets a populated workspace instead of an empty one with no reload path —
`CurrentUserStore` nulls its signals and re-enters `loading` on every authenticated emission, so
that ordering is routine, not exotic.

## Permission draft, revert and unsaved-change protection

The permission editor is a draft over the stored grants. Dirty means *a differing request body
exists **and** can be produced*, and `AccessPermissionDraftStore.isDirty` enforces both halves:

- **Both sides of `diff` are projected through the same catalogue** — the stored grants exactly as
  the draft is. A code the catalogue does not offer therefore counts on neither side: it can read
  neither as a pending grant nor as a pending revocation. Projecting only the draft would turn a
  granted-but-unoffered code into a permanent phantom revocation that «تجاهل التغييرات» cannot
  clear, since restoring the draft from the stored grants reproduces it. The save projects
  identically — `permissionCodesForAssignment()` returns the store's `codesForSubmission()`, the same
  catalogue projection — so such a code is also absent from the request body the save puts on the
  wire, which the diff says nothing about. That understatement is pre-existing and is Phase 8's
  tracked defect, recorded as row **AC1** in `docs/TESTING_DEBT.md`.
- **`canAssign` must hold**, because a failed, unready or empty catalogue can produce no request
  body at all. A draft made before assignment was withdrawn is kept, not dropped, and reads as dirty
  again once the catalogue recovers. That is an in-memory guarantee only: while assignment is
  withdrawn `isDirty` is `false`, so `hasUnsavedChanges()` is `false` and
  `accessAdminUnsavedChangesGuard` lets a route change through unprompted and the draft leaves with
  the component. That is the intended trade — the bar carrying «تجاهل التغييرات» is hidden while
  assignment is unavailable, so a prompt would offer a choice about a draft the operator can neither
  save nor discard.

Both halves are load-bearing: without them a user nobody touched reads as dirty over a degraded
catalogue — every stored grant falls into `revoked`, the summary prints above the catalogue error,
switching users and leaving the page prompt, and relink is held back with no way to clear it, since
the bar carrying the only discard control is hidden while assignment is unavailable.

- While the draft differs, the section heading carries a `+N / −M` summary and a bar under the
  editor offers the save entry point and «تجاهل التغييرات». `discardDraft()` restores the draft from
  the stored grants and issues no request. The summary's glyphs are `aria-hidden` beside a
  `.qd-sr-only` Arabic sibling that carries the reading: `aria-label` on a bare `<span>` names an
  element with the implicit `generic` role, which ARIA 1.2 prohibits, so it may never be announced.
- **The save entry point exists only while the draft is dirty**, which is how no-op saves are
  blocked; the confirmation's own confirm button is additionally disabled if the draft is reverted
  while it is open. The backend already short-circuits an empty change set, so this prevents a
  wasted round-trip, not audit pollution.
- The mandatory trimmed 1..1024-character reason is unchanged: reverting is the only way to leave
  an edit without one.
- A dirty draft holds back relink. Relink is an identity operation that refreshes the selected
  user, so running it mid-edit would silently drop the draft.

Two mechanisms protect the draft, because they cover different exits:

- **Switching users is not a route change**, so the router cannot see it. `selectUser` parks the
  requested id and opens the shared `qd-confirm-dialog`; only confirming discards the draft and
  loads the new user, and declining leaves both the draft and the current selection untouched.
- **Leaving the page is a route change.** `accessAdminUnsavedChangesGuard` is a functional
  `CanDeactivateFn` on the single route. It reads the **component instance** — the facade is
  component-provided, so injecting it into a guard would resolve a different instance — through the
  public `hasUnsavedChanges()`. It uses `window.confirm` deliberately: hoisting dialog state out of
  the component to reuse `qd-confirm-dialog` from a guard buys nothing, and the in-page path above
  is where the interaction actually happens.

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

That announcement is bought with vertical space, and the cost is real: the region is rendered even
with nothing to say, so the top of the detail card carries roughly **6.5rem** of permanently blank
space — `.qd-empty-state` contributes `padding: var(--qd-space-6)` on both block edges (2 × 2rem)
and `[reserve]` holds `min-block-size: var(--qd-control-block-size)` (≈ 2.53rem) for the message
line. There is no padding modifier on the state primitive today, so the only way to shrink it is a
change to the shared `qd-state` styles; that is layout work and belongs with the page redesign, not
with a behaviour change. It is recorded here rather than left to be rediscovered.

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
- `state/access-permission-draft.store.ts` owns the permission slice — catalogue, assignment
  readiness, the stored grants, the draft, the diff and dirty state — as a plain signal-backed class
  the facade composes. It performs no HTTP: the facade calls the API and hands it the outcome, so
  error mapping stays in one place. The facade re-exports its signals, so the store is an internal
  seam and no consumer outside this folder sees it.
- `models/access-admin.labels.ts` holds the Arabic copy that TypeScript needs (dialog and
  `window.confirm` wording, the diff-summary label builder). Template-only copy stays in the
  templates. **The page component reads it through a getter, not a class field**
  (`access-admin-page.component.ts` `get labels()`), matching how `abwab-page.component.ts` exposes
  `ABWAB_LABELS`. This is load-bearing, not style: with `protected readonly labels =
  ACCESS_ADMIN_LABELS` every test in the page spec fails on a cache-cleared build with «Cannot read
  properties of undefined (reading 'unsavedChangesTitle')». The unit-test builder's Vite SSR
  transform hoists a bare imported identifier used as a class-field initialiser into a module-level
  `const` snapshot of the import, taken before the lazily-initialised chunk that assigns
  `ACCESS_ADMIN_LABELS` has run, so every instance holds `undefined` while the import itself is a
  live object by the time the constructor runs. Getter and method bodies read the import at call
  time and are unaffected. The same class-field shape works elsewhere in the repo, where chunk
  ordering populates the snapshot in time — this is a property of the bundle, not a rule about class
  fields — and the page spec is what fails if the getter is reverted. The builder persists no
  transformed module and emits no surviving chunk, so that mechanism cannot be re-inspected after the
  run: it is an observation under `@angular/build` 20.3.27, `vite` 7.3.2, `vitest` 3.2.6 and
  `esbuild` 0.28.0 (vite bundles its own 0.27.7) on Node 20.20.2. To falsify it after a toolchain
  upgrade rather than inherit it: delete `.angular/cache` and `node_modules/.vite`, revert the getter
  to `protected readonly labels = ACCESS_ADMIN_LABELS`, and run the feature lane below — if the page
  spec passes, the mechanism no longer holds here and the getter can go.
- `components/` renders feature UI and emits interactions. It does not call HTTP directly.
  `access-user-workflows` derives its own dirty predicate (`hasUnsavedPermissions()`) from the
  `permissionDiff` and `canAssignPermissions` inputs instead of taking a separate `isDirty` input,
  so the bar, the summary and the relink gate cannot disagree with the diff rendered beside them.
- Owner membership, role editing, group grants, and navbar changes are out of scope.

## Testing

`npm run test:feature:access-admin` is this feature's primary test lane. The repository-wide gate
checker requires this lane to be configured whenever specs in this folder change.
