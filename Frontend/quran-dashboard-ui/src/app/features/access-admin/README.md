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
  There is only one modal in the feature: the unsaved-changes confirmation described below.
- **There is no audit-history section and no advanced-security section.** The سجل الوصول and
  الأمان المتقدم tabs and every frontend surface behind them were removed — see *Removed frontend
  surfaces* below. `GET /api/access/audit-events`, the two `logto-sub/relink` endpoints,
  `GET /api/access/owner-reconciliation/status`, the backend audit trail and owner reconciliation
  itself are all untouched; nothing on this page reads them.

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

**The page is Workspace-only, and there is no tab strip.** With مساحة العمل the sole surviving
section, the one-tab `qd-tabs` strip, the `@switch` that wrapped its single `@default` case, the
`activeTab` signal, `selectTab`/`showTab`, the `?tab=` query-parameter subscription and
`models/access-admin-tabs.ts` (`ACCESS_ADMIN_TAB_KEYS`, `AccessAdminTab`, `DEFAULT_ACCESS_ADMIN_TAB`,
`parseAccessAdminTab`) are all gone. The workspace — the sticky 20rem user-list aside plus the
selected-user panel at Wide, the context bar plus sheet below it — is rendered directly under the
access check, and it carries neither `role="tabpanel"` nor an `aria-labelledby`, because a tabpanel
with no tab to label it is invalid ARIA rather than harmless leftover markup.

**Stale `?tab=` links degrade by construction, not by a parser.** `/settings/access?tab=audit`,
`?tab=security` and any other value render exactly the workspace: the route reads no query
parameter, nothing branches on one, and no code path exists that a value could send anywhere else.
That is why the parser was removed rather than retained. `parseAccessAdminTab`'s
`?? DEFAULT_ACCESS_ADMIN_TAB` made an unknown value *fall back* to the workspace; deleting the whole
branch means there is nothing to fall back *from*. Keeping a one-member enum, its parser and a
subscription that could only ever re-select the section already showing would have left dead code
whose only effect was to re-derive a constant. Browser back/forward across those URLs is a
same-component query-parameter change with no subscriber, so it re-renders nothing and cannot strand
the page. **If a second section is ever added, the strip and the parser come back together** — the
URL contract is a keys-and-parser pair, not a label concern, which is what the deleted
`models/access-admin-tabs.ts` existed to keep separate from the Arabic copy.

**Not a child route, and no user deep-link at all.** `FRONTEND_STRUCTURE.md` requires this to be
written down. A child route would orphan the `title` on the single route in
`access-admin.routes.ts`, and there is now exactly one view over this Owner-only
screen anyway. The selected **user** is deliberately *not* in the
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
optimistic concurrency stays in state and out of view, along with every other technical identifier.

**The email is never truncated.** It is the target of a safety decision, so it wraps in full inside
`.qd-ltr-isolate` rather than eliding behind a `[title]` the operator has to hover to read; only the
account *name* truncates, and only where a focusable row owns it.

**The surviving Access surface stopped truncating for the same reason (D35).** The Compact/Medium
context-bar identity had `[title]` as its *only* disclosure, which reaches no rung of the Golden
§8.1 disclosure ladder: the name sits in a static bar with no owning control, and `title` is
unreachable by keyboard and by touch. It now wraps (`min-inline-size: 0; overflow-wrap: anywhere`)
and carries neither `.qd-truncate` nor `[title]`.
Adding `tabindex="0"` to the text node is explicitly prohibited and was not the fix. User-list rows
are deliberately untouched because they *do* have a rung: they truncate inside a focusable row button
whose accessible name is the full value. The other three surfaces D35 originally covered — the audit
row's `الحساب:` / `المنفّذ:` lines, the owner-reconciliation candidate emails, and the user picker's
chosen identity — are all deleted, the first two with the audit and advanced-security sections and
the picker with the consumerless components that outlived them.

A list row is the shared `qdResultList`/`qdResultItem` pair (`listVariant="master"`): `role="list"` /
`role="listitem"`, `aria-posinset`/`aria-setsize`, `aria-current` on the selected row, and the
logical `border-inline-start` selection thread reserved as transparent on every row so selecting one
causes no shift. Names compose `.qd-truncate` with the mandatory `[title]`. The thread rests on the
shared `.qd-result-item` rules.

## Permission-assignment failure model

`GET /api/access/permissions` answers with `{ items, assignmentReady }`. A readable catalogue does
not mean a writable one, so the two failure modes are kept distinct and neither is allowed to change
stored access.

- **The catalogue request fails.** Only the الصلاحيات المباشرة region degrades: it renders an error
  with a retry that re-issues the catalogue request. Identity, status badges and lifecycle actions
  keep rendering, because none of them reads the catalogue.
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
and switching users and leaving the page prompt with no way to clear the draft, since the bar
carrying the only discard control is hidden while assignment is unavailable.

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
  mounts, and `access-permission-draft-bar` names exactly one element in either band, under the
  correct owning ancestor on both sides of the boundary.
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
  every other decision in the app. Four rules make that at least as strong as the native prompt:
  a clean draft resolves `true` with no dialog; a dirty draft
  holds the navigation open until the operator answers; **repeated guard calls share the one pending
  promise**, so a second navigation cannot open a second dialog or strand the first; cancelling
  resolves `false` and leaves the draft and the selection exactly as they were; and confirming
  resolves `true` **without** eagerly discarding, because the component is about to be destroyed and
  an eager discard would corrupt a navigation that a later guard still refuses. Destroying the
  component settles any open decision as `false`.

## Per-status semantics

`accessAccountVariant()` (`models/access-admin.models.ts`) is the **exhaustive discriminator** the
detail panel switches on: `pending-non-owner`, `active-non-owner`, `disabled-non-owner`,
`active-owner`, `pending-owner`, `disabled-owner`, `unknown-status`. Exactly one body renders per
account, including an `unknown-status` branch so an unrecognised status never falls through to the
disabled body. Each account state explains what the page can and cannot
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
  owner reconciliation rather than from this page.

## Removed frontend surfaces

**The audit-history surface is gone from the frontend.** `components/access-audit-log/`,
`state/access-audit.store.ts`, `AccessAdminApi.listAuditEvents`, the facade's `audit*` readonlys and
its `updateAuditQuery`/`loadNextAuditPage`/`loadAuditEvents` methods, and the `?tab=audit` panel were
all deleted, so the page no longer requests `GET /api/access/audit-events` — on load or after a
mutation.

**The advanced-security surface is gone from the frontend too.** `components/access-advanced-security/`
(Logto-subject relink) and `components/access-owner-reconciliation/` (the read-only owner-reconciliation
panel) were deleted with the `?tab=security` panel, together with `AccessAdminApi.previewRelink`,
`confirmRelink` and `getOwnerReconciliationStatus`; the facade's `reconciliationStatus`/
`reconciliationLoading`/`reconciliationError`/`relinkPreview` readonlys and its relink and
reconciliation state, request-version and lifecycle methods; the page's `workflowResetToken`,
`previewRelink`/`confirmRelink`/`cancelRelink` and `politeMutationText`; the
`reconciliationCandidateState` label builder with its Arabic map; and the
`AccessRelinkPreviewRequest`/`AccessRelinkConfirmRequest` models with
`OWNER_RELINK_REQUIRED_CANDIDATE_STATE`. So the page requests neither
`POST /api/access/users/{id}/logto-sub/relink/preview` nor `…/confirm` nor
`GET /api/access/owner-reconciliation/status` — there is no identity-recovery form and no
reconciliation panel left to request them. **And `load()` is no longer a four-legged fan-out:**
`Promise.all` now holds `loadUsers()` and `loadPermissionCatalogue()` only, which is why an Owner
landing on `/settings/access` issues exactly two requests.

Removing the eager reconciliation leg does **not** move the readiness gate. `accessStateKnown()` is
computed from `CurrentUserStore.loadState()`/`authStateKnown()` alone and never read a load leg, so
which responses `load()` waits for cannot change when the access check resolves, and the check is
still what decides between the skeleton, the denied state and the workspace.

**Nothing backend was touched by either removal.** The four endpoints, the audit trail, owner
reconciliation, the writes that populate them and every authorization path around them are
unchanged. `GET /api/access/audit-events`, the two `logto-sub/relink` endpoints and
`GET /api/access/owner-reconciliation/status` are recorded as *possible unused API surface —
separate review required*, which is a decision with its own review and not this feature's to make.
Owner reconciliation in particular is a backend safety mechanism whose value does not depend on a UI
existing to read it.

**`ACCESS_AUDIT_ACTION_TYPES` stays, and is deliberately unreferenced by any component.**
It lives in `models/access-admin.models.ts` with `AccessAuditActionType` and `isAccessAuditActionType`
derived from it, and it is **not** audit-UI code: it is the frontend mirror of the backend
`AccessAuditActionType` enum, and `npm run check:audit-action-types` reads that exact file and fails
if the declaration goes missing or drifts from
`Backend/domain/QuranDashboard.Domain/Access/AccessAuditActionType.cs` in either direction. The
generated OpenAPI cannot carry this — `actionType` is a plain `string`, so nothing generated pins the
membership set. Do not delete it as dead code; having no component consumer is its expected state.

**The generated API models stay too.** `LogtoSubjectRelinkPreview`, `OwnerReconciliationStatus`,
`PreviewLogtoSubjectRelinkBody` and `ConfirmLogtoSubjectRelinkBody` remain under
`core/api/generated/`: only the *imports* of them were removed, because generated files mirror the
backend contract and are not hand-edited.

## State regions and announcement

**There is no `qd-state` left in this feature, and the adapter itself was deleted in Phase 11.** Every async surface consumes one of the five F12
owners directly: `qd-skeleton-rows` for a list whose loaded shape is known (the user list),
`qd-panel-skeleton shape="text"` for a single-value region (the access check, the detail load,
the catalogue), `qd-empty-state`, `qd-error-state severity="read"` for a
scoped read failure, `qd-error-state severity="write"` for a write failure, and `qd-notice` for a
success or `409` recovery.

**The announcer is permanent; the visible band is not.** With the workspace the only section left,
there is one announcer: the details shell's own always-mounted `role="status"`/`aria-live="polite"`
slot. It exists before any write runs, because
a live region created together with its text is generally not read out — the later text insertion is
what the screen reader announces. What sits *inside* that region is rendered only while there is a
message, so the idle mutation band is **zero height**: the ~6.5rem permanently blank slot the
previous shape paid for is gone (D41). Before any write, the announcer remains empty, keeps its
role, and occupies zero height. The `.qd-sr-only` `politeMutationText()` region went with the
الأمان المتقدم panel it belonged to.

Severity routing is unchanged in meaning and now uses the locked F12 roles: a completed change and a
`409` recovery render `qd-notice` (`status`, polite, quiet tone), and only a genuine write failure
renders `qd-error-state severity="write"` — the one `role="alert"`, which never clears the draft. The
workspace's announcer *is* the details shell's status slot and the visible surface lives inside it,
so the write-error branch is
wrapped in an `aria-live="off"` element, which shadows the polite ancestor for that subtree while the
`role="alert"` element remains its own assertive live region. The alert announces once; the F12 role
lock is untouched, and no shared shell markup changes.
The tone-suffixed `access-mutation-message-*` testid still names exactly one element.

The workspace is the only mutating section — it commits accept/disable/reactivate/replace — and it
announces into the surface that belongs to it. `runMutation` clears the message
before every write, so the region is empty again before the next text lands. **That is now the only
clear.** The section-change clear (`AccessAdminPageComponent.showTab` → `clearMutationMessage`) went
with the tab strip, and `AccessAdminFacade.clearMutationMessage` went with its last caller: with one
section it could never fire, and a facade method whose only reachable effect was to duplicate what
`runMutation` already does before every write is not a contract worth keeping alive for a section
change that cannot happen.

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
- `state/access-admin-request-failure.ts` holds the load-failure fallback and the
  `HttpErrorResponse` → operator-message mapping, so a failed read reads the same wherever it is
  raised. It outlived the audit store that shared it with the facade.
- `models/access-admin.labels.ts` holds the Arabic copy that TypeScript needs (both confirmation
  dialogs' wording, the selected-context and list-sheet copy, the diff-summary builder, and the
  lifecycle-status label builder). The `tab` label builder and `tabsAriaLabel` went with the strip,
  as did `systemActor` and `unnamedParticipant`, which were audit-row copy. Template-only copy stays in
  the templates. `userStatus` names the three modelled states
  and reads anything else as «حالة غير معروفة» rather than as «معطّل»: the generated
  `AccessUserDetail.status` and `AccessUserSummary.status` are both plain `string`, so a state this
  page does not model is reachable, and the row and the workspace header would otherwise both tell
  the operator an account is disabled when the page in fact does not know what it is.
  There is no `models/access-admin-tabs.ts` any more; if a second section ever returns, its keys and
  parser belong in a file of their own again, separate from the copy, because keys are a URL contract
  and must stay stable while the Arabic labels are free to change.
  **The page component reads the labels through a getter, not a class field**
  (`access-admin-page.component.ts` `get labels()`), matching how `abwab-page.component.ts` exposes
  `ABWAB_LABELS`. Keep the getter so the lazily initialised labels object is read at call time.
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
    the one behind the discard confirmation) and on a settled write (`success`/`409`/`401`/`403`).
    A failed write leaves the review
    open with the reason intact, so a retry does not start from a blank textarea;
  `access-user-picker` is **gone**. Its only two mounts were the audit filters, and the facade's
  `findUsers` delegation went with the audit store, so it had no consumer at all; the three model
  symbols that existed only for it — `AccessUserSearchState`, `EMPTY_ACCESS_USER_SEARCH` and
  `ACCESS_USER_PICKER_PAGE_SIZE` — were deleted with it. It survived the audit removal only because
  it sat outside that phase's enumerated surface, not because anything still read it.
  `access-account-permissions` is what keeps `access-admin-page.component.html` a composition file:
  it owns the Wide split and below-Wide context bar, the two confirm dialogs, the review
  dock and the mutation surface, and delegates every account-detail block to a
  component — `FRONTEND_STRUCTURE.md` §1.2 caps a template at 300 lines before its hard limit, and
  the inlined blocks had taken it past 400.
  The dirty predicate is **not** re-derived per component any more: the draft bar and the `+N / −M`
  summary both read
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

The repository Test Freeze applies. Use the three-command frontend verification chain in
`../../../../README.md`; any new Playwright journey requires owner approval under
`../../../../../../TESTING_CONSTITUTION.md`.
