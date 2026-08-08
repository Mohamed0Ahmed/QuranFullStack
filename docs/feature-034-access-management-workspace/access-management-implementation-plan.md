# Access Management workspace — implementation plan

Companion to [`access-management-current-state-report.md`](./access-management-current-state-report.md).
Plan only. No code was written, no migration created, no database touched.

Every integration point below was verified against the repository. Where two readings conflicted the
disagreement is recorded and settled with evidence rather than averaged.

**Revision 2** — applies the locked product decisions in §6 and the fail-closed write-readiness rule
in §1.3. The eight-phase structure is unchanged.

---

## 0. Current state and root cause, in brief

The feature is **implemented end to end** — 13 routes, a typed frontend boundary, a working grouped
permission editor with select-all and indeterminate state, mandatory reason capture, and optimistic
concurrency on every write. Permission assignment is *positively* proven to work by a
rendered-component test that drives a real checkbox through to the PUT body
(`access-admin-page.component.spec.ts:354-392`).

It fails at runtime for one reason, in two layers:

1. **The `permissions` table is never populated.** No migration seeds it, `reset-db`/`update-db`
   never sync it, `PermissionCatalogueSynchronizer` is registered in DI
   (`AccessDependencyInjection.cs:35`) with **zero production call sites**, and the production image
   publishes only `QuranDashboard.Api` (`Dockerfile:27-30, :47`) so the CLI documented as the remedy
   is not even present. `EfPermissionCatalogueReader.GetActiveAsync:15-22` then throws, and
   `GlobalExceptionHandler` turns that into 500 + «حدث خطأ غير متوقع».
2. **One misordered template branch turns a data fault into total failure.** The detail card
   evaluates `catalogueError()` before `selectedUser()` (`access-admin-page.component.html:31-57`),
   erasing the identity header, status badges, lifecycle actions and relink form — none of which read
   the catalogue — with no retry for the rest of the session.

**Two facts discovered during planning change the shape of the fix:**

- **A readable catalogue does not mean a writable one.**
  `EfAccessUserMutationService.ResolvePermissionsAsync:318-352` validates every submitted code
  against **non-retired database rows**. A read path served from the compiled catalogue will happily
  render a full editor while the table is empty and *every save 400s*. Serving the catalogue safely
  and making writes actually resolvable are two different problems, and the UI must be able to tell
  them apart — see §1.3.
- **A failed or unready catalogue can currently cause a silent revoke-all.**
  `setSelectedPermissionCodes` filters the draft through the *catalogue*
  (`access-admin.facade.ts:230`), so an empty catalogue empties the draft, and
  `permissionCodesForSubmission` then returns `[]` at submit time (`:243, :285`). An accept or
  replace issued in that state would revoke every grant while the confirmation diff showed nothing.
  This makes the fail-closed rule a **security requirement**, not a polish item.

---

## 1. Target architecture

### 1.1 Backend

| Concern | Target |
|---|---|
| Catalogue source of truth | `AbwabPermissionCatalogue.All` — unchanged |
| Catalogue read contract | **Returns the canonical active (non-retired) permission catalogue safely.** Cannot 500 on drift |
| Catalogue population | Existing `PermissionCatalogueSynchronizer`, invoked automatically at startup, race-safe, non-fatal |
| **Write readiness** | Explicitly computed and exposed, so the UI can fail closed (§1.3) |
| Drift visibility | Startup log + a `permission_catalogue` health check (**Degraded**, never Unhealthy) + the existing CLI preflight |
| Write path | **Untouched.** All six validation gates stay, including the non-retired DB check |
| Schema | **No migration.** Table, unique index and check constraint already exist |
| User discovery | Substring match over `NormalizedEmail` + `DisplayName`; no index, no migration |
| Audit identity | Human names projected from the existing `ActorUser`/`TargetUser` FK navigations |

> **Catalogue size is not a contract.** The catalogue currently holds 19 permissions in 5 groups, and
> tests pin that number to catch accidental loss. **19 is today's size, not a permanent API
> invariant.** No requirement in this plan is written as "always returns exactly 19"; the contract is
> *"returns the canonical active/non-retired catalogue safely"*. A future feature that adds or
> retires a permission changes the count and updates those tests — it does not break the contract.

### 1.2 Frontend

Master/detail workspace: sticky ~20rem user-list aside + flex-1 selected-user workspace, modelled on
`abwab-page.component.scss:22-52`. Permissions are the primary section in a responsive 2–3 column
grid. Audit, reconciliation and **Advanced Security** (relink) move behind `qd-tabs` — the only
tablist primitive that exists.

**Component tree (9 nodes):**

```
AccessAdminPageComponent              providers:[AccessAdminFacade]; public hasUnsavedChanges()
├─ qd-tabs + qdTab                    ?tab= driven
├─ .access-admin-page__layout (flex)
│  ├─ <aside> sticky 20rem
│  │  └─ qd-access-user-list          [KEEP] + qd-result-count, qd-pagination, qd-state
│  └─ <div> flex:1; min-inline-size:0
│     ├─ qd-access-user-summary-card  [NEW]  .explorer-panel-header, .qd-badge
│     ├─ <section> الصلاحيات المباشرة   ← isolated error region + fail-closed region
│     │  └─ qd-access-permission-editor [KEEP, regridded]
│     ├─ qd-access-lifecycle-actions  [NEW]
│     └─ qd-access-change-review      [NEW]  diff + reason + save/cancel
├─ qd-access-audit-log                [NEW]   secondary tab
├─ qd-access-advanced-security        [NEW]   secondary tab — hosts relink + reconciliation
└─ qd-confirm-dialog ×N               switch-user discard + destructive lifecycle
```

**Primitives confirmed absent — never name them:** `.qd-table`, `.qd-toolbar`, `.qd-sidebar`,
`.qd-btn-danger`, any accordion, any segmented control. Danger tone comes only from
`qd-confirm-dialog [tone]="'danger'"`. Button variants are single-dash (`.qd-btn-primary`);
everything else is BEM double-dash (`.qd-card--feature`).

**URL decision — `?tab=<closed enum>` only, no user deep-link.** This is forced, not preferred:
`AccessUserSummary` carries no slug and no `sub` (`sub` exists only on `AccessUserDetail`, i.e. only
*after* selection), and `AccessAdminApi.userListParams:102-115` has no filter that could resolve a
handle back to a user. An opaque handle would need a new backend contract and would still be an
identifier; email is PII. Use a query param, not a child route — a child route breaks
`access-admin.routes.spec.ts:7` and orphans `title` at `access-admin.routes.ts:6`.

### 1.3 The failure model — one coherent rule across startup, read and write

Three distinct conditions, deliberately kept distinct:

| Condition | Public/read app | Catalogue read | Permission **writes** | UI behaviour |
|---|---|---|---|---|
| Healthy | available | canonical catalogue | resolvable | full editor |
| Catalogue **request** failed (transport/auth/500) | available | unavailable | unknown | permission region degrades with retry; identity, status and lifecycle actions survive |
| Catalogue **persistence not ready** (rows missing) | available | canonical catalogue served | **would 400** | **editor fails closed**: read-only, no Save, no Accept-with-permissions |

**The rules, in force everywhere in this plan:**

1. Public and read-only application functionality stays available. A catalogue problem is never
   allowed to take down anything outside Access Management.
2. **A safe read never implies safe writes.** If permission-catalogue persistence is degraded such
   that writes cannot resolve canonical non-retired DB rows, the permission editor **fails closed**.
3. **No usable Save / Accept-with-permissions path is presented until write readiness is restored.**
4. **A readiness failure must never produce an empty replacement set or an accidental revoke-all.**
   This is the concrete danger identified in §0.
5. Lifecycle actions that do not depend on the catalogue — accept-without-permissions, disable,
   reactivate — remain available, because they carry no revoke risk (`accept` requires zero existing
   grants; `disable` intentionally revokes; `reactivate` requires zero grants).
6. **The existing write-path DB validation is not weakened.** Its 400s on an unseeded database are
   the fail-safe working correctly. The cure is readiness, never a looser validator.

**The readiness signal.** `GET /api/access/permissions` returns a wrapper instead of a bare array:

```
ApiResponse<PermissionCatalogueResponse>
  items: PermissionCatalogueItem[]      // canonical, non-retired
  assignmentReady: bool                 // every offered code has a non-retired DB row
```

`assignmentReady` is computed in the **same** query the reader already needs — read `(Code,
RetiredAtUtc)` for all rows once, derive both the retired set and the active set:

```
offered   = AbwabPermissionCatalogue.All.Where(d => !retiredDbCodes.Contains(d.Code))
assignmentReady = offered.All(d => activeDbCodes.Contains(d.Code))
```

Behaviour by state: empty table → `offered` = full catalogue, `activeDbCodes` empty → **not ready**
(correct). Fully synced → ready. One canonical code retired → it is not offered, the rest are ready
(correct — retirement is a legitimate product state, not a fault). Extra unknown DB row → readiness
unaffected, read still safe (correct).

> **This is a contract change and it is deliberate.** Revision 1 claimed Phase 1 needed no Swagger
> regeneration; adding `assignmentReady` makes that false. Phase 1 now includes
> `Backend/scripts/export-swagger` + `npm run generate:api` and a minimal frontend adapter so the app
> keeps compiling. The alternative — inferring readiness from an empty `items` array — was rejected:
> it conflates "no permissions exist" with "not ready", which is exactly the silent-wrongness this
> rule exists to prevent.

---

## 2. Phases

Eight phases. Phases 1–3 are correctness and can ship independently of any visual change; Phase 6 is
the expensive redesign; Phases 7–8 depend on earlier work.

---

### Phase 1 — Catalogue availability, safe read, and write readiness *(backend + a thin frontend adapter)*

**Goal.** `GET /api/access/permissions` always answers safely with the canonical active catalogue and
states whether assignments can currently be persisted; permission **writes** work on a
migrations-only database without an operator running anything by hand.

**Why the read fix and the sync cannot be split.** A safe read on an empty table renders a full
editor whose every save 400s at `ResolvePermissionsAsync:318-352`. Shipping the read fix alone
converts a visible error into a silent dead end. The readiness flag is what makes the two states
distinguishable to the UI, so all three land together.

**Backend work.**

1. **Safe read + readiness** — rewrite `EfPermissionCatalogueReader.GetActiveAsync`
   (`.../Reads/Access/EfPermissionCatalogueReader.cs:8-34`): read `(Code, RetiredAtUtc)` once; derive
   the retired and active sets; **delete the equality gate at `:15-22`** — that is the 500; return
   the canonical catalogue minus retired codes through the **existing** projection at `:24-33`,
   together with `assignmentReady` per §1.3.
   *Do not left-join DB rows into the item list* — with today's empty table that returns zero items,
   which the UI renders as "no permissions": silently wrong and worse than the 500.
2. **Contract** — introduce `PermissionCatalogueResponse { Items, AssignmentReady }` in
   `Application.Abstractions/Access/PermissionCatalogueContracts.cs`; `IPermissionCatalogueReader`,
   `GetPermissionCatalogueHandler` and `AccessPermissionsController:12-20` carry it through.
   The controller still returns `Ok(...)` — do **not** add `ToActionResult` or an
   `AccessOperationFailure` member; none of the twelve existing variants describes a server-side
   projection fault.
3. **Race-safe synchronizer** — wrap `PermissionCatalogueSynchronizer.SynchronizeAsync` in an
   explicit transaction and take a **blocking** `pg_advisory_xact_lock` as the first statement,
   mirroring `OwnerReconciliationStore.cs:112-118` with its own key constant. Blocking, not
   `try_`: a second booting instance must wait and then find the rows. Read `existing` *under* the
   lock, and compute `unknownCodes`/`retiredCanonicalCodes` off post-insert state rather than the
   stale snapshot at `:55-64`.
4. **Startup hook** — add `SynchronizePermissionCatalogueAsync(this WebApplication app)` to
   `WebApplicationExtensions.cs`, beside the existing `UnsafeEndpointMetadataValidator` startup call,
   and invoke it in `Program.cs` **between `UseApiPipeline()` and `app.Run()`** (top-level statements
   support `await`; one-line change).
   **Not `AddHostedService`** — `GenericWebHostService` (Kestrel) is registered first and hosted
   services start in registration order, so a hosted service would run with the socket already open
   and could serve the endpoint against an empty table. There is no `IHostedService` or
   `IStartupFilter` anywhere in the solution today, so neither is "the existing pattern".
   Body order: honour the enable flag → `CreateAsyncScope()` (both dependencies are scoped) → a 15s
   `CancellationTokenSource` so an unreachable DB cannot eat Railway's 120s healthcheck window → if
   `GetPendingMigrationsAsync` is non-empty, log Warning, mark not-ready, return → else synchronize,
   log added/updated/unknown/retired counts, store the result in a singleton state object.
5. **Failure policy — start degraded, never refuse to start.** Migrations are applied by a human
   running `Backend/scripts/update-db`; `railway.json:6-12` has no `startCommand` and no
   `preDeployCommand`, and Railway auto-deploys from `main`. **A deploy can legitimately precede its
   migration.** Fail-fast would exhaust `restartPolicyMaxRetries: 10` and take down every anonymous
   public GET over one Owner-only endpoint. Catch the operational exception set that
   `AccessAdmin/Program.cs:81-84` already treats as operational, log at Error, and return normally.
   This mirrors how the codebase already separates infrastructure failure
   (`AuthorizationStateAccessEvaluator.cs:59-71` degrades to 503) from configuration failure
   (`ValidateOnStart` ×3, which does refuse to start).
   **Degraded startup is not permission to write.** It keeps the public app alive; `assignmentReady`
   is what governs the editor, and it will be `false` in exactly this state.
6. **Health check** — register `permission_catalogue` beside `.AddDbContextCheck<…>("database")`
   (`ServiceCollectionExtensions.cs:60-61`) with **`failureStatus: HealthStatus.Degraded`**.
   **Degraded is mandatory:** `HealthController.cs:29-40` returns 503 only for Unhealthy and
   `railway.json:8` gates the deploy on `/api/health`, so an Unhealthy catalogue check would make
   the app permanently undeployable.
7. **Config flag** — `PermissionCatalogueStartupOptions` bound to
   `Access:PermissionCatalogueStartupSync`, `Enabled` defaulting to **true** in code (no appsettings
   edit; production can flip via `Access__PermissionCatalogueStartupSync__Enabled=false`).

**Frontend work — thin adapter only.** Update `access-admin.api.ts` `getPermissionCatalogue` to read
`.items`, store `assignmentReady` in a facade signal, and regenerate models. **No UX change here** —
enforcement lands in Phase 2. This exists only so the app compiles and the signal is available.

**Tests.**

| # | Regression | File | Harness |
|---|---|---|---|
| 1 | Migrations-only DB, no manual sync → 200 with the full canonical catalogue *(pin today's 19 as the current size)* | `Api/Access/PermissionCatalogueStartupSyncTests.cs` **(NEW)** | `[Collection(nameof(AccessProcessGlobalCollection))]` **with no ctor param**; per case `PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(...)`; own `WebApplicationFactory<AccessController>` configured like `AccessTestFixture.cs:167-204` |
| 2 | The leased clone really starts with `permissions` empty | same | raw command on the lease *before* touching `Factory.Services` — guards the class from a false pass |
| 3 | Startup sync idempotent across a second host boot | same | second factory on the same connection string |
| 4 | Leftover unknown row → still 200, full catalogue, `assignmentReady: true` | same | insert `future.example`, boot, assert |
| 5 | **Sync disabled + empty table → 200, full catalogue, `assignmentReady: false`** | same | flag off; this is the fail-closed contract |
| 6 | Endpoint returns a controlled envelope, never an uncontrolled 500 | extend `AccessAdministrationEndpointTests.cs:522-527` | `AccessCollection` + `CreateOwnerClient()` |
| 7 | Retired canonical code → absent from `items`, `assignmentReady` still true | same | retirement is a product state, not a fault |
| 8 | Synchronizer unknown-code behaviour | **rewrite** `PermissionCatalogueSynchronizerTests.cs:45-81` | existing `AccessCollection` |
| 9 | Health: missing rows → Degraded; healthy → Healthy | new cases | health factory |
| 10 | **Write path not weakened** — a retired code still 400s with zero grants and zero audit rows | `AccessAdministrationEndpointTests.cs:359-374` | **leave exactly as is; it must keep passing** |

> **The migrations-only test is the one with a trap, and it is already solved by an existing
> pattern.** `AccessTestFixture.ResetAsync` TRUNCATEs `permissions` and each test opts into sync, a
> behaviour deliberately pinned by `AccessCollectionResetContractTests`. Do **not** touch it.
> `AccessSchemaDriftTests` already sits in `AccessProcessGlobalCollection` *without injecting that
> collection's fixture* and leases its own migrated clone; copy that shape.

**Test-fixture opt-out (mandatory, or the suite flakes).** Set
`["Access:PermissionCatalogueStartupSync:Enabled"] = "false"` in the in-memory configuration of:
`AccessTestFixture.cs` (~`:172-188`) — **required**, because without it
`PermissionCatalogueSynchronizerTests:22` flakes whenever that class is first in `AccessCollection`
to touch `ApiServices` (reset truncates, then the lazy host build steals the inserts the test asserts
it made) — plus `Api/Health/HealthApiFactory.cs:23-29`,
`Api/RateLimiting/RateLimitingApiFactory.cs:33-40`, `Api/ApiBehavior/ApiBehaviorTestFactory.cs:22-28`
(the last three point at deliberately dead databases), plus
`Quran/WordsWordTypes/WordTypesTestFixture.cs` — *(corrected during Phase 1: the original list of
four was incomplete)* — because its connection string can come from
`ExternalReadOnlyDatabaseOptIn.TryLease(...)`, and that opt-in is a read-only **convention**, not a
connection-level grant, so only the flag stops the sync writing to a database the test process does
not own. **Five factories opt out, not four.**
**Leave `Smoke/SmokeApiHost.cs` opted in** — it leases a real migrated DB and is the only host that
can prove the change end to end.

**Docs.** `Backend/api/QuranDashboard.Api/README.md` (startup behaviour + the new config key);
`Backend/infrastructure/QuranDashboard.Infrastructure/Access/README.md` (the advisory lock; and the
read-path rule — the endpoint is served from the compiled catalogue, the DB is consulted for retired
codes and readiness, and divergence is a health-check/preflight concern, never an HTTP failure);
`Backend/tools/QuranDashboard.AccessAdmin/README.md:38-49` (normal deploys no longer need a manual
`catalogue sync`; it remains the remedy for unknown/retired codes);
`Backend/tests/QuranDashboard.Tests/README.md`; `docs/contracts/security-access.md` (the endpoint now
carries a readiness flag).
Add `QuranDashboard.Tests.Api.Access.PermissionCatalogueStartupSyncTests	Access	Database	TierB	Schema`
to `TestSupport/Execution/test-gates.tsv`.

**Files.** `EfPermissionCatalogueReader.cs`, `PermissionCatalogueContracts.cs`,
`GetPermissionCatalogueHandler.cs`, `AccessPermissionsController.cs`,
`PermissionCatalogueSynchronizer.cs`, `WebApplicationExtensions.cs`, `Program.cs`,
`ServiceCollectionExtensions.cs`, new startup options/health-check types, five test factories,
one new test class, `test-gates.tsv`, five READMEs; frontend `access-admin.api.ts`,
`access-admin.facade.ts`, regenerated models, committed `openapi/swagger.json`.

**Acceptance gates.** Build once, then `Backend/scripts/test-backend access-db --no-build` →
`access --no-build` → `smoke --no-build` → `tier-b --no-build`; plus
**`Backend/scripts/check-api-contract`** (contract changed) and `npm run test:feature:access-admin`.
Do **not** run `migration`, `pipeline`, or `canonical-data` — no migration is created, so
`check-pending-model` is not required and the canonical smoke dump stays valid.

**Out of scope.** Fail-closed UX enforcement (Phase 2). Deleting or retiring unknown DB codes (stays
an operator decision). Relaxing any write validation. Any schema object, including a trigram index.

---

### Phase 2 — Fail closed, and isolate catalogue failure *(frontend; security)*

**Goal.** A catalogue failure or an unready catalogue can never cause data loss, and degrades only
the permission region.

**Backend work.** None — Phase 1 supplies `assignmentReady`.

**Frontend work.**

1. **Fail closed on write readiness.** Replace the placeholder notion of "catalogue loaded" with the
   real signal: `canAssignPermissions = computed(() => assignmentReady() && !catalogueError())`.
   Gate `canSelectPermissions()` / `canReplaceSelectedPermissions()`
   (`access-admin.facade.ts:489-497`) on it. When false:
   - the editor renders **read-only/disabled**, showing current grants for inspection;
   - **no Save and no Accept-with-permissions** is presented;
   - an explicit Arabic message states that permission assignment is temporarily unavailable and
     that existing access is unchanged;
   - lifecycle actions (accept-without-permissions, disable, reactivate) remain available per §1.3
     rule 5.
2. **Close the silent revoke-all.** Two independent guards, because one is not enough:
   - `setSelectedPermissionCodes` (`:226-231`) must filter by `isPermissionCode`, **not** through
     `permissionCodesForSubmission(catalogueState(), …)` — otherwise an empty catalogue eats the
     draft on every keystroke;
   - `replaceSelectedPermissions` / `acceptSelectedUser` must **refuse to submit** when
     `canAssignPermissions()` is false, rather than sending whatever the projection produced.
   Neither guard alone prevents the `[]` PUT; both are required.
3. **Restructure the detail card's `@if` chain** (`access-admin-page.component.html:31-57`) so
   `selectedUser()` renders first and only the permission `<section>` degrades on `catalogueError()`.
   The audit card at `:91-92` already demonstrates the correct scoping.
4. **Make the catalogue error recoverable** — pass `actionLabel` + `action` to `qd-state` (the
   primitive already supports both) wired to `loadPermissionCatalogue`, and include the catalogue in
   `refreshAfterMutation` (`:476-480`) so readiness is re-evaluated after every mutation.
5. **Region-scoped states** — adopt `qd-state [reserve]` sized from `--qd-control-block-size` /
   `--qd-pagination-slot-block-size` so load/error transitions stop resizing both panes. Add the
   missing reconciliation loading signal instead of using "data is null" as a proxy.
6. **Route `mutationMessage` by severity** — the 409 recovery message
   («تغيرت بيانات المستخدم…», `:453`) must not render as an error — and add a success message
   (`runMutation` currently never sets one). Remove the unreachable `clearProtectedState` write.
7. **Fix `canAccess()`** to consult `loadState` / `authStateKnown` so a token renewal cannot flash
   the permission-denied error.

**Tests.**

- Catalogue **request** failure isolates only the permission region; identity, badges and lifecycle
  actions survive (new cases in `access-admin-page.component.spec.ts`, forking `renderPage()` to
  flush 500 for `/permissions` only).
- Retry recovery (page spec + `access-admin.facade.spec.ts`).
- **Fail-closed, `assignmentReady: false`:** editor read-only; no Save control; accept offers no
  permission payload; **`httpTesting.expectNone` on any PUT to `/users/*/permissions`** — the
  security case.
- **No empty-replacement regression:** with a failed catalogue and a previously non-empty grant set,
  no request body containing `permissionCodes: []` can be produced by any UI path.
- Readiness recovering from false → true re-enables Save without a page reload.

**Docs.** `features/access-admin/README.md` — the failure model in §1.3, the fail-closed rule, and
catalogue-failure isolation.

**Files.** `access-admin.facade.ts`, `access-admin-page.component.html/.ts`,
`access-user-workflows.component.*`, the specs.

**Acceptance gates.** `npm run test:feature:access-admin`.

**Depends on:** Phase 1 (`assignmentReady`).

**Out of scope.** Layout, decomposition, dirty state, audit changes.

---

### Phase 3 — Permission workflow: dirty, revert, no-op, unsaved protection

**Goal.** Editing permissions reads as a workflow with an honest Cancel.

**Backend work.** None.

**Frontend work.**

1. `isDirty = computed(...)` derived from the **existing** `permissionDiff` (`:105-112`) so dirty
   means "the request body would differ", not "a set changed". `discardDraft()` restores the draft
   from `selectedPermissionsState` — identical to `:214`.
2. Live diff summary (`+N / −M`) beside the section heading, bound to that same computed.
3. Sticky Save/Cancel bar, visible only when dirty **and** `canAssignPermissions()`; **Cancel
   genuinely reverts**. Today `cancelAction()` (`access-user-workflows.component.ts:72-77`) resets
   only `pendingAction` and `actionReason`, leaving the checkboxes dirty with nothing indicating it.
4. **No-op saves stay blocked in the UI** — confirm is disabled on an empty diff. The backend already
   short-circuits with zero audit rows and no version bump
   (`EfAccessUserMutationService.cs:265-268`), so this prevents wasted round-trips, not audit
   pollution. *(Locked decision — do not revisit.)*
5. **Mandatory reason is unchanged** — every permission-set change still requires a trimmed
   1..1024-character reason. *(Locked decision — do not relax.)*
6. **Switch-user protection** — switching users is *not* a route change, so the route guard cannot
   cover it. `selectUser` opens the existing **`qd-confirm-dialog`** when `isDirty()`; confirm →
   `discardDraft()` then switch. Wording mirrors the existing `abwab.labels.ts:152-154` pattern in a
   **new access-admin labels file** — do not import across features.
7. **Route/navigation protection is required** — functional
   `CanDeactivateFn<AccessAdminPageComponent>` on the single route (Angular 20.3.24 supports it;
   `app.routes.spec.ts:20` `GUARD_KEYS` excludes `canDeactivate`, so no route spec breaks). It must
   read the **component instance** — the facade is component-provided
   (`access-admin-page.component.ts:20`), so `inject(AccessAdminFacade)` in a guard is wrong. Make
   `hasUnsavedChanges()` public.
   **`window.confirm` is accepted here** *(locked decision)*: reusing `qd-confirm-dialog` from a
   route guard would require hoisting dialog state out of the component for disproportionate gain.
   The in-page path (item 6) uses the real dialog, which is where the interaction actually happens.
8. **Relink vs. draft** — `refreshAfterMutation` resets the draft after *every* mutation including
   relink-confirm, which does not touch permissions. Phase 4 moves relink out of the workspace
   entirely, which removes the adjacency; until then gate the relink entry point on `isDirty()`.
   Do **not** fork the refresh — the 409 reset is asserted by `facade.spec.ts:206-241` and is correct.

**Tests.** Dirty state visible; Cancel/Revert restores and issues no HTTP; no-op save blocked (button
state + `expectNone`); unsaved-change guard in a **new `access-admin-unsaved-changes.guard.spec.ts`
kept inside the feature folder** so the existing lane picks it up with no gate edit; switch-user
dialog appears when dirty and discards only on confirm; group select-all/indeterminate (jsdom
supports `.indeterminate`).

**Docs.** `features/access-admin/README.md` — dirty state, Cancel/Revert, no-op prevention, and the
two protection mechanisms.

**Acceptance gates.** `npm run test:feature:access-admin`.

**Depends on:** Phase 2.

---

### Phase 4 — User-state semantics; relink moves to Advanced Security

**Goal.** Each status explains itself; Owner is honest; relink leaves the routine flow.

**Backend work.** **None. The Owner relink capability is unchanged** — no new Owner guard, no status
guard, no change to its signed-evidence or reconciliation semantics. *(Locked decision.)*

**Frontend work.**

- **Pending** — the editor renders but the commit is `accept` (the backend rejects a PUT for a
  non-Active user). State that selected permissions are granted on activation and label the button
  accordingly. When `assignmentReady()` is false, accept proceeds **without** a permission payload
  and says so.
- **Active non-Owner** — unchanged behaviour with Phase 3's affordances. Separate the affirmative
  save from «تعطيل الحساب», which currently shares a flat flex row with no danger treatment.
- **Disabled** — no editor (correct: the backend rejects a replace). Say so, and state that
  reactivation restores nothing.
- **Owner** — **no 19 checked/disabled boxes.** A concise read-only statement that an Active Owner
  receives all administrative access through Owner bypass, and that Owner membership is managed by
  reconciliation, not here.
- **Relink → Advanced Security.** Move the relink panel out of the selected-user workspace into a
  clearly secondary **Advanced Security** tab. It must not appear as part of routine permission
  editing. Present it as identity recovery, with its existing signed-evidence and confirmation
  requirements intact.

> **The product-copy contradiction is resolved by placement, not by code.** The backend genuinely
> permits relinking an Owner: `ConfirmCoreAsync` (`EfLogtoSubjectRelinkService.cs:57-95`) has no
> Owner guard and no status guard, and `ValidateBindingAsync:137-139` branches toward
> `ValidateOwnerConfigurationAsync`, which allows the relink when the Owner's email is configured and
> reconciliation reports `Unchanged` (`:142-158`). Today's «view-only» notice sits directly above
> that live form. **The fix is to stop implying Owner accounts are entirely uneditable in the
> permission context, and to relocate relink to Advanced Security** — the copy there describes an
> identity-recovery operation that legitimately applies to Owners. No backend security semantics
> change.

**Tests.** Disabled behaviour (`access-user-workflows.component.spec.ts:190-196`, keep/extend); Owner
behaviour (`:198-221`, keep, extended to assert the bypass statement and the absence of an editor);
Pending acceptance **with non-empty selected permissions** (extend the `accept` row at
`access-admin-page.component.spec.ts:278-352`, which currently sends `permissionCodes: []`); relink
is **not** reachable from the permission workspace.

**Docs.** `features/access-admin/README.md` — per-status behaviour, the Owner statement, and the
Advanced Security placement.

**Acceptance gates.** `npm run test:feature:access-admin`.

**Depends on:** Phase 3.

---

### Phase 5 — Natural user discovery *(backend + frontend, small)*

**Goal.** Find users by name or email. No IDs, no 400 on a partial token.

**Backend work — two files, no migration, no index.**

1. `ListAccessUsersHandler.cs:38-47` — replace `TryNormalizeSearch` with a free-text normalizer:
   trim; empty → `null` (no filter, no 400); reject only above a length cap (128) so validation stays
   fail-safe. Drop the now-unused `IEmailIdentityNormalizer` ctor parameter (`:8`) — leave the
   interface and its five other consumers alone; no DI edit needed
   (`DependencyInjection.cs:163` registers the handler with no explicit args).
2. `EfAccessUserReader.cs:27-30` — substring match:
   ```csharp
   var term = search.ToUpperInvariant();
   users = users.Where(user =>
       user.NormalizedEmail.Contains(term)
       || user.DisplayName != null && user.DisplayName.ToUpper().Contains(term));
   ```
   `Contains` translates to `strpos(...) > 0` — a literal substring test with no `%`/`_`/`\` escaping
   to get wrong, unlike `EF.Functions.ILike`. Matching `NormalizedEmail` (already
   `Email.ToUpperInvariant()`) gives case-insensitive email search with no second predicate. Do not
   add `UserName` — the list response does not expose it, so a hit there would be unexplainable.
   Verify the emitted SQL once with query logging during implementation.

**No-migration verdict — confirmed.** The only insert path into `users` is an interactive Logto
sign-in (`UserProvisioningService.cs:74`), so the table holds one row per human who has signed in; a
sequential scan is the right plan, and the existing unique btree on `normalized_email` could not
serve an unanchored `%term%` predicate anyway. A trigram GIN index would be the only index that
helps and `pg_trgm` is already installed (`ModelSnapshot.cs:23`), but an index is a schema object and
is not required for correctness at this size — it stays out.

**Frontend work.** The search box already sends free text and submits on form submit
(`access-user-list.component.html:12`, `.component.ts:61-71`) — only the label/placeholder changes to
«الاسم أو البريد». Separately fix the degrade at `access-user-list.component.html:46`:
`user.displayName || user.email` renders a blank label for a whitespace-only Logto name — use a
trim-aware fallback everywhere a name is shown.

**Tests.** Partial-name and partial-email matches return results; a non-email token no longer 400s;
the existing exact-email assertion at `AccessAdministrationEndpointTests.cs:501-506` still passes
under substring matching; blank search still means "no filter".

**Docs.** `features/access-admin/README.md` search behaviour; check
`Backend/api/QuranDashboard.Api/Controllers/README.md:82`.

**Acceptance gates.** `Backend/scripts/test-backend access-db --no-build`, then
`npm run test:feature:access-admin`.

**Out of scope.** Any index or schema object. Ranking/relevance ordering.

---

### Phase 6 — Page redesign *(the expensive phase)*

**Goal.** The polished desktop-first master/detail workspace.

**Visual language — locked.** Use the **currently shipped** dashboard visual system and the existing
`qd-*` tokens and primitives. Do not restore an older palette. Do not introduce a new palette or
design system. No new design token, colour, or primitive is created in this phase.

**Backend work.** None.

**Frontend work.**

1. **Page frame** — `.qd-page` → `.qd-container.qd-page-frame`, following
   `abwab-page.component.html:2`. Drop the bespoke root rule: today
   `.access-admin-page { display:grid; padding:… }` overrides `.qd-page-frame` at higher specificity
   and the `padding` shorthand wipes its deliberate `padding-block-end` reserve, so the shared class
   contributes only `width:100%; max-width:none`.
2. **Layout** — flex split with a sticky ~20rem aside and `flex:1; min-inline-size:0` main, modelled
   on `abwab-page.component.scss:22-52`; single column at `bp.$qd-bp-tablet-max`. **Replace the
   non-canonical `@media (max-width: 56rem)`** (`:114`) — the stylesheet never `@use`s
   `_breakpoints.scss`, and 56rem ≈ 896px sits inside the tablet band.
3. **Workspace chrome** — `.explorer-detail-panel` + `.explorer-panel-header` +
   `.explorer-detail-panel__body` as the panel's only scroller, giving the primary work surface a
   real visible heading (today it has only `aria-label`).
4. **Permission grid** — responsive 2–3 columns ≥1024px, 1 column on tablet. Shorten the select-all
   label to «تحديد الكل» (the current full sentence repeats 5× and out-shouts the legend). Move raw
   `abwab.*` codes to `[title]`; **keep them in the confirmation diff**, where a stable identifier
   earns its place.
5. **Selection** — compose `.qd-is-selected` rather than the hand-rolled rule, and reserve the 2px
   green thread with compensating padding so selection causes **no 1px shift**. Add `.qd-truncate`
   plus `[title]` on name/email. Give the ARIA list real items (today `<div role="list">` wraps bare
   `<button>`s).
6. **Component decomposition** — the 9-node tree in §1.2, including the Advanced Security tab.
7. **Eyebrow cleanup** — five accent-coloured eyebrows currently carry five unrelated meanings, one
   of them a verbatim repeat of the page title.

**Tests.** Budget the **full rewrite** of `access-admin-page.component.spec.ts` (549 lines) and
`access-user-workflows.component.spec.ts` (235 lines). These specs locate elements by `data-testid`
and by raw internal selectors (`.access-user-workflows__header .qd-badge`,
`.access-admin-page__audit-filters`), so a restructure fails them for reasons unrelated to
correctness while the states a redesign most likely breaks stay unasserted. **Rewriting them toward
behaviour is part of this phase, not a follow-up.** Add RTL structure cases (jsdom can read `dir`).

**Docs.** `features/access-admin/README.md` — layout, primitives used, and the no-deep-link decision
(`FRONTEND_STRUCTURE.md:357` requires the explanation).

**Acceptance gates.** `npm run test:feature:access-admin`, `npm run test:composition`.

**Depends on:** Phases 2–4 (behaviour settled before markup is rebuilt, so the specs are rewritten
once rather than twice).

**Out of scope.** Audit/reconciliation content changes (Phase 7).

---

### Phase 7 — Audit and reconciliation: demote and humanize *(completes the no-IDs rule)*

**Goal.** No technical identifier is visible in normal UI; audit becomes secondary and legible.

**Backend work — one contract addition, no schema change.**

`EfAccessAuditReader.ListAsync:55` — `.Include(...)` the existing `ActorUser`/`TargetUser` FK
navigations (already mapped with real FKs and `DeleteBehavior.Restrict`,
`AccessAuditEventConfiguration.cs:103-111`) and project four new nullable fields
(`TargetDisplayName`, `TargetEmail`, `ActorDisplayName`, `ActorEmail`) in `ToItem:95` onto
`AccessAuditEventItem` (`AccessAuditContracts.cs:16-29`). Two LEFT JOINs on primary keys.

*Source names from the FK navigations, not the jsonb snapshots* — the snapshots come in three
incompatible shapes across two casings, one without an `email` field at all, and the generated TS
types them as `{}`.

**Keep the id fields in the payload** (the filter round-trip needs them); just stop rendering them.

**Frontend work.**

1. **Delete both numeric-ID filters** (`access-admin-page.component.html:70-73`) and `positiveUserId`
   (`.component.ts:125-128`). Replace with one presentational `qd-access-user-picker` used twice
   (target, actor) that reuses the **existing** `AccessAdminApi.listUsers({ search, page: 1, pageSize: 10 })`,
   renders each candidate as `displayName?.trim() || email`, and emits the summary object. The page
   reads `.id` off it and passes it to `updateAuditQuery`. **The integer is constructed in TS, sent
   as a query param, and never bound into a template or a route.**
2. **Stop rendering IDs in audit rows** (`:100, :102`); render the new human names, with «النظام» for
   `actorType === 'System'` (the label already exists at `.component.ts:94-96`).
3. **Humanize `actionType`** into Arabic labels and turn its **free-text** filter into a dropdown
   (the permission filter beside it already is one). Map `candidate.state` to Arabic — the panel
   already maps `isReady`/`canApply`, proving the omission is an oversight.
4. **Format timestamps** to local time — the page component currently imports no `DatePipe` and no
   `CommonModule`, so formatting is not even possible today.
5. **Hide the 64-char SHA-256 fingerprint** behind an advanced/diagnostic affordance, or truncate
   with a copy action.
6. **`canApply` is diagnostic only** *(locked decision)*. **No apply endpoint is added in this
   feature.** The UI must not imply an in-product Apply action exists — no button, no call to action,
   no wording suggesting the Owner can trigger reconciliation. Present it as read-only status.
7. **Demote** audit and reconciliation behind `qd-tabs` with `.qd-card--quiet`. Replace the raw
   `<ol>` with `.qd-detail-list__*` rows.

**Tests.** **Rewrite** `access-admin-page.component.spec.ts:510-513` and `:515-526` (the latter
asserts the literal string `'معرّف المستخدم المنفّذ: 9'`); assert no numeric-ID text and no numeric
filter inputs remain; assert the reconciliation panel offers no apply affordance; extend
`access-admin.routes.spec.ts:5-12` to assert no route carries a `:param`. **Keep
`access-user-workflows.component.spec.ts:84-100`** — permission *codes* stay visible in the diff; do
not over-apply the no-IDs rule.

**Docs.** `features/access-admin/README.md:22-24` currently promises "actor ID attribution" — update
it, and record that `canApply` is diagnostic.

**Acceptance gates.** `Backend/scripts/check-api-contract`,
`Backend/scripts/test-backend access-db --no-build`, `npm run test:feature:access-admin`.

**Depends on:** Phase 5 (the picker reuses its search) and Phase 6 (tabs and layout).

---

### Phase 8 — Catalogue duplication and gate hardening

**Goal.** One source of truth for permission codes; gates that fail on drift.

**Backend work.** Optionally add the Arabic group label to `PermissionDefinition` so the UI never
hardcodes الأبواب / الأقسام / … (today `Group` holds the English `"Doors"`, `"Sections"`, …). Contract
change → swagger regeneration.

**Frontend work.** Remove the hand-duplicated code allowlist in `core/auth/permission-code.ts`
(generate it from the backend, or derive `PermissionCode` from the generated
`PermissionCatalogueItem`). At minimum, stop dropping unknown server codes silently — today three
filters remove them and the confirmation diff **understates what the save does**.

> **Note:** the silent drop is currently *enshrined as intended* by a passing test
> (`access-admin-permissions.spec.ts:68-72`). That test must change; it is not merely an uncovered
> path. `permission-code.ts` has **no spec file at all**.

**Gate hardening.** `scripts/check-permission-catalogue.mjs` has a real vacuous-pass hole — both
sides empty → 0 codes → PASS. Add a floor assertion, and ideally cross-check
`AbwabPermissionCatalogue.cs` (it currently reads `AbwabPermissions.cs`, so it pins the constants
list, never the catalogue definitions and never the database). **Write the gate so that adding or
retiring a permission is a deliberate, visible change — not so that the count is frozen at 19.**

**Acceptance gates.** `npm run test:pre-pr`, `npm run test:gates`.

**Depends on:** Phase 1.

---

## 3. Cross-cutting invariant — technical IDs stay hidden

Enforced in every phase, verified in Phases 6 and 7:

- **No technical/database user ID appears anywhere in normal dashboard UI** — not in labels, cards,
  tables, filters, audit rows, confirmations, diffs, or normal diagnostics.
- **Owners select users by human identity** — display name and email, via the list and the picker.
- **Numeric IDs may exist only internally**, in TypeScript state and API query parameters.
- **No technical user ID in a visible URL.** `?tab=` is the only query state; there is no user
  deep-link (§1.2).
- The `xmin` version powering optimistic concurrency stays internal — it is currently rendered as
  «الإصدار {{ version }}» in the user header and must be removed from view while remaining in state.
- Permission **codes** are not user IDs and remain visible in the confirmation diff by design.
- The only permitted exception is an explicitly advanced diagnostic affordance (e.g. the
  reconciliation fingerprint), never a default view.

---

## 4. Phase order and dependencies

```
Phase 1 (backend availability + safe read + readiness) ─┬─► Phase 2 ─► Phase 3 ─► Phase 4 ─► Phase 6 ─┬─► Phase 7
                                                        └─────────────────────────────────► Phase 8   │
Phase 5 (search) ──────────────────────────────────────────────────────────────────────────────────────┘
```

**Recommended order:** 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8.

Phase 2 now **depends on** Phase 1 (it consumes `assignmentReady`); the two can no longer run in
parallel. Phase 5 may run any time after Phase 1 and is a prerequisite for Phase 7. Behaviour (2–4)
deliberately precedes the redesign (6) so the page specs are rewritten **once**.

---

## 5. Final verification gates

**Backend** — build once, then in order:
`test-backend access-db --no-build` → `access --no-build` → `smoke --no-build` → `tier-b --no-build`.
Add `check-api-contract` for **Phases 1, 7 and 8** (the three contract changes). Do **not** run
`migration`, `pipeline`, or `canonical-data` — no migration is created.

**Frontend** — `npm run test:feature:access-admin`, `npm run test:composition`,
`npm run test:gates` (report separately; it is not in `test:pre-pr`), then `npm run test:pre-pr` once
at the review/PR boundary.

**Manual** — with a freshly migrated, never-synced database: boot the API, confirm `/api/health`
returns 200 with a `permission_catalogue` entry; then with startup sync **disabled**, confirm the
workspace loads, the editor is read-only, and **no Save is offered**; then with sync enabled, confirm
a permission set saves and the audit row shows a human name.

**Formal review** — `engineering-review` at the end of each phase; `test-guard` whenever specs change.

---

## 6. Locked product decisions

All previously open clarifications are now decided. None blocks any phase.

| Decision | Locked outcome |
|---|---|
| **Catalogue contract** | `GET /api/access/permissions` returns the canonical active/non-retired catalogue **safely**. 19 is today's size, pinned by tests, **not** a permanent API invariant |
| **Write readiness** | Public/read functionality stays available; the permission editor **fails closed** when writes cannot resolve canonical non-retired rows. No Save/Accept-with-permissions until readiness returns. Never an empty replacement set or accidental revoke-all. Existing write-path DB validation is **not** weakened |
| **Owner relink** | Backend capability **unchanged** — no new Owner guard, no change to signed-evidence or reconciliation semantics. Treated as advanced identity recovery and **moved to a secondary Advanced Security area**, out of routine permission editing |
| **Visual language** | The **currently shipped** dashboard visual system and existing `qd-*` tokens/primitives. No older palette restored, no new palette or design system |
| **Reason capture** | Mandatory trimmed **1..1024** characters for every permission-set change. Not relaxed |
| **`canApply`** | **Diagnostic only.** No apply endpoint in this feature; the UI must not imply an in-product Apply action exists |
| **Unsaved changes** | Drafts are protected. Same-page user switching uses the existing **`qd-confirm-dialog`**; route/navigation protection is **required**, and `window.confirm` is **accepted** for the `CanDeactivate` guard |
| **No-op saves** | Blocked in the UI |
| **Technical IDs** | Hidden everywhere in normal UI, including URLs (§3) |

---

## 7. Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | **Startup sync flakes the existing suite.** Every `WebApplicationFactory` boots the real `Program.cs`; `PermissionCatalogueSynchronizerTests:22` asserts *it* made the inserts | The opt-out flag applied to exactly four factories (Phase 1). Still the most likely way Phase 1 goes red |
| R2 | **A deploy precedes its migration** — migrations are manual with no release hook | Non-fatal policy + pending-migrations pre-check + Degraded health + `assignmentReady: false` so the editor fails closed rather than offering broken saves |
| R3 | **Health check registered as Unhealthy** would make the app permanently undeployable (`railway.json:8` gates on `/api/health`; `HealthController` 503s on Unhealthy) | Explicitly `failureStatus: HealthStatus.Degraded`; covered by a test |
| R4 | **Multi-instance boot race** — the synchronizer is read-then-insert behind a unique index | Blocking `pg_advisory_xact_lock`. *Whether Railway runs >1 replica is UNCONFIRMED* — the lock makes it moot |
| R5 | **Fail-closed is mistaken for a broken page.** An Owner sees a read-only editor with no Save and no obvious cause | Explicit Arabic message stating assignment is temporarily unavailable and existing access is unchanged; retry affordance; health check for the operator |
| R6 | **Phase 1 contract change** desynchronizes the committed OpenAPI, and the thin frontend adapter must land in the same change or the app breaks | `check-api-contract` is a Phase 1 acceptance gate; the adapter is explicitly in Phase 1 scope |
| R7 | **Spec churn in Phase 6** — 780+ lines of markup-coupled specs | Budgeted inside Phase 6; behaviour phases deliberately land first |
| R8 | Substring search returns unexpected hits as the table grows | Table holds one row per signed-in human; trigram GIN index available later as a standalone change |
| R9 | Relink moved to a secondary tab becomes hard to find during a real identity incident | Advanced Security is a first-class tab, not a hidden menu; document the recovery path in the feature README |

**Explicitly not a risk:** the smoke Owner case (Phase 1) is **not** proof the startup sync ran —
`SmokeApiFixture.ResetAsync` does not truncate `permissions` and `SeedAuthorizationPersonasAsync`
syncs explicitly, so a sibling class may have populated the table. The load-bearing proof lives in
`PermissionCatalogueStartupSyncTests`, which owns a fresh migrations-only clone per case.

---

## 8. Verdict

**READY_FOR_IMPLEMENTATION**

All eight phases are unblocked. The clarifications that previously gated Phases 4 and 6 are locked in
§6, and no new blocker surfaced while applying these amendments.

Phase 1 is fully specified: verified insertion point, failure policy, readiness computation, test
placement, and the fixture opt-out list. Phase 2 now consumes a real signal rather than inferring
readiness from an empty list.

**Confirmed no schema migration is required anywhere in this plan.** The `permissions` table, its
unique index and its check constraint already exist; the audit FK navigations are already mapped; the
readiness flag is computed from rows that already exist; and the search change needs no index at this
table size.

**Security invariants preserved throughout:** the write path
(`ResolvePermissionsAsync:318-352`) is untouched; Owner bypass, Owner-target guards and the mutation
transaction's failure precedence are unchanged; Owner relink semantics are unchanged; no new role; no
read permissions; authorization authority stays in the database, not Logto. The plan *strengthens*
fail-safety by closing the path where an unready catalogue could silently revoke every grant.
