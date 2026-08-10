# Access administration

Owner-only security-administration feature at `/settings/access`. The navbar's «الإعدادات»
dropdown reaches it through its «إدارة الوصول» entry (`core/navigation/nav-menu.ts`,
`core/layout/top-navbar/`), which renders only for an Active Owner once the auth state is known.
That visibility is UX convenience, not authorization — `ownerGuard` on the route remains the sole
authorization boundary, and the route carries no additional guard for it.

## What it does

- Lists and filters local access users, then shows an individual account's identity and status. The
  «الاسم أو البريد» box is free text submitted with the filter form: the backend matches it as a
  substring of the account's email or stored name, ignoring letter case for Arabic and for ASCII, so a
  partial token finds the account instead of answering `400`, and only a term longer than 128
  characters is rejected.
- Labels every account through `accessUserNameLabel` (`models/access-admin.models.ts`), which falls
  back to the email when the stored name is absent **or only whitespace**. The list row and the
  workspace header both read it, so a blank-looking Logto profile name cannot render an empty label
  the way `displayName || email` did.
- Lets an active Owner accept Pending users, disable Active non-Owners, and reactivate Disabled
  non-Owners. Disable explains that it removes every direct grant; reactivate begins with none.
  Those lifecycle controls live in their own «إجراءات الحساب» region
  (`components/access-lifecycle-actions/`), never in the row that carries the permission save — see
  *Per-status semantics* below. That region renders nothing at all for an Owner target, so the
  guard in `AccessLifecycleActionsComponent` and the page's own Owner branch cannot disagree.
- Presents server-catalogued permissions by group, in a compact container-driven grid —
  `repeat(auto-fit, minmax(13rem, 1fr))` in `access-permission-editor.component.scss`, so the
  column count follows the editor's own width rather than viewport breakpoints. Select-all is
  group-local and labelled «تحديد الكل»: a partial group is indeterminate, selecting it adds that
  group's individual `PermissionCode` values, and clearing it removes only that group. Every
  individual value can still be unchecked. Requests contain the flattened known codes only, never a
  group or select-all sentinel; a later code is not silently granted to existing selections. The raw
  `abwab.*` code is carried on each row's `[title]` rather than printed beside its Arabic label — it
  stays visible where it earns its place, in the confirmation diff.
  **Group headings are the server's Arabic labels, not a frontend copy of them.**
  `PermissionDefinition.GroupArabicLabel` carries «الأبواب» / «الأقسام» / «العلاقات» / «القوالب» /
  «عناصر القوالب», `EfPermissionCatalogueReader` projects it into the wire field `groupLabel`, and
  `<legend>` renders it verbatim. The wire shape is unchanged; only the value it carries is, which is
  why no OpenAPI regeneration attends the change. Hardcoding the five Arabic headings here would have
  created a second hand-maintained mirror of a backend enumeration — the very debt this feature spent
  its last phase retiring.
- Fails closed when permission assignment is not available — see *Permission-assignment failure
  model* below.
- Treats permission editing as a draft with an honest exit — see *Permission draft, revert and
  unsaved-change protection* below.
- Confirms grant/status changes through an inline review step with an **optional** reason. Every
  changed permission is shown by its Arabic label and stable code before confirmation; a blank
  reason travels as `null` and the audit rows store `NULL`, while a typed one persists verbatim.
  Relink keeps its mandatory reason. There is only one modal in the feature: the unsaved-changes
  confirmation described below.
- Recovers a lost login identity through Logto-subject relink, presented in its own
  الأمان المتقدم section outside the permission workspace — see *Advanced Security* below. The flow
  is unchanged: preview first, then a separate explicit confirmation; the UI submits a new subject
  plus masked verification evidence, clears that evidence after preview/cancellation/completion,
  and has no email-only relink path.
- Displays keyset-paginated audit history attributed to **human identities**, plus account, actor,
  event-type, and permission filters, in its own سجل الوصول section
  (`components/access-audit-log/`); owner-reconciliation status is read-only diagnostic data beside
  identity recovery — see *Audit and reconciliation* and *Layout and URL state* below.

## Layout and URL state

The page is the Golden F19 master/detail workspace. Its root section carries `.qd-page` (block
rhythm only) and its single child carries `.qd-page-shell.qd-page-shell--split-workspace` — the
**one** inline-gutter owner on the route, capped at the `split-workspace` measure. No feature frame
adds a second gutter, and page-level horizontal scrolling is a defect in every band.

**Wide (`>= 1080`) is the only band with a rail.** `qd-page-split` puts a `.qd-page-rail--l`
(`20rem`) user list beside the details. Below Wide the rail is gone: a pinned
`access-selected-context-bar` carries the selected account's full identity, its LTR email, its
membership, its lifecycle badge **and the account search** (`access-context-search`), whose submit
applies the search to the shared user query and opens the results in the sheet. «اختيار حساب» opens
the *same* user list inside the shared `qd-modal-shell` sheet (`access-user-list-sheet`) —
focus-trapped, scroll-locked, Compact-full-bleed, and closed again the moment an account is chosen.
Because the context bar already carries identity, membership and lifecycle below Wide, the details
shell's metadata slot renders `qd-access-user-summary-card` **only at Wide**: the summary — and its
`access-user-summary-email` / `-lifecycle` / `-membership` test IDs — exists exactly once in the DOM
in every band. The band is read once through `QD_BP_WIDE_QUERY` and kept in a signal; nothing in
this feature writes a pixel threshold.

Three sections sit behind `qd-tabs`, the only tablist primitive in the app:

| Tab | `?tab=` | Contents |
|---|---|---|
| مساحة العمل | *(absent — the default)* | sticky 20rem user-list aside + the selected-user panel |
| سجل الوصول | `audit` | `qd-access-audit-log` |
| الأمان المتقدم | `security` | `qd-access-advanced-security` + owner reconciliation |

`?tab=` is a **closed enum** parsed by `models/access-admin-tabs.ts`; anything else falls back to the
workspace, so a stale or hand-edited link cannot render a blank page. Choosing the default section
writes `tab: null`, which removes the parameter rather than spelling out the default. The tabs are
`<button>`s that navigate — `qdTab` supplies `role="tab"`, `aria-selected` and the roving tabindex,
and each panel carries `role="tabpanel"` labelled by its tab.

**A query param, not a child route, and no user deep-link at all.** `FRONTEND_STRUCTURE.md` requires
this to be written down. A child route would break `access-admin.routes.spec.ts` and orphan the
`title` on the single route in `access-admin.routes.ts`, and these are view modes over one Owner-only
screen rather than destinations anyone links to. The selected **user** is deliberately *not* in the
URL: `AccessUserSummary` carries no slug and no `sub` (`sub` exists only on `AccessUserDetail`, i.e.
only after selection), `AccessAdminApi.userListParams` has no filter that could resolve a handle back
to an account, the numeric id is a technical identifier that must never appear in a visible URL, and
the email is PII. A deep link would therefore need a new backend contract to carry an opaque handle,
and that handle would still be an identifier. Selection stays in memory, which is also why switching
users needs its own confirmation (below) — the router cannot see it.

The selected-user panel is the shared F11 `qd-details-workspace` (`variant="safety"`). The page
supplies the account name as its `identity`, `qd-access-user-summary-card` projects the LTR email
and the two badges into the identity zone (at Wide — see above), and the shell's permanently mounted
`role="status"` slot carries the mutation surface.

**The panel is only a scroller where it has a definite height, and that is Wide.** The shared shell
declares `block-size: 100%` on `.qd-details__shell` and `overflow: auto` on `.qd-details__body`, but
a percentage height resolves to `auto` unless an ancestor has a definite one — `.qd-page` is a block
with a `min-block-size`, and the Wide split aligns its items to `start`. So the Access-owned
`.access-admin-page__detail` takes `block-size: calc(100dvh - navbar - 2 × space-4)` plus
`position: sticky` at `>= 1080`: measured in Chromium at 1080/1440 × 900 the panel is 812px tall, the
body scrolls 974px inside it, and the identity header, the status slot and the 64px review dock keep
identical viewport rects while it scrolls. Below Wide the panel stays in the document flow — the
document is the region's single scroller, identity and status are pinned by the sticky
`access-selected-context-bar` (which is why that bar, and not the details header, carries identity,
status, membership and search there), and the save/discard actions are pinned by the page-owned
sticky review dock described under *Permission drafts*. Do not restore a `block-size: 100%` on the
host without a definite ancestor height; it silently degrades to a page-tall panel with a dock at
the very bottom.

`layout="no-selection"` renders the designed prompt instead of an empty split. The panel does
**not** carry the `xmin` version:
optimistic concurrency stays in state and out of view, along with every other technical identifier
outside the audit rows.

**The email is never truncated.** It is the target of a safety decision, so it wraps in full inside
`.qd-ltr-isolate` rather than eliding behind a `[title]` the operator has to hover to read; only the
account *name* truncates, and only where a focusable row owns it.

**Four Access surfaces stopped truncating for the same reason (D35).** The Compact/Medium
context-bar identity, the audit row's `الحساب:` / `المنفّذ:` lines, the owner-reconciliation
candidate emails, and the user picker's chosen identity all had `[title]` as their *only*
disclosure. None reaches a rung of the Golden §8.1 disclosure ladder: the context-bar name sits in a
static bar with no owning control; audit rows and reconciliation candidates are deliberately
non-focusable `qdResultItem`s; and the picker's `<p>` is not focusable, while the button beside it
*clears* the selection rather than revealing it. `title` is unreachable by keyboard and by touch, so
all four now wrap (`min-inline-size: 0; overflow-wrap: anywhere`) and carry neither `.qd-truncate`
nor `[title]`. Adding `tabindex="0"` to the text node is explicitly prohibited and was not the fix.
Two Access surfaces are deliberately untouched because they *do* have a rung: user-list rows
truncate inside a focusable row button whose accessible name is the full value, and picker
candidates are `role="option"` elements carrying name + email in their own `aria-label`.

A list row is the shared `qdResultList`/`qdResultItem` pair (`listVariant="master"`): `role="list"` /
`role="listitem"`, `aria-posinset`/`aria-setsize`, `aria-current` on the selected row, and the
logical `border-inline-start` selection thread reserved as transparent on every row so selecting one
causes no shift. Names compose `.qd-truncate` with the mandatory `[title]`. The thread itself is a
resolved border width jsdom does not compute, so it rests on the shared `.qd-result-item` rules until
a browser check exists — recorded as row **AC2** in `docs/TESTING_DEBT.md`.

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
  draft projection produced.

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

- **Both sides of `diff` are projected identically, and neither is narrowed to the served
  catalogue.** `permissionCodesForSubmission` orders a selection by `PERMISSION_CODES` and keeps
  every entry `isPermissionCode` accepts; the stored grants pass through the same function. So a
  code the served catalogue omits — a canonical code retired on the server, say — sits on both sides
  and counts as neither a pending grant nor a pending revocation, which is what stops it becoming a
  permanent phantom revocation that «تجاهل التغييرات» could not clear. **What changed in Phase 8 is
  that the same code now also reaches the request body**: `codesForSubmission()` no longer intersects
  the draft with the rendered catalogue, so the save can no longer drop a grant the confirmation
  never mentioned. The diff and the wire agree. That symmetry is why the editor's own narrowing is
  correct rather than contradictory: `AccessPermissionEditorComponent.emitSelection` reports only
  codes it rendered, so the first checkbox the operator touches drops an unoffered code out of the
  draft — and the diff then shows it under «إزالة», because that is exactly what the save will do.
  The remaining case — a draft holding an unoffered code with no editor interaction to resolve it —
  offers no permission save at all: an untouched draft still equals the stored grants, so the diff is
  empty, `isDirty` is `false`, and «مراجعة تعديل الصلاحيات» is never rendered, because every mount of
  the review dock gates on `facade.isDirty()` — the band decides *which* mount, never *whether* one
  exists (below). No
  editor interaction can add such a code either, only remove one. The single save not gated on
  `isDirty` is `acceptSelectedUser`, and it runs only for a non-Owner `pending` user
  (`access-admin.facade.ts:246`) — a status that has no grants to seed a draft with, because accept
  creates a user's first grants itself (`EfAccessUserMutationService.cs:117-127`) and the replace
  path refuses anyone not `Active` (`EfAccessUserMutationService.cs:241`). The server's `400` for a
  code it will not assign therefore backs the write path rather than describing an exposure this
  page offers: a visible refusal, never a silent revocation.
- **A granted code this build does not model is preserved, not dropped.** `PermissionCode` is
  generated from the backend catalogue (see below), so a server that has moved ahead of the deployed
  bundle can return a grant the editor cannot render. `codesForSubmission()` appends those codes
  verbatim after the modelled ones, so a save leaves them exactly as they were; they appear on
  neither side of the diff because nothing on this page can change them. An older dashboard cannot
  revoke a permission it does not understand.
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

- While the draft differs, the section heading carries a `+N / −M` summary and a **64px sticky
  review dock** offers the save entry point and «تجاهل التغييرات». Lifecycle actions stay outside
  the dock. **It is pinned in every band, and the two bands reach that through different owners.**
  At Wide it is the details shell's footer, outside the body scroller: the viewport-height panel
  above holds it in place while the editor scrolls (measured `64px` tall, bottom `861px` of a `900px`
  viewport, unmoved after the body scrolls `974px`). Below Wide the shell has no definite height, so
  a footer-mounted dock would have no sticky range at all — its containing block is exactly its own
  box. There the page renders the dock itself instead, as the last child of the workspace panel with
  `position: sticky; inset-block-end: 0` and the `--pinned` surface, which is what the
  `env(safe-area-inset-bottom)` padding has always been for. Measured at 390/767/1024 × 800 it sits
  flush against the viewport bottom at every scroll offset. `hasFooter` on the details shell is
  therefore `isDirty() && isWide()`, the dock body is one `#reviewDock` template used by both
  mounts, and `access-permission-draft-bar` names exactly one element in either band — the page spec
  asserts the count and the owning ancestor on both sides of the boundary, so a band losing its dock
  cannot pass again.
- **On a short viewport the dock outranks the context bar.** Two pinned bars do not fit under
  `32rem` of height — a landscape phone, or a portrait one whose on-screen keyboard shrank the
  layout viewport. Measured at `390×400` the 201px context bar plus the 115px dock took 79% of the
  height and left a 29px slot with **no** permission row visible. So `@media (max-height: 32rem)`
  returns the context bar to the flow while the dock stays pinned; the same measurement then shows
  10 rows at `390×400` and 12 at `740×360`, with the dock still flush to the bottom. Identity is not
  lost — it is in the details header a scroll away — but the save and discard actions are the ones
  that must never be. Tall viewports are untouched (`390×800`: bar pinned, 40% chrome). Note this is
  a viewport-*height* condition; it is orthogonal to the four named inline bands and introduces no
  second breakpoint truth. One artefact is inherent to any bottom-pinned bar and is not a defect:
  at scroll offset `0` the dock paints over whatever sits at the viewport bottom, which on a
  `≤400px`-tall screen is the context bar (measured `−3px` at `740×360`, `−98px` at `390×400`). It
  clears on the first scroll, it never covers the dock's own controls, and no control is
  permanently unreachable. While the draft matches the stored
  grants the dock does not exist at all, so the idle state costs no vertical band (D41). `discardDraft()` restores the draft from
  the stored grants and issues no request. The summary's glyphs are `aria-hidden` beside a
  `.qd-sr-only` Arabic sibling that carries the reading: `aria-label` on a bare `<span>` names an
  element with the implicit `generic` role, which ARIA 1.2 prohibits, so it may never be announced.
- **The save entry point exists only while the draft is dirty**, which is how no-op saves are
  blocked; the confirmation's own confirm button is additionally disabled if the draft is reverted
  while it is open. The backend already short-circuits an empty change set, so this prevents a
  wasted round-trip, not audit pollution.
- The reason is optional: it is trimmed and bounded at 1024 characters by the backend; left blank,
  it is sent and stored as `null`. The review step itself is still the only save path.
- A dirty draft still holds back relink, even though relink now lives outside the permission
  workspace. The reason was never adjacency: `confirmSelectedUserRelink` runs through `runMutation`,
  and every successful mutation calls `refreshAfterMutation`, which re-selects the user and makes
  `AccessPermissionDraftStore.adopt` overwrite the draft with the stored grants. Moving the panel
  removed the confusion of a live identity form sitting mid-edit; it did not remove the overwrite.
  The gate reads `AccessAdminFacade.isDirty` through the panel's `hasUnsavedPermissions` input —
  the same signal the draft bar and the diff summary render from, so the three cannot disagree about
  whether a draft exists. It holds back **both**
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
  public `hasUnsavedChanges()`, and when that is true it returns the component's
  `confirmRouteLeave(): Promise<boolean>`. `window.confirm` is gone: the promise is resolved by the
  page's own `qd-confirm-dialog` (`access-leave-page-confirm`), so the route decision looks like
  every other decision in the app. Four rules make that at least as strong as the native prompt, and
  `access-admin-unsaved-changes.guard.spec.ts` proves each through **real Router navigation**,
  including a `Location.back()` move: a clean draft resolves `true` with no dialog; a dirty draft
  holds the navigation open until the operator answers; **repeated guard calls share the one pending
  promise**, so a second navigation cannot open a second dialog or strand the first; cancelling
  resolves `false` and leaves the draft and the selection exactly as they were; and confirming
  resolves `true` **without** eagerly discarding, because the component is about to be destroyed and
  an eager discard would corrupt a navigation that a later guard still refuses. Destroying the
  component settles any open decision as `false`.
- **The Router proof runs against the real page, not only a stub.** The spec keeps a lightweight
  stub for the pending-promise/back-navigation mechanics, and adds a second suite that routes
  `AccessAdminPageComponent` itself behind the guard over `HttpTestingController`: it selects an
  account, stages a real dirty draft, navigates, and answers the page's own dialog. Cancelling keeps
  both `router.url` **and** `Location.path()` on `/settings/access` — the address-bar assertion is
  the part that proves the async `canDeactivate` is at least as strong as `window.confirm` on a
  popstate move, where the browser URL moves first and Angular's `canceledNavigationResolution`
  restores it — and the draft and the selection survive. Confirming leaves and issues no request.

## Per-status semantics

`accessAccountVariant()` (`models/access-admin.models.ts`) is the **exhaustive discriminator** the
detail panel switches on: `pending-non-owner`, `active-non-owner`, `disabled-non-owner`,
`active-owner`, `pending-owner`, `disabled-owner`, `unknown-status`. Exactly one body renders per
account, and `access-admin.models.spec.ts` pins every branch, including that an unrecognised status
never falls through to the disabled body. Each account state explains what the page can and cannot
do to it, because the backend accepts a different commit for each one.

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
  so weight comes from placement and from the copy, not from a red button.
  **`--qd-danger` is reserved for the confirmation**, and that is a rule, not a preference: the
  at-rest line under «إجراءات الحساب» is `.qd-text-muted`, because it describes a destructive action
  nobody has chosen yet and every healthy Active account would otherwise carry permanent red text.
  The red sentence appears once the operator asks to disable, in `access-change-review`, where it is
  about a decision actually in front of them.
- **Disabled.** No editor renders, because the backend rejects a replace on a non-Active user. The
  region says the account holds no direct permissions and that none can be assigned before
  reactivation, and repeats that reactivation starts from none.
- **Unknown.** A status this build does not model renders neither the editor nor a single lifecycle
  control: the page says it does not know the account's state and offers no mutation. The lifecycle
  badge reads «حالة غير معروفة» through its own `qd-badge--lifecycle-unknown` semantics — it is never
  mapped onto Disabled, in the row or in the workspace.
- **Owner.** No editor and no checked-and-disabled catalogue. An Owner's access does not come from
  direct grants at all: `PermissionAuthorizationHandler` succeeds on `state.IsOwner`, and
  `AuthorizationStateAccessEvaluator.ResolveActiveStateAsync` returns a state only for an Active
  user — so the bypass statement is made **only** for an Active Owner, and a Pending or Disabled
  Owner is told the bypass does not apply yet. Both variants add that Owner membership is managed by
  owner reconciliation rather than from this page. Nothing here claims an Owner account is
  uneditable in general, because it is not: identity recovery below applies to Owners too.

## Advanced Security

`components/access-advanced-security/` hosts Logto-subject relink as identity recovery, in its own
first-class الأمان المتقدم tab rather than inside the selected-user panel. A tab, not a hidden menu:
a real identity incident is exactly when the recovery path has to be findable.
The move is a placement decision, not a capability change — `ConfirmCoreAsync` has no Owner guard and
no status guard, and `ValidateBindingAsync` routes an Owner target to
`ValidateOwnerConfigurationAsync`, which permits the relink when the Owner's configured email
reconciles as `Unchanged`. The panel says so: it applies to the selected account whatever its role,
including an Owner. Presenting it as routine permission editing was what was wrong, not the
capability.

An Owner target additionally gets that precondition in the copy, not only in this file. Because
`ValidateOwnerConfigurationAsync` fails with `OwnerConfigurationNotReconciled` unless the target's
normalized email is in the configured owner set **and** owner reconciliation reports a candidate for
that user in the `Unchanged` state, an Owner relink can otherwise fail at confirm for a reason the
panel never named. `access-relink-owner-precondition` states both halves and renders only when
`target.isOwner`.

**The precondition and the reconciliation panel name that state through one function, and that is
the point.** The panel beside it **in the same tab** no longer prints the raw
`OwnerReconciliationCandidateState` token — it prints the Arabic label — so copy naming the English
`Unchanged` would name something the operator can no longer see. Both read
`ACCESS_ADMIN_LABELS.reconciliationCandidateState`, the precondition through
`OWNER_RELINK_REQUIRED_CANDIDATE_STATE` (`models/access-admin.models.ts`), so the term in the
sentence and the term in the list are the same string by construction and renaming one renames the
other. `access-advanced-security.component.spec.ts` asserts the precondition carries that label and
**not** the raw token.

The component owns only the relink form state. It takes the selected user, the preview, the busy
action and the dirty-draft gate as inputs and emits preview/confirm/cancel; the facade still owns
every request, the evidence token and the preview lifecycle. With no user selected it renders an
empty state instead of a form, since relink targets one account.

## Audit and reconciliation

**No technical identifier is rendered in either section.** That is the feature-wide rule of §3 of the
plan finishing where it started: the audit log was the last place a database user id was printed.

- **Rows name people, not ids.** `GET /api/access/audit-events` carries `targetDisplayName`,
  `targetEmail`, `actorDisplayName` and `actorEmail` beside the existing `targetUserId`/`actorUserId`.
  The ids stay in the payload because the filter round-trip needs them; nothing renders them. A row
  reads the account through `accessUserNameLabel`, so a blank Logto name falls back to the email, and
  «حساب غير متاح» covers the shape where neither is known. A `System` actor reads «النظام» from
  `actorType` alone — the two system-actor paths (owner reconciliation, legacy-role conversion) write
  no actor row to point at.
  **The names are the account's current ones, not the ones frozen in the event's snapshot.** The
  backend sources them from the `ActorUser`/`TargetUser` foreign-key navigations, so a renamed account
  reads correctly everywhere in its history; `AccessAdministrationEndpointTests` pins the difference
  by renaming a user after the event and asserting the row and its `targetSnapshot` disagree. The
  snapshots are not a usable source: they exist in three shapes across two casings, one of them
  without an `email` field, and the generated TypeScript types all of them as `{}`.
- **The event type is a closed dropdown, not free text.** `ACCESS_AUDIT_ACTION_TYPES`
  (`models/access-admin.models.ts`) mirrors the backend `AccessAuditActionType` enum, whose names are
  the wire values `ListAccessAuditEventsHandler` accepts — anything else is a `400`, so the list is a
  contract and lives beside the other contract types rather than with the Arabic copy.
  `ACCESS_ADMIN_LABELS.auditActionType` maps each to Arabic and **returns an unmodelled value
  unchanged**: a new server-side action type stays legible instead of reading as «غير معروف».
  That list is hand-written but no longer unguarded: `npm run check:audit-action-types` compares it
  against `Backend/domain/QuranDashboard.Domain/Access/AccessAuditActionType.cs` in **both**
  directions and fails by name, so a member added or renamed on either side is a two-sided change
  rather than a filter that silently stops being offered. It runs inside `npm run test:pre-pr`. The
  generated OpenAPI cannot carry this — `actionType` and the filter parameter are both plain
  `string`, so nothing generated pins the membership set.
- **Timestamps are local.** `yyyy/MM/dd HH:mm` through `DatePipe`, inside a `<time [attr.datetime]>`
  that keeps the exact UTC instant machine-readable. Fixed field order and Western digits, matching
  the digits used everywhere else in the app, so the column stays scannable and free of ICU variance.
- **Load more is an append, never a page.** The audit slice keeps `loading` (initial and filter) and
  `appending` (cursor append) apart, so an append never unmounts the events already on screen: the
  list stays mounted with `aria-busy`, the button carries its own busy state, an append failure lands
  in its own scoped `qd-error-state` beside the untouched events rather than replacing them, and the
  appended count is announced through a permanently mounted polite region. Items are appended in
  server order. No numeric pagination is offered here, and `Load more` inherits none of F13.
  `clear()` bumps `requestVersion` as well (Phase 11): `clearProtectedState()` runs the moment the
  Owner check fails, so an audit read that is already in flight must not be able to repopulate
  `pageState` after the revocation — the version bump makes both `load()` and `loadNextPage()`
  no-ops on resolve, and `clear()` also resets `loading`/`error` to idle so a cleared store never
  presents a stale spinner or a stale failure.
  An append snapshots `requestVersion` without bumping it, so an initial/filter load that starts
  while the append is in flight invalidates it. That invalidation is also what has to release
  `appending`: `clearAppendState()` — called by both `load()` and `clear()` — resets `appending`,
  `appendError` and `appendedCount` together, because the append's own `finally` is version-guarded
  and deliberately does nothing once it has been superseded. Without that reset `appending` would
  stay `true` forever and `Load more` would stay disabled for the rest of the session.
- **Accounts are chosen by identity.** `components/access-user-picker/` replaces the two numeric-id
  inputs. It searches through the same `AccessAdminApi.listUsers` the list uses (`pageSize: 10`),
  renders each candidate by name with the email beneath, and emits the summary object. Since the
  Golden pass the candidates open in the shared F15 `qdFloatingLayer` (`select-listbox`) anchored to
  the search field, so the picker gets the one keyboard script — Arrow/Home/End over the options,
  Escape to close with focus returned, Tab to close, outside press to close — while the query, the
  facade call and the two result signals stay in Access. Enter on the layer activates the option the
  shared cursor points at; Enter in the field still searches and is `preventDefault`ed. The audit
  section keeps the chosen summary and reads `.id` off it only when it builds the query — the integer
  is constructed in TypeScript, sent as a query parameter, and never bound into a template or a route.
  Enter inside the picker searches and is `preventDefault`ed, because the picker sits inside the
  filter `<form>` and Enter would otherwise submit the filters instead. Each candidate's
  `data-testid` ends in that candidate's id — `<prefix>-candidate-<id>`, the same shape
  `access-user-list` uses for its rows — so a spec names the candidate it means instead of resolving
  the first match; the audit-log and page specs both pick a **non-first** candidate out of a
  multi-result list, which is what makes "the wrong candidate was emitted" a failing test. A
  `data-testid` is not visible UI, so carrying an id there does not breach the no-IDs rule above.
- **The picker is presentational like every other component here.** It holds the typed term and
  nothing else; the page owns the two result signals and calls `AccessAdminFacade.findUsers`, which
  gates on Owner access and delegates to `AccessAuditStore.findUsers` — between them the only place
  the lookup request lives. Two signals, not one, because the two pickers must not show each
  other's candidates. `findUsers` returns the outcome instead of storing it, so a lookup for a filter
  cannot disturb the listed page; a failed lookup returns its message and the picker renders an
  error state, which is why a failed search does not read as «لا توجد حسابات مطابقة».
- **Owner reconciliation is diagnostic, and the page says so.** `canApply` is **status, never an
  offer**: there is no apply endpoint in this feature, so the panel presents it as «مؤشّر التنفيذ»
  under a line stating that reconciliation runs outside the dashboard and is not applied from this
  page. The page spec asserts the section's only `<button>` is the fingerprint disclosure, so adding
  an Apply control fails a test.
- **Candidate states read in Arabic** through `ACCESS_ADMIN_LABELS.reconciliationCandidateState`,
  which is also what the Owner relink precondition names — see *Advanced Security*.
- **The 64-character configuration fingerprint is behind a disclosure**, collapsed by default, with
  `aria-expanded` on the toggle. §3 permits a technical value only as an explicitly advanced
  diagnostic affordance, never in a default view; this is the one such affordance in the feature.

## State regions and announcement

**There is no `qd-state` left in this feature, and the adapter itself was deleted in Phase 11.** Every async surface consumes one of the five F12
owners directly: `qd-skeleton-rows` for a list whose loaded shape is known (the user list, the audit
log), `qd-panel-skeleton shape="text"` for a single-value region (the access check, the detail load,
the catalogue, the reconciliation status), `qd-empty-state`, `qd-error-state severity="read"` for a
scoped read failure, `qd-error-state severity="write"` for a write failure, and `qd-notice` for a
success or `409` recovery.

**The announcer is permanent; the visible band is not.** In the workspace the announcer is the
details shell's own always-mounted `role="status"`/`aria-live="polite"` slot; in الأمان المتقدم it
is a `.qd-sr-only` region carrying `politeMutationText()`. Both exist before any write runs, because
a live region created together with its text is generally not read out — the later text insertion is
what the screen reader announces. What sits *inside* those regions is rendered only while there is a
message, so the idle mutation band is **zero height**: the ~6.5rem permanently blank slot the
previous shape paid for is gone (D41). The page spec asserts the empty announcer, its role and its
zero height before any write.

Severity routing is unchanged in meaning and now uses the locked F12 roles: a completed change and a
`409` recovery render `qd-notice` (`status`, polite, quiet tone), and only a genuine write failure
renders `qd-error-state severity="write"` — the one `role="alert"`, which never clears the draft. In
الأمان المتقدم the polite announcer deliberately carries the non-error text only, so an alert is not
also announced politely. The workspace cannot do the same, because its announcer *is* the details
shell's status slot and the visible surface lives inside it: the write-error branch is therefore
wrapped in an `aria-live="off"` element, which shadows the polite ancestor for that subtree while the
`role="alert"` element remains its own assertive live region. The alert announces once; the F12 role
lock is untouched, and no shared shell markup changes.
The tone-suffixed `access-mutation-message-*` testid still names exactly one element.

**Both mutating sections keep their own region** — the workspace commits accept/disable/reactivate/
replace, and الأمان المتقدم commits relink — because only one is ever mounted and a single hoisted
instance would put the band on the read-only audit section too. `runMutation` clears the message
before every write, so the region is empty again before the next text lands.

The message is also cleared **on a section change**, so «تم حفظ التغيير» earned by a permission save
in the workspace does not reappear at the top of الأمان المتقدم as though that panel had just
written something. The clear hangs off the `?tab=` subscription (`AccessAdminPageComponent.showTab`)
rather than off the tablist click, so a browser history move between sections clears it too.

Because that region outlives the detail pane's own load/error branches, the message channel carries
mutation outcomes and nothing else. The permission-denied text is rendered from `canAccess()`
directly, one branch higher; `load()` does not also copy it into the message, which would survive an
access state that has since changed.

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
- `state/access-audit.store.ts` owns the audit slice — the loaded page, the filter query, its
  loading and error state, the **separate** append lifecycle (`appending`, `appendError`,
  `appendedCount`), and the participant lookup the audit pickers run — as a
  second plain signal-backed class the facade composes, on the same seam and for the same reason:
  `FRONTEND_STRUCTURE.md` §5 caps a facade at 600 lines, and this slice is cohesive enough to lift
  whole. It is constructed with `new AccessAuditStore(this.api)`, **not** `providedIn: 'root'` —
  the facade is component-provided, so a root-provided store would share one audit page across every
  page instance. Unlike the draft store it does issue its own requests, because the four methods
  that make it a slice are all request-shaped. That is why the error text did not fork: the
  load-failure fallback and the `HttpErrorResponse` → operator-message mapping moved out of the
  facade into `state/access-admin-request-failure.ts`, which the facade and this store both import,
  so a failed audit page and a failed user page still read the same. **The Owner gate is not in the
  store.** `canAccess()` is the facade's, and the facade checks it before every request the store
  issues, exactly as it guards `AccessPermissionDraftStore.setCodes`. The facade re-exports the
  signals under their `audit*` names, so this store is an internal seam too.
- `models/access-admin.labels.ts` holds the Arabic copy that TypeScript needs (both confirmation
  dialogs' wording, the selected-context and list-sheet copy, the diff-summary and appended-count
  builders, and the tab, lifecycle-status, audit-action-type and reconciliation-candidate-state
  label builders). Template-only copy stays in
  the templates. The two builders added for the audit and reconciliation sections **return an
  unrecognised value unchanged** rather than mapping it to an «غير معروف» constant: both name
  server-side enums this page mirrors rather than owns, and a value it does not model is diagnostic
  information the operator may need to quote. `userStatus` names the three modelled states
  and reads anything else as «حالة غير معروفة» rather than as «معطّل»: the generated
  `AccessUserDetail.status` and `AccessUserSummary.status` are both plain `string`, so a state this
  page does not model is reachable, and the row and the workspace header would otherwise both tell
  the operator an account is disabled when the page in fact does not know what it is.
  `models/access-admin-tabs.ts` holds the tab **keys** and their parser, separately from the copy —
  the keys are a URL contract and must stay stable while the Arabic labels are free to change.
  **The page component reads the labels through a getter, not a class field**
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
- `components/` renders feature UI and emits interactions. None of them calls HTTP, and none of them
  injects the facade — the page passes signals down and turns every output back into a facade call:
  - `access-user-list` — filter form (`qd-form-field`/`qdControl`), `qdResultList` rows,
    `qd-result-count`, `qd-pagination`, and the F12 owners directly;
  - `access-user-summary-card` — the LTR email and the two badges, projected into the details
    shell's identity zone (and reused verbatim in the below-Wide context bar);
  - `access-permission-editor` — the grouped checkbox grid;
  - `access-account-permissions` — the whole direct-permissions body of the selected account: it
    reads `accessAccountVariant` from the `user` input and renders exactly one of the four surfaces
    (owner bypass note, unknown-status note, disabled-account note, or the editable permissions
    section with its diff summary, catalogue skeleton/error-retry, unavailable and pending notes).
    The page keeps the facade: this component receives the catalogue slice as inputs and emits
    `selectionChange` / `catalogueRetryRequested`. Whether the lifecycle pair renders beside it is
    still the page's decision, through `accessLifecycleActionsApply` in
    `models/access-admin.models.ts` — the same closed variant set, so the body and the account
    actions cannot drift apart;
  - `access-lifecycle-actions` — «إجراءات الحساب» and its per-status commit;
  - `access-change-review` — the diff, the optional reason, and confirm/cancel; it owns the reason
    text and drops it whenever the pending action changes. **Which action is pending is the page's
    state, and the page closes the review explicitly** — on a user switch (both the direct one and
    the one behind the discard confirmation) and on a settled write (`success`/`409`/`401`/`403`,
    the same outcomes that bump the relink form's reset token). A failed write leaves the review
    open with the reason intact, so a retry does not start from a blank textarea;
  - `access-audit-log` — the audit filters and rows;
  - `access-user-picker` — the reusable find-an-account control the audit filters use twice;
  - `access-advanced-security` — the relink form;
  - `access-owner-reconciliation` — the read-only owner-reconciliation panel of the الأمان المتقدم
    section: the four read states (loading, error, empty, status), the candidate list with its
    Arabic state labels, and the technical-fingerprint disclosure whose open/closed state is the
    component's own — nothing about it is page state, and it offers no control that could apply a
    reconciliation.
  These last two extractions are what keeps `access-admin-page.component.html` a composition file:
  it owns the tabs, the Wide split and below-Wide context bar, the two confirm dialogs, the review
  dock and the mutation surface, and delegates every account-detail and reconciliation block to a
  component — `FRONTEND_STRUCTURE.md` §1.2 caps a template at 300 lines before its hard limit, and
  the inlined blocks had taken it past 400.
  The dirty predicate is **not** re-derived per component any more: the draft bar, the `+N / −M`
  summary and `access-advanced-security`'s `hasUnsavedPermissions` input all read
  `AccessAdminFacade.isDirty`, the single computed on the draft store, so they cannot disagree with
  the diff rendered beside them. The predicates that *are* shared —
  `canSelectUserPermissions`, `canReplaceUserPermissions`, `acceptGrantsPermissions` — live as pure
  functions in `models/access-admin.models.ts` rather than being copied into each consumer.
  **That includes the facade**, the consumer where it matters most: `canSelectPermissions` and
  `canReplaceSelectedPermissions` compose those same functions instead of re-encoding "not Owner and
  status ∈ {pending, active}" beside the write they gate. A change to which statuses may hold
  permissions therefore cannot move the rendered editor without moving the `PUT` gate with it. The
  facade adds only what is its own to add: the Owner access snapshot and catalogue readiness.
- Owner membership, role editing, and group grants are out of scope.

## Testing

`npm run test:feature:access-admin` is this feature's primary test lane. The repository-wide gate
checker requires this lane to be configured whenever specs in this folder change.
