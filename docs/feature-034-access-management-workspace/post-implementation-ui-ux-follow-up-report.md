# Post-implementation UI/UX follow-up report — Access Management workspace + Abwab tree search

- **Branch:** `feature/034-access-catalogue-readiness` (all 8 phases of the access-management
  implementation plan complete; working tree clean at `0c9dd215`)
- **Date:** 2026-08-08
- **Kind:** read-only inspection report. No code was modified, no migrations created, nothing
  committed. This report is the basis for a small follow-up implementation plan.
- **Scope:** five confirmed product/UI changes — (1) optional reason for permission changes,
  (2) optional reason for account lifecycle actions, (3) permission-grid compaction,
  (4) navbar entry to Access Management, (5) Abwab tree search expansion fix.

Every file reference below was verified against the current repository state. Line numbers are
current as of `0c9dd215`.

---

## Item 1 — Permission changes must not require a mandatory reason

### 1. Current behavior

A trimmed 1..1024-character reason is mandatory for `PUT /api/access/users/{id}/permissions`,
enforced **independently at five layers**:

| Layer | Where | Rule |
|---|---|---|
| Frontend confirm button | `access-change-review.component.html:75` | `[disabled]="!reason().trim() \|\| …"` |
| Frontend confirm handler | `access-change-review.component.ts:69-75` | `if (… \|\| !reason …) return;` |
| Frontend facade | `access-admin.facade.ts:286-287` | trims; empty ⇒ returns `'invalid'`, no HTTP call |
| Backend model binding | `AccessAdministrationBodies.cs:12-15` (`string Reason`, non-nullable) + implicit MVC requiredness (`QuranDashboard.Api.csproj:5` `<Nullable>enable`, no suppression) | omitting the key ⇒ 400 `ValidationFailed` before the handler runs |
| Backend handler | `ReplaceUserPermissionsHandler.cs:14-19` → `AccessAdministrationValidation.TryGetReason` (`AccessAdministrationValidation.cs:9-13`, `reason.Length is > 0 and <= 1024`) | `""`/whitespace ⇒ 400 `AccessAdministrationInvalidRequest` |

The UI additionally interposes a full review step: «مراجعة تعديل الصلاحيات»
(`access-admin-page.component.html:231`) only *opens* the `qd-access-change-review` panel
(`requestPermissionSave()` at `access-admin-page.component.ts:184-189` sets
`pendingAction = 'permissions'`); the actual save happens only from that panel's confirm.
There are **no Angular Reactive Forms validators anywhere in the feature** — the requirement is
hand-rolled signals + disabled bindings + guard clauses.

### 2. Exact root cause

The mandatory-ness is concentrated in:

- **one backend helper** — `AccessAdministrationValidation.cs:12`, the `> 0` half of
  `reason.Length is > 0 and <= MaximumReasonLength`;
- **non-nullable `string Reason`** on the body record (`AccessAdministrationBodies.cs:12-15`)
  and the command (`AccessUserContracts.cs:68-72`), which makes MVC treat the JSON key itself
  as required;
- **three frontend guards** listed above.

There is no FluentValidation and no DataAnnotations anywhere in the backend (verified by
exhaustive grep); the two backend mechanisms above are the complete set.

### 3. Backend impact

Small and well-contained:

- `AccessAdministrationBodies.cs:15` — `Reason` → `string?`.
- `AccessUserContracts.cs:72` — command `Reason` → `string?`.
- `AccessAdministrationValidation.cs` — the four workspace actions need an
  optional-reason path (trim; `null`/`""` accepted; `<= 1024` still enforced). **Do not simply
  relax `TryGetReason`:** `ConfirmLogtoSubjectRelinkHandler.cs:19` shares it, so relaxing the
  helper silently makes the high-risk relink confirmation reason optional too. Add a separate
  `TryGetOptionalReason` (or equivalent) and leave `TryGetReason` for relink.
- `AccessAuditContracts.cs:62` — `AccessAuditEntry.Reason` → `string?`, and
  `EfAccessUserMutationService.cs:361` (`Append`'s `string reason` param) → `string?`, so an
  omitted reason is stored as `NULL`, consistent with the read contract
  (`AccessAuditContracts.cs:32` is already `string? Reason`) and with system-generated rows.
- **No migration.** The `reason` column is already nullable:
  `AccessAuditEventConfiguration.cs:87-89` has no `.IsRequired()`, migration
  `20260805121524_AddAuthorizationAccessFoundation.cs:45` declares `nullable: true`, and the
  live-schema preflight expects nullable (`AuthorizationSchemaRequirements.cs:44`).

**Audit integrity is not weakened.** Everything the product requires is recorded independently
of the reason: actor id + snapshot, timestamp (`OccurredAtUtc`), per-code
`PermissionGranted`/`PermissionRevoked` rows from the delta computation
(`EfAccessUserMutationService.cs:252-313`), target id + snapshot, before/after state JSON,
correlation id and operation provenance (`AccessAuditAppender.cs:17-40`). No documented audit
invariant names the reason as a field of the audit contract
(`docs/contracts/security-access.md:33-38` lists the projected fields — reason is not among
them), and the no-op short-circuit already discards the reason today
(`EfAccessUserMutationService.cs:265-268`).

**Optimistic concurrency is untouched.** `ExpectedVersion` stays required in the body; the
`xmin` rowversion check (`AccessUserMutationTransaction.cs:40-45`, `:57-61` → 409) is on a
separate code path from the reason guard.

### 4. Frontend impact

Smallest coherent change (keep the review step — it carries the diff preview and the
destructive-action copy — but stop requiring a typed reason):

- `access-change-review.component.ts:71` — drop `!reason ||` from the guard (keep `.trim()`).
- `access-change-review.component.html:75` — drop `!reason().trim() ||` from `[disabled]`.
- `access-admin.facade.ts:286-287` — drop the `!normalizedReason` clause (and see Item 2 for
  the sibling clauses); send `reason: normalizedReason || null`.
- Optional copy change: mark the textarea «سبب الإجراء» as «اختياري» so the field's new
  status is visible; there is currently no validation message to remove (the requirement was
  only ever communicated by the disabled button).

After the OpenAPI regeneration the generated body type becomes `reason: string | null` — the
key stays present because `AllPropertiesRequiredSchemaFilter.cs:31-43` force-adds **every**
property to `required` (this is how `permissionCodes` already behaves). Sending `null` for an
empty reason keeps that schema filter untouched, which is the smallest contract change.

### 5. Contracts/models impacted

- `Frontend/quran-dashboard-ui/openapi/swagger.json:8871` — `ReplaceUserPermissionsBody`
  (`reason` flips to `nullable: true`; stays in `required` per the schema filter).
- Generated client: `src/app/core/api/generated/models/replace-user-permissions-body.ts:7`
  (`reason: string` → `string | null`).
- Regeneration is gated: `Backend/scripts/check-api-contract:17-24` fails with
  `STALE API CONTRACT` until `Backend/scripts/export-swagger` + `npm run generate:api` outputs
  are committed.

### 6. Files likely affected

Backend: `AccessAdministrationBodies.cs`, `AccessUserContracts.cs`,
`AccessAdministrationValidation.cs`, `ReplaceUserPermissionsHandler.cs`,
`AccessAuditContracts.cs`, `EfAccessUserMutationService.cs`,
`Backend/application/QuranDashboard.Application/Access/README.md`.
Frontend: `access-change-review.component.ts/.html`, `access-admin.facade.ts`,
`openapi/swagger.json` + 1 generated model, `features/access-admin/README.md`, specs listed in §8.

### 7. Smallest safe implementation recommendation

Make the reason optional end-to-end while keeping the review/diff step:

1. Backend: nullable DTO/command property; new optional-reason validation path (max 1024
   only); nullable `AccessAuditEntry.Reason`; store `NULL` when blank. Relink keeps its own
   strict guard.
2. Regenerate swagger + client; commit both (contract gate).
3. Frontend: remove the three empty-reason guards; send `null` for blank; label the field
   optional.

This is backward compatible in the right order: the relaxed backend still accepts
reason-bearing requests, so backend lands first, frontend second — no coordinated deploy needed.

If product wants the review step **gone entirely** for permission saves (true
"select-and-save"), that is a larger UX change: `requestPermissionSave()` would call
`runAction()` directly, and the diff preview (`access-change-review.component.html:27-53`)
and no-op guard presentation would need a new home (the draft bar already shows a diff
summary — `access-permission-diff-summary` test ids). See the clarification section.

### 8. Tests that need addition/update

- **Backend: no test asserts reason-is-required for this action** (verified exhaustively) —
  so nothing to invert. Add: one test per action proving an omitted/blank reason succeeds and
  audits with `NULL` reason; keep the 1025-char rejection bound (only
  `OwnerReconciliationServiceTests.cs:370-387` covers max-length today, on a different path).
  Existing reason-carrying tests (`AccessAdministrationEndpointTests.cs:14,75,124,325,…`)
  keep passing.
- **Frontend:** `access-change-review.component.spec.ts:84-96`
  (`requires a reason before it will confirm anything`) must be inverted;
  `:98-105` (trim) survives; `:127-140` (no-op guard) survives but its setup leans on
  `fillReason` enabling the button. `access-admin.facade.spec.ts:740-748`
  (`does not submit a permission replacement without a confirmation reason`) must be
  deleted/inverted with documented replacement coverage (frontend CLAUDE.md test-deletion
  rule). The page-spec `confirmAction()` helper (`access-admin-page.component.spec.ts:321-332`)
  is the single choke point for the remaining flow tests.

### 9. UX/accessibility considerations

- The reason textarea has no error message today; making it optional removes a silent
  dead-end (disabled button with no explanation) — a small a11y win.
- Keep the review step's diff (Arabic label + stable code per changed permission) — the
  current-state report called this "genuinely good", and it is the only place the Owner sees
  what will actually change before committing.
- If the field stays, placeholder/label copy should say «اختياري» explicitly; screen-reader
  users otherwise cannot tell the requirement changed.

### 10. Risks / interactions with the completed 8 phases

- **Supersedes a locked decision.** The plan says *"Mandatory reason is unchanged … (Locked
  decision — do not relax.)"* (`access-management-implementation-plan.md:395-396`, `:743`).
  This is an intentional product reversal, not a regression. Notably the current-state report
  had already flagged it as an open product question
  (`access-management-current-state-report.md:1168-1169`).
- **The confirm button loses its last enable predicate for lifecycle actions.** Today confirm
  enables on non-empty reason alone; for permission saves the no-op guard
  (`isNoOpPermissionSave`) remains, and the save entry point already exists only while the
  draft is dirty (`features/access-admin/README.md:203-206`), so no-op saves stay blocked.
- **Relink and the CLI are separate strict paths** — `OwnerReconciliationService.cs:359-364`
  and `AccessAdmin/Program.cs:306-310` have their own required-reason rules. Recommended:
  leave both strict (high-risk identity/system operations); this costs nothing in the change.
- Phase 8's catalogue/allowlist gates (`ResolvePermissionsAsync:318-352`,
  `check-permission-catalogue.mjs`, `check-audit-action-types.mjs`) never read the reason —
  unaffected, though the contract regeneration runs in the same gate family.

---

## Item 2 — Account lifecycle actions must not require a mandatory reason

### 1. Current behavior

Accept, disable, and reactivate (`AccessUsersController.cs:50-84`) share **exactly the same
machinery** as Item 1: non-nullable `string Reason` bodies (`AccessAdministrationBodies.cs:6,8,10`),
`TryGetReason` guards in `AcceptAccessUserHandler.cs:14-17`, `DisableAccessUserHandler.cs:14-17`,
`ReactivateAccessUserHandler.cs:14-17`, and the same frontend review step —
`requestLifecycleAction(kind)` (`access-admin-page.component.ts:180-182`) only opens the
review panel; the reason gate is the same component and the same facade guards
(`access-admin.facade.ts:246` accept, `:262` disable, `:274` reactivate).

### 2. Exact root cause

Identical to Item 1 — the shared helper's `> 0`, the non-nullable DTO properties, and the
shared review component. There is no *additional* lifecycle-specific reason rule anywhere.

### 3. Backend impact

The same edits as Item 1 extended to the three lifecycle bodies, commands, and handlers.
Audit remains complete without the reason: accept writes `UserAccepted` + `UserActivated` +
per-code `PermissionGranted` rows (`EfAccessUserMutationService.cs:98-137`); disable writes
per-grant `PermissionRevoked` + `UserDisabled` (`:157-194`); reactivate writes
`UserReactivated` (`:213-226`) — all with actor/target snapshots, timestamps, state JSON, and
operation provenance, none of which depend on the reason.

**Security eligibility rules are untouched**, as required: the Active-Owner recheck, the
Owner-target guards, status transition legality, and the fail-closed catalogue behavior all
live in `AccessUserMutationTransaction.cs` / `EfAccessUserMutationService.cs` on paths that
never read the reason.

### 4. Frontend impact

Covered by the same three edits as Item 1 plus the two remaining facade clauses
(`access-admin.facade.ts:246`, `:262`, `:274`). One consequence to design deliberately: for
lifecycle actions the empty-reason check is currently the **only** thing the confirm button
waits for. With it gone, the review step becomes a pure confirmation (destructive-action copy
for disable at `access-change-review.component.html:9-11`, grant preview for accept at
`:16-26`) — which is still the right UX for disable (a destructive act should keep a confirm),
and arguably still right for accept (it shows which permissions will be granted).

### 5. Contracts/models impacted

`swagger.json:5820` (`AcceptAccessUserBody`), `:6937` (`DisableAccessUserBody`), `:8695`
(`ReactivateAccessUserBody`) — `reason` flips to nullable; regenerated models
`accept-access-user-body.ts:7`, `disable-access-user-body.ts:6`,
`reactivate-access-user-body.ts:6`. Same `check-api-contract` gate as Item 1.

### 6. Files likely affected

Same set as Item 1 plus `AcceptAccessUserHandler.cs`, `DisableAccessUserHandler.cs`,
`ReactivateAccessUserHandler.cs` and the three body/command declarations.

### 7. Smallest safe implementation recommendation

Do Items 1 and 2 as **one change** — they share the helper, the DTO file, the review
component, the facade, the swagger regeneration, and the README sentences. Splitting them
would ship the same files twice.

### 8. Tests that need addition/update

- Backend: same as Item 1 — nothing asserts required-ness today; add omitted-reason success
  cases for all three verbs (assert the audit rows still appear, reason `NULL`).
- Frontend: the `it.each` HTTP-boundary tests (`access-admin-page.component.spec.ts:440-503`)
  and accept-grant test (`:530-555`) keep passing if the helper still types a reason; decide
  whether to switch the helper to a no-reason path to *prove* the new behavior, and add one
  test that confirms without typing anything. `:973-994` (review opens on disable, closes on
  user switch) survives as long as the review step is kept.

### 9. UX/accessibility considerations

- Disable keeps its red confirmation sentence (`--qd-danger` is deliberately reserved for
  exactly this moment — `features/access-admin/README.md:251-257`); removing the *reason* must
  not remove the *confirmation* for a destructive action.
- Accepting a Pending user grants the drafted permission set in the same act
  (`facade.ts:251-255` sends the draft codes); the review's grant preview is the only place
  that is made visible — keep it.

### 10. Risks / interactions

- The plan's locked wording covered only "every permission-set change"; **for lifecycle it was
  silent** — the mandatory lifecycle reason is documented in the current-state report
  (`:69-71`) and in `Backend/application/QuranDashboard.Application/Access/README.md:20`
  ("Write handlers require a bounded audit reason" — the one backend README invariant that
  must be rewritten in the same change).
- No change to Pending/Active/Disabled/Owner eligibility, per the product constraint.

---

## Item 3 — Permission-grid visual improvement

### 1. Current behavior

One bespoke component, `access-permission-editor`
(`features/access-admin/components/access-permission-editor/`, 4 files), rendered once at
`access-admin-page.component.html:219-224`. Layout
(`access-permission-editor.component.scss`, 51 lines, verified):

- outer grid: `gap: var(--qd-space-3)`, `align-items: start` (line 6);
- 2 columns at ≥1024px (`:41-45`), 3 columns at ≥1440px (`:47-51`), 1 column below;
- each group is a hand-rolled card (`fieldset` with border + `--qd-radius-sm` +
  `--qd-section-bg` + `--qd-space-3` padding, `:9-18`) — nearly the `.qd-card qd-card--quiet
  qd-card--mini` recipe re-implemented locally;
- no `min-height` anywhere; hierarchy is carried by font-weight 700 on both the legend and
  «تحديد الكل», an accent color on the latter, and a 1rem codes indent.

The five groups come from the backend catalogue (single source
`AbwabPermissionCatalogue.cs:7-25`, 19 codes), grouped/sorted client-side
(`models/access-admin-permissions.ts:11-50`): **الأبواب 6, الأقسام 4, العلاقات 2, القوالب 3,
عناصر القوالب 4**.

### 2. Exact root cause

Three compounding causes, all CSS:

1. **`align-items: start` + content-height cards in fixed-count rows.** The grid row is sized
   by its tallest card; a short card top-aligns and leaves the rest of the row blank. In the
   3-column row ‹doors(6)·sections(4)·relations(2)› the relations card leaves ~4 row-heights
   (~112px) of dead space.
2. **5 groups never divide evenly into 2 or 3 columns** — there is always a trailing empty
   grid cell (a whole empty column-cell at both breakpoints).
3. **The media queries measure the viewport, but the real constraint is the 20rem sidebar**
   (`access-admin-page.component.scss:25-31`). At 1023px the editor has ~911px and renders
   **one** column; at 1024px it has ~576px and renders **two** (~282px each, into which
   «إعادة ترتيب عناصر القوالب» must wrap). Columns get narrower exactly where the code adds
   one.

### 3. Backend impact

None. Groups, labels, and order are backend-owned and locked by
`AbwabPermissionCatalogueTests.cs:53` (exact label sequence) — the layout change must not
touch them.

### 4. Frontend impact

A **CSS-only** change to `access-permission-editor.component.scss` (and optionally a
class-composition change in the HTML). No TypeScript, no models, no state.

### 5. Contracts/models impacted

None. (If group *presentation order* were ever changed for packing, it must be done in CSS —
`access-admin.facade.spec.ts:224` asserts catalogue order of the flattened codes.)

### 6. Files likely affected

`access-permission-editor.component.scss` (primary), optionally
`access-permission-editor.component.html` (compose `.qd-card qd-card--quiet qd-card--mini`
instead of the hand-rolled card — fixes existing drift against
`.architecture/UI_STYLE_SYSTEM.md:338-343`), `features/access-admin/README.md:23`
(the "2–3 column grid" sentence).

### 7. Smallest safe implementation recommendation

Replace the two viewport media queries with an intrinsic, container-driven grid — the
pattern already shipped at `lemma-ayah-type-filters.component.scss:12-16`:

```scss
.access-permission-editor {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr));
  gap: var(--qd-space-3);
  align-items: stretch;
}
```

Effects, with the real numbers above: at ~992px of editor width (1440px viewport) this yields
4 columns of ~15rem instead of 3 of ~20rem; at ~576px (1024px viewport) it naturally gives 2;
at ~911px (1023px, sidebar stacked) it gives 4 instead of collapsing to 1 — the sharpest
defect (more columns in *less* space) disappears because the track count follows the actual
container. Cards per row are height-similar (2–6 rows), `align-items: stretch` + the existing
card background make remaining differences read as intentional card edges rather than holes,
and 5 groups across 4 tracks wrap 4+1 with the last card able to remain natural width (or
stretch via `:only-child`-style handling if desired — not required).

Small hierarchy polish inside existing tokens (optional, same file): a hairline
`border-block-start: 1px solid var(--qd-border)` + `padding-block-start: var(--qd-space-2)`
on `__codes` to separate «تحديد الكل» from the rows it controls, and reduce the codes indent
from `--qd-space-4` to `--qd-space-3`. No new token, colour, or primitive — the plan's locked
visual-language rule (`implementation-plan.md:539-541`) is respected.

Not recommended: CSS multicol/masonry (no precedent in the app, breaks `fieldset` grouping
semantics across column breaks) and `subgrid` row-alignment across cards (the app's only
subgrid use is `abwab-tree.component.scss`; it is heavier than the problem warrants).
No fake content is needed or proposed.

### 8. Tests that need addition/update

**A pure CSS change breaks zero tests** (verified: the component spec queries test-ids and
class names, not layout; `access-permission-editor.component.spec.ts:88-105` is fragile only
to *markup* changes — extra visible text in a row, renaming `__code`, moving `[title]`).
If the card is recomposed onto `.qd-card`, keep the `access-permission-editor__group` class.
There is **no automated visual coverage** for this page (no e2e spec, no screenshot tests) —
this widens `docs/TESTING_DEBT.md:296` row AC2 (layout claims "only a browser can judge");
add the compacted grid to AC2's list rather than inventing a new gate.

### 9. UX/accessibility considerations

- Keep `fieldset`/`legend`: the select-all's accessible name is the bare literal «تحديد الكل»
  in all five cards; only the fieldset scopes it (`UI_STYLE_SYSTEM.md:1080-1085`). Any
  restructure that drops the fieldset must add explicit `aria-label`s naming the group.
- Narrower columns increase label wrapping; `minmax(13rem, …)` is chosen so the longest label
  («إعادة ترتيب عناصر القوالب») still fits one line at typical rendering; verify in-browser.
- RTL is unaffected (all spacing is logical-property based already).

### 10. Risks / interactions

- Supersedes the plan's "responsive 2–3 column grid" target (`implementation-plan.md:76-78`,
  `:559-562`) and the same wording in `features/access-admin/README.md:23` — an intentional
  refinement of Phase 6's outcome, not a regression.
- Must preserve the page's sticky-aside arithmetic
  (`UI_STYLE_SYSTEM.md:1479-1520` names `access-admin-page.component.scss` explicitly) — the
  editor change does not touch it, but any temptation to "fix the page shell too" should be
  resisted here.

---

## Item 4 — Settings navbar entry for Access Management

### 1. Current behavior

- `nav-items.ts:22` registers `{ key: 'settings', labelAr: 'الإعدادات', route: '/settings',
  group: 'actions' }` — a **flat link, no children**, rendered by the desktop actions loop
  (`top-navbar.component.html:133-137`) which has no dropdown branch at all.
- Clicking it lands on the generated **placeholder page** («سيتم ربط هذا القسم ضمن خطة
  الميزات التالية.») via `placeholderRoutes` (`app.routes.ts:11-21`).
- The access route exists and works: `/settings/access`
  (`route-paths.ts:25`, registered at `app.routes.ts:66-71` **before** the placeholder,
  `canActivate: [ownerGuard]`, lazy → `ACCESS_ADMIN_ROUTES`, route `title: 'إدارة الوصول'` at
  `access-admin.routes.ts:8`) — but nothing in the UI links to it. Two READMEs record this as
  deliberate: *"intentionally absent from the navbar"*
  (`features/access-admin/README.md:3-4`) and *"non-navigated … keeps the administration
  boundary out of normal navigation"* (`core/README.md:126-131`).

### 2. Exact root cause

Not a bug — a recorded design decision now reversed by product. Mechanically, two gaps:
(a) the settings item has no `children`, and (b) the desktop `actionItems` loop renders a
flat anchor with no `@if (item.children)` dropdown branch (the primary loop has one at
`top-navbar.component.html:14-70`).

### 3. Backend impact

None. `ownerGuard` (`owner.guard.ts:10-27`) remains the sole authority; no duplicate
authorization rule is created (per the product constraint).

### 4. Frontend impact

Three precise edits:

1. **`nav-menu.ts:24-27`** — add `settings: [{ key: 'settings-access', labelAr:
   'إدارة الوصول', labelEn: 'Access Management', route: SETTINGS_ACCESS_ROUTE_PATH,
   group: 'actions' }]` to `childrenByParentKey`. **It must go here, not `nav-items.ts`** —
   `route-paths.ts` imports `NAV_ITEMS` at module init; nesting children into the registry
   creates an import cycle → TDZ `ReferenceError` (recorded at `core/README.md:80-83`
   precisely "so nobody 'simplifies' the children back in").
2. **`top-navbar.component.html:133-137`** — give the actions loop the same data-driven
   `@if (item.children)` dropdown branch the primary loop already has (trigger with
   `aria-haspopup`/`aria-expanded`/`data-testid="nav-settings-trigger"`, `#settings-menu`
   list, hover-intent + click toggle + Escape + outside-click via the existing
   `openMenuKey` machinery in `top-navbar.component.ts:51-142` — no new state needed).
3. **Mobile: zero template change.** The mobile panel loops the full `NAV_MENU` and already
   renders children generically (`top-navbar.component.html:266-283`) — adding `children` in
   `nav-menu.ts` makes the mobile sublist appear for free.

The canonical label is **«إدارة الوصول»** — already used as the route title
(`access-admin.routes.ts:8`) and the page `<h1>` (`access-admin-page.component.html:3`).
Use it verbatim; do not invent a second label.

### 5. Contracts/models impacted

None (`NavItem` already supports `children?` — `nav-items.ts:1-9`).
`docs/contracts/frontend-shell.md` is a pointer page; the authoritative claim to update is in
`core/README.md`.

### 6. Files likely affected

`nav-menu.ts`, `top-navbar.component.html` (+ small SCSS reuse — `.dropdown-menu` styles at
`top-navbar.component.scss:61-91` already exist), `top-navbar.component.spec.ts`,
`core/README.md:126-131` + `:75-85`, `features/access-admin/README.md:3-4` + `:507`,
optionally `top-navbar.component.ts` if Owner-gating is added.

### 7. Smallest safe implementation recommendation

Do edits 1–2 above, and **gate the visibility of the menu entry (or the whole dropdown
child) on `CurrentUserStore.isOwner`** (`current-user.store.ts:38`), the same predicate the
guard uses (`owner.guard.ts:24`). This is cosmetic defense-in-depth, not authorization — the
guard remains the authority, and a non-Owner who somehow clicks it is redirected to the
dashboard as today. Gate on `authStateKnown` (`current-user.store.ts:36`) to avoid a
flash-of-appearing-item while `/api/access/me` resolves. Note this is the app's **first**
auth-gated nav entry (`TESTING_DEBT.md:109` anticipated exactly this).

Open presentation choice (see clarifications): whether «الإعدادات» itself remains a link to
the `/settings` placeholder or becomes a trigger-only parent like «المزيد»
(`GROUP_ONLY_ROUTE` pattern, `top-navbar.component.ts:14-23`). Recommended: trigger-only —
a dropdown whose parent navigates to a placeholder is worse than one that just opens.

### 8. Tests that need addition/update

- `top-navbar.component.spec.ts:14` — add `'settings'` to `DROPDOWN_KEYS`; the whole
  parameterized suite (open/close/hover/Escape/focus-return, `:93-195`) then covers the new
  dropdown for free. The `CurrentUserStore` mock (`:24`, currently `{ clear: vi.fn() }`)
  must gain `isOwner`/`authStateKnown` signals or **every test in the file breaks** once the
  template reads them.
- This change is the *named trigger* for four open debt rows — `TESTING_DEBT.md:109-112`
  H1 (navbar unit spec — partially stale, a spec now exists; verify and update the row),
  H2 (active-state matrix on nav-entry addition), H3 (mobile flattened children),
  H4 (`e2e/shell-nav.e2e.ts` dropdown flow). Pay them in this change: add an active-state
  case for the settings child and extend the e2e nav spec with the settings→access flow
  (opt-in lane, not a gate).
- `app.routes.spec.ts:68` (`guardedPaths === ['settings/access']`) stays green — UI
  visibility gating adds no route guard.

### 9. UX/accessibility considerations

- The existing navbar dropdown pattern has `aria-haspopup`/`aria-expanded`/focus-return but
  **no arrow-key roving and no `role="menu"`** — acceptable for parity (it is a list of
  links, not a menu widget), but do not regress below the existing pattern.
- Dropdown shadow is explicitly sanctioned (`PRODUCT.md:59-61`: the single shadow exists
  only on floating layers); z-index must stay on the shared navbar rung
  (`UI_STYLE_SYSTEM.md:171-173`).
- RTL alignment of the menu follows the existing `.dropdown-menu` styles; the actions cluster
  sits at the inline-end of the bar.

### 10. Risks / interactions

- **Supersedes two recorded decisions** (`features/access-admin/README.md:3-4`, `:507`;
  `core/README.md:126-131`) — both READMEs must be rewritten in the same change (frontend
  CLAUDE.md same-change README rule).
- A navbar path into the workspace newly exercises `accessAdminUnsavedChangesGuard`
  (`window.confirm`-based, deliberate — plan `:401-409`) from in-app navigation — already
  handled, no change needed, but worth one manual check.
- The navbar gains its first `CurrentUserStore` read in a template; the store auto-refreshes
  on auth emissions (`current-user.store.ts:41-52`), so sign-out correctly hides the entry.

---

## Item 5 — Abwab tree search expansion accumulates across query changes

### 1. Current behavior

Search is fully client-side: the only tree read is `GET /api/abwab/tree`
(`abwab.api.ts:39-44`; `AbwabTreeController.cs` has a single parameterless `[HttpGet]` — no
backend search endpoint exists, so **the issue is not backend-driven**). Per keystroke
(no debounce) the toolbar emits → the query is written to the URL (`?q=`) → parsed back →
`searchAbwabNodes` (`abwab-tree.builder.ts:127-165`) recomputes `matchedIds` and
`autoExpandedIds` (ancestors of matches, `:154`). The page unions search ancestors with
reveal seeds into one input (`abwab-page.component.ts:157-167`) and the tree merges that
seed into its **single** expansion set. Branches opened by earlier partial queries stay open
even when they no longer contain any match; clearing the query leaves everything open.

### 2. Exact root cause

`abwab-tree.component.ts` (verified in place):

```ts
private readonly manuallyExpandedIds = signal<ReadonlySet<number>>(new Set());   // :58 — the ONLY store

constructor() {
  effect(() => {
    const seed = this.expandSeedIds();
    if (seed.size === 0) {
      return;                                                                    // :65-67 — empty seed = no-op
    }
    untracked(() => this.manuallyExpandedIds.update(
      (current) => new Set([...current, ...seed])));                             // :68 — pure union, never subtracts
  });
}

private readonly effectiveExpandedIds = this.manuallyExpandedIds.asReadonly();   // :72 — alias, not a merge
```

Two defects on those lines:

1. **`:68` — monotonic union with no provenance.** Search-derived ids are written into the
   same set as user toggles; nothing records *which* ids came from search, so nothing can be
   retracted when the query changes. `«ال» → «الرح»` unions `{A,B,C,D} ∪ {A}` = `{A,B,C,D}`.
2. **`:65-67` — empty seed early-returns**, so a zero-match query and a cleared query both
   leave every previously seeded branch open.

There is no distinction anywhere between manual and search-driven expansion; `:72` is the
vestige of a deleted `forceExpandedIds` input (commit `711dcb6d`). The **correct pattern
already exists in the same feature**: the move picker derives search expansion at render time
and never writes it into the expanded set
(`abwab-move-picker.component.ts:83`: `expanded.has(node.id) || (query !== '' && hasChildren)`),
which its spec pins as *"clearing returns expansion to exactly what the user opened by hand"*
(`abwab-move-picker.component.spec.ts:239`).

**Superseded-decision alert:** the current behavior is *documented as intended* —
`features/abwab/README.md:115-119`: *"seeds accumulate … is accepted and intended"* — and
pinned by a test added for that purpose (commit `ec28c3c1`;
`abwab-page.component.spec.ts:1306-1327`, whose `:1316-1319` asserts the chain **stays open
after the query clears**). The product decision reverses this; treat it as intentional
supersession, and rewrite both in the same change.

### 3. Backend impact

None. Do not touch Abwab search semantics — repository evidence (above) proves the entire
pipeline is client-side over the cached snapshot.

### 4. Frontend impact

Split search expansion out of the manual set and make it **derived**:

1. `abwab-tree.component.ts` — keep `manuallyExpandedIds` for user toggles and reveal seeds
   only; add a `searchExpandedIds` input (derived, replaced wholesale each query); make
   `effectiveExpandedIds` a real `computed` union of the two (restoring the pre-`711dcb6d`
   shape). The seed-merge effect keeps **only** the reveal source — the reveal-seed behavior
   ("seeded open, collapsible, survives") is separately pinned by
   `abwab-page.component.spec.ts:1409` and `:1430` and must not change.
2. `abwab-page.component.ts:157-167` — stop unioning reveal + search into one input; pass
   `revealExpandSeedIds` to the (renamed) seed input and `searchResult().autoExpandedIds`
   to the new derived input. This also dissolves the `NO_IDS` shared-identity trap
   (`README.md:578-585`) for the search half — a derived input read inside a `computed`
   doesn't need the identity trick, though keeping `NO_IDS` for the empty case is harmless
   and avoids re-render churn.
3. `abwab-page.component.html:166` — bind the second input.

Resulting semantics, matching the requested behavior exactly:

- **Search starts / query changes:** derived set = ancestors of *current* matches only;
  branches from earlier partials close automatically (they were never in the manual set).
- **Search cleared:** derived set empties → tree returns to exactly the user's manual state,
  neither corrupted nor collapsed — the move-picker contract, now on the main tree.
- **Manual collapse during search:** with a plain union, collapsing a search-opened branch
  will not stick while the query still derives it. If that matters, the minimal extension is
  a small `searchCollapsedIds` set (ids the user closed during this query; subtracted from
  the derived set; cleared on query change). Recommended: ship the plain union first — the
  user's stated pain is stale expansion, and the derived model already fixes it; add the
  subtraction set only if collapse-during-search proves annoying in practice.

### 5. Contracts/models impacted

None (component inputs only). The `?q=` URL contract (`abwab-url-sync.ts`, README `:490`)
is unchanged.

### 6. Files likely affected

`abwab-tree.component.ts` (state + inputs), `abwab-page.component.ts:157-167` + `.html:166`,
`abwab-page.component.spec.ts:1298-1327` (rewrite), new cases in
`abwab-tree.component.spec.ts`, `features/abwab/README.md:115-119` (+ touch `:578-585`).

### 7. Smallest safe implementation recommendation

The 3-edit derived-expansion split above. It is the smallest change that is *robust*: a
"clear the search-seeded ids on query change" patch inside the existing single-set model
would need provenance bookkeeping anyway, at which point the derived model is simpler, is
already proven in the move picker, and cannot corrupt manual state by construction.

Performance: strictly better than today. The tree is fully in memory; per keystroke the
pipeline already walks the tree once (`searchAbwabNodes`) and re-flattens visible rows; the
derived model *removes* a signal write + full-set allocation per keystroke (`:68` currently
allocates and rewrites even when `seed ⊆ current`) and typically shrinks `visibleRows`
(stale branches close instead of staying rendered — today accumulated expansion multiplies
rendered DOM rows as the user types, with no virtualization). An input debounce remains
optional and out of scope.

### 8. Tests that need addition/update

- **Rewrite** `abwab-page.component.spec.ts:1306-1327` — the `:1316-1319` assertion
  (expansion survives clear) inverts to: after clearing, only manually opened rows remain;
  document the supersession in the test name. Keep `:1409`/`:1430` (reveal) green.
- **Add** direct specs on `abwab-tree.component.spec.ts` (today the merge effect has *no*
  direct coverage): (a) query change replaces derived expansion — no accumulation;
  (b) clearing restores the exact manual set (model:
  `abwab-move-picker.component.spec.ts:239`); (c) zero-match query closes all
  search-derived branches; (d) reveal seeds still merge and stay collapsible.
- `abwab-tree.builder.spec.ts:294-345` (pure `searchAbwabNodes`) is unaffected.
- Marks/count tests (`abwab-page.component.spec.ts:642-…`, toolbar `:183-…`) unaffected.

### 9. UX/accessibility considerations

- The 500 ms settled announcement (`abwab-toolbar.component.ts:9,47-62`) already prevents
  per-keystroke screen-reader spam; closing stale branches doesn't change the announcement
  contract.
- The roving-focus model (`abwab-tree-keyboard.controller.ts`) reads `visibleRows`; when a
  branch containing the focused row closes on query change, focus must land somewhere sane —
  the existing `rovingId` fallback handles disappearing rows; add one keyboard case to the
  new specs if cheap.
- Preserving manual state across search (the recommendation) is exactly the "safest UX"
  the product asked for: search never becomes destructive to the user's tree.

### 10. Risks / interactions

- None with the access-management phases (different feature).
- Supersedes `features/abwab/README.md:115-119` and the pinned page-spec case — both must be
  rewritten in the same change (README same-change rule; test deletion requires the
  documented supersession as its proof + the new named coverage as replacement).
- Do not conflate the tree's contract with the picker's: the picker *forces* matching paths
  open (`README.md:262-266`); the tree should *derive but not force* (matches with children
  still user-collapsible via the subtraction set if/when added). The tree also deliberately
  **marks without filtering** (`README.md:88-99`) — this change touches expansion only, not
  filtering; do not start pruning the tree.

---

## Cross-cutting: conflicts with completed work, invariants, and docs

**Authorization/security invariants — no conflicts.** All five changes leave `[RequireOwner]`
(12 admin routes), `ownerGuard` as the sole route guard (`app.routes.spec.ts:68` stays true),
the Active-Owner transaction recheck, fail-closed catalogue behavior, `xmin` concurrency, and
audit atomicity ("a failed audit append leaves the target and grants unchanged") untouched.

**Previous decisions now superseded (intentional product changes, not regressions):**

| Superseded decision | Where it is recorded |
|---|---|
| "Mandatory reason … *(Locked decision — do not relax.)*" for permission-set changes | `access-management-implementation-plan.md:395-396`, `:743`, `:17` |
| Mandatory reason on the three lifecycle verbs | `access-management-current-state-report.md:69-71`, `:696-697`; `Backend/application/QuranDashboard.Application/Access/README.md:20` |
| "Requires an inline reason … mandatory trimmed 1..1024-character reason is unchanged" | `features/access-admin/README.md:41`, `:207-208`, `:487-492` |
| "Intentionally absent from the navbar"; "navbar changes are out of scope"; "non-navigated" | `features/access-admin/README.md:3-4`, `:507`; `core/README.md:126-131` |
| "Responsive 2–3 column grid" as the permission-grid target | `implementation-plan.md:76-78`, `:559-562`; `features/access-admin/README.md:23`; current-state report `:825-826` |
| Abwab search-seed accumulation "accepted and intended" | `features/abwab/README.md:115-119`; pinned test `abwab-page.component.spec.ts:1306-1327` |

**Decisions that remain in force and constrain the follow-up:** no technical id in UI or URL
(version stays hidden — `access-admin-page.component.spec.ts:1334`); `?tab=` closed enum only,
no user deep-link; `CanDeactivate` unsaved-changes protection; visual language locked to
existing `qd-*` tokens; catalogue labels/order backend-owned; children attach in `nav-menu.ts`
never `nav-items.ts` (TDZ cycle); relink stays in Advanced Security with its own checkbox.

**Housekeeping observation (out of scope, worth knowing):** root `CLAUDE.md` says
"Active Spec Kit Feature: None" while `docs/feature-034-access-management-workspace/` still
exists — feature 034 has not yet run its deletion commit. This report is itself a
feature-scoped artifact in that folder and dies with it; any fact here that must survive
belongs in the READMEs listed below, proven from code.

---

## Final summary

### Overall verdict

All five requested changes are **feasible, small-to-medium, and conflict-free with the
security architecture**. None weakens audit integrity, optimistic concurrency, or
authorization. Four of the five reverse *documented* decisions — the work is as much
README/test supersession as code. The largest single unit is the reason change (Items 1+2,
one coordinated backend-contract + frontend-workflow change); the grid fix is CSS-only; the
navbar entry is two small edits plus the test debt it deliberately triggers; the tree fix is
a contained state-model split with an in-repo reference implementation.

### Confirmed product changes

1. Reason becomes optional (never blocking) for permission replace and accept/disable/
   reactivate; audit continues to record actor, time, permission deltas, target, and version.
2. Permission grid becomes compact and container-driven; five groups, per-group select-all,
   and existing tokens preserved.
3. «الإعدادات» becomes a dropdown exposing «إدارة الوصول» → `/settings/access`.
4. Abwab tree search expansion is recalculated per query; stale branches close; manual state
   is preserved and restored on clear.

### Functional changes required

- Optional-reason path in one validation helper + nullable `Reason` across 4 bodies,
  4 commands, the audit entry contract, and the mutation service append signature; store
  `NULL` for blank. Relink and CLI reconciliation keep their own strict guards.
- Remove three frontend empty-reason guards; send `null` when blank.
- Nav model gains a settings child; desktop actions loop gains the existing dropdown branch.
- Tree expansion state splits into manual (+reveal-seeded) vs search-derived (computed union).

### UI/UX changes required

- Reason textarea labeled optional; review/diff step retained (recommended), destructive
  disable confirmation retained.
- Permission grid: `repeat(auto-fit, minmax(13rem, 1fr))` + `align-items: stretch` (+ optional
  hairline between select-all and codes; optional `.qd-card` composition). No new tokens.
- Settings dropdown reuses the navbar's existing data-driven dropdown markup/behavior; mobile
  works with zero template change; entry visible to active Owners only (cosmetic gating,
  guard stays authoritative).
- Tree: search never corrupts manual expansion; clearing restores it exactly.

### Backend changes required

`AccessAdministrationBodies.cs`, `AccessUserContracts.cs`, `AccessAdministrationValidation.cs`,
`AcceptAccessUserHandler.cs`, `DisableAccessUserHandler.cs`, `ReactivateAccessUserHandler.cs`,
`ReplaceUserPermissionsHandler.cs`, `AccessAuditContracts.cs`, `EfAccessUserMutationService.cs`,
`Application/Access/README.md`. **No migration** (audit `reason` column already nullable).
Then `Backend/scripts/export-swagger`.

### Frontend changes required

Reason: `access-change-review.component.ts/.html`, `access-admin.facade.ts`, regenerated
`openapi/swagger.json` + 4 generated body models (`npm run generate:api`).
Grid: `access-permission-editor.component.scss` (+ optional `.html`).
Navbar: `nav-menu.ts`, `top-navbar.component.html` (+ `.ts` for Owner gating),
`top-navbar.component.spec.ts`.
Tree: `abwab-tree.component.ts`, `abwab-page.component.ts/.html`, specs.

### Abwab search root cause

`abwab-tree.component.ts:62-70`: a single expansion set receives search-derived ancestor ids
via a union that never subtracts (`:68`), with an empty-seed early return (`:65-67`) that
makes zero-match queries and clearing no-ops. No provenance distinguishes user expansion from
search expansion, so nothing can be retracted per query. Client-side only — the backend has
no tree-search endpoint. Fix: make search expansion derived (move-picker pattern,
`abwab-move-picker.component.ts:83`), union it with manual state in a `computed`.

### Recommended implementation grouping/phases

| Phase | Content | Depends on |
|---|---|---|
| **F1 — Optional reason, backend contract** | Items 1+2 backend + swagger/client regeneration + backend tests + `Application/Access/README.md` | — |
| **F2 — Optional reason, frontend workflow** | Guard removal, optional-field copy, spec updates, `features/access-admin/README.md` reason sentences | F1 (regenerated client) |
| **F3 — Navbar entry** | `nav-menu.ts` child, actions dropdown branch, Owner gating, spec + debt rows H1–H4, README supersessions | — |
| **F4 — Permission grid compaction** | CSS-only + README grid sentence + AC2 debt note | — |
| **F5 — Abwab search expansion** | Derived-expansion split, spec rewrite + new direct specs, abwab README | — |

F1→F2 is the only ordering constraint; F3/F4/F5 are independent and can land in any order.
Backward-compatible deploys throughout (F1 alone changes nothing observable to the current UI).

### Tests/gates to run

- **F1:** `Backend/scripts/test-backend` — Access lane first
  (`AccessAdministrationEndpointTests`, `AccessAuditEventPersistenceTests`,
  `LogtoSubjectRelinkEndpointTests`), then `Backend/scripts/check-api-contract` (hard gate —
  fails until swagger + generated client are committed).
- **F2:** `npm run test` narrowed to `features/access-admin` (facade, page, change-review,
  api specs), then the feature lane.
- **F3:** `top-navbar.component.spec.ts`, `app.routes.spec.ts`; optional opt-in
  `npm run e2e` (`shell-nav.e2e.ts` extension) — never cited as gate evidence.
- **F4:** `access-permission-editor.component.spec.ts` (should pass untouched); in-browser
  RTL check at ~1023/1024/1440px (Playwright per the local-HTTPS note; no automated visual
  gate exists).
- **F5:** abwab lane (`abwab-page`, `abwab-tree`, `abwab-tree.builder`, move-picker specs).
- **Boundary:** full Frontend suite + production build + `npm run test:pre-pr` (includes the
  permission-catalogue and audit-action-type parity gates) once, at the pre-PR milestone —
  not per edit.

### Documentation that must be updated (same change as the code, per README rules)

- `Backend/application/QuranDashboard.Application/Access/README.md:20` — "Write handlers
  require a bounded audit reason" → optional-reason wording (the only backend README reason
  invariant).
- `Frontend/…/features/access-admin/README.md` — `:3-4` (navbar absence), `:23` (2–3 column
  grid), `:41`, `:207-208`, `:487-492` (mandatory reason), `:507` (navbar out of scope).
- `Frontend/…/src/app/core/README.md` — `:126-131` (non-navigated claim), `:75-85` (nav
  children inventory gains `settings`).
- `Frontend/…/features/abwab/README.md` — `:115-119` (accumulation intended → derived
  contract), touch `:578-585` (seed-union description).
- `docs/TESTING_DEBT.md` — H1–H4 paid/updated by F3 (H1's "no unit spec exists" looks stale
  already — verify); AC2 widened by F4.
- `docs/contracts/*` need no content change (pointer-only; the pointed-at READMEs above are
  the truth).

### Genuine clarifications still required

1. **Review step for permission saves:** keep the diff/confirm step with an optional reason
   (recommended — preserves the diff preview and the destructive-disable confirmation), or
   remove the step entirely so save is one click? The product wording "select permissions and
   save directly" is compatible with either; the report's recommendation assumes keep-with-
   optional-reason.
2. **«الإعدادات» parent behavior:** trigger-only dropdown (recommended; «المزيد» precedent),
   or keep it navigating to the `/settings` placeholder page?
3. **Navbar entry visibility:** Owner-only cosmetic gating (recommended), or visible to all
   authenticated users with the guard redirecting non-Owners?
4. **Relink + CLI reasons:** confirmed to stay mandatory? (Recommended and assumed above —
   they are separate strict paths and cost nothing to leave alone.)
