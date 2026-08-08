# Post-implementation follow-up plan — Access Management + Abwab tree search

- **Source of truth:** `docs/feature-034-access-management-workspace/post-implementation-ui-ux-follow-up-report.md`
  (all file:line evidence lives there) + current repository state at `0c9dd215`.
- **Branch:** continue on `feature/034-access-catalogue-readiness`.
- **Locked decisions:** optional reasons for the four workspace write actions (blank → `null`);
  relink and CLI reconciliation stay strict; the review/diff step stays; grid keeps five groups,
  per-group select-all, and individual toggles with no new tokens; «الإعدادات» becomes a
  trigger-only dropdown with an «إدارة الوصول» child shown to Active Owners only (route guard
  remains the authorization boundary); Abwab search expansion is derived from the current query
  only, manual expansion stays separate and is restored on clear; no backend search changes.

---

## Phase 1 — Access behavior: optional reasons

### Goal

An Owner can confirm permission replace, accept, disable, and reactivate without typing a
reason. Blank reasons travel and persist as `null`. Audit rows, concurrency, and the strict
relink/CLI paths are unchanged.

### Exact changes

Backend:

1. `AccessAdministrationBodies.cs` — `Reason` → `string?` on `AcceptAccessUserBody`,
   `DisableAccessUserBody`, `ReactivateAccessUserBody`, `ReplaceUserPermissionsBody`.
   **Not** on `ConfirmLogtoSubjectRelinkBody`.
2. `AccessUserContracts.cs` — same four commands: `Reason` → `string?`.
3. `AccessAdministrationValidation.cs` — add `TryGetOptionalReason(string? value, out string? reason)`:
   trim; blank → `null` and valid; length ≤ `MaximumReasonLength`. Keep `TryGetReason`
   untouched for relink.
4. `AcceptAccessUserHandler.cs`, `DisableAccessUserHandler.cs`, `ReactivateAccessUserHandler.cs`,
   `ReplaceUserPermissionsHandler.cs` — switch to the optional helper.
   `ConfirmLogtoSubjectRelinkHandler.cs` stays on `TryGetReason`.
5. `AccessAuditContracts.cs` — `AccessAuditEntry.Reason` → `string?`;
   `EfAccessUserMutationService.cs` `Append(... string reason ...)` → `string?` so blank
   stores as `NULL`. No migration — the `reason` column is already nullable.
6. Regenerate the contract: `Backend/scripts/export-swagger`, then in the frontend
   `npm run generate:api`; commit `openapi/swagger.json` + the four regenerated body models
   together (generated type becomes `reason: string | null`; the key stays required per the
   schema filter — clients send `null`).

Frontend:

7. `access-change-review.component.ts` `confirm()` — drop the `!reason` clause;
   emit the trimmed reason (possibly empty).
8. `access-change-review.component.html` — drop `!reason().trim() ||` from the confirm
   `[disabled]`; mark the «سبب الإجراء» label «(اختياري)».
9. `access-admin.facade.ts` — remove the empty-reason `'invalid'` clauses in
   `acceptSelectedUser`, `disableSelectedUser`, `reactivateSelectedUser`,
   `replaceSelectedPermissions`; send `reason: normalizedReason || null`.
   `confirmSelectedUserRelink` keeps its guard.

The review/diff step itself is untouched: `requestPermissionSave()` /
`requestLifecycleAction()` still open `qd-access-change-review`, which still shows the
permission diff, accept-grant preview, and the red disable warning.

### Main files/areas affected

`Backend/api/QuranDashboard.Api/Controllers/Access/AccessAdministrationBodies.cs`,
`Backend/application/QuranDashboard.Application/Access/**` (validation + 4 handlers),
`Application.Abstractions/Access` (commands + audit entry contract),
`EfAccessUserMutationService.cs`, `openapi/swagger.json` + generated client,
`features/access-admin/` (review component, facade).

### Minimal required tests

- Backend, one focused addition per verb (extend `AccessAdministrationEndpointTests`):
  omitted/blank reason → success, audit rows present with `Reason == null`.
- Backend, one pin: relink confirm with blank reason still returns 400 (no such test exists
  today — add it so the non-relaxation is asserted).
- Frontend: invert `access-change-review.component.spec.ts:84-96` (confirm enabled with
  empty reason); invert/delete `access-admin.facade.spec.ts:740-748` (supersession recorded
  in the commit; replacement is the new enabled-path assertion); update body expectations
  that hardcode `reason:` where the test now confirms without typing.
- Run only: backend Access lane (`Backend/scripts/test-backend`, narrowed) and the
  `features/access-admin` Vitest specs.

### Acceptance criteria

- Each of the four actions completes from the UI with the reason field left empty.
- A typed reason still persists verbatim; a blank one persists as `NULL`.
- Relink confirm and `owners reconcile` still reject blank reasons.
- `Backend/scripts/check-api-contract` passes with the regenerated artifacts committed.
- 409/conflict and no-op-save behavior unchanged.

### Dependencies

None. Backend edits land before the frontend guard removal (the relaxed backend accepts
reason-bearing requests, so order within the phase is backend → regenerate → frontend).

### Out of scope

Removing the review step; relink/CLI reason rules; audit read model; any migration.

---

## Phase 2 — Access UI polish: grid compaction + navbar entry

### Goal

The permission grid is compact and container-driven with no large blank areas, and
«الإعدادات» is a dropdown whose «إدارة الوصول» entry takes an Active Owner to
`/settings/access`.

### Exact changes

Grid (CSS-only; repository inspection confirmed auto-fit/minmax fits — precedent at
`lemma-ayah-type-filters.component.scss:12-16`):

1. `access-permission-editor.component.scss` — replace the two viewport media queries
   (`:41-51`) with `grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr))` on the
   root and change `align-items: start` → `stretch`. Optional, same file: hairline
   `border-block-start: 1px solid var(--qd-border)` + `padding-block-start: var(--qd-space-2)`
   on `__codes` for hierarchy. Keep the `fieldset`/`legend` markup, all class names,
   and both `data-testid` patterns exactly as they are.

Navbar (reuse the existing dropdown implementation and mobile behavior):

2. `nav-menu.ts` — add to `childrenByParentKey`:
   `settings: [{ key: 'settings-access', labelAr: 'إدارة الوصول', labelEn: 'Access Management', route: SETTINGS_ACCESS_ROUTE_PATH, group: 'actions' }]`.
   Children attach here, never in `nav-items.ts` (documented TDZ import cycle).
3. `top-navbar.component.html` actions loop (`:133-137`) — add the same data-driven
   `@if (item.children)` dropdown branch the primary loop has (`:14-70`): trigger button with
   `aria-haspopup`/`aria-expanded`/`data-testid="nav-settings-trigger"`, `#settings-menu`
   list, wired to the existing `openMenuKey` open/close/Escape/outside-click machinery —
   no new state. The parent is trigger-only (the «المزيد» `GROUP_ONLY_ROUTE` precedent,
   `top-navbar.component.ts:14-23`); it no longer links to the `/settings` placeholder.
4. Owner-only visibility (UX convenience; `ownerGuard` remains the authority):
   `top-navbar.component.ts` exposes `isOwner`/`isActive`/`authStateKnown` from
   `CurrentUserStore`; the «الإعدادات» item renders only for an Active Owner (its sole child
   is Owner-only, so hiding the empty trigger with it) — in both the desktop actions loop
   and the mobile panel. Gate on `authStateKnown` to avoid a flash while `/api/access/me`
   resolves.

Mobile needs no template change for the child itself — the panel already renders `children`
generically (`top-navbar.component.html:266-283`); only the visibility gate applies.

### Main files/areas affected

`access-permission-editor.component.scss`; `core/navigation/nav-menu.ts`;
`core/layout/top-navbar/` (html + ts + spec).

### Minimal required tests

- `top-navbar.component.spec.ts` — add `'settings'` to `DROPDOWN_KEYS` (`:14`) so the
  existing parameterized open/close/hover/Escape/focus suite covers the new dropdown; extend
  the `CurrentUserStore` mock (`:24`) with the new signals; add one visibility pair
  (Active Owner sees the item, non-Owner/unknown does not).
- Grid: `access-permission-editor.component.spec.ts` must pass **untouched** (it asserts
  behavior and markup, not layout). One manual RTL browser check at ~1023 / 1024 / 1440px
  (Playwright boots both servers per the local-HTTPS note); no automated visual gate exists.
- `app.routes.spec.ts` stays green unmodified (`guardedPaths === ['settings/access']` —
  visibility gating adds no route guard).

### Acceptance criteria

- Grid: no single-column collapse at 1023px, no 2-narrow-columns jump at 1024px; track count
  follows container width; العلاقات no longer strands a large blank block; five groups,
  select-all, and toggles unchanged; no new tokens or primitives.
- Navbar: Active Owner opens «الإعدادات» → clicks «إدارة الوصول» → lands on
  `/settings/access` (desktop and mobile); non-Owners see no settings entry; keyboard
  Escape/outside-click/focus-return behave like the existing dropdowns; nobody needs to type
  the route.

### Dependencies

None (independent of Phase 1).

### Out of scope

Page-shell/sticky-aside changes; new dropdown component or CDK overlay; arrow-key roving
beyond the existing dropdown pattern; any additional settings children; route/guard changes.

---

## Phase 3 — Abwab tree search expansion

### Goal

Search-driven expansion reflects only the current query: stale branches close on every query
change, and clearing the search restores the user's manual expansion exactly.

### Exact changes

1. `abwab-tree.component.ts` — keep `manuallyExpandedIds` for user toggles + reveal seeds
   only. Add a `searchExpandedIds` input (`ReadonlySet<number>`, replaced wholesale per
   query). Restrict the constructor seed-merge effect (`:62-70`) to the reveal source, and
   make `effectiveExpandedIds` a real `computed` union of `manuallyExpandedIds` and
   `searchExpandedIds()` (replacing the `:72` alias). Plain union first — no
   collapse-during-search subtraction set unless it proves annoying in use.
2. `abwab-page.component.ts` — split `expandSeedIds` (`:157-167`): pass
   `revealExpandSeedIds()` to the seed input and `searchResult().autoExpandedIds` to the new
   input (keep returning the shared `NO_IDS` for the empty case to avoid identity churn).
3. `abwab-page.component.html:166` — bind both inputs.

No changes to `searchAbwabNodes`, marking, filtering, the `?q=` URL contract, or anything
backend (search is fully client-side — verified in the report).

### Main files/areas affected

`features/abwab/components/abwab-tree/` (component + spec),
`features/abwab/pages/abwab-page/` (ts + html + spec).

### Minimal required tests

- Rewrite `abwab-page.component.spec.ts:1306-1327` — the pinned "expansion survives clearing"
  assertion inverts to "clearing restores exactly the manual state" (supersession noted in
  the test name/commit; the model is `abwab-move-picker.component.spec.ts:239`).
- Add direct `abwab-tree.component.spec.ts` cases (the merge effect currently has none):
  (a) query change replaces derived expansion — no accumulation across `«ال» → «الرح»`;
  (b) zero-match query closes all search-derived branches;
  (c) reveal seeds still merge into manual state and stay collapsible.
- Keep green untouched: `abwab-page.component.spec.ts:1409` and `:1430` (reveal), the
  marks/count suites, `abwab-tree.builder.spec.ts`.

### Acceptance criteria

- Typing `ال` → `الرح` → `الرحمن` leaves open only the ancestors of current matches at each
  step; branches opened by earlier partials close automatically.
- Branches the user opened by hand stay open through search and after clearing; branches
  they never touched are closed after clearing.
- Reveal-driven expansion behavior is unchanged.

### Dependencies

None (independent of Phases 1–2).

### Out of scope

Tree filtering/pruning, search debounce, Arabic normalization in matching, backend search
endpoints, URL-state changes, virtualization.

---

## Superseded documentation and tests (update in the same commit as the change)

| Phase | Superseded item |
|---|---|
| 1 | `Backend/application/QuranDashboard.Application/Access/README.md:20` ("Write handlers require a bounded audit reason") |
| 1 | `features/access-admin/README.md:41`, `:207-208`, `:487-492` (mandatory reason) |
| 1 | `access-change-review.component.spec.ts:84-96`; `access-admin.facade.spec.ts:740-748` |
| 2 | `features/access-admin/README.md:3-4` ("intentionally absent from the navbar"), `:23` ("2–3 column grid"), `:507` ("navbar changes are out of scope") |
| 2 | `core/README.md:126-131` ("non-navigated") and `:75-85` (nav children inventory gains `settings`) |
| 2 | `docs/TESTING_DEBT.md:109-112` rows H1–H4 (this change is their named trigger — update/pay), `:296` AC2 (note the compacted grid) |
| 3 | `features/abwab/README.md:115-119` (accumulation "accepted and intended"), touch `:578-585` |
| 3 | `abwab-page.component.spec.ts:1306-1327` (pinned surviving-expansion case) |

The plan docs in `docs/feature-034-access-management-workspace/` (including this file) remain
feature-scoped artifacts and are removed by the feature's deletion commit after review.

---

## Phase order

Phase 1 → Phase 2 → Phase 3. Only Phase 1 has internal ordering (backend → contract
regeneration → frontend); Phases 2 and 3 are independent and may be reordered or
parallelized if convenient.

## Final verification (once, after all three phases — not per phase)

1. Focused lanes for all changed areas: backend Access lane
   (`Backend/scripts/test-backend`, `--no-build` after the first build); frontend
   `features/access-admin`, `core/layout` + `core/navigation`, and `features/abwab` specs.
2. Builds: `dotnet build` (backend), `npm run build` (frontend production).
3. `Backend/scripts/check-api-contract` — hard gate; the reason DTOs changed.
4. Full Backend suite + full Frontend suite / `npm run test:pre-pr` once at this boundary
   (includes the permission-catalogue and audit-action-type parity gates), per the
   documented pre-PR trigger in `TESTING_STRATEGY.md`.
5. Final engineering review (`engineering-review`) of the whole branch afterward.

## Status

**READY_FOR_IMPLEMENTATION**
