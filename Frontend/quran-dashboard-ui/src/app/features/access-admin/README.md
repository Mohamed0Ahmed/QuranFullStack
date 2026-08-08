# Access administration

Owner-only security-administration feature at `/settings/access`. It is intentionally absent from
the navbar and is reached through the guarded route only.

## What it does

- Lists and filters local access users, then shows an individual account's status and version.
- Lets an active Owner accept Pending users, disable Active non-Owners, and reactivate Disabled
  non-Owners. Disable explains that it removes every direct grant; reactivate begins with none.
  Those lifecycle controls live in their own «إجراءات الحساب» region, never in the row that carries
  the permission save — see *Per-status semantics* below.
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
- Recovers a lost login identity through Logto-subject relink, presented as a secondary
  الأمان المتقدم region outside the permission workspace — see *Advanced Security* below. The flow
  is unchanged: preview first, then a separate explicit confirmation; the UI submits a new subject
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
  the identity-recovery panel keep rendering, because none of them reads the catalogue.
- **The catalogue is served but assignment is not ready.** The editor stays visible and read-only so
  current grants can be inspected, an Arabic notice states that assignment is unavailable and that
  existing access is unchanged, and no permission-save path is offered. A pending account's accept
  button changes with it, from «قبول وتفعيل مع الصلاحيات المحددة» to «قبول وتفعيل دون صلاحيات».

Readiness is not the only thing that label answers to. `acceptGrantsPermissions()` requires assignment
readiness **and** a non-empty `permissionDiff().granted`, and `showsPermissionDiff()` routes the accept
confirmation through that same predicate — so the button and the confirmation one click later cannot
state opposite things, and neither promises a payload the facade will not send. A pending account
begins with no grants, so «قبول وتفعيل دون صلاحيات» is its default reading even over a healthy
catalogue; the «مع الصلاحيات المحددة» wording appears only once something is actually selected.

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
- A dirty draft still holds back relink, even though relink now lives outside the permission
  workspace. The reason was never adjacency: `confirmSelectedUserRelink` runs through `runMutation`,
  and every successful mutation calls `refreshAfterMutation`, which re-selects the user and makes
  `AccessPermissionDraftStore.adopt` overwrite the draft with the stored grants. Moving the panel
  removed the confusion of a live identity form sitting mid-edit; it did not remove the overwrite.
  The gate reads `AccessAdminFacade.isDirty` through the panel's `hasUnsavedPermissions` input,
  which is the same predicate the draft bar and the diff summary render from. It holds back **both**
  relink steps, not just the entry point: preview and confirm each carry it in their `disabled`
  expression and each re-check it in `requestRelinkPreview()`/`confirmRelink()`. Gating the preview
  alone would leave the sequence *preview → edit a permission → confirm* open, and the confirm is the
  step that runs the mutation whose refresh overwrites the draft.

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

## Per-status semantics

Each account state explains what the page can and cannot do to it, because the backend accepts a
different commit for each one.

- **Pending.** The editor renders, but the commit is `accept`: the replace endpoint requires
  `Status == Active`, so a `PUT` for a pending user is rejected. The permission region says the
  selected permissions are granted on activation, and the button says the same —
  «قبول وتفعيل مع الصلاحيات المحددة» while assignment is available **and** something is selected,
  «قبول وتفعيل دون صلاحيات» otherwise. The confirmation shows the grant list only when there is one
  to show — the same predicate drives both, see above; otherwise it states
  the account will be activated with no direct permissions, which is exactly what the empty
  `permissionCodes` payload does.
- **Active non-Owner.** The affirmative save lives in the draft bar under the editor and appears only
  while the draft is dirty. «تعطيل الحساب» sits in the separate «إجراءات الحساب» region, under a line
  stating that disabling stops access and removes every direct permission for good — disable
  snapshots the grants, emits one revoke event each, and deletes the rows; reactivate restores none
  of them. The two controls never share a row. There is no danger button class in the style system,
  so weight comes from placement and from the warning copy, not from a red button.
- **Disabled.** No editor renders, because the backend rejects a replace on a non-Active user. The
  region says the account holds no direct permissions and that none can be assigned before
  reactivation, and repeats that reactivation starts from none.
- **Owner.** No editor and no checked-and-disabled catalogue. An Owner's access does not come from
  direct grants at all: `PermissionAuthorizationHandler` succeeds on `state.IsOwner`, and
  `AuthorizationStateAccessEvaluator.ResolveActiveStateAsync` returns a state only for an Active
  user — so the bypass statement is made **only** for an Active Owner, and a Pending or Disabled
  Owner is told the bypass does not apply yet. Both variants add that Owner membership is managed by
  owner reconciliation rather than from this page. Nothing here claims an Owner account is
  uneditable in general, because it is not: identity recovery below applies to Owners too.

## Advanced Security

`components/access-advanced-security/` hosts Logto-subject relink as identity recovery, in its own
recessive `.qd-card--quiet` region below the workspace rather than inside the selected-user panel.
The move is a placement decision, not a capability change — `ConfirmCoreAsync` has no Owner guard and
no status guard, and `ValidateBindingAsync` routes an Owner target to
`ValidateOwnerConfigurationAsync`, which permits the relink when the Owner's configured email
reconciles as `Unchanged`. The panel says so: it applies to the selected account whatever its role,
including an Owner. Presenting it as routine permission editing was what was wrong, not the
capability.

An Owner target additionally gets that precondition in the copy, not only in this file. Because
`ValidateOwnerConfigurationAsync` fails with `OwnerConfigurationNotReconciled` unless the target's
normalized email is in the configured owner set **and** owner reconciliation reports a candidate for
that user in state `Unchanged`, an Owner relink can otherwise fail at confirm for a reason the panel
never named. `access-relink-owner-precondition` states both halves and renders only when
`target.isOwner`; the reconciliation panel lower on the page prints each candidate's raw state, so
`Unchanged` is a value the operator can actually match.

The component owns only the relink form state. It takes the selected user, the preview, the busy
action and the dirty-draft gate as inputs and emits preview/confirm/cancel; the facade still owns
every request, the evidence token and the preview lifecycle. With no user selected it renders an
empty state instead of a form, since relink targets one account.

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
  so the bar and the summary cannot disagree with the diff rendered beside them.
  `access-advanced-security` renders no diff, so it takes the facade's `isDirty` as an input — the
  same computed those two are derived from.
- Owner membership, role editing, group grants, and navbar changes are out of scope.

## Testing

`npm run test:feature:access-admin` is this feature's primary test lane. The repository-wide gate
checker requires this lane to be configured whenever specs in this folder change.
