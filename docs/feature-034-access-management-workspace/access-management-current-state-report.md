# Access Management — current-state report

Read-only audit of the Owner-only access administration page (`/settings/access`) against the
locked product behaviour. **No code was modified.** This is a report, not a plan.

Scope: `Backend/api/QuranDashboard.Api/Controllers/Access/`, the Access application/infrastructure
layers, and `Frontend/quran-dashboard-ui/src/app/features/access-admin/`.

Every claim below carries a `file:LINE` citation. Where a fact could not be established from code
or tests it is marked **UNCONFIRMED** rather than asserted.

---

## 1. Functional current state

### 1.1 Headline

The feature is **not missing and not a shell**. Every capability in the locked contract is
implemented end to end — 13 HTTP routes, a typed frontend boundary consuming 12 of them, a working
permission editor with per-group select-all and indeterminate state, mandatory reason capture, and
optimistic concurrency on every write.

What is broken is narrower and more specific: **the permission catalogue table is never populated
in a normally-deployed environment, and the resulting server error is rendered in a way that
destroys the entire user workspace.** That single defect explains both symptoms in the screenshots
— the `حدث خطأ غير متوقع` box and the absence of a usable permission editor.

### 1.2 Owner capabilities that actually exist

| Capability | Exists | Where |
|---|---|---|
| List users with status/owner/email filters + paging | Yes | `AccessUsersController.cs:22-39` |
| Read one user's detail | Yes | `AccessUsersController.cs:41` |
| Accept (activate) a Pending user, optionally with initial grants | Yes | `AccessUsersController.cs:50` |
| Disable an Active non-Owner | Yes | `AccessUsersController.cs:62` |
| Reactivate a Disabled non-Owner | Yes | `AccessUsersController.cs:74` |
| Read a user's current direct permissions | Yes | `AccessUserPermissionsController.cs:15` |
| Replace a user's permission set (full-state PUT) | Yes | `AccessUserPermissionsController.cs:24` |
| Read the permission catalogue | Yes | `AccessPermissionsController.cs:12` |
| Read the access audit log (keyset-paged) | Yes | `AccessAuditEventsController.cs:12` |
| Preview + confirm a Logto subject relink | Yes | `AccessLogtoSubjectRelinkController.cs:15,27` |
| Read owner-reconciliation status | Yes | `AccessOwnerReconciliationController.cs:12` |

All 12 admin routes carry class-level `[RequireOwner]`; `GET /api/access/me` is `[Authorize]` only.
Every response uses the `ApiResponse<T>` envelope (`Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs:3-25`).
The route set is independently enumerated by `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:241-292`.

### 1.3 Backend contracts, by question

**Listing users.** `GET /api/access/users?status&isOwner&search&page&pageSize`. Ordering is fixed
(`UpdatedAtUtc DESC, Id DESC`) and not client-controllable
(`EfAccessUserReader.cs:40-41`). `pageSize` defaults to 25, `[Range]` 1..100.

> **Contract trap:** `search` is an **exact normalized-email equality match**, not a search. The
> value must parse as a valid single-`@` `MailAddress` (`EmailIdentityNormalizer.cs:8-27`) and is
> compared with `==` (`EfAccessUserReader.cs:27-30`). A partial string, a display-name fragment, or
> `search=` (present but blank) returns **400**, not an empty result set
> (`ListAccessUsersHandler.cs:38-47`).
>
> *Correction to a plausible-sounding overstatement:* the current UI does **not** 400 on every
> keystroke. The input only updates a local signal (`access-user-list.component.ts:49-51`); the
> request fires on form submit, trims, and omits an empty value (`:61-71`), and
> `access-admin.api.ts:110-113` also refuses to set a blank `search`. The blank-`search` 400 is
> therefore **unreachable from this UI**. What *is* live: submitting any partial token or
> display-name fragment returns 400 — a control labelled «البحث» that only accepts a complete valid
> email address.

**Changing user status.** Three POST verbs — `/accept`, `/disable`, `/reactivate`. No generic status
setter, no PATCH. All three require a mandatory `expectedVersion` (PostgreSQL `xmin`,
`UserConfiguration.cs:37-38`) and a mandatory reason trimmed to 1..1024 chars
(`AccessAdministrationValidation.cs:9-13`).

**Reading permissions.** `GET .../permissions` → `AccessUserPermissions(userId, status, isOwner,
version, permissionCodes[])` (`AccessUserContracts.cs:51-56`). It carries `version` (usable as the
next `expectedVersion`) and `isOwner`. It does **not** carry grant provenance — `GrantedByUserId`
and `GrantedAtUtc` are stored (`EfAccessUserMutationService.cs:296-301`) but never projected — and
there is no explicit owner-bypass flag. The mapper returns `[]` both for an Owner and for any
non-Active user (`AccessUserContractMapper.cs:47-48`), so "Owner who bypasses everything" and
"Active user with zero grants" are wire-indistinguishable except via the separate `isOwner` boolean.

**Granting/revoking.** It is a **full REPLACE**, not a delta. `PUT .../permissions` carries the
complete desired set; `permissionCodes: []` is the only revoke-all path
(`AccessUserPermissionsController.cs:24-38`). There is no POST-grant, no DELETE-revoke, no per-code
route. The internal revoke/grant delta exists purely to shape audit rows
(`EfAccessUserMutationService.cs:246-264`).

`ReplaceUserPermissionsHandler` is a thin validator (userId ≥ 1, non-null codes, reason 1..1024)
that delegates to `EfAccessUserMutationService.ReplacePermissionsCoreAsync`, which runs inside
`AccessUserMutationTransaction.ExecuteAsync`: `SELECT … FOR UPDATE` on actor + target ordered by id,
re-verify the actor is still an Active Owner, 404 on missing target, 409 on `xmin` mismatch, reject
Owner targets and non-Active targets, resolve codes against `AbwabPermissionCatalogue` **and**
non-retired DB rows, then write rows + one audit row per change in one transaction
(`AccessUserMutationTransaction.cs:18-86`, `EfAccessUserMutationService.cs:230-316`).

Three contract details worth carrying into any redesign:

- **A no-op replace is a silent 200.** An identical desired set short-circuits at
  `EfAccessUserMutationService.cs:265-268` before touching `UpdatedAtUtc` — no audit row, no version
  bump, and the supplied reason is discarded (asserted at `AccessAdministrationEndpointTests.cs:158-169`).
  The audit log therefore records permission *state changes*, never permission *decisions*.
- **`permissionCodes: null` means opposite things on two endpoints.** On accept it is coerced to an
  empty list and succeeds; on replace it is a 400 (`AccessAdministrationBodies.cs:5,14`;
  `ReplaceUserPermissionsHandler.cs:15`). The replace controller passes `body.PermissionCodes!` with
  a null-forgiving operator (`AccessUserPermissionsController.cs:34`).
- **`expectedVersion` is a non-nullable `uint`.** Omitting it deserializes to `0`, which is never a
  real `xmin`, so a caller who forgot the field gets **409 "the record changed underneath you"**
  rather than a 400 (`AccessAdministrationBodies.cs:3-4`). *Likely, not confirmed — inferred from
  System.Text.Json default-value behaviour plus the absence of `[Required]`.*

### 1.4 Frontend consumers

`data-access/access-admin.api.ts` is the typed HTTP boundary; all 12 of its methods map 1:1 onto the
routes above (`access-admin.api.ts:32,38,42,46,50,54,58,62,69,75,85,95`). No frontend call lacks a
backend and no admin endpoint is orphaned — `GET /api/access/me` is consumed separately by
`core/auth/access.api.ts:20` on behalf of `CurrentUserStore`.

`state/access-admin.facade.ts` (543 lines) owns orchestration: four parallel loads on open, two on
user selection, monotonic request-version guards against stale responses, 409 → refresh-never-retry,
401/403 → the shared `WriteAuthFailureCoordinator`.

Components: `access-user-list` (search/filter/page/select), `access-user-workflows` (identity header,
status actions, reason+diff confirmation, relink), and `access-permission-editor` (the 5 grouped
fieldsets). Generated payload models come from a real, drift-gated pipeline —
`Backend/scripts/export-swagger` → committed `openapi/swagger.json` → `ng-openapi-gen` →
models-only prune, gated by `Backend/scripts/check-api-contract`.

### 1.5 Is permission assignment implemented, hidden, broken, or missing?

**Implemented and wired — then made unreachable at runtime by a data-provisioning gap.**

The editor is genuinely rendered: `access-user-workflows.component.html:23-28` instantiates
`<qd-access-permission-editor>` as the **first** content block after the user header. The common
assumption that it is buried below the relink form is **not borne out** — the relink section sits
*below* it in every branch.

It renders only when `canSelectPermissions()` is true — non-Owner **and** status `pending` or
`active` (`access-user-workflows.component.ts:55-58`).

**Positive proof, not merely absence of a defect.** A rendered-component test checks the real
`access-permission-abwab.doors.edit` checkbox, drives the two-step confirm, and asserts the outgoing
PUT body — `{expectedVersion: 4, permissionCodes: ['abwab.doors.create','abwab.doors.edit'], reason}`
against `/users/17/permissions` — then asserts the refreshed selection
(`access-admin-page.component.spec.ts:354-392`). Permission assignment is reachable and complete end
to end for an Active non-Owner **whenever the catalogue loads.**

### 1.6 Why selecting a user does not present a usable permission editor

Two independent causes, both confirmed:

**(a) The catalogue error pre-empts everything.** The detail card's `@if` chain evaluates
`catalogueError()` at position 2, *before* `selectedUser()` at position 4
(`access-admin-page.component.html:31-57`). When `GET /api/access/permissions` fails, the whole
`qd-card--feature` becomes a single `qd-state` error box. Destroyed in that swap: the user's
name/email header, the owner/status badges, the accept/disable/reactivate buttons, and the entire
relink section — **none of which read the catalogue**. The user list stays clickable and selection
still fires its two requests, but branch 2 always wins, so the operator never sees the result.

The same file demonstrates the correct pattern one section down: the audit card scopes its error to
just the `<ol>` while keeping its filters and header alive (`access-admin-page.component.html:91-92`
vs `:69-88`).

**(b) The error is terminal for the session.** `catalogueErrorState` is cleared in exactly one place —
the top of `loadPermissionCatalogue`, which is called only from `load()`, which is called only from
`ngOnInit` (`access-admin.facade.ts:162,168,122`). Selecting another user, paging, filtering, and
every post-mutation refresh all leave it standing; `refreshAfterMutation` deliberately does not
re-fetch the catalogue (`:476-480`). Only a full page reload recovers. And `qd-state` exposes
`actionLabel`/`action` precisely for a retry button, but the page passes only `[message]`
(`access-admin-page.component.html:34`) — so no retry affordance is rendered.

### 1.7 The `حدث خطأ غير متوقع` state — root cause

**It is a backend string, not a frontend one.** `ApiMessages.UnexpectedError`
(`Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:11`) is the sole producer in the entire repo.

The chain is unbroken and every link is confirmed:

1. `EfPermissionCatalogueReader.GetActiveAsync` throws `InvalidOperationException` when the
   non-retired `AccessPermissions` rows do not exactly match `AbwabPermissionCatalogue.All` in
   count, code, Arabic label, English description, or display order
   (`EfPermissionCatalogueReader.cs:15-22`).
2. `GetPermissionCatalogueHandler` is a one-line pass-through with no try/catch
   (`GetPermissionCatalogueHandler.cs:7-8`).
3. `AccessPermissionsController` calls `Ok(...)` directly instead of the failure mapper, so it has
   **no 4xx path from the handler at all** — 200 or 500 only
   (`AccessPermissionsController.cs:16-20`). The authorization layer still yields 401/403, and the
   `InfrastructureUnavailable` 503, *before* the action runs; what is missing is any way for the
   handler itself to report a typed failure.
4. `GlobalExceptionHandler` special-cases only `UserProvisioningEmailConflictException`; everything
   else becomes 500 + `ApiMessages.UnexpectedError`
   (`GlobalExceptionHandler.cs:24-40, :51, :54`). It is wired by
   `services.AddExceptionHandler<GlobalExceptionHandler>()`
   (`ServiceCollectionExtensions.cs:62-63`) plus `app.UseExceptionHandler()`
   (`WebApplicationExtensions.cs:10`) — **both** are load-bearing; without the registration
   `UseExceptionHandler` alone yields a bodyless 500 and the frontend would show its own fallback
   «تعذر تحميل بيانات إدارة الوصول.» instead.
5. Angular's `messageFrom()` returns `response.message` verbatim and `qd-state` interpolates it
   unchanged (`access-admin.facade.ts:531-537`; `shared/ui/state/state.component.html:22`). No
   interceptor rewrites it.

**Why the table is empty.** This is the pivotal finding:

- **No migration seeds `permissions`.** `AddAuthorizationAccessFoundation` creates the table, its PK,
  a code-format check constraint and two indexes, with zero `InsertData` calls
  (`20260805121524_AddAuthorizationAccessFoundation.cs:67-83, :151-160`).
  `RemoveLegacyAdminEditorRoles` touches only `roles` (`20260807080835_…cs:15-23`).
- **`PermissionCatalogueSynchronizer` is registered in DI but never invoked by production code.**
  `AccessDependencyInjection.cs:35` is the only registration. Greps for
  `SynchronizeAsync|IPermissionCatalogueSynchronizer` across `api/ application/ infrastructure/
  domain/` return exactly three hits — interface, implementation, DI line — and **zero call sites**.
  Greps for `AddHostedService|IHostedService|IStartupFilter|BackgroundService` across the same tree
  return nothing. `Program.cs` is 15 lines with no startup hook.
- **The only caller is the manual CLI**, documented as a hand-run operator step in the deploy
  sequence (`Backend/tools/QuranDashboard.AccessAdmin/Program.cs:106-117, :301`;
  `Backend/tools/QuranDashboard.AccessAdmin/README.md:47`).
- **The repo's own DB scripts never sync.** `reset-db` delegates to `drop-db` + `update-db`
  (`reset-db:12-13`), and `update-db` runs only `dotnet ef database update` (`:14-17`). A database
  built with this project's own tooling has 0 of 19 rows.
- **The deployed image cannot run the documented remedy.** `Backend/railway.json:1-12` declares
  `"builder": "DOCKERFILE"`, and `Backend/Dockerfile:27-30` publishes **only**
  `QuranDashboard.Api`; `:33-35` copies just `/app/publish` into the runtime stage; `:44` is
  `ENTRYPOINT ["dotnet", "QuranDashboard.Api.dll"]`. There is no migrate step, no sync step, and
  **the `AccessAdmin` CLI is not present in the production image at all.**

So a database built from `dotnet ef database update` alone has **0 rows against a catalogue of 19**,
and `0 != 19` trivially fails the reader's first guard.

**Corroboration from the test fixture.** `AccessTestFixture.ResetAsync` truncates `users,
permissions` and does not re-seed (`AccessTestFixture.cs:111-112`), so every test that exercises the
catalogue calls a private `SynchronizePermissionsAsync()` helper immediately afterwards — eight call
sites (`AccessAdministrationEndpointTests.cs:637-641`; `:18,79,128,192,329,382,485,546`), and the
catalogue GET assertion runs only after it (`:522-526`). `SmokeApiFixture` does the same (`:157-158`).
**The test fixture performs precisely the step production omits.**

*Precision:* this is not universal across the suite. `AccessMeEndpointTests`,
`AccessAuditEventPersistenceTests` and `AccessCollectionResetContractTests` all reset **without**
syncing and pass — which is itself useful evidence that `/me` and the audit surface genuinely do not
depend on the catalogue.

**Ruling out competing causes.** `/me` contains no reference to the catalogue, so a catalogue
mismatch cannot break it; conversely, if `/me` itself fails, `canAccess()` goes false and the page
renders `لا تملك صلاحية إدارة الوصول.` — a *different* string — and issues no loads at all
(`current-user.store.ts:37-38`; `access-admin-page.component.html:10-11`). The users list, audit and
reconciliation errors each render into their own card, not the detail card. And `[RequireOwner]`
gates the endpoint before the handler runs, so a non-Owner gets 403 — only an actual Owner reaches
the throw, which matches the reported symptom exactly.

**One gap in that elimination, stated honestly.** `selectedUserError` renders into the *identical
slot* of the same detail card (`access-admin-page.component.html:35-36`) and is also set from the
server message. A 500 from `GET /users/{id}` or `GET /users/{id}/permissions` would produce the same
string in the same place. This is a gap in the elimination rather than a competing cause of equal
weight — `GetUserPermissionsHandler.cs:11-19` returns typed failures rather than throwing, and
`EfAccessUserReader` never references the catalogue, so no throw path is identified there. **The
distinguishing observation is whether the error appears before any user is selected:** the catalogue
branch fires on load with nothing selected, `selectedUserError` cannot.

**Reproducibility.** The *mechanism* is provable from code inspection alone (confirmed). The
*precondition* — the actual row state of the deployed database — is not observable under a read-only
audit, so it is marked **likely**, not confirmed.

> **Operator check requiring no DB access:** if only the detail card errors while the user list,
> audit log and reconciliation panel all render normally, it is the catalogue. If all four error
> together, it is connectivity or auth infrastructure.

**Nothing guards this.** The frontend `check-permission-catalogue.mjs` compares only code *strings*
between `AbwabPermissions.cs` and `permission-code.ts` — never the database
(`scripts/check-permission-catalogue.mjs`, 27 lines). `AccessSchemaDriftTests` asserts DDL only,
never row content (`:31-77`). `EfPermissionCatalogueReader` is named in **zero** test files; its
*happy path* is exercised end-to-end at `AccessAdministrationEndpointTests.cs:522-526`, but **only
after an explicit sync**, so the throw branch — the production failure mode — is entirely uncovered.
The sole DB-parity check is the manual `access-admin authorization preflight` command.

> **The one gate designed to catch exactly this bug explicitly excludes this route.**
> `SmokeRoutePipelineTests` sweeps every catalogued route against a migrated-but-**empty** schema and
> asserts `StatusCode.Should().NotBe(HttpStatusCode.InternalServerError)` (`:38-40`) — precisely this
> class of bug against precisely this DB state. But its theory data filters
> `Where(route => !route.ParityOnly)` (`:16`), and the `api/access/permissions` entry is marked
> **`ParityOnly = true`** (`SmokeRouteCatalog.cs:265-268`). The route is visible to the parity gate
> (route registration and access classification only) and **invisible to the 500 sweep**. The
> Owner-only smoke test that does dispatch it asserts 401 for anonymous and 403 for a direct-grant
> caller — never a 200 path (`SmokeAccessAdministrationAuthorizationTests.cs:22-32`).

The catalogue can therefore be empty with **every automated gate green**.

---

## 2. Contract comparison

| Expected capability | Classification | Note |
|---|---|---|
| Every signed-in user in the local Users table is manageable | **Implemented and wired** | No default filter; all statuses and Owners returned (`EfAccessUserReader.cs:14`) |
| Inspect users and their status | **Implemented and wired** | List + detail; status badges at `access-user-workflows.component.html:9-14` |
| Activate Pending users | **Implemented and wired** | `accept` also seeds initial grants |
| Disable Active non-Owners | **Implemented and wired** | Revokes every grant as part of the transaction |
| Reactivate Disabled non-Owners | **Implemented and wired** | Restores nothing by design |
| View current direct permissions | **Implemented and wired** | But blanked by the catalogue defect (§1.6) |
| Grant and revoke exact permissions | **Partially implemented** | Backend and editor are complete; **unreachable at runtime** until the catalogue is seeded, and the save flow has no dirty state or working cancel (§5) |
| Permission groups as UI-only Select All | **Implemented and wired** | Group-local, indeterminate handled; requests carry flattened known codes only, never a group sentinel (`access-admin-permissions.ts:52-66, 86-93`) |
| Save the final individual permission set | **Implemented and wired** | Full-replacement PUT with `expectedVersion` + reason |
| Groups are not roles, not persisted as group grants | **Implemented and wired** | Backend rejects group-like codes such as `abwab.doors` (`EfAccessUserMutationService.cs:318-352`, tested at `AccessAdministrationEndpointTests.cs:466-505`) |
| Active Owner bypass remains central | **Implemented and wired** | Single `state.IsOwner \|\|` disjunct (`PermissionAuthorizationHandler.cs:33-37`) |
| Owner membership not granted from this page | **Implemented and wired** | No HTTP route mutates Owner membership; reconciliation *apply* is CLI-only |
| Public/read access unrelated to permissions | **Implemented and wired** | Read routes carry no permission metadata; covered by `SmokePublicReadRegressionTests` |
| Non-Owners have no admin role | **Implemented and wired** | Legacy Admin/Editor roles deleted by migration `20260807080835` |

Two gaps that are **backend-exists-but-frontend-missing** in a weaker sense:

- **Grant provenance** (`GrantedByUserId`, `GrantedAtUtc`) is persisted but never projected into the
  contract, so the UI cannot show "granted by X on date Y" without a backend change.
- **`OwnerReconciliationStatus.canApply`** is served to the UI over HTTP, but no HTTP endpoint can
  act on it — *apply* is CLI-only (`IOwnerReconciliationService.cs:8-14`). An Owner reading
  `canApply: true` has no in-product action available.

---

## 3. Permission catalogue

### 3.1 Canonical source

`Backend/application/QuranDashboard.Application.Abstractions/Security/Permissions/AbwabPermissionCatalogue.cs`
is the single compiled source of truth: 19 `PermissionDefinition` entries in 5 groups, each built
from an `AbwabPermissions.*` constant. Its static constructor self-validates code format, uniqueness,
`DisplayOrder` = exactly 1..19, and `GroupDisplayOrder` = exactly the set 1..5, throwing if violated.

The `permissions` DB table is **not** an independent source — it is a projection that
`EfPermissionCatalogueReader` cross-checks against the compiled list, with one genuine extra
authority: `RetiredAtUtc`. A code the compiled catalogue knows but the DB has retired is rejected on
write (400).

### 3.2 The 19 assignable Abwab permissions

| Group (`groupKey`) | Arabic group | Code | Arabic label |
|---|---|---|---|
| `doors` | الأبواب | `abwab.doors.create` | إنشاء الأبواب |
| | | `abwab.doors.edit` | تعديل الأبواب |
| | | `abwab.doors.move` | نقل الأبواب |
| | | `abwab.doors.reorder` | إعادة ترتيب الأبواب |
| | | `abwab.doors.archive` | أرشفة الأبواب |
| | | `abwab.doors.restore` | استعادة الأبواب |
| `sections` | الأقسام | `abwab.sections.create` | إنشاء الأقسام |
| | | `abwab.sections.edit` | إعادة تسمية الأقسام |
| | | `abwab.sections.reorder` | إعادة ترتيب الأقسام |
| | | `abwab.sections.delete` | حذف الأقسام |
| `relations` | العلاقات | `abwab.relations.create` | إنشاء العلاقات |
| | | `abwab.relations.delete` | حذف العلاقات |
| `templates` | القوالب | `abwab.templates.create` | إنشاء القوالب |
| | | `abwab.templates.delete` | حذف القوالب |
| | | `abwab.templates.apply` | تطبيق القوالب على الأبواب |
| `template_nodes` | عناصر القوالب | `abwab.template_nodes.create` | إضافة عناصر القوالب |
| | | `abwab.template_nodes.edit` | تعديل عناصر القوالب |
| | | `abwab.template_nodes.reorder` | إعادة ترتيب عناصر القوالب |
| | | `abwab.template_nodes.delete` | حذف عناصر القوالب |

The server's `groupLabel` values come from `PermissionDefinition.Group` — currently the **English**
strings `"Doors"`, `"Sections"`, `"Relations"`, `"Templates"`, `"Template nodes"`
(`AbwabPermissionCatalogue.cs:7-25`). `groupKey` is derived as `code.Split('.')[1]`
(`EfPermissionCatalogueReader.cs:41`). **The Arabic group names in the table above are the target UX
naming, not values the API returns today** — this is a real gap (§6, §7).

No new permissions are proposed.

### 3.3 Can the frontend obtain it dynamically?

**Yes — and it does, for labels and grouping. But a hardcoded allowlist gates it.**

`GET /api/access/permissions` returns `PermissionCatalogueItem(code, arabicLabel, englishDescription,
groupKey, groupLabel, groupDisplayOrder, displayOrder)`, and `buildPermissionGroups` builds the UI
purely from that payload (`access-admin-permissions.ts:11-50`).

However `core/auth/permission-code.ts:1-53` **hand-duplicates all 19 code strings**, and
`buildPermissionGroups` silently `continue`s past any catalogue item failing `isPermissionCode`
(`access-admin-permissions.ts:25-27`).

This creates a **latent** correctness hazard — mechanism confirmed, trigger not currently reachable:

> **An unknown server permission code would be silently revoked on save and would not appear in the
> diff.** Three filters drop it — `buildPermissionGroups` never renders it,
> `permissionCodesForSubmission` can never include it, and `knownCodes()` strips it from the server
> snapshot that feeds *both* the selection seed and the `current` side of `permissionDiff`
> (`access-admin-permissions.ts:25-27, 86-93, 103-105`; `access-admin.facade.ts:527-529, 106, 214`).
> Because the PUT is a full replacement, opening such a user and saving *any* change would revoke
> that grant — and the confirmation diff would not list it under «الصلاحيات الملغاة», because it was
> filtered out of `current` before the comparison. The Owner would confirm a diff that understates
> what the save does.
>
> **Not a live defect today.** A user cannot legitimately hold such a grant: the backend rejects any
> code outside `AbwabPermissionCatalogue` on write (`EfAccessUserMutationService.cs:334-339`), and
> `check-permission-catalogue.mjs:21` gates backend codes against `permission-code.ts` pre-PR.
> Reaching this state requires an out-of-band DB insert or a bypassed gate. It is recorded because
> the *failure mode is silent* — the safest fix is to stop dropping unknown codes, not to rely on
> both guards holding forever.

**A drift concern that does *not* apply, stated to prevent a wasted fix.** The other catalogue
fields — `arabicLabel`, `groupKey`, `groupLabel`, `groupDisplayOrder`, `displayOrder` — have nothing
to drift against. `EfPermissionCatalogueReader.cs:24-32` builds every served field from the compiled
`AbwabPermissionCatalogue` definitions; the DB rows are only a consistency check (`:15-22`) and never
reach the wire. The frontend keeps no local copy, reading them straight off the response
(`access-admin-permissions.ts:30-35`). **Only the codes are duplicated frontend-side, and those are
exactly what the gate already covers.**

### 3.4 Safest source-of-truth recommendation

Keep `AbwabPermissionCatalogue.All` as the single authority and make everything else derive from it:

1. **Serve the catalogue from the compiled list, and stop letting a DB mismatch throw.** The reader's
   parity check is valuable, but it belongs in a *startup/preflight* assertion or a test, not on the
   read path of a UI-facing GET. The endpoint should return the definitions with retirement applied,
   and report drift as a typed, actionable failure rather than a bare 500.
2. **Delete the duplicated `PERMISSION_CODES` literal.** Either generate `permission-code.ts` from
   the backend as part of the existing OpenAPI codegen pipeline, or derive the `PermissionCode` type
   from the generated `PermissionCatalogueItem` and drop the runtime allowlist entirely. Filtering
   server truth through a hand-maintained client allowlist is what makes §3.3's silent-revoke
   possible.
3. **If the allowlist must stay**, at minimum stop dropping unknown codes silently: preserve them in
   `current` for diff purposes and surface them as an explicit warning.
4. **Move the group's Arabic label into the backend definition** so the UI never hardcodes Arabic
   group names (§6 needs الأبواب / الأقسام / العلاقات / القوالب / عناصر القوالب).

---

## 4. User eligibility

**No security rule below is proposed for change. This is a description of what the code does.**

### 4.1 Statuses and Owner-ness

`UserStatus` has exactly three members: `Pending = 1`, `Active = 2`, `Disabled = 3`
(`Backend/domain/QuranDashboard.Domain/Access/UserStatus.cs:5-10`). **Owner is not a status.** It is
an orthogonal nullable `User.RoleId` FK to a seeded `roles` row named `Owner`
(`User.cs:13-15`; `RoleConfiguration.cs:26-28`); `IsOwner(user)` is `user.Role?.Name == "Owner"`
(`AccessUserContractMapper.cs:8`). Owner membership is granted only by reconciliation against
`OwnerBootstrap:Emails` — never from this page.

### 4.2 Which users appear

**All of them.** `EfAccessUserReader.ListAsync` starts from the unfiltered `db.AccessUsers` set and
applies `status`, `isOwner` and `search` only when the caller supplies them
(`EfAccessUserReader.cs:14-30`). With no query parameters, Pending, Active, Disabled and Owner rows
are all returned.

A user row exists **only after that person's own first sign-in.** The only code in the backend that
inserts a `User` is `UserProvisioningService.CreateAsync` (`:60-74`), reached solely from
`GET /api/access/me`. There is no invite path and no admin-create path anywhere, including the CLI.
Provisioning hard-fails if Logto returns no primary email (`:32-37`).

> **A Pending user has a second exit besides `accept`.** `OwnerReconciliationStore.ApplyAsync`
> writes **`user.Status = roleChange.Status`**, not only `RoleId`
> (`OwnerReconciliationStore.cs:145`), and `OwnerReconciliationService` emits
> `Status = UserStatus.Active` for every interactive-promotion addition (`:222`). The trigger is a
> plain authenticated sign-in: `UserProvisioningService.cs:23-26, :40-43` call
> `ReconcileInteractiveSignInAsync` whenever the user's normalized email is in
> `OwnerBootstrap:Emails`, reached only from `GET /api/access/me` — which is `[Authorize]`, **not**
> `[RequireOwner]`. So a configured, email-verified Pending user becomes an **Active Owner** with no
> Owner actor, no `expectedVersion`, and no `accept` call. This is the intended bootstrap path, but
> it means "Pending → accept" is not the whole story, and the Access Management page is not the only
> way a user leaves Pending.

> Note: `permissionCount` in the list response is forced to `0` for Owners **and for any non-Active
> user** (`EfAccessUserReader.cs:50-52`), so a Disabled user with residual grants still reports 0.

### 4.3 Allowed actions per target

Every mutation runs through `AccessUserMutationTransaction.ExecuteAsync` with a fixed failure
precedence: **actor-not-Active-Owner (403) → target-not-found (404) → stale version (409) →
per-action guard** (`AccessUserMutationTransaction.cs:21-46`). A stale `expectedVersion` therefore
beats both `TargetIsOwner` and `InvalidTransition`.

| Target | Allowed | Blocked | Failure |
|---|---|---|---|
| **Pending** (non-Owner) | `accept` (requires zero existing grants); accept may carry initial `permissionCodes` | disable, reactivate, **replace-permissions** | 400 `InvalidTransition` — «انتقال حالة المستخدم غير صالح» |
| **Active** (non-Owner) | `disable`, `replace-permissions` | accept, reactivate | 400 `InvalidTransition` |
| **Disabled** (non-Owner) | `reactivate` (requires zero grants, guaranteed by disable) | accept, disable, replace-permissions | 400 `InvalidTransition` |
| **Owner** (any status) | **relink only**, and only when configured + reconciliation-`Unchanged` (see below) | accept, disable, reactivate, replace-permissions | 400 `TargetIsOwner` — «لا يمكن تنفيذ هذه العملية على مالك» |
| **Any status** (Owner or not) | `relink/confirm` — **no status guard** | — | — |

Guards: `EfAccessUserMutationService.cs:75-83` (accept), `:147-155` (disable), `:203-211`
(reactivate), `:236-244` (replace). The Owner check is the **first statement** in each callback,
ahead of any status check — verified end-to-end at `AccessAdministrationEndpointTests.cs:491-499`
(BadRequest + message + zero audit rows).

> **The table above covers four mutations. There is a fifth, and it obeys neither rule.**
> `POST .../logto-sub/relink/confirm` runs through the *same* `AccessUserMutationTransaction`
> (`EfLogtoSubjectRelinkService.cs:47-54`) but `ConfirmCoreAsync` (`:57-95`) contains **no
> `IsOwner(target)` rejection and no status guard at all** — it goes straight from the `OldSub`
> check to evidence validation and never reads `target.Status`.
>
> - **Owner targets are conditionally *permitted*, not blocked.** `ValidateBindingAsync` ends with
>   `IsOwner(target) ? await ValidateOwnerConfigurationAsync(...) : null`
>   (`EfLogtoSubjectRelinkService.cs:137-139`), and that path returns `null` — i.e. **allows** the
>   relink — when the Owner's normalized email is in `OwnerBootstrap:Emails` **and** reconciliation
>   reports that candidate as `Unchanged` (`:142-158`).
> - **Any status can be relinked** — Pending, Active and Disabled alike.
> - **Self-targeting is reachable, not merely unguarded.** There is no `actor.Id == target.Id` check
>   anywhere in the backend (§4.3 below). For the four guarded mutations that is harmless because
>   the Owner-target guard catches the self case. Relink has no such guard, so an Active Owner can
>   pass their own `userId` and rewrite their own `LogtoSub` (`:80`), subject only to the
>   reconciliation-`Unchanged` check.
>
> This is a deliberate design — relinking an Owner's identity provider subject is exactly the
> recovery operation the flow exists for, and it is gated by signed ID-token evidence plus
> reconciliation state. It is recorded here because it **contradicts the UI copy** (§5.9, §6.4) and
> because any statement of the form "Owners cannot be mutated from this page" is false as written.

**A Pending user cannot have permissions set via PUT.** The replace guard requires `Status == Active`;
initial grants are delivered only through `accept`. This matters for §6 — the editor is rendered for
Pending users, and the codes are submitted with the accept action (`access-admin.facade.ts:243`),
but the button says only "قبول وتفعيل".

**Disable revokes everything; reactivate restores nothing.** Disable snapshots the grants, emits one
`PermissionRevoked` per grant plus `UserDisabled`, then hard-deletes the rows
(`EfAccessUserMutationService.cs:157-195`). Reactivate performs no restoration
(`:198-227`). Confirmed at `AccessAdministrationEndpointTests.cs:76-122`.

**Self-targeting.** There is **no** `actor.Id == target.Id` check anywhere in the backend. For the
four guarded mutations self-disable is blocked only *incidentally*: the actor must be an Active
Owner, so `IsOwner(target)` is necessarily true when target == actor, and the Owner-target guard
catches it. **That reasoning does not extend to relink**, which has no Owner guard — see the callout
above. The last active Owner additionally cannot be removed by reconciliation
(`RemovalBlockedByLastOwner`, `OwnerReconciliationService.cs:156-165`), and the CLI's legacy-role
conversion cannot touch an Owner (`LegacyRoleConversionStore.cs:90-93` throws unless every locked
user's role is exactly `Admin` or `Editor`).

### 4.4 Caller-side eligibility

`AuthorizationStateAccessEvaluator` is shared by the Owner and Permission handlers and records
first-wins (`AuthorizationFailureState.cs:9`, `Reason ??= reason`). Consequently a Pending or
Disabled caller — **including a Pending/Disabled Owner** — hitting any `[RequireOwner]` endpoint
receives `AccessInactive` («حسابك غير نشط», 403), not `AccessOwnerRequired`
(`AuthorizationStateAccessEvaluator.cs:79-83`). An Active non-Owner gets
«يتطلب هذا المورد صلاحية المالك».

**Active Owner bypass** is a single disjunct: `state.IsOwner || state.PermissionCodes.Contains(...)`
(`PermissionAuthorizationHandler.cs:33-37`). An Owner's `PermissionCodes` is projected empty at three
independent layers — the authorization SQL projection, the API contract mapper, and the provisioning
projection — so the `Contains` branch is dead for Owners and the UI sees `isOwner: true` with
`permissionCodes: []`.

---

## 5. UI/UX audit

The current design is not acceptable, and the reasons are concrete.

### 5.1 Structure as built

Four top-level `qd-card` blocks in a 2×2 slab: user list + detail on the first row, audit log +
owner reconciliation on the second (`access-admin-page.component.html:14,30,62,118`).

### 5.2 Poor use of desktop width

**There is no width constraint anywhere in the chain.** The app shell renders the route directly into
`<main>` with no `qd-container` wrapper (`app-shell.component.html:6-8`); `.qd-page-frame` explicitly
sets `max-width: none` (`_layout.scss:49-52`); the page's own root rule declares none
(`access-admin-page.component.scss:1-5`).

Worse, `.qd-page-frame` is applied but **entirely overridden**: the component's own
`.access-admin-page { display: grid; gap: …; padding: … }` beats the global single-class rule on
specificity under Angular's emulated encapsulation, and the `padding` shorthand wipes both
`padding-inline` and the deliberate `padding-block-end` reserve. The shared class contributes only
`width: 100%; max-width: none`. The markup claims to opt into the shared frame while opting out of
its behaviour — a future change to `.qd-page-frame` will silently not reach this page.

Inside, nothing reflows: `access-permission-editor.component.scss` has **no `max-width`, no
multi-column grid, and no media query**. All 19 checkboxes render as one tall single-column stack
whose labels sit against a very long empty measure. Five groups of ~4 items would fit comfortably in
2–3 columns.

### 5.3 Excessive empty space

Both grid rows declare `align-items: start` (`:36-39`, `:48-51`), so neither column stretches to its
sibling's height.

- **First load:** the left card renders up to 25 users + filters + pagination; the right card renders
  exactly one thing — a centred `qd-state` empty message with 2rem padding
  (`_components.scss:561-572`). The detail card collapses to that height beside a very tall list,
  producing a full-width void down the primary column. **This is the dominant empty-space defect.**
- **Supporting row:** the mirror image — 25 audit events beside three `dl` rows.

### 5.4 Weak information hierarchy

Heading *levels* are valid (h1 → h2 → h3, no skips). The problems are semantic:

- **The page's primary work surface has no visible heading.** The detail card carries only
  `aria-label="تفاصيل المستخدم"` (`:30`). The only h2 inside it is a *person's name*, sitting at the
  same outline level as the three region names. Scanning headings yields
  «المستخدمون / أحمد / سجل الوصول / حالة مطابقة المالكين» — one of these is not like the others.
- **Five accent-coloured eyebrows carry five unrelated meanings.** The page eyebrow is «إدارة الأمان»
  above the h1 «إدارة الوصول»; the user-list card's eyebrow is «إدارة الوصول» — a verbatim repeat of
  the page title. «الحالة الحالية» is used as an eyebrow above a person's *name*, labelling the wrong
  thing (the status badges are on the opposite side of the same header).

### 5.5 Fragmented cards

Four top-level cards plus three more nested bordered levels inside the detail card:
`qd-card--feature` → `.access-user-workflows__section` → `<fieldset>` ×5.

**The fills make it worse.** `.access-user-workflows__section` sets `background: var(--qd-surface)` —
the *identical* token `.qd-card` uses. In light theme `--qd-shadow-sm` is `none` by contract, so the
card contributes no shadow either. Only two fill tokens are in play across the whole three-level
stack and they are ~1.5% lightness apart (`--qd-surface` 0.994 vs `--qd-section-bg` 0.979). The
result reads as a mesh of hairline rectangles rather than a hierarchy. `.qd-card--quiet` exists
precisely for recessive panels and is unused.

### 5.6 Audit and reconciliation dominate

Nothing demotes them:

- Both use the plain `qd-card` class, identical to the primary user list.
- **The read-only audit log is given more horizontal weight than the primary user list** — `1.4fr`
  versus `0.8fr` (`:49` vs `:37`). The widest column on the page belongs to a read-only log.
- Their `<h2>`s are styled identically to the user list's.
- Both load eagerly in the same `Promise.all` as the workspace; neither is collapsible or deferred.
- «تحميل المزيد» appends to the same in-page `<ol>` with no virtualization and no collapse, so the
  history can push the page to many screens while the administration surface stays pinned at the top.

The only demotion signal is the word «قراءة فقط» — text, not visual treatment.

### 5.7 Raw technical values shown directly

Ten distinct classes reach the screen:

1. **Permission code strings on all 19 rows** — `access-permission-editor.component.html:30`, placed
   flush under each Arabic label with no `row-gap`.
2. **Codes in the confirmation diff** — `:65, :75`. *The most defensible instance; a confirmation
   step is where a stable identifier earns its place.*
3. **Raw codes as the audit filter's dropdown options** — `access-admin-page.component.html:82`,
   while the enclosing `<optgroup>` one line earlier uses the Arabic label. Same `<select>`, two
   languages — and `permissionLabelFor` was already available.
4. **Raw code in each audit event** — `:104`, with no label pairing at all.
5. **A 64-character SHA-256 hex fingerprint** — `:131`, untruncated, no copy affordance, no
   `overflow-wrap`, inside a `space-between` flex row with no `min-inline-size: 0`. It will overflow.
6. **Bare numeric DB user IDs as display text** — `:100`, `:102` — an integer where a name belongs.
7. **Numeric user IDs as the only audit filter input** — `:71`, `:73`. To filter the log for a user
   the Owner must know that user's integer primary key; the user list is on the same screen and
   offers no "show this user's history" link.
8. **Untranslated `actionType` enum as the most prominent element of each audit row** — `:99`,
   styled as the only non-muted text. Ten English PascalCase tokens (`UserAccepted`,
   `PermissionGranted`, …) as the headline of an Arabic audit log. Worse, the matching filter at
   `:75` is a **free-text input** — the Owner must type these from memory, while the *permission*
   filter beside it is a dropdown.
9. **Untranslated `candidate.state` enum** — `:135`. Eight values, none mapped — while `isReady` and
   `canApply` *are* mapped to جاهزة/نعم two lines earlier, proving this is an oversight.
10. **Raw ISO UTC timestamps** — `:100`. No formatting is even possible in this template: the
    component's `imports` array contains no `DatePipe` and no `CommonModule`.

Also exposed, though arguably load-bearing: the `xmin` version number in the user header, and the
Logto subject identifiers in the relink preview.

*Not a live defect:* `actorType` is mapped for both of its two real members; the raw fallthrough is
latent, not reachable.

### 5.8 Unclear selected-user state

Selection is two CSS declarations plus one ARIA attribute:

- **The tint is barely visible.** `--qd-selected-bg` resolves to `--qd-accent-tint` — a ~4% lightness
  step in light theme, ~2% in dark. **In dark theme the two cues disagree on hue**: the tint is at
  hue 281 (bluish) while the accent border on the same element is hue 82 (gold).
- **It bypasses the design system's own treatment.** `.qd-is-selected` exists and is what
  `.qd-tabs__tab` and `.qd-chip` use; this component hand-rolls a different one.
- **1px layout shift on selection.** The base item has a 1px border; the selected rule overrides only
  the inline-start edge to 2px with no compensating padding, so selecting nudges content and can
  jitter the column.
- **No echo in the detail pane** beyond the user's own name, and no "محدَّد" marker on the row.
- On mobile, selection performs no scroll — with 25 rows above, the operator may see no change at all.
- A11y: the container is `<div role="list">` whose children are bare `<button>`s with no
  `role="listitem"`, so the ARIA list has zero items.

### 5.9 Absence of a clear permission workflow

Grouping and per-group select-all (including indeterminate) **work correctly**. What is missing:

- **No dirty state anywhere.** Ticking a box flips the checkbox and changes nothing else — no
  counter, no «غير محفوظ» marker, no modified-row highlight, no newly-enabled save control. There is
  no `isDirty` signal, no `CanDeactivate` guard and no `beforeunload` handler in the whole app.
- **The diff exists continuously but is hidden until you commit to acting.** `permissionDiff` is a
  `computed` recalculated on every change (`access-admin.facade.ts:105-112`), yet it renders only
  inside the confirmation panel. A live "+3 / −1" summary is one binding away and absent.
- **«إلغاء» is a false affordance.** `cancelAction()` resets only `pendingAction` and `actionReason`;
  the selection lives in the facade and is untouched. The panel closes and the checkboxes stay dirty,
  with nothing indicating it.
- **Switching users silently discards edits.** `selectUser` overwrites the selection from the server
  with no guard and no prompt — and it fires automatically after every successful mutation and every
  409.
- **The commit control is detached from the editor and shares a row with a destructive action.**
  «مراجعة تعديل الصلاحيات» sits beside «تعطيل الحساب» in a flat flex row, the latter styled only as
  `qd-btn-secondary` with no danger treatment.
- **No global select-all / clear-all** across the 19 codes.
- **For a Pending user the commit button doesn't say what it commits** — the editor renders, but the
  only button is «قبول وتفعيل». *Partial mitigation:* the confirmation step does render the diff for
  accept as well, so the Owner sees the grants before confirming.
- **The select-all label is a full sentence repeated 5×** — «تحديد جميع الصلاحيات في المجموعة»,
  bolded and accent-coloured, competing with the `<legend>` it sits under.
- **A no-op save is permitted.** The confirm button enables on a non-empty reason alone; the empty
  diff does not disable it.

*Reason capture is done well and deserves noting:* every mutation requires a non-empty reason before
Confirm enables, and relink additionally requires an explicit checkbox.

**One real ordering defect, and it is not the one usually assumed.** The relink `<section>` sits
*outside* the owner/non-owner branch (`access-user-workflows.component.html:95`, after the `@else`
closes at `:93`). So for an Owner the component renders «تُعرض عضوية المالك للمتابعة فقط ولا يمكن
تعديلها من هذه الواجهة» and then, immediately below it, **a live form that rebinds that Owner's
identity-provider subject.** The copy and the adjacent affordance contradict each other on the same
screen.

### 5.10 Error-state presentation

- The catalogue error destroys the entire detail region (§1.6) — the single most serious UI defect.
- **`mutationMessage` is rendered unconditionally as `variant="error"`** (`:38-40`), but the 409 path
  sets that same signal to the *informational* «تغيرت بيانات المستخدم. تم تحديث الحالة الحالية.»
  (`access-admin.facade.ts:453`). A successful recovery is painted red.
- **There is no success feedback at all.** `runMutation` nulls the message on entry and never sets a
  success string, so a completed accept/disable/replace is silent — the confirmation panel simply
  disappears.
- **The reconciliation panel has no loading signal**, so the template uses "data is null" as a proxy.
  An absent status and an in-flight request are indistinguishable.
- **Nothing is reserved.** `qd-state` is used without `[reserve]` at all ten call sites, so every
  load/error transition resizes both panes — against the app's own no-layout-shift doctrine.
- `canAccess()` conflates "identity not yet known" with "denied": `CurrentUserStore.startLoad()` nulls
  the user before `/me` returns, and the store refreshes on every `isAuthenticated$` emission, so a
  token renewal mid-session can flip the page to the hard «لا تملك صلاحية» error. The store exposes
  `loadState` and `authStateKnown` precisely to distinguish these; neither is consulted.

### 5.11 Responsive behaviour

**One media query in the entire feature**, at `@media (max-width: 56rem)` — a bespoke value outside
the shared scale (`_breakpoints.scss` defines 767/1023/1024/1440px, and the stylesheet never `@use`s
it). 56rem ≈ 896px sits inside the tablet band, so from ~897px to 1023px the page keeps its
two-column workspace, squeezing the detail column that must hold five fieldsets, a diff, a textarea
and the relink form.

The three component stylesheets contain **zero** `@media` rules. Rows that will overflow on phones:
the permission-diff `li` and the reconciliation `dl` row (both `space-between` flex with no
`min-inline-size: 0`, the latter carrying the 64-char fingerprint). The shared mobile overrides in
`_layout.scss:78-86` target `.qd-page` / `.qd-container`, neither of which this page uses, so it
keeps its 1.5rem padding down to the smallest viewport.

### 5.12 RTL

**Correct at the CSS layer.** All four stylesheets use logical properties exclusively — a grep for
`left`/`right` across them returns nothing. Flex alignment uses writing-mode-relative values.

**Inconsistent at the bidi-isolation layer.** The author demonstrably knows the pattern —
`actorUserId` is wrapped in `<bdi dir="ltr">` at `:102` — yet two lines earlier `:100` renders
«المستخدم {{ targetUserId }} · {{ occurredAtUtc }}» with **neither** the integer nor the ISO-8601
timestamp isolated, leaving the bidi algorithm to resolve a run of Arabic, a bare number, a neutral
`·`, and a string full of `-`, `:` and `T`. Two more of the same shape at
`access-user-workflows.component.html:7` and `access-admin-page.component.html:135`.

---

## 6. Proposed target UX

Desktop-first, Arabic RTL, built **only** from primitives that exist today.

> **Visual-language note — needs your decision.** The brief asks to keep "Navy/Gold/Parchment".
> `DESIGN.md` records that direction as **superseded for the light theme**: the shipped system is
> flat **parchment + one scholarly green**, navy demoted to footer-only, gold retired. Dark theme
> still runs interim navy+gold pending reconciliation. Since the brief also says "reuse existing
> `qd-*` primitives and tokens; do not propose a new visual system", this proposal is written against
> the **shipped green tokens**. See §9.

> **Primitives that do NOT exist — do not name them.** `.qd-table`, `.qd-toolbar`, `.qd-sidebar`,
> `.qd-btn-danger`, and a base `.qd-detail-list` are all named in documentation but absent from code.
> There is **no accordion and no segmented-control primitive**; `qd-tabs` is the only tablist.
> Button variants are single-dash (`.qd-btn-primary`); everything else is BEM double-dash
> (`.qd-card--feature`).

### 6.1 Page frame

```
.qd-page  →  .qd-container.qd-page-frame
  <header class="qd-page-header">
    <h1 class="qd-page-title">إدارة الوصول</h1>
    <p class="qd-text-muted">…one quiet line…</p>
  </header>
```

Drop the current bespoke root rule so the shared frame's padding and bottom reserve actually apply,
and follow `abwab-page.component.html:2`'s precedent of combining `qd-container` with
`qd-page-frame`. Remove the redundant «إدارة الأمان» eyebrow (it repeats the h1's meaning).

### 6.2 Master–detail split

Reuse the **abwab pattern** (`abwab-page.component.scss:22-52`) — the closest existing exemplar —
mirrored so the *list* is the aside and the *workspace* is the main column:

```
.access-admin__layout   display:flex; gap:var(--qd-space-4); align-items:flex-start
  ├── <aside>  inline-size: ~20rem; flex:none; position:sticky;
  │            top: calc(var(--qd-navbar-block-size) + var(--qd-space-4))
  │            → user search + filters + list + qd-pagination
  └── <section> flex:1; min-inline-size:0   → the selected-user workspace
```

At `≤ bp.$qd-bp-tablet-max` (the **canonical** breakpoint, not 56rem) go single-column and make the
aside static, exactly as abwab does.

Give the workspace the shipped detail chrome rather than a bare card: `.explorer-detail-panel` +
`.explorer-panel-header` (`__label` / `__entity` / `__end`) + `.explorer-detail-panel__body` as the
panel's only scroller. That yields a real visible header and a dedicated scroll region — both absent
today.

### 6.3 User list (aside)

- Search box, status filter, owner filter, `qd-result-count` for the "المستخدمون: N" stat (the
  primitive that exists for exactly this and holds its line).
- Rows compose **`.qd-is-selected`** rather than a hand-rolled rule, and reserve the 2px inline-start
  green thread with compensating padding so selection causes **no 1px shift**.
- `.qd-truncate` plus a `[title]` for name and email (§17 treats the missing `title` as a contract
  violation, not a style nit).
- Wrap rows in a real list structure so the ARIA list has items.
- **The search box must not bind directly to `?search=`** (§1.3) — see §7.

### 6.4 Selected-user workspace

**Identity header** (`.explorer-panel-header`): display name as the entity, email as an isolated LTR
island, status badge, and an owner badge when applicable. Move the `xmin` version out of the header —
it is machine state, not identity. Replace the «الحالة الحالية» eyebrow with a region label such as
«حساب المستخدم».

**Permissions as the primary section**, immediately below the header, grouped visually by:
الأبواب · الأقسام · العلاقات · القوالب · عناصر القوالب

- Groups laid out in a responsive multi-column grid (2–3 columns ≥1024px, 1 column on tablet) instead
  of today's single 19-row stack.
- Each group keeps `<fieldset>`/`<legend>` and a `qd-check-row` + `qd-checkbox` select-all with the
  indeterminate state preserved. **Shorten the label to «تحديد الكل»** — the current full sentence
  repeats five times and out-shouts the legend.
- Individual permissions stay independently toggleable. **Show the Arabic label only**; move the raw
  `abwab.*` code to a `[title]`/tooltip and keep it visible only in the confirmation diff, where a
  stable identifier earns its place.
- Add a global «تحديد الكل» / «مسح الكل» for the whole set.

**Unsaved-changes state — the biggest functional gap in the UX:**

- A live diff summary beside the section heading (`+N / −M`), bound to the existing `permissionDiff`
  computed — one binding, already computed on every change.
- Modified rows visually marked (without relying on colour alone, per `PRODUCT.md`).
- A **sticky action bar** at the bottom of `.explorer-detail-panel__body` that appears only when
  dirty: «حفظ التغييرات» (primary, the one primary action in the view) and «إلغاء» — where **Cancel
  genuinely reverts** the selection to the server snapshot.
- Guard against losing edits when switching users or navigating away.

**Current vs changed must be understandable.** Keep the existing reason + diff confirmation step,
which is genuinely good, and make it show three columns: current set, proposed set, and the delta.
The README records that the absence of a modal is deliberate — so keep it inline rather than
switching to `qd-confirm-dialog`, unless you want to re-litigate that decision (§9).

**Owner accounts.** Replace today's bare notice with an explicit read-only panel: show that the
Owner inherits **all 19 permissions through Active Owner bypass**, render the catalogue in the same
grouped layout but disabled and visibly "inherited", and state that Owner membership is managed by
reconciliation and not from this page.

The relink placement needs a **product decision, not just a CSS move** (§9). The backend genuinely
*does* permit relinking an Owner under strict conditions (§4.3), so the honest options are: (a) keep
relink available for Owners but replace the blanket «view-only» notice with copy that says exactly
what *is* editable, or (b) hide relink for Owners in the UI and accept that the recovery path becomes
CLI/operator-only. What is not acceptable is today's state, where a "view-only" notice sits directly
above a live form that rebinds that Owner's identity.

**Pending.** The editor renders, but the commit is `accept`. Make that explicit: a short line saying
the selected permissions will be granted on activation, and label the button «قبول وتفعيل مع الصلاحيات
المحددة» (or similar). Backend cannot accept a PUT for a Pending user, so no other framing is honest.

**Disabled.** No editor renders today, which is correct — the backend rejects a replace on a
non-Active user. Make the reason visible: state that a disabled account holds no direct permissions
and that reactivation starts from none, then offer «إعادة التفعيل» as the single action.

### 6.5 Secondary sections

Move the access audit and owner reconciliation **below the workspace, behind `qd-tabs`** — the only
tablist primitive — with `.qd-card--quiet` (transparent border, `--qd-section-bg`) so they read as
recessive rather than as peers. Do not invent an accordion.

- **Audit:** replace the raw `<ol>` with `.qd-detail-list__*` rows. Humanize `actionType` into Arabic
  labels and turn its free-text filter into a **dropdown** (matching the permission filter beside
  it). Format timestamps to local time via `DatePipe`. Replace the numeric user-ID filters with a
  user picker, and add a "show this user's history" affordance from the selected user — which is what
  an Owner actually wants.
- **Reconciliation:** map `candidate.state` to Arabic (the panel already maps `isReady`/`canApply`,
  proving the omission is an oversight). Truncate the 64-char fingerprint with a copy affordance, or
  hide it behind a details toggle. Since no HTTP endpoint can *apply* reconciliation, present
  `canApply` as diagnostic information, not as an implied action.

### 6.6 States

Adopt the app's reserved-slot doctrine: `qd-state` with `[reserve]`, sized from
`--qd-control-block-size` / `--qd-pagination-slot-block-size`, so load/error transitions stop
resizing both panes.

---

## 7. Architecture recommendation

The smallest coherent change set. Ordered by whether it is required for correctness.

### 7.1 Backend — one required change, no schema change

**Required: make the permission catalogue reachable in a normally-deployed environment.** Today it
depends on an operator remembering a CLI command, and failing that produces a bare 500. Three viable
options:

| Option | Effect | Assessment |
|---|---|---|
| **A.** Run `PermissionCatalogueSynchronizer` at startup (`IHostedService`) | Adds/updates the 19 canonical rows | Necessary but **not sufficient** — see the caveat below. No schema change; reuses tested code (`PermissionCatalogueSynchronizerTests`). |
| **B.** Seed the rows in a migration | Table populated on `database update` | Rejected: migration seed data goes stale the moment a label changes, re-creating the exact drift the reader detects. |
| **C.** Serve the catalogue from `AbwabPermissionCatalogue.All`, using the DB only for `RetiredAtUtc` | Read path cannot throw | **The one that actually closes the failure mode.** |

> **Caveat that makes A alone insufficient.** `PermissionCatalogueSynchronizer` only *computes*
> `unknownCodes` — it never deletes or retires them (`PermissionCatalogueSynchronizer.cs:55-59`).
> `EfPermissionCatalogueReader` counts every row with `RetiredAtUtc == null` (`:10-15`), so 19
> canonical rows **plus one leftover unknown row = 20 ≠ 19**, and the endpoint keeps returning 500
> *after* a successful sync. The CLI signals this by exiting non-zero
> (`Backend/tools/QuranDashboard.AccessAdmin/Program.cs:114-116`), which an operator reading only
> `catalogue_added=` will miss.

**Recommended: A + C, with C as the non-negotiable half.** A guarantees the rows exist without an
operator remembering a CLI command that isn't even in the production image; C guarantees a UI-facing
GET never returns a bare 500 for a data-provisioning reason, including the leftover-row case A
cannot fix. Keep the parity assertion — move it to startup/preflight or a test, where a mismatch is
actionable, rather than on the read path of a UI GET.

**Also worth fixing while in there (all small, none schema-affecting):**

- Give `AccessPermissionsController` and `AccessOwnerReconciliationController` a real failure path;
  today both bypass `ToActionResult` entirely, so the handler can only produce 200 or 500 (the
  authorization layer's 401/403/503 still apply ahead of it).
- Add the Arabic group label to `PermissionDefinition` so the frontend never hardcodes الأبواب /
  الأقسام / … .
- Consider projecting grant provenance (`GrantedByUserId`, `GrantedAtUtc`) — needed only if the UX
  should show "granted by X".
- Declare `[ProducesResponseType]` on the Access routes; only `AccessUsersController.List` has them
  today, so the generated client advertises just the inferred 200.

**Needs a decision, not a default:** `search` is an exact-email-equality contract, so a control
labelled «البحث» that rejects partial names is honest to the API but wrong for the user. Either
rename/reshape the control to "find by email address" (no backend change), or add real partial
matching server-side. Do not loosen the existing parameter silently — it is security-adjacent and
its exactness is deliberate.

### 7.2 Frontend API / model / state

- **Fix the guard chain** in the detail card: evaluate `selectedUser()` before `catalogueError()`,
  and scope the catalogue error to the permission-editor block only. This alone restores the
  identity header, status actions and relink for an Owner during a catalogue outage.
- **Make the catalogue error recoverable**: pass `actionLabel` + `action` to `qd-state` and re-run
  `loadPermissionCatalogue`, and include the catalogue in `refreshAfterMutation`.
- **Remove the duplicated code allowlist** (§3.4). At minimum, stop silently dropping unknown server
  codes from `current` — today that makes the confirmation diff understate the save.
- **Add dirty tracking**: an `isDirty` computed from the existing `permissionDiff`, a real revert on
  Cancel, and a guard against switching users / navigating away with unsaved edits.
- **Fix `canAccess()`** to consult `loadState` / `authStateKnown` so a token renewal cannot flash the
  permission-denied error.
- **Route `mutationMessage` by severity** — the 409 recovery message must not render as an error —
  and add a success message.
- **Add a loading signal for reconciliation** instead of using "data is null" as a proxy.
- **Reshape the user search** so it cannot 400 on a partial token: validate client-side and only
  issue `?search=` for a well-formed email, or relabel it explicitly as an email lookup. (Blank
  values are already handled correctly — the 400 risk is partial input, not empty input.)
- **Remove the dead write**: `clearProtectedState`'s `mutationMessage` set is unreachable in both
  directions.

### 7.3 UI component decomposition

Keep the existing four-component shape — it is sound — and split only where the workspace is doing
too much:

- `access-admin-page` — frame, layout, tabs, URL state.
- `access-user-list` — unchanged in role; compose `.qd-is-selected`, `.qd-truncate`, `qd-result-count`.
- **`access-user-identity-header`** *(new, extracted)* — `.explorer-panel-header` composition.
- **`access-permission-workspace`** *(new, extracted)* — hosts the editor, the live diff summary, and
  the sticky save/cancel bar. This is what makes permissions read as the primary section.
- `access-permission-editor` — unchanged logic; multi-column layout, shortened select-all label,
  codes moved to `[title]`.
- **`access-user-status-actions`** *(new, extracted)* — accept/disable/reactivate + reason confirm,
  visually separated from the affirmative save.
- **`access-subject-relink`** *(new, extracted)* — moved inside the non-Owner branch.
- `access-audit-panel` / `access-reconciliation-panel` *(new, extracted)* — behind `qd-tabs`,
  `.qd-card--quiet`.

### 7.4 Does the catalogue need a read endpoint?

**No — it already has one** (`GET /api/access/permissions`, `[RequireOwner]`). What it needs is to
stop 500-ing (§7.1) and to carry the Arabic group label. Note the `[RequireOwner]` gate is correct
for this page but means no non-Owner surface can read the catalogue.

### 7.5 Loading / error / saving states

Per-region, never page-wide: catalogue failure degrades only the permission block; user-detail
failure degrades only the workspace; audit/reconciliation failures stay inside their tabs. Use
`qd-state` with `[reserve]` everywhere. Saving disables the editor and shows progress on the save
button only.

### 7.6 URL state

Worth adding: `?user=<id>` for the selected user (deep-linkable, survives reload, and gives the
"show this user's audit history" link a target) and `?tab=audit|reconciliation` for the secondary
section. Do **not** put draft permission selections in the URL.

### 7.7 Tests that should protect the flow

The gaps that let this ship are as important as the defects:

**Current coverage, briefly.** Backend Access coverage is deep — 29 files / ~6,158 lines in
`Api/Access` plus 6 access-touching Smoke classes — with authorization heavily covered and the
catalogue read barely. Frontend: 8 specs / ~1,628 lines, mutation flows well covered, read/failure/
empty states not. `docs/TESTING_DEBT.md` records nothing about the catalogue.

**Backend**
- **Remove `ParityOnly = true` from the `api/access/permissions` catalog entry** (or add an
  equivalent 500-sweep case). This is the highest-value single change in the whole report: it
  re-enters the route into the gate that already exists and already runs against an empty schema.
- A test asserting `GET /api/access/permissions` returns 200 with **19** items **against a
  migrations-only database** — one that does *not* call the sync helper first. Today's single
  assertion (`AccessAdministrationEndpointTests.cs:522-527`) runs only after an explicit sync at
  `:485`, compares the response length to the same in-code `AbwabPermissionCatalogue.All` that
  produced it, and inspects only element 0 — it is self-referential, cannot fail on an
  unsynchronized table, and never pins 19 over HTTP.
- A test asserting the catalogue endpoint returns a typed failure, not a bare 500, on drift —
  including the **leftover-unknown-row** case the synchronizer does not clean up.
- If option A is taken: a startup test asserting the synchronizer ran.

**Frontend**
- The catalogue-failure branch: assert the identity header, status actions and relink **survive** a
  catalogue error, and that only the permission block degrades. No test references `catalogueError`
  today.
- Retry restores the editor after a transient catalogue failure.
- Dirty tracking: Cancel reverts; switching users with unsaved edits warns.
- An unknown server code is not silently dropped from the diff.
- Owner: editor disabled, inheritance explained, relink behaviour matching whichever option
  clarification 3 resolves to.
- Pending: selected codes travel with `accept`. Disabled: no editor, reactivate offered.
- The permission editor's **indeterminate** group state — asserted by the README, asserted by no test.
- Empty catalogue (`permissionGroups() === []`) and empty user list.
- `permission-code.ts` has **no spec file at all**. If the backend adds a 20th code the frontend
  silently renders 19 with no Vitest failure anywhere; only the pre-PR `check:permission-catalogue`
  node script would catch it. Note that script reads `AbwabPermissions.cs`, **not**
  `AbwabPermissionCatalogue.cs` — it pins the constants list, never the catalogue definitions and
  never the database.
- **The silent drop of unknown server codes is currently enshrined as intended behaviour** by a
  passing test (`access-admin-permissions.spec.ts:68-72`). If §3.4 is adopted, that test must change
  — it is not merely an uncovered path.

**Lane note.** `npm run test:feature:access-admin` globs exactly
`src/app/features/access-admin/**/*.spec.ts` (`angular.json:133-137`). It does **not** pull in
`core/auth/**` (that is the `authorization` lane) and does **not** run `check:permission-catalogue`,
which appears only in `test:pre-pr`.

**Redesign risk.** The page specs locate elements exclusively by `data-testid` and, in places, by raw
internal selectors (`.access-user-workflows__header .qd-badge`, `.access-admin-page__audit-filters`).
A markup restructure will fail them for reasons unrelated to correctness, while the states a redesign
is most likely to break remain unasserted — high false-positive churn, low true-positive sensitivity.
Rewriting these specs toward behaviour is part of the redesign work, not a follow-up.

**Record in `docs/TESTING_DEBT.md`** any assertion that has nowhere to live yet, per the workspace
lifecycle rule.

---

## 8. Scope boundaries — confirmed respected

Nothing in this report proposes: new roles beyond Owner; read permissions; Logto permissions/roles as
an authorization source; changes to Owner bootstrap or reconciliation rules; unrelated Abwab
functionality; or new permissions.

**No database or schema change is required.** The catalogue defect is a data-provisioning and
error-handling problem, and the recommended fix (run the existing synchronizer at startup) touches
neither schema nor migrations.

---

## 9. Verdicts

### Current-state verdict

**Substantially implemented, correctly designed at the contract level, and blocked at runtime by a
single data-provisioning gap — then made to look like total failure by one misordered template
branch.**

Every capability in the locked contract exists end to end, and permission assignment is *positively*
proven to work by a rendered-component test that drives a real checkbox through to the PUT body
(`access-admin-page.component.spec.ts:354-392`).

The `حدث خطأ غير متوقع` box is a backend 500 from `EfPermissionCatalogueReader` throwing because the
`permissions` table was never populated: no migration seeds it, the repo's own `reset-db`/`update-db`
scripts never sync it, `PermissionCatalogueSynchronizer` is registered in DI but has **zero
production call sites**, and the production Docker image does not even contain the CLI that is
documented as the remedy. *The mechanism is confirmed from code; the deployed table's actual row
state is not observable under a read-only audit, so the diagnosis is **likely**, not confirmed.*

The permission editor is genuinely wired and is the first block after the user header — not buried.
It disappears because the detail card evaluates `catalogueError()` before `selectedUser()`, so one
failing GET erases the header, badges, status actions and relink form as well, with no retry path
for the rest of the session.

### Functional gaps

1. Catalogue never populated in a migrations-only environment; **no automated gate detects it**, and
   the documented CLI remedy is absent from the production image. A leftover unknown row would keep
   the endpoint 500-ing even after a successful sync.
2. A catalogue failure blanks the whole workspace and is terminal for the session.
3. No dirty tracking; «إلغاء» does not revert; switching users silently discards edits.
4. Unknown server codes are silently revoked on save **and omitted from the confirmation diff**.
5. The relink form renders for Owners, directly contradicting the "view-only" notice above it — and
   the backend genuinely permits it, so this is a copy/product mismatch, not just a stray element.
6. `canAccess()` conflates "identity unknown" with "denied".
7. The 409 recovery message renders as an error; no success feedback exists at all.
8. The user search binds directly to an exact-email-match parameter that 400s on partial input.
9. `canApply` is displayed with no in-product action able to act on it.

### UI/UX gaps

Full-bleed with no width cap and a `qd-page-frame` whose contract is entirely overridden; 19
checkboxes in one column; `align-items: start` voids; four peer cards with a read-only audit log
given *more* width than the primary user list; three nested bordered levels sharing the same fill;
ten classes of raw technical values including a 64-char hash and untranslated English enums as the
headline of an Arabic log; a barely-visible selected state that hue-disagrees with itself in dark
theme and shifts layout by 1px; no permission workflow affordances; one bespoke media query outside
the shared breakpoint scale; inconsistent bidi isolation.

### Recommended target layout

Page header → sticky ~20rem user-list aside + flex-1 selected-user workspace using
`.explorer-detail-panel` chrome → permissions as the primary section in 2–3 responsive columns,
grouped الأبواب / الأقسام / العلاقات / القوالب / عناصر القوالب with per-group «تحديد الكل», a live
diff summary and a sticky Save/Cancel bar → audit and reconciliation demoted behind `qd-tabs` in
`.qd-card--quiet`.

### Recommended implementation phases

1. **Unblock** — make the catalogue read path non-throwing (the half that actually closes the
   failure mode), run the synchronizer at startup, and add the regression test that runs against a
   migrations-only database. Note the synchronizer does not remove leftover unknown rows, so the
   read-path fix is required, not optional.
2. **Repair the workspace** — reorder the guard chain, scope the catalogue error, add retry, move
   relink inside the non-Owner branch.
3. **Complete the workflow** — dirty state, real Cancel, live diff, sticky save bar, unsaved-changes
   guard, Owner/Pending/Disabled framing.
4. **Relayout** — page frame, master-detail split, multi-column permission grid, `.qd-is-selected`,
   reserved states, canonical breakpoints.
5. **Demote and humanize** — audit + reconciliation behind tabs, Arabic enum labels, formatted
   timestamps, user picker instead of numeric IDs, hidden fingerprint.
6. **Remove the duplication** — delete the hardcoded code allowlist or generate it; extend the drift
   gate to labels and ordering.

### Files likely affected

**Backend:** `Api/Program.cs` (or a startup hook), `Infrastructure/DependencyInjection/AccessDependencyInjection.cs`,
`Infrastructure/Persistence/Reads/Access/EfPermissionCatalogueReader.cs`,
`Api/Controllers/Access/AccessPermissionsController.cs`,
`Application.Abstractions/Security/Permissions/PermissionDefinition.cs` + `AbwabPermissionCatalogue.cs`
(group label only), `tests/QuranDashboard.Tests/Api/Access/`, and
`tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs` (drop `ParityOnly` on the catalogue route).

**Frontend:** all of `features/access-admin/` (page, facade, three components + the new extractions,
models), `core/auth/permission-code.ts`, `scripts/check-permission-catalogue.mjs`, and the feature
README.

**Docs:** `docs/contracts/security-access.md` if the catalogue contract changes;
`docs/TESTING_DEBT.md` for any assertion without a home.

### Product clarification still required

1. **Visual language.** The brief says Navy/Gold/Parchment; `DESIGN.md` says that direction is
   superseded for light (flat parchment + one scholarly green, navy footer-only, gold retired), with
   dark still interim navy+gold. **This proposal assumes the shipped green tokens.** Confirm.
2. **Reason capture on every permission save.** Mandatory 1..1024 chars on every write. Is that the
   intended friction for routine grants, or should it apply only to destructive actions?
3. **Relink placement and Owner relink.** Should subject relink stay on this page at all? It is a
   rare, high-risk identity operation currently sharing the workspace with routine permission
   editing — and it is the one mutation with **no Owner guard and no status guard** (§4.3), so an
   Active Owner can relink their own subject. Confirm this is intended, and decide whether the UI
   should expose it for Owner targets or route it to the CLI.
4. **Owner permission display.** Should an Owner's inherited set render as 19 checked-and-disabled
   boxes (explicit but potentially confusing) or as a single statement of bypass?
5. **`canApply`.** Should reconciliation apply become an endpoint, or should the flag be removed from
   the UI since nothing can act on it?
6. **No-op saves.** Should a save with an empty diff be blocked in the UI, given the backend
   discards the reason and writes no audit row?
