# Authorization and Granular Permissions Implementation Plan

> For the implementation session: use the repository's test-first workflow and the
> `superpowers:executing-plans`, `superpowers:test-driven-development`, and
> `superpowers:verification-before-completion` skills. Stop at every phase gate below; do not
> treat this document as authority to commit, push, deploy, or mutate production data.

**Goal:** Close the currently anonymous Abwab write surface and introduce database-authoritative,
Owner-bypassed, direct granular write permissions without changing anonymous public-read behavior.

**Architecture:** Logto remains the authentication and identity provider. The application database
resolves one request-scoped authorization snapshot from `sub`; explicit endpoint metadata then
requires either one known permission or an active Owner. One canonical catalogue supplies all 19
Abwab codes, direct grants are current state, and append-only audit events preserve every security
change. Public GET endpoints opt into no authorization policy.

**Technology:** ASP.NET Core authorization, .NET 10, EF Core/Npgsql/PostgreSQL, Angular signals and
functional guards, generated OpenAPI models, xUnit/Testcontainers route smoke, Vitest, and
supplementary Playwright.

**Planning authority:**

1. Target decisions: `docs/security/authorization-permissions-design-decisions.md`.
2. Inspected baseline: `docs/security/authorization-permissions-current-state-report.md`.
3. Current implementation details: code plus the nearest living `README.md`.
4. Test selection and gates: `TESTING_STRATEGY.md`.

**Global constraints:**

- Public Quran, Mushaf, Words, Dashboard-content, Abwab, relation, and template GETs remain
  anonymous. `GET /api/health` remains public.
- `GET /api/access/me` remains authenticated and is not a public-read dependency.
- Security-administration reads and writes are active-Owner-only.
- `Owner` remains the only role; non-owners have `RoleId = null`.
- No feature-specific read permissions, generic RBAC, group grants, cross-request authorization
  cache, or permission for an unimplemented action is introduced.
- Backend authorization is the security boundary. Frontend checks never make a request authorized.
- Existing Abwab domain outcomes and response semantics remain unchanged after authorization
  succeeds.
- Every implementation phase must update the nearest living README in the same change when its
  current contract changes.

## 1. Executive summary

The current API has JWT validation and an authenticated provisioning endpoint, but all 21 Abwab
write routes are open. This is confirmed by the controller inventory and by the explicit production
warning in `Backend/api/QuranDashboard.Api/Controllers/README.md:8-20` and
`Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:11-27`. The current role policies are
registered but attached to no write endpoint
(`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:50-58`), and there is
no fallback policy (`Backend/api/QuranDashboard.Api/Authentication/README.md:34-52`).

The target closes that exposure with four mutually reinforcing controls:

1. an additive access schema containing the canonical permission rows, current direct grants, and
   append-only audit history;
2. validated multi-Owner configuration and explicit reconciliation against verified Logto
   identities;
3. request-scoped database authorization plus explicit known metadata on every unsafe endpoint;
4. startup validation and route-parity tests that prevent a future `POST`, `PUT`, `PATCH`, or
   `DELETE` from being exposed without a classification.

The safest sequence prepares tests and additive contracts first, establishes the database-level
normalized-email invariant, proves a valid configured Owner, then installs and tests the
authorization core and all Abwab write metadata. Phase 5 implementation can be reviewed and
activated in development/staging before the Owner-administration APIs are complete, but the
preferred production sequence completes and reviews Phase 6 before Phase 5 enforcement is
activated. Earlier production activation is allowed only after explicit acceptance of a temporary
Owner-only write period or after a trusted operator command can grant/revoke individual direct
permissions through the same transactional, audited application services. Frontend permission UX
still follows stable Backend contracts, and legacy Admin/Editor data and code are removed last.

The work is split into controlled phases because schema, Owner bootstrap, protection metadata,
administration transactions, OpenAPI, and UI gating have different rollback boundaries. In
particular, enforcement must never be activated before a verified local active Owner exists, and an
operational rollback must never restore the old anonymous-write Backend. Public GET availability is
an acceptance invariant in every phase, not a final cleanup item.

The unresolved authoritative Logto verified-primary-email signal is intentionally isolated: it does
not block the immediately implementable Phase 1 test/contract work or Phase 2 normalized-email and
additive-schema work. It does block Phase 3 acceptance and Phase 5 production activation because
Owner membership cannot be trusted without it.

## 2. Scope and non-goals

### In scope

- Replace singular `Auth:BootstrapOwnerEmail` with validated `OwnerBootstrap:Emails` configuration.
- Add required, uniquely indexed `Users.NormalizedEmail` while preserving `Users.Email` as the
  original/display value; use one shared application normalizer for every identity-sensitive email
  operation.
- Add an idempotent, operator-invoked Owner preflight/reconciliation operation and recovery path.
- Serialize every Owner reconciliation apply operation with one dedicated PostgreSQL
  transaction-scoped advisory lock (or an explicitly equivalent database serialization boundary).
- Retain one Owner role row and allow multiple active users to reference it.
- Add the fixed 19-permission catalogue with Arabic label, English description, group, and order
  metadata.
- Add current direct grants for active non-owner users.
- Add append-only access-audit persistence and Owner-only retrieval.
- Add active-account, exact-permission, and Owner-only authorization requirements.
- Add controlled shared-envelope `401` and `403` handling.
- Protect all 21 current Abwab writes with their accepted exact permission.
- Add startup/runtime unsafe-endpoint metadata validation and method/route/access parity tests.
- Add Owner-only Backend use cases and APIs for users, grants, audit, reconciliation status, and
  explicit Logto `sub` relinking.
- Evolve `/api/access/me` to `sub`, `email`, `displayName`, `status`, `isOwner`, and `permissions`,
  with a bounded transitional `roleName`.
- Add a frontend access store, typed permission checks, Owner-only administration routing, and
  exact-permission Abwab write UX.
- Pay the five mandatory Abwab smoke debt obligations and add the full authorization persona
  matrix.
- Inventory and convert legacy Owner/Admin/Editor user data, then remove Admin/Editor seeds,
  policies, constants, claims transformation, cache, and frontend assumptions.
- Update living READMEs, generated OpenAPI contracts, the smoke catalogue, contract pointers, and
  the testing-debt ledger.
- Perform production preflight, coordinated Backend/frontend rollout, and post-deployment security
  smoke without interrupting public reads.

### Explicit non-goals

- Authentication, active-user guards, or permission checks for normal public content reads.
- Read permissions of any kind.
- Admin, Editor, Supervisor, or any other non-owner named role.
- Generic RBAC, role-permission tables, hierarchical permissions, or delegated permission
  administration.
- Runtime-editable permission groups or a persisted “manage all” authority.
- A door-protection permission before a real protection action exists.
- Permissions or write UI for unimplemented modules.
- Cross-request permission caching or cache-invalidation infrastructure in v1.
- A comprehensive audit-history analysis UI; the append-only store and Owner-only paginated
  retrieval are in scope.
- Unrelated Backend/frontend performance work.
- Testing-debt rows whose recorded triggers are not reached by this feature.
- A Spec Kit, unrelated feature planning, or changes to the two authoritative source documents.

## 3. Current-to-target gap map

| Current implementation | Accepted target | Layers and likely files | Compatibility and migration risk | Required proof |
|---|---|---|---|---|
| One optional `Auth:BootstrapOwnerEmail` (`Infrastructure/Access/OwnerBootstrapOptions.cs`) | Required normalized unique `OwnerBootstrap:Emails` list | Infrastructure Access options/DI; API settings; smoke/access fixtures; new operator tool | Existing deployment key must be deliberately mapped during rollout; silently accepting both indefinitely would create two sources of truth | Options validation tests; environment-array binding; duplicate/invalid cases; deployment preflight |
| `Users.Email` is the only persisted email comparison value and its current uniqueness is not a normalized identity invariant | Preserve `Users.Email` as original/display email; add required `Users.NormalizedEmail`, populated by one shared normalizer and uniquely indexed | Domain `User`; EF user configuration/migrations; provisioning; Owner options/reconciliation; relink; Access tests/tool preflight | Existing values can collide after casing/whitespace normalization; a silent merge or relink would transfer authority. Old Backend instances cannot keep inserting rows without the new value after it becomes required | Pre-migration collision scan; casing/whitespace/duplicate vectors; safe backfill; provisioning/Owner/relink comparison tests; unique-index integration test |
| Owner/Admin/Editor seeds (`RoleConfiguration.cs:26-31`) | One Owner seed referenced by multiple users | Domain `RoleNames`; EF role config; migrations; access tests | Deleting seeds while users still reference IDs 2/3 violates the restrictive FK; intended Owners could be lost | Legacy-role inventory; conversion rehearsal; role-FK integration test; post-conversion query |
| `RoleClaimsTransformation` + cached `IUserRoleResolver` (`Authentication/RoleClaimsTransformation.cs`, `Infrastructure/Access/CachedUserRoleResolver.cs`) | Database authorization snapshot resolved once per protected request | Application abstractions; Infrastructure access reads; API authorization | Token/claim behavior must not remain an alternate authorization path; cache removal must not break `/me` transition | Handler tests, token-claim-smuggling tests, one-query/request integration proof |
| No permission catalogue | Exactly 19 immutable Abwab codes | Application abstractions canonical catalogue; Infrastructure synchronizer; API startup validator; generated/checked frontend constants | Missing rows at enforcement time must fail preflight; code/DB/frontend drift could deny valid users or grant the wrong action | Catalogue uniqueness/format/order tests; DB parity test; frontend contract parity |
| No direct grants | `UserPermissions` current active grants | Domain Access; EF config/DbContext; Access application services; Owner APIs | Duplicate grants, grants to Owner/inactive user, or inferred Admin/Editor grants are security defects | Unique-FK persistence tests; transactional grant/revoke tests; zero automatic conversion grants |
| No security audit | Append-only `AccessAuditEvents` from first security mutation | Domain Access; EF config; audit writer/reader; administration transactions | State could commit without history, or user deletion/FK behavior could erase evidence | Same-transaction failure tests; no update/delete application path; snapshot/retrieval tests |
| Shared-envelope `401` only (`UnauthorizedRejectionWriter.cs`) | Controlled Arabic `401` and scenario-appropriate `403` from one path | API Authentication/Authorization; `ApiMessages`; middleware result handler | Competing JwtBearer and middleware writers can double-write; returning bare 403 breaks frontend contract | Anonymous/invalid token, unknown/Pending/Disabled/missing-permission/Owner-only envelope tests |
| All Abwab writes open | Every write gets one exact known permission; active Owner bypasses | Five Abwab controllers; authorization metadata; smoke catalogue | Partial rollout can leave one anonymous route or make all active users writers | 21-route persona matrix; handler-not-called assertions on denial; handcrafted HTTP tests |
| Smoke catalogue has only `Open`/`RequiresAuthentication`; unsafe routes are `ParityOnly` | Public, authenticated-only, exact-permission, and Owner-only access metadata with bidirectional parity | `SmokeRouteCatalog.cs`, `SmokeCoverageParityTests.cs`, new authorization metadata parity tests | Method/path parity alone cannot detect a missing or neighboring permission | Startup validation tests and route/method/access parity over live endpoint metadata |
| `/me` returns numeric `roleId` and `roleName` (`AccessController.cs:11-43`) | Add `isOwner` and direct permission codes; retire numeric ID; temporary `"Owner"|null` role name only | Access handler/DTO; OpenAPI; generated TS model; current-user store | Backend/frontend deployment order can break older clients; Owner permission array semantics must be explicit | Additive contract tests, generated contract check, Pending/Disabled/Owner/read-only cases |
| Frontend `RoleName = Owner|Admin|Editor` and unattached `roleGuard` (`core/auth/current-user.model.ts`, `role.guard.ts`) | Reusable access state and active-Owner guard only for security administration | `core/auth`; app routes; new access-admin feature | A blanket route guard would violate public browsing; stale state can leave controls visible | Anonymous route tests; Owner-only guard tests; access-store refresh/concurrency tests |
| Abwab API/controller/UI exposes generic writes with generic 401/403 transport failures | Every entry, event path, and submission checks its exact code; Backend remains authoritative | Abwab pages, overlay controllers, state controllers, components, data access | Hiding one button while leaving keyboard/context/modal dispatch reachable is insufficient | Component/page tests for all event paths, stale-403 refresh, direct Backend rejection |
| Current Logto adapter infers verification from linked identities (`LogtoManagementApiUserProfileSource.cs:40-47`) | Tenant-authoritative verified primary-email evidence | Infrastructure Logto adapter and contract tests | A wrong signal can grant Owner to an unverified identity | Targeted tenant/API verification spike; recorded fixture; fail-closed unresolved cases |
| No operator reconciliation surface | Trusted idempotent deployment/recovery operation, not a dashboard mutation; every apply caller uses one serialized service | New `Backend/tools/QuranDashboard.AccessAdmin/`; shared Application/Infrastructure service | Startup-only auto-mutation, duplicated mutation logic, or concurrent last-Owner decisions risk lockout and partial authority changes | Dry-run/preflight output tests, dedicated advisory-lock integration tests, transaction/audit tests, last-active-Owner races |
| Phase 5 currently closes writes before application grant administration exists | Phase 5 implementation/staging activation is separable from production activation; production preferably activates only after Phase 6 is complete and reviewed | Release/deployment gates; AccessAdmin tool/application services; Phase 5–6 Backend packages | Early production activation makes active Owners the only writers until an approved audited grant mechanism exists; delayed activation leaves anonymous writes exposed | Explicit activation decision; Phase 5 readiness split; direct-write smoke; operator-grant or Phase 6 API proof; rollout audit |

Compatibility is intentionally additive until Phase 9. Old clients may read `roleName` while new
clients consume `isOwner` and `permissions`; no client is ever permitted to authorize from
`roleName`. The current-state report’s earlier active-administrator read recommendation is not a
gap to close: design-decisions §2 and §16 supersede it, so normal GET routes stay open.

## 4. Target architecture and ownership

### Responsibility map

| Component | Planned responsibility | Proposed location |
|---|---|---|
| Logto | Login, session, access-token issuance, `sub`, primary email identity, and the authoritative email-verification evidence used by server-side profile lookup | Existing tenant plus `Infrastructure/Access/LogtoManagementApiUserProfileSource.cs` |
| `ICurrentUser` | Expose only the authenticated request’s raw `sub`; do not expose roles or permissions from claims | Existing `Application.Abstractions/Security/ICurrentUser.cs` and `Api/Authentication/HttpContextCurrentUser.cs` |
| `IEmailIdentityNormalizer` | Produce the sole canonical `NormalizedEmail` value for provisioning, Owner configuration/reconciliation, duplicate detection, and relink comparisons while preserving `Email` for display | `Application.Abstractions/Access/IEmailIdentityNormalizer.cs` with one Infrastructure/Application implementation used by all callers |
| `AuthorizationState` | Immutable result containing local user ID, status, `isOwner`, and direct permission-code set | `Application.Abstractions/Security/AuthorizationState.cs` |
| `IAuthorizationStateResolver` | Resolve by `sub` once per scoped lifetime; fail closed for no row or infrastructure failure; never provision | `Application.Abstractions/Security/IAuthorizationStateResolver.cs`; implementation under `Infrastructure/Persistence/Reads/Access/` |
| Owner reconciliation service | Compare normalized configured desired state with verified local/Logto state; serialize every apply with one dedicated transaction-scoped database lock; recompute under that lock; apply additions/removals, Owner-promotion grant revocations, and audit atomically | `Application/Access/OwnerReconciliation/`; Infrastructure implementation/adapters under `Infrastructure/Access/` |
| Owner operator command | Run validate, dry-run, serialized reconcile, and production preflight without exposing an Owner mutation HTTP endpoint; if explicitly selected for early Phase 5 activation, expose individual grant/revoke commands only as thin callers over Phase 6 application services | New `Backend/tools/QuranDashboard.AccessAdmin/` project and README |
| Canonical permission catalogue | Own constants and immutable code plus Arabic label, English description, group, order; expose lookup and validation | `Application.Abstractions/Security/Permissions/AbwabPermissionCatalogue.cs` |
| Catalogue synchronizer | Insert missing known permissions, update display metadata, refuse duplicate/malformed state, and never repurpose/delete codes | `Infrastructure/Access/PermissionCatalogueSynchronizer.cs`, invoked by the operator/deployment tool |
| `PermissionRequirement` and handler | Require authentication, local user, Active status, then central Owner bypass or exact direct code | `Api/Authorization/Permissions/` |
| `OwnerOnlyRequirement` and handler | Reuse the same snapshot and require Active plus Owner; no granular code can satisfy it | `Api/Authorization/Owner/` |
| Endpoint attributes/metadata | Mark each unsafe action as one exact permission or active-Owner-only without controller queries or string literals | `Api/Authorization/Metadata/RequirePermissionAttribute.cs` and `RequireOwnerAttribute.cs` |
| Authorization response handler | Return the shared `ApiResponse<object>` envelope for `401`/`403`, selecting centralized Arabic messages from resolved failure reason | `Api/Authorization/ApiAuthorizationMiddlewareResultHandler.cs` plus one rejection writer |
| Unsafe-endpoint validator | Enumerate built endpoints at startup; reject missing, unknown, duplicate, conflicting, or public unsafe metadata | `Api/Authorization/Validation/UnsafeEndpointMetadataValidator.cs` |
| User/permission administration services | Enforce transitions, Owner-target restrictions, optimistic concurrency, direct grants, and audit in one transaction | `Application/Access/Commands/` and `Queries/`; persistence under `Infrastructure/Persistence/{Reads,Writes}/Access/` |
| Audit writer/reader | Append required event snapshots in the caller’s transaction; provide Owner-only cursor/page retrieval | Abstraction in `Application.Abstractions/Access/`; Infrastructure under `Persistence/{Writes,Reads}/Access/` |
| Frontend current-user/access store | Load `/api/access/me` only for authenticated sessions/access-aware UI; derive `isActive`, `isOwner`, permission set, and `can` | Existing `Frontend/.../core/auth/current-user.store.ts`, renamed only if the implementation proves a clearer boundary |
| Frontend permission catalogue | Typed codes and group/select-all metadata contract-checked against Backend | `Frontend/.../core/auth/permissions/` generated from a Backend-exported JSON/OpenAPI enum or checked by a parity script |
| Frontend reusable checks | `can(code)`, `canAny(codes)`, and active-Owner route guard for security administration only | `Frontend/.../core/auth/permission-access.ts` and `owner.guard.ts` |

The preferred ASP.NET Core shape is requirement-data endpoint attributes rather than a custom
database query in controllers or 19 hand-registered policies. `RequirePermissionAttribute`
contributes a `PermissionRequirement` and carries a known catalogue code as inspectable endpoint
metadata. `RequireOwnerAttribute` contributes `OwnerOnlyRequirement`. If the framework integration
forces a dynamic policy provider, it must parse only a fixed prefix plus a code accepted by the
catalogue; it must not accept arbitrary policy strings.

The Application layer owns transitions and transaction semantics; Infrastructure owns EF and Logto;
the API owns HTTP authorization and response mapping. This follows
`Backend/.architecture/CLEAN_ARCHITECTURE.md` and the feature-first placement rules in
`Backend/.architecture/BACKEND_STRUCTURE.md`. Angular remains page/store/API layered according to
`Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`.

## 5. Database and migration plan

### V1 entity shape

| Entity | Key fields | Constraints and indexes | Delete/update behavior |
|---|---|---|---|
| `User` (existing) | Existing identity/profile/status/nullable `RoleId`; preserve `Email` as the original/display address; add required `NormalizedEmail`; add an optimistic concurrency token using PostgreSQL `xmin` as `Version` | Unique `LogtoSub`; unique index on `NormalizedEmail`; do not rely on PostgreSQL’s default case-sensitive uniqueness of `Email`; index `(Status, Id)` for administration; keep role FK restrictive | No ordinary hard delete. One shared application normalizer writes `NormalizedEmail`; Owner/non-owner and status invariants are enforced transactionally. |
| `Role` (existing) | Existing `Id`, `Name`, `DisplayName` | One unique row named `Owner`; after cleanup no Admin/Editor rows | Restrict deletion while referenced. No role-management endpoint. |
| `Permission` | `Id`, immutable `Code`, `ArabicLabel`, `EnglishDescription`, `DisplayOrder`, optional `RetiredAtUtc` | Unique `Code`; check/lifecycle validation for lowercase dotted code; ordered catalogue index if retrieval needs it | Code cannot be updated or reused. Metadata may be synchronized. Rows are retired, never silently deleted. Group/group order stay code-defined and are not persisted. |
| `UserPermission` | `UserId`, `PermissionId`, `GrantedByUserId`, `GrantedAtUtc` | Composite PK/unique `(UserId, PermissionId)`; indexes on `UserId` and `PermissionId`; FKs to users/permission | Explicit delete means revoke current state. All FKs restrict user/permission hard deletion; audit preserves history. |
| `AccessAuditEvent` | `Id` (`bigint`), `OccurredAtUtc`, `ActionType`, `ActorType`, nullable `ActorUserId`, required `TargetUserId`, actor/target snapshots, `PermissionCode`, before/after JSON, reason, metadata JSON | Index `(OccurredAtUtc DESC, Id DESC)`; `(TargetUserId, OccurredAtUtc DESC, Id DESC)`; `(ActionType, OccurredAtUtc DESC)`; optional partial index on non-null permission code | Insert-only through application abstractions. No ordinary update/delete repository, endpoint, or EF path; snapshots remain after identity changes. |

### Relational and transactional invariants

- All non-owner users have `RoleId = null`; every user referencing the sole role row is an Owner
  candidate reconciled from configuration.
- `User.Email` is preserved for display/audit fidelity. Every identity-sensitive comparison uses
  required `User.NormalizedEmail`, produced by the shared application normalizer. No provisioning,
  Owner configuration/reconciliation, duplicate check, or relink comparison may compare raw
  `Email`.
- Owners have zero `UserPermissions` rows. The resolver never treats a corrupt Owner grant as
  authority, preflight blocks on it, and the row must be removed through an approved audited
  correction rather than left dormant or ignored as acceptable state.
- Promoting an active non-owner with direct grants is valid. Under the serialized reconciliation
  transaction, lock the target and its current grants, append one canonically ordered
  `PermissionRevoked` event per grant with Owner-promotion/reconciliation reason, delete every
  `UserPermission`, attach the sole Owner role, then append
  `OwnerGrantedByReconciliation`. An audit insertion failure rolls back the entire promotion.
- An already valid Owner with zero grants is a reconciliation no-op and creates no false audit
  event. An Owner with any direct grant is always a preflight invariant violation.
- A direct grant may be created only for an `Active`, non-owner target by an active Owner.
- Disable locks/checks the target row, snapshots all grants, appends one `PermissionRevoked` event
  per grant plus `UserDisabled`, deletes all current grants, and changes status in one transaction.
- Reactivation changes `Disabled` to `Active` and emits `UserReactivated`; it never inserts grants.
- Grant replacement locks/checks the target version, computes a known-code set difference, inserts
  or deletes current rows, appends one event per delta, and updates the user concurrency token in one
  transaction.
- Audit actor and target snapshots include local ID, normalized email, display name, status,
  `isOwner`, and the identity fields relevant to that event. Relink before/after includes old/new
  `sub`; reconciliation metadata includes the normalized configured email and deployment/config
  fingerprint.
- Audit JSON schemas are versioned in metadata (for example `schemaVersion: 1`) so later readers can
  interpret historical snapshots without rewriting them.

### Additive and cleanup migrations

Use additive and cleanup boundaries, with the additive boundary deliberately staged around the
normalized-email backfill:

1. **Additive access/normalized-email migrations (Phase 2):**
   - before applying DDL, run a read-only scan that passes every current `Users.Email` through the
     shared application normalizer and reports null/invalid results and duplicate normalized values;
   - fail on any collision and require explicit operator resolution; never merge users, choose a
     winner, or relink a `sub`;
   - add `NormalizedEmail` nullable first, create `permissions`, `user_permissions`, and
     `access_audit_events`, and add supporting indexes plus the user concurrency mapping;
   - deploy compatible provisioning code that writes both display `Email` and normalized identity,
     then backfill every existing row through the shared application normalizer;
   - verify every row is populated and still collision-free, then make `NormalizedEmail` required
     and create its unique index.
2. **Legacy-role cleanup migration:** after verified data conversion, remove Admin/Editor seed rows
   and any schema artifacts proven obsolete. It retains nullable `users.role_id` and the Owner row.

The Phase 2 additive boundary may use two ordered generated migrations so application-produced
backfill can occur between nullable introduction and the final not-null/unique constraint. It must
not compress this into a database-only `lower(email)` shortcut that can drift from the shared
normalizer. The temporary compatibility window is bounded, and no Phase 2 completion or later
enforcement is allowed while `NormalizedEmail` is nullable or non-unique.

The 19 permission rows are synchronized from the canonical catalogue by the operator/deployment
tool immediately after the final Phase 2 additive migration. The synchronizer is idempotent: insert
missing codes, update labels/descriptions/permission order, report unknown rows, and refuse
enforcement if a required code is absent. Group metadata is read from code and is never synchronized
into an authorization table. Numeric IDs are not part of any API contract.

Do not infer grants in a migration. Before cleanup, the operator tool must:

1. list every user with Owner/Admin/Editor role ID/name, status, normalized email, and `sub`;
2. match intended Owners only through the validated configured list and verified Logto evidence;
3. reconcile intended Owners to the single Owner row;
4. set all former Admin/Editor `RoleId` values to null transactionally;
5. leave those users with zero current grants;
6. verify no non-configured user references Owner and no user references Admin/Editor;
7. retain an operator artifact containing counts and non-secret identifiers, not credentials.

The cleanup migration is generated only after that conversion is rehearsed against a production-like
copy. The restrictive current FK in
`Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:33-40` is a safety gate:
the migration must fail rather than cascade if conversion was skipped.

### Preflights required before enforcement

- Schema tables/indexes are present at the expected migration.
- Every user has a required `NormalizedEmail` equal to the shared normalizer output for `Email`;
  the unique index exists and the pre-migration collision report is clean.
- The database catalogue equals the 19 active Backend codes.
- Exactly one Owner role row exists; Admin/Editor may remain only during the transition.
- Every configured email is valid/unique and its reconciliation status is known.
- At least one configured, verified, local `Active` Owner is reconciled.
- No Disabled Owner is counted; no Owner has direct grants. Any Owner grant row blocks preflight
  rather than being treated as dormant or ignored.
- Every former Admin/Editor is inventoried; no permission has been inferred.
- Every live unsafe endpoint has one recognized access classification.
- Public GET route classifications still match the smoke catalogue.

Any failed preflight blocks enforcement and leaves the previous additive deployment in place.

## 6. Owner configuration and reconciliation plan

### Configuration contract

Replace `OwnerBootstrapOptions.SectionName = "Auth"` and its scalar property with an options object
bound from `OwnerBootstrap` and `Emails`. Production examples use:

```text
OwnerBootstrap__Emails__0=owner-one@example.test
OwnerBootstrap__Emails__1=owner-two@example.test
```

Validation trims each value, parses it as an email address, applies one invariant case-insensitive
normalization, and rejects blank, invalid, or duplicate normalized entries. Production enforcement
also rejects an empty list. Development/test may use explicit fixture values; “empty disables
bootstrap” must not survive as a production authorization mode.

The email normalizer is one shared service/value object used by configuration, provisioning,
reconciliation, relink validation, duplicate detection, and persistence of
`Users.NormalizedEmail`. `Users.Email` remains the original/display value. Do not distribute ad hoc
`ToLowerInvariant()` calls or compare identity through raw `Email`.

### Verification prerequisite

Before implementation of Owner grant/reconciliation is accepted, perform a targeted Logto tenant/API
check to identify the authoritative server-side verified-primary-email signal. The current linked
identity inference in `LogtoManagementApiUserProfileSource.cs:40-47` is not sufficient merely
because it exists. Record the chosen Logto response field/endpoint in the nearest security README
and pin verified, unverified, missing-email, mismatched-email, and unavailable-provider fixtures.
Every unresolved or unverified result is fail-closed.

### Four distinct flows

| Flow | Trigger and behavior | Owner mutation authority |
|---|---|---|
| Initial bootstrap | A configured identity calls authenticated `/api/access/me`; Backend fetches that exact `sub`, proves verified matching email, creates/reuses the local user, and applies guarded reconciliation. The event is audited. | Configuration plus verified Logto identity; no existing Owner required for the first valid Owner. |
| Normal provisioning | `/api/access/me` stores the provider address as display `Email`, computes `NormalizedEmail` through the shared normalizer, and creates an ordinary verified-email identity as `Pending`, role-less, and grant-less. Existing normalized email with different `sub` remains `409` and is never relinked. | No Owner mutation unless the identity is configured and passes the bootstrap rules. |
| Explicit reconciliation | Operator runs the idempotent tool in dry-run, then confirmed apply mode. It resolves configured candidates, computes additions/removals/unresolved entries, checks last-active-Owner safety, applies effective changes, and audits each. | Trusted deployment/recovery operation only; not a dashboard mutation endpoint. |
| Emergency recovery | Operator adds another verified email to environment configuration, provisions that identity through `/me`, runs preflight/reconciliation, verifies it is Active Owner, then performs any Owner-only recovery. | Configuration remains the sole source. Existing inaccessible Owner need not be relinked first. |

### Reconciliation rules

- An existing `Pending` configured user may become `Active` Owner after verified matching identity.
- A configured `Disabled` user remains Disabled and does not bypass status. Reconciliation reports it
  as configured-but-inactive; recovery requires another configured Owner, not auto-reactivation.
- A configured email with no local user is unresolved and does not count. It must provision through
  `/api/access/me`.
- Adding an Owner never creates direct grants. If an active non-owner already has direct grants,
  reconciliation must revoke and delete them as part of promotion rather than reject the valid
  configured Owner, leave the rows dormant, or merely ignore them.
- Removing an email removes only the Owner role and preserves current status. An active demoted user
  becomes an active, role-less, zero-grant read-only user until an Owner grants permissions.
- A removal that would leave zero verified local active configured Owners is rejected before writes.
- Reconciliation re-fetches trusted Logto state and compares its normalized verified primary email
  to `Users.NormalizedEmail`; it does not trust display `Email` or a prior audit event as current
  verification.
- Every reconciliation apply obtains one stable, dedicated PostgreSQL transaction-scoped advisory
  lock, or an explicitly equivalent database serialization mechanism, before deciding any
  membership mutation. This is a reconciliation-specific lock, not a general distributed-locking
  framework.
- After acquiring the lock, the service reloads configured desired Owner state, current database
  Owner membership, candidate user statuses, current direct grants, and the verified Logto evidence
  required for each mutation. Last-active-Owner validation and every membership change occur under
  that same lock and transaction.
- For Owner promotion, the transaction locks the target user and all current direct grants; appends
  one `PermissionRevoked` event per grant in canonical order with
  Owner-promotion/reconciliation reason; deletes all grants; attaches the sole Owner role; and
  appends `OwnerGrantedByReconciliation`. Any audit failure rolls back grant removal and promotion.
- Lock acquisition failure or timeout fails closed and commits no partial membership, grant, or
  audit change. A concurrent-state conflict is recomputed under the lock rather than applying a
  stale dry-run.
- The result reports unchanged, added, removed, unresolved, configured-disabled, and rejected
  candidates plus a non-secret configuration fingerprint.
- Startup validates configuration shape and unsafe endpoint metadata but does not silently reconcile
  database membership. Deployment readiness invokes the explicit preflight/tool and can block
  traffic activation.

### Operator surface

Add `Backend/tools/QuranDashboard.AccessAdmin/` with narrowly scoped commands:

- `owners validate` — configuration only;
- `owners status` — read-only desired/current/verified comparison;
- `owners reconcile --dry-run` — transaction-free change preview;
- `owners reconcile --apply --reason <text>` — explicit mutation;
- `authorization preflight` — schema/catalogue/Owner/endpoint-independent database gates;
- `legacy-roles inventory` and later `legacy-roles convert --apply`.

The tool shares Application/Infrastructure services rather than duplicating EF logic. It must require
an explicit production confirmation mechanism, print no M2M secret/token, return non-zero on any
unresolved enforcement gate, and be documented in its local README and `Backend/scripts/README.md`.
Read-only status/dry-run output must state that apply reacquires trusted evidence and recomputes the
desired/current delta under the reconciliation lock, so the applied result may differ if state
changes. Bootstrap promotion through `/api/access/me`, operator apply, and every future trusted
caller must invoke the same service and serialization boundary; no caller may implement separate
Owner mutation logic.

## 7. Canonical permission catalogue plan

The Backend catalogue is the only authoring source. Its entry type contains code, Arabic label,
English technical description, group key, group display order, and permission display order. Static
construction validates uniqueness, lowercase dotted syntax, and stable ordering. Database
synchronization and endpoint metadata both consume this catalogue.

| Order | Code | Arabic label | Group | English description | Select-all bundle |
|---:|---|---|---|---|---|
| 1 | `abwab.doors.create` | إنشاء الأبواب | Doors | Create a root or child door. | Manage all doors |
| 2 | `abwab.doors.edit` | تعديل الأبواب | Doors | Edit authored door fields and aliases. | Manage all doors |
| 3 | `abwab.doors.move` | نقل الأبواب | Doors | Move one or several doors to another parent or section. | Manage all doors |
| 4 | `abwab.doors.reorder` | إعادة ترتيب الأبواب | Doors | Reorder a door in Section or Global scope. | Manage all doors |
| 5 | `abwab.doors.archive` | أرشفة الأبواب | Doors | Archive one or several door subtrees. | Manage all doors |
| 6 | `abwab.doors.restore` | استعادة الأبواب | Doors | Restore an archived door subtree. | Manage all doors |
| 7 | `abwab.sections.create` | إنشاء الأقسام | Sections | Create an Abwab section. | Manage all sections |
| 8 | `abwab.sections.edit` | إعادة تسمية الأقسام | Sections | Change a section name. | Manage all sections |
| 9 | `abwab.sections.reorder` | إعادة ترتيب الأقسام | Sections | Reorder the live section list. | Manage all sections |
| 10 | `abwab.sections.delete` | حذف الأقسام | Sections | Retire an empty section. | Manage all sections |
| 11 | `abwab.relations.create` | إنشاء العلاقات | Relations | Add one relation type from an anchor to one or more doors. | Manage all relations |
| 12 | `abwab.relations.delete` | حذف العلاقات | Relations | Remove a door relation. | Manage all relations |
| 13 | `abwab.templates.create` | إنشاء القوالب | Templates | Create a template and its root node. | Manage all templates |
| 14 | `abwab.templates.delete` | حذف القوالب | Templates | Retire a template. | Manage all templates |
| 15 | `abwab.templates.apply` | تطبيق القوالب على الأبواب | Templates | Copy template child subtrees into selected doors. | Manage all templates |
| 16 | `abwab.template_nodes.create` | إضافة عناصر القوالب | Template nodes | Add a child node to a template. | Manage all template nodes |
| 17 | `abwab.template_nodes.edit` | تعديل عناصر القوالب | Template nodes | Edit a template node; root edit also renames the template. | Manage all template nodes |
| 18 | `abwab.template_nodes.reorder` | إعادة ترتيب عناصر القوالب | Template nodes | Reorder a non-root template node. | Manage all template nodes |
| 19 | `abwab.template_nodes.delete` | حذف عناصر القوالب | Template nodes | Retire a non-root node and its subtree. | Manage all template nodes |

### Synchronization and frontend contract

- The catalogue synchronizer inserts missing rows and updates mutable display metadata. It never
  changes or deletes a code and never treats an unknown database code as authorization.
- Startup checks that every code referenced by endpoint metadata is known in code. Deployment
  preflight also checks that all 19 rows exist in the database.
- Export a small deterministic permission-contract JSON from the Backend catalogue or expose the
  catalogue in generated OpenAPI and add a parity script. The frontend typed union/constants and
  group metadata are generated or compared byte-for-byte by `check-api-contract`; hand-maintained
  duplicate strings in Abwab components are forbidden.
- “Manage all” expands to every current code in that group in the frontend form. After selecting it,
  each individual checkbox remains independently editable. The submitted request contains only the
  resulting individual code set.
- The Backend never stores, accepts, or checks a group grant or “manage all” code.
- Future retirement never repurposes a code. First remove endpoint use, prevent new grants, revoke
  remaining current grants with audit, mark the catalogue row retired, and preserve audit strings.
  Adding/replacing permissions requires a separately accepted design change.

## 8. Backend authorization foundation plan

### Request-scoped snapshot

`IAuthorizationStateResolver.ResolveAsync(sub, ct)` performs one projection query for protected
requests:

- find the user by exact `LogtoSub`;
- load status;
- determine Owner by the sole local Owner role relation;
- load direct permission codes only for an active non-owner;
- return “unknown local user” without provisioning;
- memoize the task in the scoped resolver so multiple requirements on the same request do not issue
  duplicate queries.

The query is a projection, not entity graph materialization. It uses the unique `logto_sub` index and
the `(user_id, permission_id)` key, returns a set of strings, and does not touch claims beyond the
`sub` supplied by `ICurrentUser`. A second distinct `sub` in the same scoped resolver is an invariant
failure, not a second cache entry.

### Evaluation order and requirements

The permission handler evaluates:

1. authenticated principal and nonblank `sub`;
2. existing local user;
3. `Status == Active`;
4. `IsOwner` central bypass;
5. exact permission-set membership.

The Owner-only handler performs steps 1–4 and never considers direct grants. A Disabled Owner fails
at step 3. Unknown, Pending, Disabled, missing, or unavailable database state never succeeds.

Failure reasons remain internal structured values so the shared response handler can select:

- existing unauthorized Arabic message for challenge;
- unprovisioned forbidden message;
- inactive-account forbidden message;
- missing-permission forbidden message;
- Owner-only forbidden message;
- controlled operational `503` for authorization-state infrastructure failure.

The operational response is fail-closed and logged with correlation information; it must not reveal
permissions, Owner emails, or database details.

### Endpoint application

- Use `[RequirePermission(AbwabPermissions.Doors.Create)]` or the catalogue entry constant on every
  Abwab unsafe action.
- Use `[RequireOwner]` at the security-administration controller level unless an action needs a more
  specific explicit marker for parity; no security admin endpoint uses an Abwab permission.
- Keep `[Authorize]` on `/api/access/me` only for authenticated provisioning/status.
- Apply no class-level `[Authorize]`, fallback policy, active-user filter, or convention to public
  content controllers.
- Preserve middleware order in
  `Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:19-29`.

### Response integration

Replace the 401-only writer with one authorization rejection writer and a custom
`IAuthorizationMiddlewareResultHandler`. Coordinate JwtBearer challenge handling so exactly one
component owns each response; tests must catch double bodies or response-started errors. Both 401 and
403 use `ApiResponse<object>.Fail(...)`, `application/json`, centralized `ApiMessages`, and no
controller-specific strings. Authorized calls continue to current handlers and retain all existing
`400`/`404`/`409`/success behavior.

### Legacy role path removal/narrowing

- Stop registering `RoleClaimsTransformation` before enforcement activation.
- Stop registering or consulting `IUserRoleResolver`/`CachedUserRoleResolver`; remove its 30-second
  cache and eviction semantics after transition.
- Remove Owner/Admin/Editor named policies after all consumers use the new requirements.
- Keep `RoleNames.Owner` as the database invariant; remove Admin/Editor constants only after data
  conversion.
- Ignore token-borne role/permission-looking claims throughout transition. Tests mint such claims
  and prove they have no effect.
- Retain `ICurrentUser` because it owns the authentication-to-application `sub` boundary.
- Use no cross-request authorization cache in v1; changes become authoritative on the next request.

### Public GET invariant

No change in this foundation adds a fallback policy. Public route tests cover anonymous Quran,
Mushaf, Words, Dashboard-info, all four Abwab reads, and health. The resolver is never invoked for a
public GET unless unrelated application code explicitly needs it. `/api/access/me` remains the only
authenticated provisioning read, while new security-administration GETs carry explicit
`RequireOwner` metadata.

## 9. Unsafe endpoint fail-closed plan

### Safeguard 1: startup/runtime validation

After controller endpoints are built, `UnsafeEndpointMetadataValidator` enumerates
`RouteEndpoint`/controller action descriptors and inspects HTTP methods and authorization metadata.
For every `POST`, `PUT`, `PATCH`, or `DELETE`, it requires exactly one classification:

- one `RequirePermission` containing a code present in the canonical catalogue; or
- one `RequireOwner` classification for security administration.

Startup fails with route, method, action, and reason when it finds:

- no classification;
- an unknown permission code;
- two permission codes;
- permission plus Owner-only metadata;
- multiple conflicting authorization classifications;
- explicit anonymous/public metadata on an unsafe action;
- authenticated-only metadata without a granular/Owner classification.

The requirement handlers also fail closed for invalid metadata so a validator wiring regression
does not make a route writable. The startup validator does not require metadata on GET; public GETs
remain untouched, and security GETs opt into `RequireOwner`.

### Safeguard 2: automated route/access parity

Extend `SmokeRouteAccess` beyond `Open`/`RequiresAuthentication` to represent:

- `Public`;
- `AuthenticatedOnly` for `/api/access/me`;
- `Permission` plus a required known code;
- `OwnerOnly`.

Update `SmokeCoverageParityTests` to compare live and catalogue entries bidirectionally by normalized
HTTP method and route template, then compare access metadata:

- every live route exists once in the catalogue;
- every catalogue route exists live;
- each unsafe route has the exact catalogue permission or Owner-only classification;
- each permission classification references a canonical code;
- `/api/access/me` is authenticated-only;
- public GET catalogue entries have no authorization metadata;
- security-administration GETs are Owner-only;
- no route has duplicate/conflicting access metadata.

Keep `ParityOnly` only as a dispatch-control concern; it no longer means “authorization unchecked.”
Dedicated smoke tests dispatch all Abwab writes and security APIs under controlled personas.

### Public-read regression gate

Add a separate data-driven assertion over the established public route catalogue that all normal
GETs lack `IAuthorizeData`, permission, and Owner-only metadata. This prevents a controller-level
attribute or future fallback convention from silently authenticating public content. Conditional
GET tests for Abwab tree/templates must use anonymous clients and prove normal `ETag`/`304` behavior.

Any route or metadata change must update `SmokeRouteCatalog` in the same change, as required by
`TESTING_STRATEGY.md:520-540`.

## 10. Abwab endpoint protection rollout

The protection change is metadata plus authorization tests. It must not alter request models,
handlers, EF writers, cache generation, concurrency, or success/domain response mapping. Current
controllers and line evidence come from design-decisions §8, verified against the live controller
actions.

### Complete 21-write application matrix

| # | Method and route | Controller action | Permission metadata constant | Authorization personas | Existing behavior that remains unchanged |
|---:|---|---|---|---|---|
| 1 | `POST /api/abwab/sections` | `AbwabSectionsController.Create` (`AbwabSectionsController.cs:9-35`) | `AbwabPermissions.Sections.Create` → `abwab.sections.create` | Anonymous 401; unknown/Pending/Disabled/read-only/neighbor 403; exact/Owner reaches handler | `201`, validation, ordering |
| 2 | `PUT /api/abwab/sections/{id}` | `Rename` (`:37-60`) | `Sections.Edit` → `abwab.sections.edit` | Same matrix | Rename-only behavior and concurrency |
| 3 | `DELETE /api/abwab/sections/{id}` | `Delete` (`:62-79`) | `Sections.Delete` → `abwab.sections.delete` | Same matrix | Empty-section precondition; no client version body |
| 4 | `POST /api/abwab/sections/{id}/order` | `Reorder` (`:81-100`) | `Sections.Reorder` → `abwab.sections.reorder` | Same matrix | `200`/`400`/`404`/`409`, live resequencing |
| 5 | `POST /api/abwab/doors` | `AbwabDoorsController.Create` (`AbwabDoorsController.cs:13-51`) | `AbwabPermissions.Doors.Create` → `abwab.doors.create` | Same matrix | Root/child section derivation and validation |
| 6 | `PUT /api/abwab/doors/{id}` | `Edit` (`:53-75`) | `Doors.Edit` → `abwab.doors.edit` | Same matrix | Authored fields, aliases, xmin concurrency |
| 7 | `POST /api/abwab/doors/{id}/move` | `Move` (`:77-104`) | `Doors.Move` → `abwab.doors.move` | Exact grants both single and bulk move only | Reparent/section/cycle validation and transactional resequencing |
| 8 | `POST /api/abwab/doors/{id}/order` | `Reorder` (`:106-129`) | `Doors.Reorder` → `abwab.doors.reorder` | Same matrix | Section/Global scope rules |
| 9 | `POST /api/abwab/doors/bulk-move` | `BulkMove` (`:131-159`) | `Doors.Move` → `abwab.doors.move` | Same move grant as #7; neighboring archive grant denied | All-or-nothing move and current conflict semantics |
| 10 | `POST /api/abwab/doors/bulk-archive` | `BulkArchive` (`:161-179`) | `Doors.Archive` → `abwab.doors.archive` | Same archive grant as #11 | All selected subtrees archived atomically |
| 11 | `DELETE /api/abwab/doors/{id}` | `Delete` (`:181-197`) | `Doors.Archive` → `abwab.doors.archive` | Same archive grant as #10 | Soft archive, never hard delete |
| 12 | `POST /api/abwab/doors/{id}/restore` | `Restore` (`:199-225`) | `Doors.Restore` → `abwab.doors.restore` | Archive-only grant denied; restore grant/Owner reaches handler | Swept subtree, destination/parent validation, xmin |
| 13 | `POST /api/abwab/doors/{doorId}/relations` | `AbwabDoorRelationsController.AddForDoor` (`AbwabDoorRelationsController.cs:31-60`) | `AbwabPermissions.Relations.Create` → `abwab.relations.create` | Delete-only grant denied | Multi-target add remains one all-or-nothing action |
| 14 | `DELETE /api/abwab/relations/{relationId}` | `Delete` (`:62-77`) | `Relations.Delete` → `abwab.relations.delete` | Create-only grant denied | One relation soft delete and current status mapping |
| 15 | `POST /api/abwab/templates` | `AbwabTemplatesController.Create` (`AbwabTemplatesController.cs:62-79`) | `AbwabPermissions.Templates.Create` → `abwab.templates.create` | Same matrix | Template plus mandatory root creation |
| 16 | `DELETE /api/abwab/templates/{templateId}` | `Delete` (`:81-96`) | `Templates.Delete` → `abwab.templates.delete` | Same matrix | Template retirement behavior |
| 17 | `POST /api/abwab/templates/{templateId}/apply` | `Apply` (`:98-125`) | `Templates.Apply` → `abwab.templates.apply` | Create/delete/node grants denied | Children-only deep copy, N targets, all-or-nothing, existing `400`/`404`/`409` |
| 18 | `POST /api/abwab/templates/{templateId}/nodes` | `AbwabTemplateNodesController.Add` (`AbwabTemplateNodesController.cs:9-43`) | `AbwabPermissions.TemplateNodes.Create` → `abwab.template_nodes.create` | Same matrix | Child node creation and sibling rules |
| 19 | `PUT /api/abwab/template-nodes/{nodeId}` | `Edit` (`:45-65`) | `TemplateNodes.Edit` → `abwab.template_nodes.edit` | Template-create/edit neighbors denied | Root edit continues to rename the template |
| 20 | `POST /api/abwab/template-nodes/{nodeId}/order` | `Reorder` (`:67-86`) | `TemplateNodes.Reorder` → `abwab.template_nodes.reorder` | Same matrix | Root refusal and sibling resequencing |
| 21 | `DELETE /api/abwab/template-nodes/{nodeId}` | `Delete` (`:88-105`) | `TemplateNodes.Delete` → `abwab.template_nodes.delete` | Same matrix | Root refusal and subtree retirement |

All four Abwab reads remain public and receive no authorization metadata:
`GET /api/abwab/tree`, `GET /api/abwab/doors/{doorId}/relations`,
`GET /api/abwab/templates`, and `GET /api/abwab/templates/{templateId}`.

### Group execution details

#### Sections

- **Controllers:** `Controllers/Abwab/AbwabSectionsController.cs`.
- **Metadata:** four distinct constants; no controller-level permission because actions differ.
- **Smoke:** expand `SmokeAbwabWriteTests.cs` or split focused files so create/rename/delete/reorder
  each exercises success/domain failures plus authorization personas. Pay F3 for reorder.
- **Personas:** active user holding each one of the four exact codes, every neighboring section code,
  read-only, Owner, Disabled Owner, and the common anonymous/unknown/Pending/Disabled set.
- **Behavioral guard:** do not “fix” section ordering debt F1/F2 while applying authorization.
  Existing domain paths and debt triggers remain as recorded.

#### Doors

- **Controllers:** `Controllers/Abwab/AbwabDoorsController.cs`.
- **Metadata:** six codes over eight actions; move is intentionally shared by single/bulk, archive
  by single/bulk.
- **Smoke:** retain/extend `SmokeAbwabWriteTests.cs` with exact/neighboring personas for all eight
  routes. Preserve `AbwabDoorWriteBehaviorTests.cs` and `CreateDoorHandlerTests.cs`.
- **Composite guard:** moving, archiving, restoring, and resequencing require exactly the visible
  permission listed in the matrix; no hidden create/edit permission is added.
- **Cache guard:** authorized writes continue to invalidate Abwab conditional-read generations in
  the existing writers; authorization denial must execute no handler and no invalidation.

#### Relations

- **Controllers:** `Controllers/Abwab/AbwabDoorRelationsController.cs`.
- **Metadata:** create and delete remain isolated.
- **Smoke:** pay `abwab-relations` row 3 by dispatching the GET, POST, and DELETE status/envelope
  contracts, including archived-anchor `200 []`; add authorization personas to both writes.
- **Behavioral guard:** preserve `AbwabRelationWriteBehaviorTests.cs` and
  `AddDoorRelationsHandlerTests.cs`; no relation writer change is needed.

#### Templates

- **Controllers:** `Controllers/Abwab/AbwabTemplatesController.cs`.
- **Metadata:** create, delete, and apply are independent.
- **Smoke:** pay templates row 8 and apply G3; preserve anonymous conditional reads and
  `SmokeAbwabTemplateReadTests.cs`.
- **Behavioral guard:** preserve `AbwabTemplateApplyBehaviorTests.cs`; applying a template remains
  one permission despite internal door creation and subtree copying.

#### Template nodes

- **Controllers:** `Controllers/Abwab/AbwabTemplateNodesController.cs`.
- **Metadata:** create/edit/reorder/delete are independent and not implied by a template permission.
- **Smoke:** pay the relevant node portion of templates row 8 under exact and neighboring personas.
- **Behavioral guard:** root edit still renames, root reorder/delete still refuse, and subtree
  retirement remains one visible delete action.

### Composite-action decisions

| User-visible action | Required permission | Why one permission is sufficient | Privilege-escalation guard |
|---|---|---|---|
| Single or bulk move | `abwab.doors.move` | Same visible capability and domain operation at different cardinality | No create/edit/archive side authority; all targets validated atomically |
| Single or bulk archive | `abwab.doors.archive` | Both archive selected root subtrees | Does not authorize restore or hard delete |
| Restore | `abwab.doors.restore` | The swept subtree is the consequence of the selected restore | Does not inherit archive permission |
| Door/section reorder | Corresponding `.reorder` | Internal sibling resequencing is implementation detail | Scope and concurrency checks remain |
| Delete section | `abwab.sections.delete` | Structural precondition and race translation belong to one visible deletion | Refuses live doors; does not grant door archive/move |
| Relation multi-target add | `abwab.relations.create` | One submitted relationship operation | All-or-nothing; delete remains separate |
| Create template | `abwab.templates.create` | Mandatory root creation is part of creating a template | Does not authorize later node writes |
| Edit template root node | `abwab.template_nodes.edit` | Existing UI/domain models root name as a node edit | Does not grant template create/delete |
| Apply template | `abwab.templates.apply` | Copying child subtrees is the visible operation | No hidden door-create grant; target validation and collision checks prevent arbitrary escalation |
| Delete template node | `abwab.template_nodes.delete` | Descendant retirement is the selected subtree delete | Root refusal remains; template delete separate |

## 11. Security-administration Backend plan

All routes in this section carry explicit active-Owner-only metadata. None accepts or assigns a role
and none uses the 19 Abwab permissions as its own authorization.

### Proposed route contracts

| Method and route | Use case and planning-level response | Transaction/concurrency/audit |
|---|---|---|
| `GET /api/access/users` | Paged users; filters `status`, `isOwner`, normalized search; summary includes local ID, email/display name, status, isOwner, permission count, created/updated time, version | Read-only projection; deterministic `(updatedAt desc,id desc)` or `(id)` ordering |
| `GET /api/access/users/{userId}` | Detail including profile, current `sub`, status, isOwner, current permission codes, version, and relevant timestamps | Read-only; `404` unknown |
| `POST /api/access/users/{userId}/accept` | Accept a Pending non-owner; body has expected version, optional initial known permission codes, and reason | One transaction emits `UserAccepted`, `UserActivated`, then per-code `PermissionGranted`; empty list is valid; `409` stale |
| `POST /api/access/users/{userId}/disable` | Disable an Active non-owner; body has expected version and required reason | One transaction removes every current grant, emits per-grant revoke with disable reason, then `UserDisabled`; `409` stale |
| `POST /api/access/users/{userId}/reactivate` | Reactivate a Disabled non-owner with zero grants; body has expected version and reason | One transaction emits `UserReactivated`; never restores grants |
| `GET /api/access/permissions` | Complete active catalogue: code, Arabic label, English description, group key/label/order, permission order | Unpaged 19-row result ordered by canonical metadata; database/catalogue parity asserted |
| `GET /api/access/users/{userId}/permissions` | Target status/isOwner/version plus current direct code set | Owners must return empty direct set and cannot be grant targets |
| `PUT /api/access/users/{userId}/permissions` | Replace the complete current direct code set; body has known codes, expected user version, and reason | Locks/checks target, computes grant/revoke deltas, audits each, commits atomically; idempotent identical set emits no false events |
| `GET /api/access/audit-events` | Owner-only keyset-paginated events with filters for target user, action, permission code, actor, and UTC range | Stable `(occurredAt desc,id desc)` cursor; bounded page size; snapshots are read-only |
| `POST /api/access/users/{userId}/logto-sub/relink/preview` | Server fetches proposed `newSub`, verifies matching primary email, reports target and old/new binding plus validation result | No mutation/audit change event; rate-limit and do not expose Logto tokens/raw payload |
| `POST /api/access/users/{userId}/logto-sub/relink/confirm` | Explicit confirmation body repeats expected target version, old/new `sub`, reason, and confirmation flag | Re-fetch/re-verify server-side; one transaction updates `LogtoSub` and emits `LogtoSubjectRelinked`; `409` if state changed |
| `GET /api/access/owner-reconciliation/status` | Read-only desired/current/unresolved/configured-disabled status and last reconciliation summary | Computes current comparison using trusted service; no mutation endpoint |

`Pending → Active` is the accepted transition; there is no fourth “accepted but inactive” status in
`UserStatus`. Therefore `accept` is the activation endpoint for Pending users and emits both audit
facts. `reactivate` is the separate Disabled → Active operation. Do not add a generic status setter.

### Request/response rules

- Reuse the shared `ApiResponse<T>` envelope and existing pagination conventions where the data size
  is naturally bounded. Audit uses an opaque keyset cursor because it grows indefinitely.
- User and grant mutation DTOs carry the user concurrency version; never use last-write-wins.
- Permission inputs are codes, not numeric IDs. Unknown, retired, duplicate, or group-like codes are
  `400`.
- Owner targets are rejected by normal accept/disable/reactivate/grant endpoints. Owner membership
  changes only through reconciliation.
- A permission replacement for Pending/Disabled or Owner is rejected; accepting may include initial
  grants only inside the acceptance transaction after the target becomes Active.
- Every mutation requires a nonblank, bounded reason where the locked audit contract says it is
  applicable. Server metadata adds request correlation and actor identity.
- Application handlers fetch actor state from the same database source and recheck Active Owner
  inside the transaction; endpoint authorization alone is not the transaction-time concurrency
  guarantee.
- Domain/transition errors map to controlled `400`; unknown target to `404`; unique/relink or
  optimistic race to `409`; authentication/authorization remain centralized `401`/`403`.

### Service boundaries

Use focused commands/queries under `Application/Access/`:

- `ListAccessUsers`, `GetAccessUser`;
- `AcceptAccessUser`, `DisableAccessUser`, `ReactivateAccessUser`;
- `GetPermissionCatalogue`, `GetUserPermissions`, `ReplaceUserPermissions`;
- `ListAccessAuditEvents`;
- `PreviewLogtoSubjectRelink`, `ConfirmLogtoSubjectRelink`;
- `GetOwnerReconciliationStatus`.

Controllers remain HTTP-only and call these handlers. EF reads/writes implement Application
abstractions under `Infrastructure/Persistence/Reads/Access/` and
`Infrastructure/Persistence/Writes/Access/`. The mutation service owns the transaction so audit and
state cannot diverge.

### Owner and relink safety

- Ordinary user operations cannot target an Owner even if another Owner is acting.
- Last-active-Owner checks exist only in reconciliation because no ordinary endpoint mutates Owner.
- Relink never changes role, status, or grants.
- The proposed `newSub` must be unlinked locally and have a currently verified primary email equal to
  the target `Users.NormalizedEmail` after both values pass through the shared application
  normalizer. Raw/display `Users.Email` and provider casing/whitespace are never compared for
  identity. Email matching alone never performs the update.
- A different local user with the same normalized email is a fail-closed `409`/preflight error and
  requires explicit identity resolution. Relink never merges users or transfers authority to
  resolve a normalized collision.
- Confirmation redoes verification; a stale preview cannot be replayed after target version,
  old `sub`, new-sub ownership, config membership, or verification changes.
- Relinking an Owner additionally requires that normalized email remain in successfully reconciled
  Owner configuration.
- A failed confirm leaves old subject, grants, status, Owner state, and audit unchanged.

Backend OpenAPI and integration tests must stabilize before implementing the administration UI.

## 12. `/api/access/me` transition plan

### Target response

```text
sub
email
displayName
status
isOwner
permissions
```

`permissions` is an ordered array of active direct codes. It is empty for Owners, Pending users,
Disabled users, and active read-only users. `isOwner` is authoritative for Owner UX. Public or
internal numeric `roleId` is removed from the target contract.

### Additive rollout

1. Change `ProvisionedUser` and `CurrentUserResponse` to add `IsOwner` and `Permissions` while
   retaining nullable `RoleName` temporarily.
2. Stop returning `RoleId` to new generated clients; if simultaneous removal would break the
   currently deployed frontend, retain it for one Backend deployment only and mark it
   compatibility-only. It must never be read for authorization.
3. Constrain transitional `roleName` to `"Owner"` or null after legacy conversion. Admin/Editor
   values remain possible only during the bounded data transition and are ignored by new clients.
4. Export OpenAPI using `Backend/scripts/export-swagger`, regenerate models with
   `npm run generate:api`, and run `Backend/scripts/check-api-contract`.
5. Update `Frontend/.../core/auth/current-user.model.ts`,
   `current-user.store.ts`, and generated `current-user-response.ts` through generation, never by
   hand-editing generated output.
6. Remove `roleId`, then `roleName`, only after frontend access-store deployment and legacy role
   cleanup are verified.

### Provisioning/status behavior

- Authenticated existing users receive their current status and authorization projection.
- A new ordinary verified identity is provisioned Pending, role-less, grant-less, and returns
  `isOwner:false`, `permissions:[]`.
- A configured verified Owner identity may bootstrap through the guarded reconciliation flow and
  return Active Owner with `permissions:[]`.
- A Disabled configured Owner remains Disabled, may have `isOwner:true` as identity/status data, but
  `can` remains false because status is not Active. The Backend handler independently denies writes.
- A same-email/different-`sub` collision remains `409` and never inherits the existing local user’s
  grants. “Same email” means the shared normalized value stored in `Users.NormalizedEmail`, not raw
  display-string equality.
- `/me` does not make public pages wait for provisioning and is called only when a Logto session
  exists or access-aware UI needs it.

### Refresh semantics

`CurrentUserStore.refresh()` must bypass the existing load-once promise and replace the access
snapshot after:

- a `403` on any write;
- a current-account grant/revoke/status change observed by the session;
- successful relink login under the new subject;
- an Owner reconciliation that affects the current account;
- authentication callback/session renewal.

Administration mutations also refresh the target detail/grant query in the Owner UI. No mutation is
automatically retried after refresh. Because v1 has no cross-request Backend cache, the next request
sees committed state without an invalidation message.

### Contract tests

- Old-compatible DTO deserialization while additive fields land.
- Exact target field casing/types in OpenAPI.
- Owner has `isOwner:true` and empty permissions.
- Active non-owner has only current known direct codes.
- Pending/Disabled have empty permissions.
- Read-only active user has empty permissions.
- Legacy Admin/Editor role does not produce `isOwner` or permissions.
- Unknown `sub` provisions only through `/me`; authorization handlers never invoke provisioning.

## 13. Frontend authorization integration plan

### Access-state foundation

Evolve the current signal store rather than adding a competing feature-local auth service. It owns:

- Logto authentication state;
- the current `/api/access/me` snapshot or null;
- load, force-refresh, and clear-on-logout behavior;
- `isActive`;
- `isOwner`;
- an immutable `ReadonlySet<PermissionCode>`;
- `can(code) = isActive && (isOwner || permissionSet.has(code))`;
- `canAny(codes)` for presenting a container whose children remain independently gated;
- controlled load/error status.

Concurrent `ensureLoaded` calls share one request; `refresh` supersedes stale in-flight results so an
older `/me` response cannot overwrite a newer permission state. Logout clears snapshot, permission
set, and pending promises. Unknown/loading/error access state is fail-closed for write affordances,
so controls do not flash enabled before `/me` resolves. An anonymous public page does not call
`/me` simply to render.

### Route behavior

- Keep existing Dashboard, Mushaf, Words, Abwab, templates, placeholder/content routes, callback,
  and wildcard behavior unguarded (`app.routes.ts:22-69`, `abwab.routes.ts:12-23`).
- Add a lazy Owner security-administration child at a proposed `/settings/access` route. Protect only
  that route with an `ownerGuard` that authenticates, loads `/me`, requires Active Owner, and
  preserves the intended URL through Logto.
- Keep `/settings` itself public unless it begins returning security-administration data; use an
  Owner-only card/link inside the existing settings surface rather than changing
  `NAV_ITEMS`/top-navbar in v1. This avoids turning normal navigation into an auth boundary and does
  not trigger navbar testing debt H1.
- An anonymous deep link to `/abwab`, `/abwab/templates`, or any public content URL renders directly
  without Logto.
- An anonymous deep link to `/settings/access` initiates Logto and returns to that route.
- A non-owner/Pending/Disabled security-admin deep link receives a controlled access page/redirect;
  it must not redirect public content routes.

### Owner security-administration frontend

After the Backend/OpenAPI contracts in section 11 are stable, add a feature folder such as:

```text
Frontend/quran-dashboard-ui/src/app/features/access-admin/
  pages/
  components/
  data-access/
  state/
  models/
  access-admin.routes.ts
  README.md
```

The first UI provides:

- paged/filterable user list and user detail;
- accept, disable, and reactivate flows with visible status/version;
- current grant editor grouped as Doors, Sections, Relations, Templates, Template nodes;
- per-group “manage all” select-all that submits only individual codes and permits unchecking;
- permission-diff confirmation and reason;
- relink preview plus separate explicit inline confirmation;
- reconciliation status read-only view;
- basic paginated/filterable audit retrieval sufficient to verify history, without promising a
  comprehensive audit analytics UI.

Use inline confirmation panels rather than introducing a new modal/backdrop consumer solely for this
feature; if implementation chooses a new dialog, `TESTING_DEBT.md` E2/E3 triggers must be paid and
scope re-approved. Owner status is displayed but has no editable role/status/grant controls.

### `401` behavior

One core HTTP/auth error coordinator handles administrative write `401`:

1. capture the current intended router URL/query/fragment in the established safe return-location
   mechanism;
2. invoke Logto authorization/session flow once;
3. do not retry the mutation;
4. after successful callback, restore the read location, not the submitted mutation;
5. leave anonymous public GET failures to normal transport handling rather than forcing login.

### `403` behavior

The write orchestration layer:

1. extracts and displays the Backend’s controlled Arabic envelope message;
2. force-refreshes `/api/access/me`;
3. re-evaluates `can`;
4. closes, cancels, or disables the stale write surface while retaining the public base page;
5. clears incompatible URL-restored write-modal state;
6. never automatically retries the write.

Update `abwab-write.controller.ts`, whose current failure mapping treats 401/403 as generic transport
errors (`docs/security/authorization-permissions-current-state-report.md` §10.1), or add a small
core reusable write-auth failure mapper consumed by Abwab and access-admin. Preserve existing
`400`/`404` invalid and `409` conflict handling.

## 14. Abwab frontend permission-gating matrix

Read navigation, search, tree/cards/archive rendering, relation viewing, template list/detail, and
template tree rendering remain available to everyone. The table describes only write affordances.

| Surface/event path | Permission | Authorized UX | Authenticated without permission | Anonymous UX |
|---|---|---|---|---|
| Main-page “create root door” toolbar action | `abwab.doors.create` | Enabled; opens create modal | Hide write-only action | Show content; optional single login affordance in the page authoring area, not an enabled action |
| Door row `＋`, side-panel add child, row-menu add child, quick-add dispatch | `abwab.doors.create` | All paths enabled | Hide each path; event handlers reject stale/programmatic emission | No active control |
| Door edit side-panel/menu action, edit modal submit, inline edit if present | `abwab.doors.edit` | Enabled | Hide contextual action; modal cannot open/submit | No active control |
| Single move action and picker confirmation | `abwab.doors.move` | Enabled | Hide contextual action; picker cannot open/confirm | No active control |
| Tree inline door order button, Enter commit, keyboard reorder | `abwab.doors.reorder` | Order value is an editable button; Enter submits | Render read-only order value, not an interactive editor; keyboard path inert | Read-only order |
| Single archive action and confirm | `abwab.doors.archive` | Enabled | Hide contextual action; confirm cannot open/submit | No active control |
| Archive-view restore button and restore confirm | `abwab.doors.restore` | Enabled subject to existing parent/section rules | Prefer disabled stable restore control with accessible Arabic permission explanation so archived hierarchy remains understandable | Read-only archive; no enabled restore |
| Bulk-mode entry | Any of `doors.move`, `doors.archive`, `relations.create` | Show if at least one bulk-capable action is allowed | Hide when none; selecting rows never implies authority | No bulk mode |
| Bulk move | `abwab.doors.move` | Enabled when selection/domain rules allow | Hide/disable independently of other bulk actions | No active control |
| Bulk archive | `abwab.doors.archive` | Enabled when selection/domain rules allow | Hide/disable independently | No active control |
| Bulk relation flow | `abwab.relations.create` | Enabled | Hide/disable independently | No active control |
| “Manage sections” container | Any section permission | Open read-aware modal when at least one child write is possible | Hide container when none; content page still renders | No active control |
| Section create field/submit | `abwab.sections.create` | Enabled | Hide create form | No active control |
| Section rename trigger/draft/submit | `abwab.sections.edit` | Enabled | Render name read-only; no edit state | Read-only |
| Section order trigger/Enter commit | `abwab.sections.reorder` | Enabled | Render order read-only; Escape/blur semantics unchanged for authorized edit | Read-only |
| Section delete | `abwab.sections.delete` | Enabled subject to empty-section rule | Hide contextual delete | No active control |
| Relations modal read/list | None | Always readable | Always readable | Always readable through public GET |
| Relation add form/submit, anchor/multi-target flow | `abwab.relations.create` | Enabled | Hide add mode; modal remains view-only | View-only |
| Relation delete affordance/confirm | `abwab.relations.delete` | Enabled | Hide delete affordance; relation remains visible | View-only |
| Templates page “new template” and submit | `abwab.templates.create` | Enabled | Hide authoring action; list remains | Read-only list |
| Template delete menu/confirm | `abwab.templates.delete` | Enabled | Hide contextual delete | No active control |
| Template apply/copy action and submit | `abwab.templates.apply` | Enabled | Hide action; a stale open flow may submit once and handle Backend 403 without retry | No active control |
| Template-node row `＋`, quick-add, add modal submit | `abwab.template_nodes.create` | Enabled | Hide all create paths; dispatch guard remains | Read-only tree |
| Template-node edit/root rename and modal submit | `abwab.template_nodes.edit` | Enabled | Hide edit; node/root name remains readable | Read-only tree |
| Template-node inline order, Enter, keyboard path | `abwab.template_nodes.reorder` | Enabled for non-root | Render read-only order; root refusal unchanged | Read-only |
| Template-node delete menu/confirm | `abwab.template_nodes.delete` | Enabled for non-root | Hide delete | Read-only |
| Doors/templates context menus | Exact permission per menu item | Menu contains only authorized write items plus any read actions | Hide unauthorized item; right-click/ContextMenu/Shift+F10 cannot dispatch absent item | Read-only menu or no write menu |
| Modal submission in every write modal | Same code as opener | Submit rechecks `can` before API call | Disabled/closed when fresh state lacks permission; stale state relies on Backend 403 and refresh | No mutation |
| URL-restored write overlay (`modal` query state) | Permission for represented action | Restore normally | Strip/replace only the unauthorized write-modal key and keep the public page/query state | Keep public page; optional login affordance, never auto-open after login without deliberate flow |

### Implementation placement

- Inject the access store into page-scoped orchestration/controllers, not each presentational leaf
  when inputs can carry `canX` state.
- Page/controller dispatch methods recheck permission even when the visible child control was gated.
- Presentational components receive explicit booleans for stable disabled/read-only rendering and do
  not import raw permission strings.
- `AbwabPageComponent`/`AbwabPageOverlaysController` gate door, section, relation, archive, bulk,
  context, keyboard, and URL-modal paths
  (`abwab-page.component.html:27-290`, `abwab-page-overlays.controller.ts`).
- `AbwabTemplatesPageComponent` and its controllers gate template/node actions
  (`abwab-templates-page.component.html:42-251`).
- `AbwabWriteController` and `AbwabTemplatesController` remain the last frontend dispatch guard and
  explicit 401/403 mapper.
- Existing component behavior tests are extended rather than duplicating a second authorization
  harness in every leaf.

## 15. Access-audit implementation plan

### Event model

Use stable action values:

- `UserAccepted`;
- `UserActivated`;
- `UserDisabled`;
- `UserReactivated`;
- `PermissionGranted`;
- `PermissionRevoked`;
- `LogtoSubjectRelinked`;
- `OwnerGrantedByReconciliation`;
- `OwnerRemovedByReconciliation`.

Each event contains:

- immutable numeric/UUID event ID and UTC time;
- actor type `User` or `System`;
- nullable actor local user ID plus immutable actor snapshot;
- target local user ID plus immutable target snapshot;
- action type;
- nullable stable permission code;
- versioned before-state and after-state JSON;
- bounded human reason where applicable;
- structured metadata including correlation ID, schema version, and operation-specific provenance.

### Writer contract and atomicity

`IAccessAuditAppender` appends to the caller’s EF transaction and exposes no save/commit of its own.
The use-case service:

1. authorizes/rechecks actor;
2. loads and locks/checks target state;
3. constructs complete before snapshots before mutating tracked rows;
4. applies state/grant changes;
5. appends events in deterministic order;
6. saves and commits once.

Disable event order is permission revocations ordered by canonical permission order, then
`UserDisabled`. Acceptance is `UserAccepted`, `UserActivated`, then optional grants. Reconciliation
orders candidates by `Users.NormalizedEmail` so retries produce explainable output. Owner promotion
orders `PermissionRevoked` events by canonical permission order, deletes all direct grants, attaches
the Owner role, then appends `OwnerGrantedByReconciliation`, all in the same serialized transaction.
An event insert failure at any point rolls back every grant, role, status, and audit change.

### Immutability

- `AccessAuditEvent` construction requires all invariant fields and exposes no ordinary mutation
  methods.
- Infrastructure contains an append-only writer and read-only projection reader; no update/delete
  abstraction exists.
- DbContext save interception or equivalent invariant validation rejects tracked audit entities in
  `Modified` or `Deleted` state during ordinary application saves.
- No controller, administration command, cascade delete, retention job, or EF relationship can
  update/delete audit rows.
- Operational DBA recovery remains outside ordinary application behavior and must not be modeled as
  a dashboard feature.

### Retrieval

`GET /api/access/audit-events` is Owner-only and keyset-paginated. Default/max page sizes are bounded.
Filters include target user, actor user, action type, permission code, and UTC range. Results include
snapshots and metadata necessary to explain the event but redact infrastructure secrets and raw
Logto tokens. Ordering is newest first with `Id` as deterministic tie-breaker.

### Required audit tests

- Each event type includes required actor/target/before/after fields.
- User actor and system reconciliation actor shape differ correctly.
- Grant/revoke event permission codes remain readable after catalogue metadata changes.
- Disable with N grants produces N revoke events plus one disable event and zero current grants.
- Any forced audit insert failure leaves status/role/sub/grants unchanged.
- Relink failure writes no false success event; success records old/new `sub`.
- Reconciliation no-op writes no false membership event; effective changes write one each.
- Owner promotion with zero, one, and several grants produces respectively zero, one, and several
  ordered revocation events before `OwnerGrantedByReconciliation`, and leaves zero direct grants.
- Forced audit failure during Owner promotion rolls back grant removal and role assignment.
- Ordinary EF update/delete attempts fail.
- Owner-only retrieval enforces 401/403 and pagination/filter ordering.

## 16. Testing strategy and debt payoff

Tests are written before protection is activated. Selection follows `TESTING_STRATEGY.md`; route,
contract, authentication, middleware, model-binding, and migration phases require the Backend Smoke
gate, and evidence must state whether the data tier ran or skipped.

### Test families

| Family | Planned coverage | Likely files |
|---|---|---|
| Pure unit | Shared email normalizer casing/whitespace vectors; Owner-list normalization/duplicates; catalogue syntax/uniqueness/order; permission-group expansion; failure-reason mapping | Existing `Tests/Api/Access/` plus focused normalizer/catalogue/options tests |
| Authorization handlers | Active Owner bypass, disabled Owner, exact/neighboring permission, unknown/Pending/Disabled, missing `sub`, token claim smuggling, resolver failure | New `Tests/Api/Authorization/` or current `Api/Access/` convention |
| Persistence/integration | Required/unique `Users.NormalizedEmail`; normalized collision preflight/backfill; provisioning and relink collisions; resolver query result; one scoped resolution; grant uniqueness; transition/concurrency; disable/revoke/audit atomicity; serialized reconciliation; Owner promotion with zero/one/many grants and audit rollback; catalogue DB parity | `Tests/Api/Access/` Testcontainers fixture or focused `Tests/Access/` collection |
| Endpoint metadata parity | Missing/unknown/duplicate/conflicting unsafe metadata; public GET no-auth; method/route/access bidirectional parity | `SmokeCoverageParityTests.cs` plus focused startup-validator tests |
| API access | Shared 401/403 envelopes; `/me`; Owner-only admin routes; request DTO/status mappings; handler not invoked on denial | `Tests/Api/Access/` |
| Abwab route smoke | All 21 writes under persona matrix; existing success/domain status contracts; all four anonymous reads | `SmokeAbwabWriteTests.cs` split by resource if size demands |
| Conditional-read smoke | Anonymous ETag/304/malformed/mismatch/404/zero-query cases | Existing/new `SmokeAbwab*ReadTests.cs` |
| Frontend store/guard | load/refresh race, logout clear, `can`, Owner, read-only, 401 return URL, 403 refresh/no retry, Owner-only route guard | `core/auth/*.spec.ts`, app/access-admin route specs |
| Frontend component/page | Exact visibility/disabled/read-only behavior for all paths in §14; programmatic event guard; URL modal stripping | Existing Abwab page/component/controller specs and new access-admin specs |
| Supplementary E2E | Anonymous public browse, read-only persona, one exact grant, Owner, handcrafted API denial, stale revoke/403, deep-link behavior | `Frontend/quran-dashboard-ui/e2e/`; never a substitute for Backend Smoke |

### Data-driven authorization persona matrix

Run every unsafe classification through these personas:

| Persona | Local state/claims | Expected write/security result |
|---|---|---|
| Anonymous | No token | `401` shared envelope; no handler/mutation |
| Invalid token | Bearer present but invalid | `401` shared envelope |
| Authenticated unknown local user | Valid `sub`, no row | `403`; no implicit provisioning |
| Pending | Local Pending, no grants | `403` inactive |
| Disabled | Local Disabled; even with stale grant rows in corruption fixture | `403` inactive |
| Active read-only | Active, role null, no grants | Every write `403`; public reads `200`/domain result |
| Exact permission | Active non-owner, one mapped code | Mapped action reaches current domain handler |
| Neighboring permission only | Active non-owner, related but different code | `403`; no handler/mutation |
| Active Owner | Active, Owner role, zero grants | Every write/admin action reaches handler |
| Disabled Owner | Disabled, Owner role, zero grants | `403`; bypass does not run |
| Claim smuggling | Non-owner/unknown state plus JWT claims named Owner or known permission | Same denial as database state; claims ignored |

Use data rows for all 21 Abwab method/routes and the exact matrix rather than copying bespoke tests.
Authorized success fixtures must still supply valid request bodies/data; authorization tests should
assert reachability separately from domain success when fixture setup would obscure the security
question.

Identity/reconciliation integration data must additionally cover normalized casing and surrounding
whitespace, two current users collapsing to one normalized value, provisioning collision, Owner-list
comparison, and relink comparison. Serialized reconciliation scenarios cover promotion with zero,
one, and several grants; deterministic event ordering; forced audit failure rollback; concurrent
grant modification; two concurrent applies; concurrent Owner addition/removal; last-active-Owner
races; lock acquisition timeout; and idempotent retry after lock release.

### Five mandatory debt obligations

| Debt row | Required payoff in this feature | Removal condition |
|---|---|---|
| Abwab relations row 3 (`docs/TESTING_DEBT.md:33-37`) | Dispatch relation GET/POST/DELETE across `200`/`201`/`204`/`400`/`404`/`409`, including archived-anchor `200 []`, and write personas | Delete row only after tests land and pass |
| Abwab templates row 8 (`:58-63`) | Dispatch all nine template/template-node routes across success/failure envelopes plus auth personas | Delete row only after full nine-route coverage |
| F3 section reorder (`:83-87`) | Dispatch `200`/`400`/`404`/`409` plus auth personas | Delete F3 after coverage lands |
| G3 template apply (`:98-102`) | Cover narrowed `400` and reshaped `409` plus auth personas; does not replace row 8 | Delete G3 when both specific cases land |
| I2 conditional GET (`:138-142`) | Anonymous match/mismatch/malformed/`*`, headers, bodiless `304`, `404` header behavior, and zero-query list-read `304` | Delete I2 only when all remaining cases are covered |

### Public-read regressions

Anonymous callers must continue to receive:

- normal Quran/Mushaf/Words/Dashboard-info/public route results;
- `GET /api/health`;
- all four Abwab reads;
- Abwab `ETag` and `Cache-Control`;
- matching `If-None-Match` bodiless `304`;
- mismatch/malformed/`*` normal `200`;
- normal `404` behavior without auth challenge.

Pending and Disabled tokens must not make these public GETs forbidden. Tests assert the
authorization resolver is not required for these reads.

### Debt rows that remain deferred

The following recorded triggers are not reached if implementation respects this plan’s boundaries:

- relations rows 1–2: no relation reader/writer or door/section writer behavior change;
- templates rows 6–7 and G1–G2: no template/node/apply writer, unique-index, or refusal-order change;
- templates rows 9–10: permission gating does not change template-tree context-menu geometry,
  detachment, or workshop structure;
- F1–F2: no section writer/order correctness change;
- G4: gate template apply at page/controller boundaries; do not change the copy modal’s
  empty-template behavior;
- H1–H4: do not change navbar/nav model, mobile menu, or Abwab archive URL contract;
- I1, I3, I4: no cache-generation, templates-facade response, new Abwab write, or multi-instance
  change;
- J1, E1–E3, C1–C5, P1–P2, and R1: their writer/visual/modal/performance/import/comment/tree-reader
  triggers are unrelated and remain untouched.

If implementation changes one of those named areas—for example adds a new modal, edits the template
copy modal, changes navigation, or touches an Abwab writer—the phase stops and the newly triggered
debt becomes acceptance scope instead of being silently skipped. The feature-scoped Engineering
Review checkpoints below do not claim to pay C4’s separate whole-repository review debt.

### Verification commands by tier

Focused commands used in phases:

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.Access"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Frontend focused and full gates:

```sh
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/core/auth/**/*.spec.ts"
npm test -- --include="src/app/features/access-admin/**/*.spec.ts"
npm test -- --include="src/app/features/abwab/**/*.spec.ts"
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
npm test
npm run build
```

Backend Tier B/C no-pipeline regression uses the exact strategy filter:

```sh
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke."
```

`npm run e2e` is supplementary. Any generated migration changes the smoke dump’s migration manifest;
regenerate with `Backend/scripts/create-smoke-dump --yes` before counting a smoke run as valid, per
`TESTING_STRATEGY.md:176-207`.

## 17. Safe phase decomposition

This plan uses eleven phases rather than the suggested ten. The added Phase 8 gives Owner
user/permission administration UI its own boundary after Backend contracts stabilize and before
Abwab controls depend on grants. This prevents the exposed-write fix from being delayed by a large
combined frontend phase. Phase 5 can be implemented, reviewed, and activated in controlled
development/staging independently of Phase 6. Production activation is a separate gate: it
preferably follows completed/reviewed Phase 6, or explicitly accepts either a temporary Owner-only
write period or the trusted audited operator grant/revoke mechanism defined in Phase 5.

### Phase 1 — Test harness and contract preparation

**Goal:** Establish target test personas, route-access metadata types, public-read regressions, and
contract fixtures without changing runtime authorization.

**Depends on:** Nothing beyond the authoritative documents and current green baseline.

**In scope:**

- Add reusable Backend test builders for Anonymous, unknown local subject, Pending, Disabled,
  read-only, one-permission, neighboring-permission, Owner, Disabled Owner, and claim-smuggling.
- Refactor `SmokePersonas.cs`, `SmokeApiFixture.cs`, and Access fixtures so local status/role/grant
  setup can be expressed before grant tables exist, using phase-appropriate helpers.
- Add explicit public-read regression theory data for existing Quran/Mushaf/Words/Dashboard/health
  and four Abwab GET routes.
- Define the planned `SmokeRouteAccess` shape in tests/catalogue while preserving current dispatch.
- Add target `/me` and permission-contract fixtures without changing generated production models.
- Add shared email-normalization contract vectors for casing, surrounding whitespace, invalid
  values, and duplicate normalized results, plus a read-only current-user collision-scan fixture.
- Record baseline route count/method/path/access output for later bidirectional comparison.

**Out of scope:** Runtime authorization components, endpoint metadata, schema/migrations, Owner
mutation, frontend behavior changes.

**Likely files:**

- `Backend/tests/QuranDashboard.Tests/Api/Access/AccessTestFixture.cs`
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokePersonas.cs`
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeApiFixture.cs`
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`
- new focused public-access/contract theory files under `Tests/Api/Access/` and `Tests/Smoke/`
- `Frontend/.../core/auth/auth.testing.ts`

**Migrations:** Forbidden.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.Access"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/core/auth/**/*.spec.ts"
```

Smoke evidence must state data tier ran or skipped. Phase 1 changes test infrastructure, so also run
the Tier B no-pipeline filter before completion.

**Completion criteria:**

- Current public reads are explicitly pinned anonymous.
- Test personas/builders are reusable and do not authorize from claims.
- The current route catalogue still matches live method/path pairs.
- No production behavior, schema, or generated contract changed.
- The Phase 2 normalized-email migration/backfill contract and collision cases are executable test
  inputs; no verified-primary-email provider decision is required yet.

**Engineering Review checkpoint:** Review test quality against `.claude/skills/test-guard/`,
especially real entities/DTOs, data-driven variants, and genuine boundaries. Confirm no test-only
constant can become a second production permission authority.

**Rollback/stop condition:** Revert only the isolated harness change if it changes runtime
registration, relies on order-dependent smoke data, or cannot preserve the current route parity.
Stop if the current live route inventory differs from design-decisions §8.

### Phase 2 — Additive access schema, catalogue, and audit foundation

**Goal:** Establish required unique normalized user identity and add persistence for the 19-code
catalogue, current grants, and append-only audit while leaving roles and endpoint exposure
unchanged.

**Depends on:** Phase 1 fixtures.

**In scope:**

- Add the shared application email normalizer; preserve `Users.Email` for display and add
  `Users.NormalizedEmail`.
- Run the pre-DDL normalized-collision scan; fail for explicit resolution rather than merge/relink.
- Add `Permission`, `UserPermission`, `AccessAuditEvent`, action/actor value types, and user
  concurrency mapping.
- Add EF configurations, DbSets, indexes, FKs, and append-only safeguards from §5/§15.
- Update normal provisioning to persist the shared normalized value and fail closed on normalized
  identity collision, without changing status/role semantics.
- Add canonical Backend catalogue and idempotent synchronizer.
- Add initial `QuranDashboard.AccessAdmin` tool skeleton with normalized-email collision scan,
  catalogue sync, and schema/catalogue preflight only.
- Generate the staged additive migrations: introduce nullable `NormalizedEmail` and access storage,
  backfill through the application normalizer, then enforce not-null and unique index.
- Add normalized identity, schema/catalogue/grant/audit persistence tests.
- Regenerate the canonical smoke dump because migration count changes.

**Out of scope:** Owner list/reconciliation, granting production permissions, endpoint protection,
user administration APIs, Admin/Editor removal.

**Likely files/folders:**

- `Backend/domain/QuranDashboard.Domain/Access/`
- `Backend/application/QuranDashboard.Application.Abstractions/Security/Permissions/`
- `Backend/application/QuranDashboard.Application.Abstractions/Access/`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`
- `Backend/infrastructure/.../Persistence/Configurations/Access/`
- `Backend/infrastructure/.../Access/PermissionCatalogueSynchronizer.cs`
- `Backend/infrastructure/.../Migrations/` (generated)
- `Backend/tools/QuranDashboard.AccessAdmin/`
- `Backend/tests/QuranDashboard.Tests/Api/Access/`

**Migrations:** The staged Phase 2 additive boundary may contain two ordered generated migrations
solely so the application normalizer can backfill between nullable introduction and
required/unique enforcement. No legacy cleanup/data-conversion migration is allowed.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.Access"
Backend/scripts/create-smoke-dump --yes
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Then run the Tier B no-pipeline filter.

**Completion criteria:**

- Empty-to-current and current-to-additive migration paths succeed in Testcontainers.
- Casing and surrounding whitespace normalize consistently; current-user and provisioning
  collisions fail without merge/relink; all rows finish with required `NormalizedEmail` and its
  unique database index.
- All 19 rows synchronize idempotently and database/code parity is green.
- Duplicate grant and invalid FK cases fail.
- Audit rows can be appended/read but no ordinary update/delete path exists.
- Existing users/roles and public/write behavior remain unchanged.

**Engineering Review checkpoint:** Review entity responsibility, EF constraints/indexes, generated
migration/snapshot, audit immutability, and the tool boundary. Verify no permission group or
role-permission table slipped in.

**Rollback/stop condition:** Because this is additive and unused by authorization, rollback may
remove only the new access tables before any audit/grant data is accepted; `NormalizedEmail` is an
identity invariant and must not be dropped after compatible provisioning is active without an
explicit rollback design. Stop before DDL if normalized collisions exist; stop if any backfill row
is missing/different from the shared normalizer, if old and new provisioning cannot be coordinated,
if migration requires changing role data, if catalogue synchronization can delete/repurpose codes,
or if smoke-dump regeneration cannot be validated.

### Phase 3 — Owner-list configuration and reconciliation

**Goal:** Replace the scalar Owner bootstrap with validated multiple-Owner desired state and prove a
safe explicit reconciliation/recovery operation.

**Depends on:** Phase 2 audit storage and operator tool.

**In scope:**

- Validate the tenant-authoritative Logto verified-primary-email signal.
- Bind/validate `OwnerBootstrap:Emails` through the Phase 2 shared normalizer and compare only
  `Users.NormalizedEmail`.
- Update provisioning to use the list and same guarded reconciliation rules.
- Implement status/dry-run/apply/preflight Owner tool commands.
- Serialize every apply with the dedicated transaction-scoped Owner-reconciliation advisory lock;
  reload desired/config/database/verified state under the lock.
- Apply Owner additions/removals transactionally with audit and last-active-Owner protection.
- Promote active non-owners with zero, one, or several grants by auditing/revoking every grant,
  deleting the grants, assigning Owner, and auditing `OwnerGrantedByReconciliation` atomically.
- Update API/test configuration from `Auth:BootstrapOwnerEmail` to array values.
- Add multiple Owner, normalized-list comparison, duplicate/invalid/unresolved/unverified/Disabled,
  removal/recovery, promotion, audit-order/rollback, and concurrent-reconciliation tests.

**Out of scope:** Endpoint permission enforcement, direct-grant admin APIs, Admin/Editor conversion,
frontend permission UX.

**Likely files/folders:**

- `Infrastructure/Access/OwnerBootstrapOptions.cs`
- `Infrastructure/Access/UserProvisioningService.cs`
- `Infrastructure/Access/LogtoManagementApiUserProfileSource.cs`
- `Infrastructure/DependencyInjection/AccessDependencyInjection.cs`
- `Application/Access/OwnerReconciliation/`
- `Application.Abstractions/Security/IExternalUserProfileSource.cs`
- `Backend/tools/QuranDashboard.AccessAdmin/`
- API `appsettings*.json` placeholders and test host/fixtures
- current Access provisioning tests and new reconciliation integration tests

**Migrations:** Forbidden unless Phase 2 schema is proven insufficient; any such discovery is a stop
and plan amendment, not an ad hoc migration.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.Access"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Run tool dry-run/preflight against an isolated Testcontainers or disposable local database, never
production during development.

**Completion criteria:**

- Array binding and normalization are deterministic and fail fast.
- Several configured verified Owners coexist on one role row.
- Pending configured user may become Active Owner; Disabled configured user remains denied.
- Add/remove events are audited; no-op is idempotent.
- Owner promotion never leaves direct grants and does not reject a valid configured candidate merely
  because grants existed.
- Two concurrent applies, concurrent addition/removal, last-Owner races, lock timeout, and
  idempotent retry after lock release are fail-closed and deterministic.
- Zero/last-active-Owner state blocks enforcement preflight.
- Owner recovery through an added configured identity is proven.
- The verified-email signal has repository evidence and tests.

**Engineering Review checkpoint:** Security review the Logto evidence, configuration provenance,
system actor audit, transaction/retry behavior, output redaction, and last-Owner race. This checkpoint
must explicitly reject email-only trust.

**Rollback/stop condition:** Phase 3 cannot be accepted if Logto verification remains an inference,
any configured candidate is unresolved for the only intended Owner, reconciliation can
auto-reactivate Disabled users, promotion can leave grants, or apply can mutate without the shared
serialization boundary. Configuration rollback must be another explicit reconciled desired state,
not a database role edit. This verified-email prerequisite does not block Phases 1–2.

### Phase 4 — Backend authorization core and controlled responses

**Goal:** Implement and prove request-scoped database authorization, exact permission and Owner-only
requirements, and controlled denials before attaching them to production writes.

**Depends on:** Phase 2 schema/catalogue. It may be implemented against controlled Owner fixtures
while Phase 3 validation is finishing, but no production activation may bypass the accepted Phase 3
verified-Owner preflight.

**In scope:**

- Add `AuthorizationState`/resolver and request-scoped memoization.
- Add permission and Owner-only requirements/handlers/metadata attributes.
- Add centralized rejection messages/writer/result handler for 401/403.
- Add unsafe metadata validator implementation and unit/startup fixture tests, but defer production
  registration until every existing unsafe endpoint is annotated atomically in Phase 5.
- Prove infrastructure failure is fail-closed.
- Stop using transformed/token role or permission claims in the new path.

**Out of scope:** Abwab controller annotations, security admin routes, frontend controls, legacy
class deletion.

**Likely files/folders:**

- `Application.Abstractions/Security/AuthorizationState.cs`
- `Application.Abstractions/Security/IAuthorizationStateResolver.cs`
- `Infrastructure/Persistence/Reads/Access/`
- new `Backend/api/QuranDashboard.Api/Authorization/`
- `Authentication/AuthenticationRegistration.cs`
- `Common/ApiMessages.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- authorization/access tests

**Migrations:** Forbidden.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.Access"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

**Completion criteria:**

- All persona/claim-smuggling handler tests pass.
- One scoped resolver query is shared by multiple requirements.
- 401/403 envelopes and Arabic-message selection are exact.
- Unknown/Pending/Disabled/Disabled Owner/DB failure never succeeds.
- Public GET and `/me` behavior remain unchanged.
- Validator tests reject every invalid metadata shape.

**Engineering Review checkpoint:** Review the authentication/authorization boundary,
challenge/forbid ownership, response double-write risk, resolver SQL shape, fail-closed exceptions,
and absence of controller/claim authorization.

**Rollback/stop condition:** Core registration may be removed because no production endpoint depends
on it yet. Stop if response handling breaks invalid-token 401, if resolver provisioning is observed,
or if public GET requests invoke authorization.

### Phase 5 — Protect all Abwab writes and pay mandatory smoke debt

**Goal:** Make the Backend implementation ready to close the production open-write exposure by
annotating all 21 writes, enabling startup validation, and completing required smoke coverage;
activate it in production only through the explicit gate below.

**Depends on:** Phases 1, 2, and 4 green for implementation/testing. Staging activation additionally
uses controlled Owner fixtures. Production activation requires accepted Phase 3 verified-Owner
reconciliation, all 19 database rows, and the Phase 5 production gate below.

**In scope:**

- Apply the exact metadata in §10 to all five Abwab controller groups.
- Register startup endpoint validation.
- Extend smoke access metadata and bidirectional parity.
- Add complete authorization personas for every write.
- Pay relations row 3, templates row 8, F3, G3, and I2.
- Add handler-not-run/no-mutation/no-cache-invalidation assertions for denied writes.
- Preserve all four anonymous Abwab reads and public conditional GET behavior.

**Out of scope:** Security administration APIs/UI, Abwab frontend visibility, any Abwab handler/writer
refactor or domain correction. No temporary public or broadly authenticated grant-management
endpoint and no weakened authorization fallback may be introduced to bridge rollout.

**Likely files/folders:**

- `Api/Controllers/Abwab/*.cs`
- `Api/Authorization/Validation/` registration
- `Tests/Smoke/SmokeRouteCatalog.cs`
- `Tests/Smoke/SmokeCoverageParityTests.cs`
- `Tests/Smoke/SmokeAbwabWriteTests.cs` and focused relation/template/read smoke files
- `docs/TESTING_DEBT.md`
- Abwab/API/Authentication READMEs

**Migrations:** Forbidden.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Run the Tier B no-pipeline filter and `Backend/scripts/check-api-contract` if authorization response
metadata changes OpenAPI.

**Completion criteria and activation gates:**

- **Implementation readiness:** startup refuses any missing/unknown/conflicting unsafe
  classification; every one of the 21 routes produces 401/403/exact/neighbor/Owner behavior; active
  Owner with zero grants reaches all actions; existing authorized domain tests remain green; the
  five mandatory debt rows are deleted only after their tests land; and anonymous Abwab reads,
  ETag/304, and other public content reads remain green.
- **Staging activation readiness:** implementation readiness passes in the deployed staging shape
  with controlled test Owners/non-owners, Phase 3 reconciliation evidence is available for those
  fixtures, and direct requests prove enforcement. Development/staging may activate here.
- **Production activation readiness:** Phase 3 is accepted with a verified configured local Active
  Owner and production preflight passes. The preferred gate also requires Phase 6 implementation
  and Engineering Review to be complete so Owners can administer grants as soon as enforcement is
  live.
- Production may activate before Phase 6 only when one of two conditions is explicitly accepted and
  verified: a temporary Owner-only write period is operationally acceptable; or the trusted
  operator tool exposes secure, transactional, append-only-audited grant/revoke commands for
  individual direct permissions.
- Any operator grant/revoke command must invoke the same application validation, transaction,
  active-Owner authorization, grant-delta, concurrency, and audit services used by Phase 6. It must
  not duplicate business logic or become an alternate authorization path.

**Engineering Review checkpoint:** Formal security-focused engineering review of all live route
metadata, endpoint parity, denial bodies, public-read regressions, and composite permissions. Compare
the live endpoint list mechanically to §10 rather than sampling. The review records implementation,
staging-activation, and production-activation readiness separately.

**Rollback/stop condition:** No partial controller deployment. If any route or smoke obligation is
missing, do not activate the Backend. Do not activate in production merely because Phase 5 code is
complete. If Phase 6 is not ready, stop unless one approved early-activation condition is recorded.
After production activation, never roll back to the prior open build; use a protected previous/new
build or an operational write-maintenance response while public reads remain online.

### Phase 6 — Owner-only user, permission, audit, and relink Backend APIs

**Goal:** Add stable Owner-only contracts and transactional use cases needed to administer active
non-owner users and inspect security history.

**Depends on:** Phase 5 Backend authorization/Owner-only metadata being implemented and reviewed;
Phase 5 does not need to be production-activated first.

**In scope:**

- Implement every route/use case in §11.
- Add target row concurrency, transition validation, transaction-time actor recheck, and audit.
- Add catalogue/grant/audit/relink/reconciliation-status projections.
- Add API/OpenAPI tests and route catalogue entries with `OwnerOnly`.
- Keep Owner configuration mutation out of HTTP.
- If an early Phase 5 production activation uses operator grant/revoke commands, implement them as
  thin trusted callers over these same application transaction/validation/audit services; do not
  add a temporary HTTP endpoint.

**Out of scope:** Frontend administration screens; exhaustive audit analytics; ordinary Owner
mutation; generic role management.

**Likely files/folders:**

- `Application/Access/{Commands,Queries}/`
- `Application.Abstractions/Access/`
- `Infrastructure/Persistence/{Reads,Writes}/Access/`
- `Api/Controllers/Access/` split by route family before controller size limits
- `Api/Contracts/Access/` if API-local request contracts are required
- `Tests/Api/Access/`, `Tests/Smoke/`

**Migrations:** Forbidden unless a proven contract cannot fit Phase 2; stop for review if so.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.Access"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
Backend/scripts/check-api-contract
```

**Completion criteria:**

- All routes are Owner-only in live/catalogue parity.
- Normal operations cannot target Owners.
- Accept/disable/reactivate/grant/revoke/relink atomicity and concurrency pass.
- Disable removes every current grant; reactivate restores none.
- Audit retrieval is bounded, filterable, immutable, and Owner-only.
- Relink repeats server verification and cannot transfer authority early.

**Engineering Review checkpoint:** Review controller thinness, transactional boundaries, owner-target
and last-owner separation, pagination/query limits, audit snapshots, relink takeover resistance, and
OpenAPI envelope consistency.

**Rollback/stop condition:** Backend can roll back this administration surface while retaining Phase
5 write protection only when another approved grant-administration mechanism remains available or a
temporary Owner-only period is explicitly accepted. Stop if any mutation can commit without audit,
if a direct permission can satisfy Owner-only, or if concurrency is last-write-wins.

### Phase 7 — `/api/access/me` additive contract and frontend access foundation

**Goal:** Make current authorization state available to the frontend without changing public route
admission.

**Depends on:** Stable Phase 6 contracts.

**In scope:**

- Add `isOwner`/`permissions`, retain bounded transitional `roleName`, remove or deprecate `roleId`.
- Regenerate OpenAPI/frontend models.
- Evolve current-user store, typed permission catalogue/parity, refresh/race/logout behavior.
- Replace `roleGuard` with Owner-only security-admin guard; attach it only to `/settings/access`.
- Add core 401/403 coordination and return-location behavior.

**Out of scope:** Access-admin pages/forms; Abwab control gating; public route guards.

**Likely files/folders:**

- `Api/Controllers/Access/AccessController.cs`
- `Application.Abstractions/Security/ProvisionedUser.cs`
- provisioning/current-user handlers
- `Frontend/.../core/api/generated/`
- `Frontend/.../core/auth/`
- `Frontend/.../app.routes.ts` and route specs
- OpenAPI JSON and contract scripts

**Migrations:** Forbidden.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.Access"
Backend/scripts/check-api-contract
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/core/auth/**/*.spec.ts"
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
npm test
npm run build
```

Run Backend Smoke because auth and response contracts changed.

**Completion criteria:**

- New and transitional clients deserialize `/me`.
- Store derives exact permission behavior and refreshes without stale overwrite.
- Owner has empty permission array and still `can` all writes.
- Public routes render without authentication or `/me`.
- Only security admin route invokes the Owner guard.
- 401 preserves intended location; 403 refreshes and never retries.

**Engineering Review checkpoint:** Review generated/handwritten model boundary, race handling,
anonymous bootstrap behavior, route guard placement, raw string drift, and absence of `roleName`
authorization.

**Rollback/stop condition:** Backend additive fields may remain if frontend rollout is paused.
Stop if any public route waits on `/me`, if the generated client is stale, or if old frontend clients
break before additive compatibility is deployed.

### Phase 8 — Owner security-administration frontend

**Goal:** Give active Owners a safe UI to accept users, manage direct grants, inspect audit, and
perform explicit relink, using only stable Phase 6 contracts.

**Depends on:** Phases 6–7.

**In scope:**

- Add `/settings/access` feature route/page/store/API/models/README.
- Implement user list/detail and status transitions.
- Implement grouped individual permission editor with select-all bundle behavior.
- Implement inline diff/reason confirmation, relink preview/confirm, reconciliation status, and
  basic audit retrieval.
- Add exact Owner/Pending/Disabled/non-owner routing and component tests.

**Out of scope:** Navbar/nav model changes; role editor; Owner mutation; group grants; comprehensive
audit analytics; new modal/backdrop primitives.

**Likely files/folders:**

- new `Frontend/.../features/access-admin/`
- `core/navigation/route-paths.ts` for the route constant without adding a navbar entry
- `app.routes.ts`/route specs
- `core/auth/owner.guard.ts`
- generated access models

**Migrations:** Forbidden.

**Required commands:**

```sh
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/core/auth/**/*.spec.ts"
npm test -- --include="src/app/features/access-admin/**/*.spec.ts"
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
npm test
npm run build
```

Run Backend API/Smoke contract tests as a compatibility gate even if Backend source is unchanged.

**Completion criteria:**

- Only active Owners enter the route and call security APIs.
- No role selector/Owner mutation exists.
- Select-all expands to individual codes and users can uncheck any code.
- Disable warning states that grants are removed; reactivate starts empty.
- Relink is two-step, rechecked by Backend, and never email-only.
- Version conflicts refresh target state rather than overwrite.

**Engineering Review checkpoint:** Review Arabic-first UX, accessibility, security wording,
permission-diff correctness, concurrency conflict handling, and confirmation flow. Confirm no new
modal or nav-model debt trigger was reached.

**Rollback/stop condition:** Frontend administration UI may be withdrawn without weakening Phase 5
Backend protection; Owners can still use verified API/operator processes as scoped. Stop if UI can
submit Owner changes, group identities, unknown codes, or stale overwrites.

### Phase 9 — Abwab frontend permission-aware controls

**Goal:** Align every current Abwab write affordance/event path with the exact Backend permission
while preserving anonymous read UX.

**Depends on:** Phase 7 access store; Phase 8 supplies the normal grant-management path.

**In scope:**

- Implement every row in §14 across pages, controllers, components, context/keyboard/bulk/modal/URL
  paths.
- Add explicit Abwab 401/403 behavior and stale access refresh.
- Preserve public read rendering and all current domain/conflict UX.
- Add component/page/controller/persona tests and supplementary E2E.

**Out of scope:** Abwab domain/API changes, new write features, visual redesign, navbar changes,
template-copy behavior change.

**Likely files/folders:**

- `features/abwab/pages/abwab-page/`
- `features/abwab/pages/abwab-templates-page/`
- `features/abwab/state/abwab-page-overlays.controller.ts`
- `features/abwab/state/abwab-write.controller.ts`
- `features/abwab/state/abwab-templates.controller.ts`
- relevant side-panel/tree/archive/sections/relations/template-tree components and specs
- Abwab E2E persona fixtures

**Migrations:** Forbidden.

**Required commands:**

```sh
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/core/auth/**/*.spec.ts"
npm test -- --include="src/app/features/abwab/**/*.spec.ts"
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
npm test
npm run build
npm run e2e
```

Also rerun Backend API/Abwab/Smoke filters because Backend enforcement remains the acceptance
authority.

**Completion criteria:**

- Every §14 path is covered under anonymous, read-only, exact, neighboring, and Owner personas.
- A handcrafted request still fails independently of UI.
- 403 refreshes and closes/disables stale state without retry.
- URL-restored unauthorized write overlays do not open; public base URL remains.
- Public Abwab pages and templates remain anonymous.
- No unrelated debt trigger is reached.

**Engineering Review checkpoint:** Formal cross-layer review from visible control to dispatch to
Backend metadata. Sample no paths: mechanically enumerate outputs/handlers/context/keyboard/modal
URL actions against §14.

**Rollback/stop condition:** Frontend may roll back while Phase 5 Backend stays protected; the result
may show controls that receive 401/403 but cannot expose writes. Stop if the implementation needs an
Abwab writer/domain change or alters public routing.

### Phase 10 — Legacy data conversion and Admin/Editor cleanup

**Goal:** Rehearse and prepare the irreversible transition to Owner-only roles, then remove legacy
role authorization code after the new path is proven.

**Depends on:** Phases 3, 5, 6, 7, and a successful production-like inventory rehearsal.

**In scope:**

- Finalize `legacy-roles inventory/convert` operator commands.
- Reconcile every intended Owner from configuration/verification.
- Convert all Admin/Editor references to null with zero grants.
- Generate cleanup migration removing Admin/Editor seeds.
- Remove Admin/Editor constants/policies/tests/frontend union members.
- Remove `RoleClaimsTransformation`, `IUserRoleResolver`, `CachedUserRoleResolver`, DI/cache tests.
- Remove transitional `/me` `roleId`, then `roleName`, after frontend no longer consumes them.
- Regenerate OpenAPI and smoke dump.

**Out of scope:** Inferring grants, converting role names to permissions, deleting Owner role,
hard-deleting users/audit, changing public reads.

**Likely files/folders:**

- `Domain/Access/RoleNames.cs`
- `Persistence/Configurations/Access/RoleConfiguration.cs`
- generated migrations/snapshot
- `Api/Authentication/AuthenticationRegistration.cs`, old role files
- `Infrastructure/Access/CachedUserRoleResolver.cs`
- `Application.Abstractions/Security/IUserRoleResolver.cs`
- legacy Access tests and frontend role guard/model
- operator tool and READMEs

**Migrations:** One generated cleanup migration is allowed after data-conversion preflight. It must
not carry inferred grants.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.Access"
Backend/scripts/create-smoke-dump --yes
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
Backend/scripts/check-api-contract
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/core/auth/**/*.spec.ts"
npm test
npm run build
```

Run Tier B no-pipeline after Backend build.

**Completion criteria:**

- Production-like rehearsal inventory balances before/after.
- Every intended Owner is configured, verified, reconciled, and preserved.
- All former Admin/Editor users are role-less and grant-less.
- Only Owner seed/constant/policy semantics remain.
- No transformed/cached role claim path is registered.
- Current clients consume only `isOwner`/permissions.
- Cleanup migration fails safely if legacy references remain.

**Engineering Review checkpoint:** Review conversion reports, migration operations/snapshot, Owner
preservation, zero-grant proof, removal reachability, generated contract, and lack of dangling
Admin/Editor references via whole-repo `rg`.

**Rollback/stop condition:** Before cleanup migration, stop on any unidentified user, intended Owner
not in verified config, or zero valid Owner. After cleanup, rollback must restore only schema/code
compatibility and must not recreate implicit Admin/Editor authority or reopen writes.

### Phase 11 — Full verification, documentation lifecycle, deployment preflight, and rollout

**Goal:** Prove the complete feature, update living truth, pass formal review, and execute the
protected rollout sequence in §18.

**Depends on:** Phases 1–10 and deployment authority outside this planning task.

**In scope:**

- Run Tier C plus all required focused/security/smoke/frontend gates and supplementary personas.
- Execute production-read-only inventory and Owner/catalogue/route preflight.
- Complete formal Engineering Review and close every blocking/major finding.
- Update all living READMEs/contracts/testing ledger listed in §20.
- After review passes, apply repository planning-artifact lifecycle: prove/fold current truths,
  repoint references, then delete the current-state report, design decision record, and this plan in
  the feature’s final pre-merge deletion change.
- Coordinate additive DB, reconciliation, Backend enforcement, frontend, cleanup, and post-deploy
  smoke.

**Out of scope:** Unrelated full performance audit, unrelated debt, release `dev → main` unless
explicitly requested, or production mutation without operator approval.

**Likely files:** Living documentation and generated contracts already touched; no new feature
architecture should appear here.

**Migrations:** No new migration. Apply only the reviewed additive/cleanup migrations according to
§18.

**Required commands:**

```sh
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke."
Backend/scripts/check-api-contract
cd Frontend/quran-dashboard-ui
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
npm test
npm run build
npm run e2e
```

Tier D/E data-pipeline/canonical gates run only if their documented triggers or the actual release
boundary require them. Smoke reporting always states data tier ran/skipped.

**Completion criteria:**

- Formal review passes with no open security blocker.
- All 21 routes, 19 codes, five debt rows, public reads, admin APIs, audit, relink, Owner
  reconciliation, frontend paths, and rollout preflights have traceable evidence.
- Production has at least one verified configured Active Owner.
- The chosen Phase 5 production activation gate is recorded: preferably reviewed Phase 6 was
  available at activation, otherwise the accepted Owner-only period or shared-service audited
  operator grant mechanism is evidenced.
- Backend enforcement is live before/with frontend controls.
- Post-deployment direct anonymous/neighboring writes fail; Owner/exact writes reach domain behavior;
  public reads/304 remain live.
- Living READMEs are current and planning artifacts are removed only after their facts are preserved
  or made executable tests.

**Engineering Review checkpoint:** Final formal `engineering-review` over the implementation diff and
contracts, followed by a re-review of every finding. The final review is a gate before documentation
deletion and rollout.

**Rollback/stop condition:** Any failed zero-Owner, reconciliation, catalogue, route metadata,
public-read, audit atomicity, or direct-write test blocks rollout. Once enforcement is live, rollback
must retain protected unsafe routes; if necessary, place administrative writes in temporary
maintenance/deny mode while keeping public reads online.

## 18. Migration and deployment sequence

### Production rollout sequence

1. **Read-only inventory and backup**
   - Record applied migration, current Owner/Admin/Editor users, statuses, display emails, shared
     application-normalized email candidates, `sub` values, and role references.
   - Run the normalized collision preflight against every current user before applying Phase 2 DDL.
     Any duplicate/invalid normalized result blocks migration and requires explicit resolution; do
     not merge or relink users.
   - Verify a recoverable database backup and restoration procedure.
   - Validate proposed `OwnerBootstrap:Emails` without mutating database state.
   - Identify at least one operator-controlled verified identity that can log in.

2. **Deploy additive schema without enforcement**
   - Apply the first additive migration that introduces nullable `NormalizedEmail` and access
     storage; deploy compatible provisioning that writes the shared normalized value.
   - Backfill existing users through the application normalizer, re-run collision/completeness
     checks, then apply the second additive migration that makes `NormalizedEmail` required and
     unique.
   - Run the catalogue synchronizer and assert the exact 19 codes.
   - Deploy code capable of `/me` multiple-Owner bootstrap/reconciliation, with Abwab behavior still
     unchanged until the complete protection release is ready.
   - Do not apply cleanup migration or remove legacy roles.

3. **Bootstrap and reconcile Owners**
   - Have each intended initial Owner authenticate through `/api/access/me` as needed.
   - Run `owners status` and dry-run.
   - Apply reconciliation with explicit reason/deployment fingerprint through the shared service.
     Confirm it acquires the dedicated transaction-scoped advisory lock and recomputes configuration,
     membership, status, grants, and verified Logto evidence under that lock.
   - Verify at least one—and preferably two for recovery—configured, verified, local Active Owners;
     several Owners are equally authoritative.
   - Verify promotion of any granted non-owner emitted ordered per-grant
     `PermissionRevoked` events, deleted those grants, then emitted
     `OwnerGrantedByReconciliation` in the same transaction.
   - Verify audit events and zero Owner grant rows; any Owner grant row blocks rollout.

4. **Run enforcement preflight**
   - Schema/catalogue/Owner/unsafe-route/public-GET tests must all pass against the candidate build.
   - Inventory former Admin/Editor users but do not infer or grant permissions.
   - Verify concurrent reconciliation apply behavior, last-Owner race refusal, lock timeout
     fail-closed behavior, and idempotent retry after lock release.
   - Verify the protected Backend can serve `/me`, Owner writes, and public reads in staging.
   - The unresolved authoritative Logto verified-primary-email signal blocks this Owner preflight
     and Phase 5 production activation, even though it did not block Phases 1–2.

5. **Choose and record the production activation gate**
   - Prefer completing and reviewing Phase 6 before activating Phase 5 enforcement in production.
     Phase 5 implementation and staging activation alone do not authorize production activation.
   - Earlier activation requires an explicit operational acceptance of a temporary Owner-only write
     period or a verified trusted operator command for individual grant/revoke operations.
   - The operator command, if used, must call the same active-Owner authorization, validation,
     concurrency, transaction, grant-delta, and append-only audit services used by Phase 6. Do not
     duplicate EF/business logic and do not expose a temporary public or broadly authenticated
     endpoint.
   - If activation waits for Phase 6, keep anonymous write exposure recorded as Critical and
     minimize the interval; consider edge-level unsafe-method maintenance rather than weakened
     Backend authorization.

6. **Activate Backend enforcement and administration**
   - In the preferred path, deploy the reviewed Phase 5 enforcement and Phase 6 Owner-only
     administration Backend in the same release. In an explicitly approved early path, deploy Phase
     5 alone and record which permitted grant-administration condition applies.
   - Backend enforcement must precede, or be atomic with, any frontend permission-aware assumptions.
   - If the platform performs a rolling deployment where old open and new protected instances may
     coexist, temporarily reject unsafe `/api/abwab` methods at the edge or enter
     administrative-write maintenance for the mixed-version window. Keep GETs online.
   - Immediately test anonymous 401, neighboring 403, Owner reachability, and public GET/304.

7. **Deploy the remaining administration/access foundation**
   - If Phase 6 was not in the activation release, deploy it at once and end the accepted
     Owner-only/operator-tool transition. Then deploy the additive `/me` contract and frontend
     store/Owner route.
   - Keep transitional fields only for the verified compatibility window.
   - Deploy access-admin UI after Backend OpenAPI is live.

8. **Assign direct permissions**
   - An active Owner accepts intended Pending users and grants each code explicitly.
   - Former Admin/Editor role names provide no defaults. Record every grant in audit.
   - Verify a read-only user and representative exact/neighboring users before wide frontend rollout.

9. **Deploy Abwab permission-aware frontend**
   - Deploy Phase 9 only after Backend enforcement is confirmed.
   - Verify anonymous/read-only/one-permission/Owner UI and direct HTTP behavior.
   - A frontend rollback is safe because Backend remains authoritative.

10. **Convert legacy roles and apply cleanup**
   - Place security-administration mutations in a short write freeze while running the final
     inventory, Owner reconciliation, Admin/Editor conversion, and cleanup migration.
   - Public reads and already protected Abwab GETs stay online.
   - Confirm no Admin/Editor references remain and non-owners have null role IDs.
   - Deploy code/generated contracts with legacy role machinery removed.

11. **Post-deployment verification and observation**
    - Run the route/security smoke subset against production-safe seeded targets or a production-like
      environment, avoiding destructive data.
    - Verify Owner count, unresolved reconciliation count, 401/403 envelope metrics, audit inserts,
      resolver query latency, and public GET success/304.
    - Retain deployment/preflight evidence without secrets.

### Maintenance requirements

- **No general read maintenance:** additive schema, Owner configuration validation, catalogue sync,
  Backend/frontend deployment, and public-read checks should not interrupt GET traffic.
- **Possible brief DDL window:** generated migrations may take short locks; evaluate on a
  production-size copy. This is database deployment caution, not application-wide maintenance by
  default.
- **Possible brief provisioning-write window:** coordinate the nullable/backfill/required
  `NormalizedEmail` transition so an old Backend cannot insert a row without the normalized value.
  Public content GETs remain online.
- **Unsafe-method maintenance required for mixed Backend versions:** if old open instances can serve
  alongside protected instances, block unsafe methods during the switch.
- **Short security-admin write freeze recommended for legacy conversion:** prevents status/grant
  changes between final inventory and conversion. Public reads continue.
- **No maintenance for ordinary permission changes:** each grant/revoke/disable/reactivate is a
  transaction and is visible next request.

### Emergency rollback

- Never redeploy the pre-feature open Backend after enforcement activation.
- Before activation, rolling Phase 5 code back does not resolve the still-critical anonymous-write
  exposure; either minimize the delay to the reviewed Phase 5+6 release or deny unsafe methods at the
  edge. Never weaken the permission handler as a transition workaround.
- Roll frontend back independently; Backend 401/403 remains secure.
- Roll Backend back only to a schema-compatible build that already protects every unsafe route.
- If no protected rollback artifact is available, deny administrative unsafe methods at the
  platform/edge while leaving public GETs available, then repair forward.
- Additive tables are not dropped after grants/audit exist. A code rollback must tolerate them.
- A mistaken Owner configuration change is corrected through another validated reconciliation;
  never manually assign the role or delete audit. Reconciliation retry uses the same dedicated lock
  and recomputes state after lock acquisition.
- A bad permission grant is revoked by an active Owner and audited; reactivation never restores it.
- A relink error that failed before commit requires no state rollback. A successfully but wrongly
  authorized relink is a security incident requiring audited Owner action and identity verification,
  not silent database editing.

## 19. Performance considerations

Correctness and enforcement precede optimization.

- Each protected request issues at most one authorization-state query. Public GETs issue none.
- Query shape is a projection keyed by unique `users.logto_sub`, with a left/secondary join to
  `user_permissions`/`permissions`. Avoid `Include` materialization and N+1 permission queries.
- Required indexes are existing unique `logto_sub`, composite/unique
  `(user_id, permission_id)`, unique `normalized_email`, and permission `code` uniqueness. Verify the
  generated SQL and query plan after representative grants exist.
- Request-scoped memoization shares one resolution within a request. It is not a cross-request
  cache, has no TTL, and requires no invalidation protocol.
- Owner requests may skip loading permission rows after status/role projection proves Active Owner,
  but only if the query remains simple and tests still prove one snapshot and disabled-Owner denial.
- Authorization adds constant overhead to bulk move/archive/template apply; it must not issue one
  permission query per selected door/target. Existing transactional bulk behavior remains the
  dominant work.
- User lists are paginated and projected; do not include full audit/grant graphs in list rows.
- Audit retrieval uses keyset pagination and indexed filters. JSON snapshots are selected only for
  returned rows.
- Audit grows indefinitely in v1. Monitor row count, index size, insert latency, and page-query
  latency. Archival/partitioning is a later accepted operational design, not this feature.
- Catalogue and reconciliation status are small; no cache is needed for authorization correctness.
- Owner reconciliation apply holds one dedicated PostgreSQL transaction-scoped advisory lock only
  for the duration of trusted-state reload, last-Owner validation, membership/grant/audit mutation,
  and commit. Measure acquisition wait/timeout and transaction duration, but do not introduce a
  general distributed-lock framework or cross-request authorization cache.

After the full feature is correct, measure:

1. resolver SQL count per protected request;
2. `EXPLAIN (ANALYZE, BUFFERS)` for active read-only, one-permission, and Owner resolution;
3. p50/p95 authorization contribution to representative single and bulk writes;
4. audit insert overhead for disabling a user with all 19 grants;
5. Owner user-list and audit-page query latency at expected production volumes.
6. Owner-reconciliation advisory-lock wait, hold time, timeout rate, and retry success.

No index or cache beyond the identified query shapes is added without measurement. This section does
not authorize the later full Backend/frontend performance audit.

## 20. Documentation updates

Update living documentation in the same phase as the behavior it describes:

| Document | Required update |
|---|---|
| `Backend/api/QuranDashboard.Api/Authentication/README.md` | JWT identity boundary, database authorization, no claim roles, request-scoped resolver, exact/Owner metadata, 401/403, no fallback/public GET posture |
| `Backend/api/QuranDashboard.Api/Controllers/README.md` | Abwab writes protected by exact codes; four reads public; Access admin route families Owner-only; `/me` contract |
| `Backend/api/QuranDashboard.Api/README.md` | `OwnerBootstrap:Emails`, `Users.NormalizedEmail`, deployment/activation preflights, single Owner role/multiple users, new auth architecture, no Admin/Editor |
| `Backend/application/QuranDashboard.Application.Abstractions/Security/README.md` | `ICurrentUser`, shared email normalizer, authorization state, normalized provisioning/relink boundaries, removal of cached role resolver |
| `Backend/tools/QuranDashboard.AccessAdmin/README.md` | Safe validate/status/dry-run/apply/preflight/conversion commands, dedicated reconciliation serialization, dry-run recompute warning, promotion grant revocation order, confirmation, output redaction, recovery, and any approved grant/revoke operator surface |
| `Backend/scripts/README.md` | How the operator tool participates in normalized-email collision/backfill checks, schema/catalogue/Owner preflight, production activation choice, and smoke-dump regeneration |
| `Backend/.architecture/API_GUIDELINES.md` | Unsafe endpoint classification convention, Owner-only security GETs, shared 401/403 envelopes, public GET exception, parity requirement |
| `Frontend/quran-dashboard-ui/src/app/core/README.md` | Public route invariant, access store, Owner guard, typed permission checks, 401/403 refresh/no retry; remove role guard truth |
| `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` | Replace open-write warning with exact permission UX/Backend authority and retain public reads |
| `Frontend/quran-dashboard-ui/src/app/features/access-admin/README.md` | Owner-only route, user/grant/relink/audit contracts, select-all semantics, no Owner mutation |
| `docs/contracts/http-api.md` / `docs/contracts/abwab.md` | Repoint route/access truth to current controller and README locations without duplicating the matrix |
| new `docs/contracts/security-access.md` and `docs/contracts/README.md` pointer | Thin pointer to Authentication, Access controller, Application security, and frontend core READMEs; no copied contract content |
| `docs/TESTING_DEBT.md` | Delete only relations row 3, templates row 8, F3, G3, and I2 when their tests land; leave other triggers |
| `TESTING_STRATEGY.md` | Change only if test selection/tier commands actually change; do not restate feature-specific cases there |
| OpenAPI/generated frontend models | Regenerate through `export-swagger`/`generate:api`; never hand-edit generated files |

Do not preserve obsolete Owner/Admin/Editor policies, cache semantics, or public-write warnings as
current truth. Historical audit behavior belongs in tests/audit rows, not stale docs.

The three files under `docs/security/` are planning artifacts, not long-lived contract truth under
`docs/README.md`. After final Engineering Review passes, apply the repository lifecycle gate:

1. prove each surviving invariant in code/test;
2. fold only unrecoverable verified facts into the nearest README;
3. add/adjust `docs/contracts/` pointers;
4. `rg` every inbound reference;
5. delete the current-state report, design-decisions record, and implementation plan in the final
   pre-merge deletion change.

## 21. Risk register

Likelihood and impact use `Low`, `Medium`, `High`, and `Critical`.

| Risk | Likelihood | Impact | Mitigation | Detection/verification | Rollback/response |
|---|---|---:|---|---|---|
| Production anonymous Abwab writes remain exposed during a long feature or while production activation waits for Phase 6 | High until activation | Critical | Prioritize Phase 5 implementation and Phase 6 Backend completion; prefer one reviewed activation release; track any delay explicitly; no partial controller rollout | Direct anonymous smoke for all 21 routes; live metadata parity; release-gate status | Block unsafe methods during deployment/delay where operationally possible; deploy protected build; never roll back open |
| Owner lockout during config/removal/conversion or concurrent reconciliation | Medium | Critical | Multiple configured Owners; one dedicated transaction-scoped reconciliation lock; recompute after lock; last-active-Owner check; recovery identity | Preflight desired/current/verified owners; concurrent removal/addition and last-Owner race tests; active Owner API/write smoke | Add verified recovery email, provision/reconcile through the same locked service; do not DB-assign Owner |
| Enforcement deployed with zero valid Owner | Medium | Critical | Production preflight blocks traffic activation; empty/unresolved/Disabled do not count | Tool non-zero result; startup/deployment gate; DB/config reconciliation query | Abort activation; remain additive or deny writes until recovered |
| Wrong/unverified Logto email signal grants Owner | Medium until remediated | Critical | Prove tenant-authoritative field/endpoint; server retrieval; fail closed | Verified/unverified/mismatch fixtures; tenant check; audit provenance | Phase 3 cannot be accepted and Phase 5 cannot activate in production; revoke only via corrected config reconciliation; incident review |
| Existing users collide after email normalization | Medium until preflight | Critical | Preserve display Email; compute candidates with one shared application normalizer; fail pre-DDL; unique `Users.NormalizedEmail`; no automatic merge/relink | Casing/whitespace vectors; full collision report; migration/backfill and unique-index integration tests | Abort Phase 2 migration; explicitly resolve identities with audit/review before retry |
| Provisioning, Owner matching, or relink compares raw email or a divergent normalizer | Medium | Critical | One normalizer abstraction and required normalized column; prohibit raw identity comparisons | Unit vectors plus provisioning/Owner/relink collision tests; code review/search | Fail closed, stop rollout, repair comparison path; never transfer grants/Owner by email |
| Owner promotion leaves direct grants, loses their history, or partially commits | Low/Medium | Critical | Serialized transaction locks user/grants, audits ordered revocations, deletes grants, assigns Owner, appends Owner event, commits once | Zero/one/many promotion tests; preflight Owner-grant violation; audit-failure fault injection; concurrent modification test | Roll back transaction automatically; block reconciliation until invariant restored through audited service |
| Reconciliation lock cannot be acquired or two callers race last-Owner decisions | Medium operationally | Critical | Dedicated stable PostgreSQL transaction-scoped advisory lock; reload all trusted state after acquisition; bounded timeout; idempotent retry | Concurrent apply/add/remove/last-Owner tests; lock wait/timeout metrics | Fail closed with no partial writes; retry after lock release; never bypass with direct role edit |
| Public content GETs accidentally become authenticated | Medium | High | No fallback policy; explicit public route parity; Owner only on security GETs | Anonymous full public route theory; ETag/304; metadata absence test | Remove misplaced controller/fallback metadata; protected writes stay |
| All active users accidentally receive write access | Low/Medium | Critical | Exact requirement after Active; no “active admin” policy; no group shortcut | Active read-only across 21 routes; neighboring tests; handler-not-called | Disable affected writes/rollback to protected build; audit and correct grants |
| New unsafe endpoint lacks metadata | Medium | Critical | Startup validator plus bidirectional test; same-change smoke catalogue rule | Application startup failure; named parity test route/method output | Do not deploy; add one accepted classification/design permission |
| Unknown/typo permission string silently denies or grants | Medium | High | Catalogue constants/requirement-data, startup known-code validation, frontend parity generation | Catalogue/DB/frontend tests; endpoint metadata report | Correct metadata/catalogue; no fallback to broad permission |
| Stale frontend permission state exposes a control | High eventually | Medium security, low authority | Backend authoritative; request-scoped next-request state; 403 refresh/no retry | Revocation-while-modal-open test; direct HTTP denial | Refresh/close UI; no server rollback needed |
| Admin/Editor conversion grants or loses unintended access | Medium | High | Inventory; no role-name inference; default zero grants; explicit Owner grants later | Before/after balanced report; query role/grant state; audit | Stop cleanup; restore backup if pre-enforcement, otherwise keep writes protected and correct explicitly |
| Audit and state transaction diverge | Low/Medium | Critical | Same transaction/appender; forced failure tests; append-only API | Integration fault injection; counts/state assertions | Transaction rollback; block mutation if audit unavailable |
| Relink enables account takeover | Low | Critical | Owner-only; explicit preview/confirm; fresh Logto verification; email match; unique new sub; version check | Collision/replay/mismatch/Owner-config tests; audit old/new sub | Deny/stop; use verified recovery Owner and audited corrective relink; incident process |
| Frontend-only enforcement mistaken for security | Medium | Critical | Phase 5 Backend first; handcrafted tests; docs state UX only | Direct curl/test client with hidden-control personas | Roll back frontend if needed; Backend remains protected |
| Partial Backend/frontend rollout creates false UI assumptions | Medium | High | Additive `/me`; Backend first; contract check; bounded compatibility window | Versioned deployment matrix; old/new client contract tests | Roll frontend back; retain protected Backend/additive fields |
| Phase 5 production activation leaves non-owners unable to receive grants | Medium if activated early | High | Prefer completed/reviewed Phase 6 before activation; otherwise explicitly accept Owner-only period or provide the shared-service audited operator grant command | Activation checklist; successful Owner grant/revoke rehearsal; non-owner exact-permission smoke | Keep Backend enforcement strong; deploy Phase 6 promptly or operate the approved tool; do not add broad endpoint or revert open |
| Disabled Owner bypasses status | Low | Critical | Evaluation order Active before Owner; shared state for both requirements | Disabled Owner handler/API matrix | Disable unsafe methods; fix central handler, not controllers |
| Token role/permission claim smuggling | Low | Critical | Remove claims transformation; database state only | Mint Owner/permission-looking claims for unknown/read-only users | Stop deployment; remove claim consultation; rotate compromised tokens if needed |
| Authorization database outage is returned as access | Medium operationally | Critical | Fail closed with controlled 503; no claim fallback; public GETs independent | Fault-injection test and monitoring | Keep public reads online; temporarily deny writes; restore DB |
| Concurrent Owners overwrite status/grants/relink | Medium | High | User xmin/version and transaction-time actor/target recheck | Two-owner concurrency integration tests; `409` refresh | Refresh and retry deliberately; never automatic client write retry |
| Audit growth degrades administration | Low initially | Medium | Indexed keyset retrieval, bounded pages, monitoring; no full graph loads | Query latency/index size metrics | Tune measured indexes; later approved archival, never ordinary deletion |
| Permission UI select-all becomes a stored authority | Low/Medium | High | Frontend expansion to codes; API accepts only known codes; no group table/code | Request contract and persistence tests | Reject request, remove invalid data through audited correction |
| Rollback reopens writes | Medium without discipline | Critical | Retain protected rollback artifact; edge unsafe-method deny fallback | Deployment checklist and post-rollback anonymous test | Immediately deny unsafe methods and repair forward |

## 22. Traceability matrix

### Accepted decisions to plan and proof

| ID | Accepted decision | Plan sections/phases | Required proof |
|---|---|---|---|
| D01 | Normal content/research reads are public and anonymous | §§1–2, 8–9, 12–14; Phases 1, 5, 7, 9, 11 | Anonymous route/parity and ETag/304 tests |
| D02 | `/api/access/me` remains authenticated | §§8–9, 12; Phases 1, 7 | Anonymous 401 plus authenticated provisioning tests |
| D03 | Security administration reads/writes are active-Owner-only | §§4, 8–9, 11, 13; Phases 4, 6, 8 | Owner-only metadata/API persona tests |
| D04 | Logto owns identity; application DB owns authorization; token claims never grant | §§4, 6, 8; Phases 3–4, 10 | Claim-smuggling and verified-profile tests |
| D05 | Several Owners from normalized `OwnerBootstrap:Emails` and verified Logto identities | §§5–6; Phase 3 | Array/duplicate/multiple Owner/reconciliation tests against `Users.NormalizedEmail` |
| D06 | Owner is only role; non-owners have nullable/null `RoleId` | §§2, 5, 10; Phases 2, 10 | Role inventory/schema/conversion tests |
| D07 | Active Owner central bypass, no direct grants; Disabled Owner denied | §§4–8, 15; Phases 3–5 | Owner/Disabled Owner persona, promotion grant-revocation, and invariant tests |
| D08 | Every administrative write needs authentication, Active local user, exact permission or Owner | §§8–10; Phases 4–6 | 21-route data-driven matrix |
| D09 | The 19 Abwab codes are fixed | §7; Phases 2, 5 | Catalogue/code/DB/frontend parity |
| D10 | “Manage all” is frontend select-all only | §§7, 13; Phase 8 | UI expansion/request/persistence tests |
| D11 | Permission/user/audit/relink administration is Owner-only and not delegatable | §11; Phases 6, 8 | Direct grant cannot authorize admin API |
| D12 | Disable atomically removes grants; reactivate restores none | §§5, 11, 15; Phases 2, 6 | Transaction/fault/restart-next-request tests |
| D13 | Access audit is append-only with required events/snapshots | §§5, 15; Phases 2, 3, 6 | Atomicity/immutability/retrieval tests |
| D14 | `sub` relink is explicit, Owner-only, verified, normalized-email fail-closed, audited | §§5–6, 11, 15; Phases 2, 6, 8 | Preview/confirm/normalized-collision/replay/audit tests |
| D15 | V1 authorization state is request-scoped; no cross-request cache | §§4, 8, 19; Phase 4 | One query/scope, next-request grant/revoke tests |
| D16 | Every unsafe endpoint has explicit recognized metadata and fail-closed parity | §9; Phases 1, 4, 5 | Startup invalid-shape and bidirectional parity tests |
| D17 | Controlled shared-envelope 401 and 403 | §§8, 13; Phases 4, 7 | Exact status/body/message tests |
| D18 | V1 tables are Users/Roles/Permissions/UserPermissions/AccessAuditEvents; no generic RBAC | §5; Phase 2 | Schema tests and absence of role-permission/group tables |
| D19 | Former Admin/Editor receive zero inferred grants | §§5, 10, 18; Phase 10 | Before/after conversion and zero-grant query |
| D20 | Backend enforcement deploys before or atomically with frontend controls | §§1, 17–18; Phases 5, 9, 11 | Deployment order and post-deploy direct requests |
| D21 | Frontend gates every event path but is UX only | §§13–14; Phase 9 | Component/path matrix plus handcrafted Backend calls |
| D22 | `GET /api/health` remains public | §§2, 9, 16; Phases 1, 5, 11 | Anonymous health smoke |
| D23 | Owner removal/recovery only through serialized config reconciliation; last Owner protected | §§4, 6, 18–19; Phases 3, 10–11 | Removal/recovery/concurrent last-owner/advisory-lock transaction tests |
| D24 | User-visible composites require one understandable permission | §10; Phase 5 | Single/bulk and apply isolation tests |
| D25 | Groups are code metadata, not authorization tables | §§5, 7; Phases 2, 8 | Schema/catalogue/request tests |
| D26 | No door-protection or unimplemented permission | §§2, 7; all phases | Exact 19-count parity and absence search |
| D27 | Public reads are independent of Pending/Disabled/account existence | §§2, 13, 16; Phases 1, 5, 9 | Anonymous/Pending/Disabled public GET matrix |
| D28 | `/me` target uses `isOwner`/permissions and retires numeric role ID | §12; Phases 7, 10 | OpenAPI/generated client compatibility tests |
| D29 | `Users.Email` is display identity; required unique `Users.NormalizedEmail` is the database identity invariant produced by one normalizer | §§3–6, 11–12, 16, 18–21; Phases 1–3, 6, 11 | Collision preflight; safe backfill/not-null/unique migration; casing/whitespace/provisioning/Owner/relink tests |
| D30 | Owner promotion atomically audits/revokes all direct grants before Owner assignment | §§5–6, 15–16, 18, 21; Phase 3 | Zero/one/many grants, event order, audit rollback, concurrent-modification tests |
| D31 | Every Owner reconciliation apply is serialized by one dedicated transaction-scoped database lock and recomputes trusted state | §§4, 6, 16, 18–19, 21; Phases 3, 11 | Concurrent apply/add/remove/last-Owner/timeout/idempotent-retry tests |
| D32 | Phase 5 implementation/staging readiness is separate from production activation; production prefers reviewed Phase 6 or one explicitly accepted early mechanism | §§1, 3, 17–18, 21; Phases 5–6, 11 | Activation record; Phase 6 or shared-service operator proof; production direct-write smoke |

### Every current Abwab write endpoint

| §10 row | Route | Permission | Protection phase | Primary test trace |
|---:|---|---|---:|---|
| 1 | `POST /api/abwab/sections` | `abwab.sections.create` | 5 | API access + section smoke |
| 2 | `PUT /api/abwab/sections/{id}` | `abwab.sections.edit` | 5 | API access + section smoke |
| 3 | `DELETE /api/abwab/sections/{id}` | `abwab.sections.delete` | 5 | API access + section smoke |
| 4 | `POST /api/abwab/sections/{id}/order` | `abwab.sections.reorder` | 5 | F3 + persona matrix |
| 5 | `POST /api/abwab/doors` | `abwab.doors.create` | 5 | door smoke + behavior regression |
| 6 | `PUT /api/abwab/doors/{id}` | `abwab.doors.edit` | 5 | door smoke + neighbor isolation |
| 7 | `POST /api/abwab/doors/{id}/move` | `abwab.doors.move` | 5 | single/bulk equivalence |
| 8 | `POST /api/abwab/doors/{id}/order` | `abwab.doors.reorder` | 5 | door smoke + scope behavior |
| 9 | `POST /api/abwab/doors/bulk-move` | `abwab.doors.move` | 5 | single/bulk equivalence |
| 10 | `POST /api/abwab/doors/bulk-archive` | `abwab.doors.archive` | 5 | single/bulk equivalence |
| 11 | `DELETE /api/abwab/doors/{id}` | `abwab.doors.archive` | 5 | soft-archive smoke |
| 12 | `POST /api/abwab/doors/{id}/restore` | `abwab.doors.restore` | 5 | archive/restore isolation |
| 13 | `POST /api/abwab/doors/{doorId}/relations` | `abwab.relations.create` | 5 | relations row 3 |
| 14 | `DELETE /api/abwab/relations/{relationId}` | `abwab.relations.delete` | 5 | relations row 3 |
| 15 | `POST /api/abwab/templates` | `abwab.templates.create` | 5 | templates row 8 |
| 16 | `DELETE /api/abwab/templates/{templateId}` | `abwab.templates.delete` | 5 | templates row 8 |
| 17 | `POST /api/abwab/templates/{templateId}/apply` | `abwab.templates.apply` | 5 | templates row 8 + G3 |
| 18 | `POST /api/abwab/templates/{templateId}/nodes` | `abwab.template_nodes.create` | 5 | templates row 8 |
| 19 | `PUT /api/abwab/template-nodes/{nodeId}` | `abwab.template_nodes.edit` | 5 | templates row 8 |
| 20 | `POST /api/abwab/template-nodes/{nodeId}/order` | `abwab.template_nodes.reorder` | 5 | templates row 8 |
| 21 | `DELETE /api/abwab/template-nodes/{nodeId}` | `abwab.template_nodes.delete` | 5 | templates row 8 |

### Every permission code

| Catalogue order/code | Endpoint rows | Backend/UI phases | Isolation proof |
|---|---|---|---|
| 1 `abwab.doors.create` | 5 | 2, 5, 9 | Does not edit/move |
| 2 `abwab.doors.edit` | 6 | 2, 5, 9 | Does not create/move |
| 3 `abwab.doors.move` | 7, 9 | 2, 5, 9 | Both move forms; no archive |
| 4 `abwab.doors.reorder` | 8 | 2, 5, 9 | Does not move/edit |
| 5 `abwab.doors.archive` | 10, 11 | 2, 5, 9 | Both archive forms; no restore |
| 6 `abwab.doors.restore` | 12 | 2, 5, 9 | Archive grant alone denied |
| 7 `abwab.sections.create` | 1 | 2, 5, 9 | Other section actions denied |
| 8 `abwab.sections.edit` | 2 | 2, 5, 9 | Create/delete/reorder denied |
| 9 `abwab.sections.reorder` | 4 | 2, 5, 9 | Rename/delete denied |
| 10 `abwab.sections.delete` | 3 | 2, 5, 9 | Door archive not implied |
| 11 `abwab.relations.create` | 13 | 2, 5, 9 | Delete denied |
| 12 `abwab.relations.delete` | 14 | 2, 5, 9 | Create denied |
| 13 `abwab.templates.create` | 15 | 2, 5, 9 | Node/template apply denied |
| 14 `abwab.templates.delete` | 16 | 2, 5, 9 | Create/apply denied |
| 15 `abwab.templates.apply` | 17 | 2, 5, 9 | No template/node/door create |
| 16 `abwab.template_nodes.create` | 18 | 2, 5, 9 | Other node actions denied |
| 17 `abwab.template_nodes.edit` | 19 | 2, 5, 9 | Create/delete/reorder denied |
| 18 `abwab.template_nodes.reorder` | 20 | 2, 5, 9 | Edit/delete denied |
| 19 `abwab.template_nodes.delete` | 21 | 2, 5, 9 | Template delete not implied |

### Mandatory debt trace

| Debt | Plan section | Implementation phase | Test family |
|---|---|---:|---|
| Relations row 3 | §§10, 16 | 5 | Dispatched relation smoke + personas |
| Templates row 8 | §§10, 16 | 5 | Nine-route template/node smoke + personas |
| F3 | §§10, 16 | 5 | Section reorder status/envelope/personas |
| G3 | §§10, 16 | 5 | Apply `400`/`409`/personas |
| I2 | §§9, 16 | 5 | Anonymous conditional-read smoke/zero-query |

### Phase and test-family trace

| Phase | Main decision coverage | Mandatory test families |
|---:|---|---|
| 1 | D01, D02, D16, D27, D29 | Public route, normalization/collision contracts, test harness, current parity |
| 2 | D09, D13, D18, D25, D29 | Normalizer unit, collision/backfill/schema, persistence, smoke migration |
| 3 | D04–D07, D23, D29–D31 | Options, Logto adapter, serialized reconciliation/promotion integration |
| 4 | D04, D07–D08, D15–D17 | Handler, resolver, response, validator |
| 5 | D01, D08–D09, D16, D20, D24, D32 | API access, all Abwab smoke, five debt rows, activation-gate proof |
| 6 | D03, D11–D14, D29, D32 | Admin API, transaction, audit, normalized relink, parity, grant-administration readiness |
| 7 | D01–D02, D21, D28 | `/me`, OpenAPI, store, guard, public routes |
| 8 | D03, D10–D14, D25 | Access-admin route/components/API concurrency |
| 9 | D01, D09, D20–D21, D24 | Abwab component/page/path personas + Backend rerun |
| 10 | D04, D06, D19, D28 | Inventory/conversion/migration/contract/full references |
| 11 | All | Tier C, smoke, frontend build, supplementary E2E, normalized/reconciliation/activation preflight |

The matrices above provide four independent omission checks: design decision, route, permission, and
phase/test. A change is not complete if it appears in one dimension but lacks the others.

## 23. Final plan verdict

`READY_WITH_REMEDIATION`

There is no contradiction between the accepted design decisions and the current 21-route controller
inventory, and no unresolved product decision is needed to begin Phases 1–2. Phase 1 test/contract
preparation and Phase 2’s staged `Users.NormalizedEmail` plus additive access-schema work are ready
to implement immediately.

One genuine prerequisite blocks Phase 3 acceptance and Phase 5 production activation: the
tenant-authoritative server-side Logto signal for a verified primary email must be identified and
proven. It does not block Phases 1–2 or Phase 5 implementation/staging verification. The current
adapter’s linked-identity inference is explicitly insufficient without that validation
(`LogtoManagementApiUserProfileSource.cs:40-47`; design-decisions §4.2). Production activation also
requires a validated `OwnerBootstrap:Emails` list, successful serialized reconciliation, and at
least one successfully provisioned, verified, local Active Owner; those are deployment preflights,
not open product questions.

Once that targeted remediation and the phase gates pass, the plan is ready to implement. No other
blocking contradiction or missing route/permission decision was found.
