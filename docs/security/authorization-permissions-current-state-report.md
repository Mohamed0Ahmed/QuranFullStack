# Authorization and Granular Permissions: Current-State Inspection

- **Inspection date:** 2026-08-05
- **Scope:** authentication, application-user access, authorization, granular permissions, all current API routes, and the complete implemented Abwab surface
- **Repository baseline:** `dev` at `5dea4ec0121cb0f91409ac9fb1a5709fa9e04e7c`; the inspected `HEAD` and `origin/dev` resolved to the same commit, and the worktree was clean before this report was created.

This is a read-only architecture inspection. It does not reopen the locked product decisions, define permissions for unimplemented features, create a migration, or constitute the final implementation plan.

## 1. Executive summary

The repository has a sound identity foundation but almost no application authorization:

- JWT bearer authentication is configured against Logto authority metadata and a required API audience; raw `sub` is retained and invalid credentials receive the shared `ApiResponse` `401` envelope (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:10-48`, `Backend/api/QuranDashboard.Api/Authentication/JwtAuthenticationOptions.cs:3-33`, `Backend/api/QuranDashboard.Api/Authentication/UnauthorizedRejectionWriter.cs:3-18`).
- Local users are provisioned by Logto `sub`. A first-time non-owner becomes `Pending` with no role; a configured, identity-verified owner email becomes `Active`/`Owner`; a disabled owner is not revived by login (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:13-32`, `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:35-66`, `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:69-97`, `Backend/tests/QuranDashboard.Tests/Api/Access/UserProvisioningServiceTests.cs:41-110`).
- The database already permits role-less users because `users.role_id` is nullable. It currently seeds three roles—`Owner`, `Admin`, and `Editor`—and contains no permission or user-permission entities (`Backend/domain/QuranDashboard.Domain/Access/User.cs:3-18`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:9-54`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:9-31`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs:53-54`).
- Only `GET /api/access/me` has `[Authorize]`. There is no fallback policy, no named policy is applied to an endpoint, and the remaining 72 catalogued routes are open (`Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:7-13`, `Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:50-58`, `Backend/tests/QuranDashboard.Tests/Api/Access/AuthorizationPolicyRegistrationTests.cs:33-39`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:117-222`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359`).
- All 25 implemented Abwab routes are open: four reads and 21 writes. This includes door, section, relation, template, and template-node mutation endpoints (`Backend/api/QuranDashboard.Api/Controllers/README.md:8-20`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359`). The nearest frontend README records that this open write surface is deployed, so closing it is a security blocker rather than optional hardening (`Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:13-26`).
- The minimal complete Abwab catalogue is 19 direct write permissions. Two user-visible actions intentionally share permissions across routes: single and bulk move use `abwab.doors.move`; single and bulk archive use `abwab.doors.archive`. No read permission and no “manage all” authorization permission is needed.
- The smallest compatible Backend direction is an active-administrator read policy, one reusable permission requirement/handler, a central Owner bypass inside that handler, request-scoped access resolution, and an explicit permission attribute/metadata value on every write. A runtime fail-closed rule plus endpoint-metadata tests must reject a newly added unsafe endpoint that lacks permission metadata.
- The minimal data-model evolution is to retain nullable `User.RoleId` only for `Owner`, remove `Admin`/`Editor` from the live role catalogue, add stable `Permissions` plus direct `UserPermissions`, and keep visual groups as code-defined metadata unless product owners require runtime-editable groups.
- Five existing debt rows are mandatory acceptance criteria for this feature: relations smoke row 3, templates smoke row 8, section-reorder smoke F3, apply smoke G3, and conditional-GET smoke I2 (`docs/TESTING_DEBT.md:20-25`, `docs/TESTING_DEBT.md:33-37`, `docs/TESTING_DEBT.md:58-63`, `docs/TESTING_DEBT.md:83-87`, `docs/TESTING_DEBT.md:98-102`, `docs/TESTING_DEBT.md:138-142`).

**Current readiness:** the repository is ready for detailed permission design, but five product-owner decisions remain around Owner recovery, permission administration authority, reactivation semantics, audit requirements, and account relinking. The final verdict is therefore `READY_WITH_DECISIONS_REQUIRED`.

## 2. Current authentication architecture

### 2.1 Responsibility boundaries

| Concern | Current implementation | Required boundary |
|---|---|---|
| Identity and login session | Logto/OIDC supplies the access token. `JwtBearer` resolves issuer/signing metadata from `Authority`, validates `Audience`, and preserves raw claims (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:29-48`, `Backend/api/QuranDashboard.Api/Authentication/JwtAuthenticationOptions.cs:3-33`). | Remains owned by Logto. Authorization must not duplicate passwords, sessions, or token issuance. |
| Stable identity key | `HttpContextCurrentUser.Sub` reads the literal `sub` claim and throws if it is absent (`Backend/api/QuranDashboard.Api/Authentication/HttpContextCurrentUser.cs:5-20`, `Backend/application/QuranDashboard.Application.Abstractions/Security/ICurrentUser.cs:3-6`). | `sub` remains the only Logto-to-application user key. |
| Email/profile/verification input | First provisioning reads the Logto Management API by escaped `sub`; the source returns primary email, username, display name, and a derived `EmailVerified` value (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/LogtoManagementApiUserProfileSource.cs:22-47`, `Backend/application/QuranDashboard.Application.Abstractions/Security/IExternalUserProfileSource.cs:3-8`). | Logto owns identity email and verification state. The application may retain profile data but must not accept a client-asserted email or verification flag. |
| Application acceptance and activation | `UserStatus` is `Pending`, `Active`, or `Disabled`; ordinary first login creates `Pending`/role-less (`Backend/domain/QuranDashboard.Domain/Access/UserStatus.cs:5-10`, `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:69-87`). | Application database owns acceptance and deactivation. Only `Active` users may reach dashboard data. |
| Owner status | A configured owner email plus `EmailVerified` assigns the seeded `Owner` role and `Active`; a disabled row is not auto-promoted (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:35-66`, `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:69-97`). | Owner remains the sole named role and bypasses granular permission checks centrally, after the active-user check. |
| Granular permissions | No permission entity, grant entity, permission requirement, or permission-bearing current-user contract exists; current authorization exposes only named-role resolution (`Backend/application/QuranDashboard.Application.Abstractions/Security/IUserRoleResolver.cs:3-8`, `Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:37-43`, `Frontend/quran-dashboard-ui/src/app/core/auth/current-user.model.ts:1-12`). | Direct application-database grants control writes only. Reads require active administrative access, not feature permissions. |

### 2.2 JWT and `sub` flow

`AuthenticationRegistration.AddApiAuthentication` binds the `Auth` section, validates it at startup, registers `JwtBearer`, sets `Authority` and `ValidAudience`, and sets `MapInboundClaims = false` so `sub` is not renamed (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:10-48`). `JwtAuthenticationOptionsValidator` rejects a non-HTTPS/invalid authority or blank audience (`Backend/api/QuranDashboard.Api/Authentication/JwtAuthenticationOptions.cs:12-33`).

The request pipeline orders authentication before authorization and maps controllers afterward (`Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:19-29`). The frontend configures Logto, treats the API base URL as a secure route, and places the OIDC token interceptor in the HTTP chain (`Frontend/quran-dashboard-ui/src/app/app.config.ts:22-48`). A real-interceptor test verifies that the bearer token is attached to API-base requests and not sent to a foreign origin (`Frontend/quran-dashboard-ui/src/app/core/auth/auth-bearer-token.spec.ts:95-143`).

### 2.3 Provisioning and identity profile behavior

`ProvisionCurrentUserHandler` passes `ICurrentUser.Sub` to `IUserProvisioningService.GetOrCreateAsync` (`Backend/application/QuranDashboard.Application/Access/Commands/ProvisionCurrentUser/ProvisionCurrentUserHandler.cs:5-10`). Provisioning:

1. Looks up an existing local user by `LogtoSub` and returns/reconciles it (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:13-22`).
2. For a new subject, obtains a server-side Logto profile and rejects a blank primary email rather than accepting caller input (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:24-32`).
3. Creates an ordinary user as `Pending` with `RoleId = null`, or creates a verified configured owner as `Active` with the `Owner` role (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:69-97`).
4. Treats a unique-email collision under a different `sub` as an application conflict (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:99-115`, `Backend/api/QuranDashboard.Api/Middleware/GlobalExceptionHandler.cs:24-40`, `Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:116-145`).

The current Logto Management API response model has no direct verification property. `LogtoManagementApiUserProfileSource` treats a primary email backed by at least one linked social or SSO identity as verified and treats a password-only account as unverified (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/LogtoManagementApiUserProfileSource.cs:40-47`). The local `User` row stores email but has no `EmailVerified` property (`Backend/domain/QuranDashboard.Domain/Access/User.cs:3-18`). This keeps verification an identity-provider concern, but the linked-identity inference is a security-sensitive integration assumption that must be revalidated against the configured Logto tenant before using it for Owner recovery.

Existing non-owner users are returned without re-reading their Logto profile; only a configured owner candidate is reconciled through the external profile source (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:35-66`). Consequently, ordinary local email/name data can become stale relative to Logto, while `sub` remains the lookup key.

### 2.4 `IUserRoleResolver` and role-claim transformation

`CachedUserRoleResolver.GetActiveRoleNameAsync` returns a role only when the subject exists, is `Active`, and has a non-null `RoleId`. It caches positive and negative results for 30 seconds in process and exposes subject eviction (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/CachedUserRoleResolver.cs:8-52`). Tests confirm that pending, disabled, and role-less active users resolve to no role and that a cached negative remains stale until `Evict` is called (`Backend/tests/QuranDashboard.Tests/Api/Access/CachedUserRoleResolverTests.cs:9-29`, `Backend/tests/QuranDashboard.Tests/Api/Access/CachedUserRoleResolverTests.cs:44-73`).

`RoleClaimsTransformation` runs for authenticated principals, resolves the database role by `sub`, and adds it on its own marked `ClaimsIdentity` (`Backend/api/QuranDashboard.Api/Authentication/RoleClaimsTransformation.cs:7-42`). Its marker prevents a token-borne role claim from short-circuiting the database lookup, and the corresponding test includes a smuggled role-claim case (`Backend/tests/QuranDashboard.Tests/Api/Access/RoleClaimsTransformationTests.cs:20-46`).

For the locked model, this role-only abstraction is too narrow for non-owner authorization. It should be replaced or narrowed to an Owner-aware access snapshot rather than extended into a mixture of role strings and permission database queries. Token role claims must remain irrelevant to application authorization.

### 2.5 Named-role assumptions and retaining only Owner

The database and provisioning flow do **not** assume that every user has a named role: `User.RoleId` is nullable, ordinary first login writes null, and the current `/me` contract permits null `roleId`/`roleName` (`Backend/domain/QuranDashboard.Domain/Access/User.cs:12-16`, `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:75-87`, `Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:37-43`).

Two reusable authorization surfaces do assume named roles:

- Backend named policies are `Owner`, `Admin`, and `Editor`, each implemented as `RequireRole` (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:50-58`, `Backend/api/QuranDashboard.Api/Authentication/AuthorizationPolicyNames.cs:5-10`).
- Frontend `roleGuard` accepts one of `Owner | Admin | Editor` and allows only an active user with that exact `roleName` (`Frontend/quran-dashboard-ui/src/app/core/auth/current-user.model.ts:10`, `Frontend/quran-dashboard-ui/src/app/core/auth/role.guard.ts:9-27`).

Repository-wide uses of `Owner`, `Admin`, and `Editor` are confined to this authentication/access foundation, its documentation, and its tests; Abwab handlers/controllers contain no role checks. The role definitions and seeds are in `RoleNames`, `RoleConfiguration`, and `AddAccessRoles` (`Backend/domain/QuranDashboard.Domain/Access/RoleNames.cs:5-10`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:26-31`, `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260718142612_AddAccessRoles.cs:30-38`).

Retaining only Owner therefore requires:

- removing `Admin` and `Editor` constants, policies, frontend union members, seeds, and their fixed-set tests;
- preserving nullable role-less users;
- converting any existing `role_id` values pointing at Admin/Editor before deleting those seeded rows;
- replacing non-owner role authorization with direct permission grants, without inventing replacement named roles.

## 3. Current database/user/role model

### 3.1 Current schema

| Table/concept | Current shape | Evidence |
|---|---|---|
| `users` | Integer identity PK; unique required `logto_sub`; unique required `email`; nullable profile fields; nullable `role_id`; required integer `status`; created/updated timestamps. | `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260718115014_AddAccessUsers.cs:15-46`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:9-54` |
| `roles` | Integer identity PK; unique required `name`; required Arabic `display_name`. | `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260718142612_AddAccessRoles.cs:16-49`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:9-26` |
| `users.role_id` | Nullable FK to `roles.id`, `Restrict` on delete. | `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260718142612_AddAccessRoles.cs:40-57`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:33-39` |
| Seed roles | `Owner`/`المالك`, `Admin`/`المشرف`, `Editor`/`المحرر`, fixed IDs 1–3. | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:28-31`, `Backend/tests/QuranDashboard.Tests/Api/Access/AccessRolesTests.cs:12-21` |
| User status | `Pending = 1`, `Active = 2`, `Disabled = 3`. | `Backend/domain/QuranDashboard.Domain/Access/UserStatus.cs:5-10` |
| Permission data | No permission/group/grant DbSet exists; access persistence exposes only `AccessUsers` and `AccessRoles`. | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs:53-54` |

### 3.2 Current model implications

- A role-less active user is representable in the schema, but the current role resolver returns null for that user (`Backend/domain/QuranDashboard.Domain/Access/User.cs:12-16`, `Backend/tests/QuranDashboard.Tests/Api/Access/CachedUserRoleResolverTests.cs:9-29`).
- Activation is distinct from role assignment at the data-model level, which is compatible with active read-only administrators (`Backend/domain/QuranDashboard.Domain/Access/User.cs:12-18`).
- `CreatedAtUtc` and `UpdatedAtUtc` exist, but there is no approver, approval time, disabler, disabled time, grantor, grant time, revoker, or revoke time (`Backend/domain/QuranDashboard.Domain/Access/User.cs:16-18`).
- The current schema does not enforce “only Owner may have a role”; it accepts any seeded role FK (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:33-39`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:28-31`).
- The repository cannot establish the contents of the production `users` table. A migration must inventory users referencing role IDs 2 and 3 rather than infer permission grants from those role names. Existing policies were never applied, while Abwab writes were open, so current code supplies no defensible Admin-to-permission or Editor-to-permission mapping (`Backend/api/QuranDashboard.Api/Authentication/README.md:48-54`, `Backend/api/QuranDashboard.Api/Controllers/README.md:17-20`).

## 4. Current endpoint-protection inventory

### 4.1 Complete application-route inventory

`SmokeRouteCatalog` contains 73 routes: 38 Words, seven Mushaf, 25 Abwab, one Dashboard, one Health, and one Access route (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:117-222`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359`). The catalogue is not merely documentation: bidirectional `EndpointDataSource` parity tests fail if a live route is missing from it or a catalogued route disappears (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:10-35`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:41-68`).

| Route family | Routes | Current protection |
|---|---:|---|
| Words | 38 | Open; catalogue access defaults to `Open` (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:67-81`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:119-194`). |
| Mushaf | 7 | Open (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:196-216`). |
| Dashboard info | 1 | Open (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:218-222`). |
| Health | 1 | Open (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:218-222`). |
| Access `/me` | 1 | Authenticated only through class-level `[Authorize]`; no active-status or role requirement (`Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:7-15`). |
| Abwab | 25 | Open; four reads and 21 writes (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359`, `Backend/api/QuranDashboard.Api/Controllers/README.md:8-20`). |

### 4.2 Policies, fallback, and caller behavior

Three named role policies are registered, but no controller/action applies a named policy. The fallback policy is null (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:50-58`, `Backend/tests/QuranDashboard.Tests/Api/Access/AuthorizationPolicyRegistrationTests.cs:18-39`). The current API documentation agrees: `/api/access/me` is the only authenticated endpoint and the rest of the product is publicly browsable (`Backend/api/QuranDashboard.Api/README.md:48-71`, `Backend/api/QuranDashboard.Api/Controllers/README.md:68-73`).

Current caller outcomes are therefore:

| Caller | `/api/access/me` | Every other current route |
|---|---|---|
| Anonymous or invalid token | `401` shared failure envelope (`Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:71-96`). | Reaches the action because the endpoint is open; the smoke sweep explicitly asserts open routes do not return `401` (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRoutePipelineTests.cs:24-48`). |
| Authenticated unknown `sub` | Provisioned `Pending`, role-less, and returned `200` (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeAuthPipelineTests.cs:30-48`). | Reaches the action; no application-user lookup or active-status check runs on open routes. |
| Authenticated pending/disabled/role-less user | Returned with its application status; disabled configured-owner users remain disabled (`Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:13-33`, `Backend/tests/QuranDashboard.Tests/Api/Access/UserProvisioningServiceTests.cs:82-110`). | Reaches the action because no policy runs. |
| Active Owner | Returned `200` with `roleName: Owner` (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeAuthPipelineTests.cs:51-63`). | Reaches the same open action; Owner status currently adds no endpoint protection or bypass behavior. |

### 4.3 `401` and `403`

`401` is deliberately normalized to `ApiResponse<object>.Fail(ApiMessages.Unauthorized)` by `JwtBearerEvents.OnChallenge` and `UnauthorizedRejectionWriter` (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:39-47`, `Backend/api/QuranDashboard.Api/Authentication/UnauthorizedRejectionWriter.cs:3-18`, `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:14`).

There is no `OnForbidden`, custom authorization middleware result handler, or forbidden-response writer in the current auth registration. The global exception handler handles only provisioning-email conflict as `409` and all other unhandled exceptions as `500` (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:39-58`, `Backend/api/QuranDashboard.Api/Middleware/GlobalExceptionHandler.cs:24-58`). The smoke route test currently asserts that no dispatched route returns `403` because no endpoint carries an authorization policy (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRoutePipelineTests.cs:34-45`). A shared `403` envelope must therefore be added as part of authorization rather than assumed to exist.

### 4.4 Documentation conflict and security gap

Current documentation is consistent with current code, but it conflicts with the locked target posture:

- the API README says the whole product remains publicly browsable (`Backend/api/QuranDashboard.Api/README.md:68-71`);
- the frontend core README says every route is unguarded and `roleGuard` is attached to nothing (`Frontend/quran-dashboard-ui/src/app/core/README.md:103-112`);
- the Abwab README records that unauthenticated writes reached production and says write protection must be the next feature (`Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:13-26`);
- the API architecture guide says admin-only behavior must not remain public without authorization (`Backend/.architecture/API_GUIDELINES.md:235-244`).

The open Abwab write surface is the immediate security blocker. Protecting reads at the same feature boundary is also required by the locked decision that dashboard access is for authenticated, active administrative users.

## 5. Complete Abwab endpoint permission matrix

All rows below were verified against controller attributes/actions and the bidirectionally locked route catalogue, not inferred from prose (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:10-35`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359`). “Open” means there is no `[Authorize]` metadata and the catalogue uses its default `SmokeRouteAccess.Open` (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:67-81`).

For future rules:

- **Active read** = authenticated application user with `UserStatus.Active`; no feature permission.
- **Permission or Owner** = active user with the exact listed direct permission, or active Owner through the central bypass.

| Method | Route | Controller/action | Read/Write | Current protection | Required future rule | Permission | Group / notes |
|---|---|---|---|---|---|---|---|
| GET | `/api/abwab/tree` | `AbwabTreeController.Get` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTreeController.cs:7-31`) | Read | Open | Active read | — | Doors/Sections snapshot; conditional GET |
| GET | `/api/abwab/doors/{doorId}/relations` | `AbwabDoorRelationsController.GetForDoor` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorRelationsController.cs:8-29`) | Read | Open | Active read | — | Relations read |
| GET | `/api/abwab/templates` | `AbwabTemplatesController.GetAll` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:11-41`) | Read | Open | Active read | — | Templates list; conditional GET |
| GET | `/api/abwab/templates/{templateId}` | `AbwabTemplatesController.Get` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:43-60`) | Read | Open | Active read | — | Template detail; conditional GET |
| POST | `/api/abwab/sections` | `AbwabSectionsController.Create` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:9-35`) | Write | Open | Permission or Owner | `abwab.sections.create` | Sections |
| PUT | `/api/abwab/sections/{id}` | `AbwabSectionsController.Rename` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:37-60`) | Write | Open | Permission or Owner | `abwab.sections.edit` | Sections; endpoint currently renames |
| DELETE | `/api/abwab/sections/{id}` | `AbwabSectionsController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:62-79`) | Write | Open | Permission or Owner | `abwab.sections.delete` | Sections; refuses a section with live doors |
| POST | `/api/abwab/sections/{id}/order` | `AbwabSectionsController.Reorder` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:81-100`) | Write | Open | Permission or Owner | `abwab.sections.reorder` | Sections |
| POST | `/api/abwab/doors` | `AbwabDoorsController.Create` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:13-51`) | Write | Open | Permission or Owner | `abwab.doors.create` | Doors |
| PUT | `/api/abwab/doors/{id}` | `AbwabDoorsController.Edit` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:53-75`) | Write | Open | Permission or Owner | `abwab.doors.edit` | Doors |
| POST | `/api/abwab/doors/{id}/move` | `AbwabDoorsController.Move` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:77-104`) | Write | Open | Permission or Owner | `abwab.doors.move` | Doors; may reparent and change section |
| POST | `/api/abwab/doors/{id}/order` | `AbwabDoorsController.Reorder` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:106-129`) | Write | Open | Permission or Owner | `abwab.doors.reorder` | Doors; Section or Global scope |
| POST | `/api/abwab/doors/bulk-move` | `AbwabDoorsController.BulkMove` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:131-159`) | Write | Open | Permission or Owner | `abwab.doors.move` | Doors; same user-visible capability as single move |
| POST | `/api/abwab/doors/bulk-archive` | `AbwabDoorsController.BulkArchive` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:161-179`) | Write | Open | Permission or Owner | `abwab.doors.archive` | Doors; archives each selected subtree |
| DELETE | `/api/abwab/doors/{id}` | `AbwabDoorsController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:181-197`) | Write | Open | Permission or Owner | `abwab.doors.archive` | Doors; implementation is soft archive, not hard delete |
| POST | `/api/abwab/doors/{id}/restore` | `AbwabDoorsController.Restore` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:199-225`) | Write | Open | Permission or Owner | `abwab.doors.restore` | Doors; restores the archived subtree swept with the root |
| POST | `/api/abwab/doors/{doorId}/relations` | `AbwabDoorRelationsController.AddForDoor` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorRelationsController.cs:31-60`) | Write | Open | Permission or Owner | `abwab.relations.create` | Relations; multi-target add is one action |
| DELETE | `/api/abwab/relations/{relationId}` | `AbwabDoorRelationsController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorRelationsController.cs:62-77`) | Write | Open | Permission or Owner | `abwab.relations.delete` | Relations |
| POST | `/api/abwab/templates` | `AbwabTemplatesController.Create` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:62-79`) | Write | Open | Permission or Owner | `abwab.templates.create` | Templates; creates the template and its root node |
| DELETE | `/api/abwab/templates/{templateId}` | `AbwabTemplatesController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:81-96`) | Write | Open | Permission or Owner | `abwab.templates.delete` | Templates |
| POST | `/api/abwab/templates/{templateId}/apply` | `AbwabTemplatesController.Apply` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:98-125`) | Write | Open | Permission or Owner | `abwab.templates.apply` | Templates; deep-copies child door subtrees to targets |
| POST | `/api/abwab/templates/{templateId}/nodes` | `AbwabTemplateNodesController.Add` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplateNodesController.cs:9-43`) | Write | Open | Permission or Owner | `abwab.template_nodes.create` | Template nodes |
| PUT | `/api/abwab/template-nodes/{nodeId}` | `AbwabTemplateNodesController.Edit` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplateNodesController.cs:45-65`) | Write | Open | Permission or Owner | `abwab.template_nodes.edit` | Template nodes; editing root renames template |
| POST | `/api/abwab/template-nodes/{nodeId}/order` | `AbwabTemplateNodesController.Reorder` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplateNodesController.cs:67-86`) | Write | Open | Permission or Owner | `abwab.template_nodes.reorder` | Template nodes; root is refused |
| DELETE | `/api/abwab/template-nodes/{nodeId}` | `AbwabTemplateNodesController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplateNodesController.cs:88-105`) | Write | Open | Permission or Owner | `abwab.template_nodes.delete` | Template nodes; deletes a node subtree; root is refused |

There is no implemented door-protection action. The route catalogue enumerates the complete live Abwab route set without one, and the side-panel contract/test explicitly says no protection entry exists (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359`, `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:221-225`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-side-panel/abwab-side-panel.component.spec.ts:53-73`). No `abwab.doors.protect` permission should be created now.

## 6. Proposed minimal permission catalogue

### 6.1 Canonical naming style

Use:

```text
<bounded_context>.<plural_resource>.<verb>
```

Rules:

- lowercase ASCII;
- dot-separated semantic segments;
- stable business verbs, not HTTP verbs;
- plural resource names;
- snake case only inside a multiword segment, producing `abwab.template_nodes.*`;
- one code per user-visible capability, reused by equivalent single/bulk routes;
- permission codes are immutable authorization identities; changing a label does not change a code.

Naming ambiguities resolved:

- **`edit` vs `rename`:** use `edit`. The current section action is rename-only, but `edit` remains stable if editable fields grow; the Arabic label can remain specific.
- **`archive` vs `delete` for doors:** use `archive` because both the `DELETE` route and bulk action set archive state and support restore (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:296-366`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:369-435`).
- **Template-node segment:** choose `template_nodes` rather than mixing kebab-case (`template-nodes`) or implying a nested template route (`templates.nodes`). Freeze this style before seeding codes.
- **Bulk:** do not create `.bulk_move` or `.bulk_archive`; bulk is a presentation/transport variant of the same capability.

### 6.2 Catalogue

Every “select-all” entry below is membership in that UI group’s bundle. The bundle does not become a persisted permission or Backend policy.

| Stable code | Arabic display label | English technical description | Covered endpoints/actions | UI select-all bundle |
|---|---|---|---|---|
| `abwab.doors.create` | إنشاء الأبواب | Create root or child doors. | `AbwabDoorsController.Create` | Doors: Manage all doors |
| `abwab.doors.edit` | تعديل الأبواب | Edit a door’s authored fields and aliases. | `AbwabDoorsController.Edit` | Doors: Manage all doors |
| `abwab.doors.move` | نقل الأبواب | Move one or several doors to another parent/section. | `Move`, `BulkMove` | Doors: Manage all doors |
| `abwab.doors.reorder` | إعادة ترتيب الأبواب | Reorder a door in Section or Global scope. | `Reorder` | Doors: Manage all doors |
| `abwab.doors.archive` | أرشفة الأبواب | Archive one or several door subtrees. | `Delete`, `BulkArchive` | Doors: Manage all doors |
| `abwab.doors.restore` | استعادة الأبواب | Restore an archived door subtree. | `Restore` | Doors: Manage all doors |
| `abwab.sections.create` | إنشاء الأقسام | Create an Abwab section. | `AbwabSectionsController.Create` | Sections: Manage all sections |
| `abwab.sections.edit` | إعادة تسمية الأقسام | Change a section name. | `Rename` | Sections: Manage all sections |
| `abwab.sections.reorder` | إعادة ترتيب الأقسام | Reorder the live section list. | `Reorder` | Sections: Manage all sections |
| `abwab.sections.delete` | حذف الأقسام | Retire an empty section. | `Delete` | Sections: Manage all sections |
| `abwab.relations.create` | إنشاء العلاقات | Add one relation type from an anchor to one or more doors. | `AbwabDoorRelationsController.AddForDoor` | Relations: Manage all relations |
| `abwab.relations.delete` | حذف العلاقات | Remove a door relation. | `AbwabDoorRelationsController.Delete` | Relations: Manage all relations |
| `abwab.templates.create` | إنشاء القوالب | Create a template and its root. | `AbwabTemplatesController.Create` | Templates: Manage all templates |
| `abwab.templates.delete` | حذف القوالب | Retire a template. | `AbwabTemplatesController.Delete` | Templates: Manage all templates |
| `abwab.templates.apply` | تطبيق القوالب على الأبواب | Copy a template’s child subtrees into selected doors. | `AbwabTemplatesController.Apply` | Templates: Manage all templates |
| `abwab.template_nodes.create` | إضافة عناصر القوالب | Add a child node to a template. | `AbwabTemplateNodesController.Add` | Template nodes: Manage all template nodes |
| `abwab.template_nodes.edit` | تعديل عناصر القوالب | Edit a template node; editing the root also renames the template. | `AbwabTemplateNodesController.Edit` | Template nodes: Manage all template nodes |
| `abwab.template_nodes.reorder` | إعادة ترتيب عناصر القوالب | Reorder a non-root template node. | `AbwabTemplateNodesController.Reorder` | Template nodes: Manage all template nodes |
| `abwab.template_nodes.delete` | حذف عناصر القوالب | Retire a non-root node and its subtree. | `AbwabTemplateNodesController.Delete` | Template nodes: Manage all template nodes |

The action coverage in this catalogue maps exactly to the controller actions listed in section 5. It introduces no read permission, no role-like bundle, and no permission for an unimplemented feature.

## 7. Composite-action decisions

The authorization boundary should follow the user-visible action. Internal resequencing, cascading section IDs, creating controlled child rows, or soft-deleting a subtree does not by itself require a second hidden permission.

| User-visible operation | Internal effects verified | Decision | Reason |
|---|---|---|---|
| Move one door | May cascade the new section to descendants, resequence old/destination siblings, and maintain global-root order (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:88-149`). | One `abwab.doors.move`. | All effects are necessary invariants of moving the selected door; none exposes another independently callable privilege. |
| Reorder one door | Resequences either all live roots in Global scope or the selected sibling set (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:152-206`). | One `abwab.doors.reorder`. | Reordering peers is the declared visible result. |
| Bulk move | Validates all requested doors, may cascade section changes, resequences all affected scopes, and saves as one batch (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:209-293`). | Same `abwab.doors.move`. | Bulk selection changes cardinality, not authority. Do not also require create/reorder. |
| Single archive | Archives the whole subtree and resequences the old sibling/global scopes (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:341-366`). | `abwab.doors.archive`. | The UI describes subtree impact in the confirmation; the route is not a hard delete. |
| Bulk archive | Archives every selected subtree, then resequences affected scopes (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:296-338`). | Same `abwab.doors.archive`. | Bulk is the same destructive capability. |
| Restore door | Restores descendants swept at the same archive timestamp, can resolve a destination section, and repairs scope/global order (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:369-435`). | One `abwab.doors.restore`. | Do not require move/reorder; those are invariant repairs within restore. |
| Delete section | Refuses a section with any live door, then soft-deletes only the section (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabSectionsWriter.cs:47-69`). | One `abwab.sections.delete`. | There is no door cascade or hidden structural delete. |
| Reorder section | Resequences the complete live section list (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabSectionsWriter.cs:72-101`). | One `abwab.sections.reorder`. | Peer resequencing is inherent in order changes. |
| Add relations to multiple targets | Validates every door, constructs all requested relations, and saves them together (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs:9-64`). | One `abwab.relations.create`. | Target count does not add authority; the action is all-or-nothing. |
| Create template | Creates a template row and mandatory root node in one transaction (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs:9-40`). | One `abwab.templates.create`. | Do not also require node-create; the root is part of template identity. |
| Apply template | In one transaction, copies every root child and its descendant subtree as doors beneath every target (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs:11-145`). | One `abwab.templates.apply`. | This deliberately grants controlled door creation through a template. Requiring hidden door-create and node-read permissions would make one visible action unintelligible. |
| Delete template node | Soft-deletes the selected node subtree and resequences remaining siblings (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs:178-213`). | One `abwab.template_nodes.delete`. | Subtree scope is the visible delete contract; no stronger group permission is needed. |

No currently implemented composite operation creates a real privilege-escalation reason to require multiple permissions. If a future action crosses into an independently protected resource with externally visible effects not inherent to the action, that future action must be assessed when it exists.

## 8. Proposed data-model direction

### 8.1 Recommended shape

```text
Users
Roles                    -- one seeded named role: Owner
Permissions              -- stable write-permission catalogue
UserPermissions          -- direct grants
```

`PermissionGroups` should not be a persisted authorization entity in the first design.

### 8.2 `RoleId` and Owner representation

Retain nullable `User.RoleId`, but restrict the live role catalogue to the single `Owner` row:

- It is the smallest migration from the existing nullable FK and verified Owner bootstrap (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:33-39`, `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:69-97`).
- It preserves a named role exactly where the locked product model permits one.
- A new `IsOwner` flag would duplicate or replace working role identity, require wider contract/provisioning changes, and make the existing `roles` table redundant without solving permission grants.
- All non-owner users should have `RoleId = null`; that is already valid in both the domain model and database (`Backend/domain/QuranDashboard.Domain/Access/User.cs:12-16`, `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260718115014_AddAccessUsers.cs:21-29`).

The eventual migration should enforce “null or Owner” in application logic and preferably with a database invariant that cannot be invalidated by later role seeds. Whether there may be exactly one Owner or multiple simultaneous Owners requires the product decision in section 13.

### 8.3 Permissions

Recommended `Permissions` properties:

- internal surrogate `Id`;
- required, immutable `Code`, unique under one canonical lowercase comparison;
- optional persisted Arabic label/English description only if the database/API is to publish catalogue metadata;
- optional `IsRetired` rather than deleting or recycling a code.

The permission **code** is the stable authorization identity. Numeric IDs are environment-local FK details and must never appear in policy attributes or frontend checks.

The 19 codes in section 6 should be seeded/upserted from one Backend catalogue. A code must not be repurposed after deployment; retirement plus a new code is safer than semantic mutation.

### 8.4 Direct grants and audit

Recommended current-grant relation:

```text
UserPermissions
  UserId
  PermissionId
  GrantedAtUtc
  GrantedByUserId
```

Use a composite uniqueness constraint on the active `(UserId, PermissionId)` grant. Grantor references should use restrictive deletion behavior so an audit subject is not erased by a user delete.

If revocation history is required, either:

1. add `RevokedAtUtc`/`RevokedByUserId` and enforce one active grant with a partial unique index; or
2. keep `UserPermissions` as current state and add an append-only permission-change audit table.

Option 2 keeps authorization queries simple; the final choice depends on the audit-retention answer in section 13.

### 8.5 Activation, approval, and deletion

The existing `Pending`/`Active`/`Disabled` status already models access state separately from permission state (`Backend/domain/QuranDashboard.Domain/Access/UserStatus.cs:5-10`). Extend it with audit fields only if required:

- `ApprovedAtUtc`, `ApprovedByUserId`;
- `DisabledAtUtc`, `DisabledByUserId`;
- optionally a controlled reason.

Do not hard-delete administrative users as an ordinary operation. Disabling must immediately deny reads and writes while preserving grants and audit evidence. The product owner must decide whether dormant grants become active again on reactivation or are cleared/reapproved.

Logto account deletion does not currently delete the local row. A recreated Logto subject with the same email hits the unique-email conflict path (`Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:116-145`). Account relinking therefore needs an explicit audited administrative/recovery flow rather than a migration-time guess.

### 8.6 Permission groups

Groups are visual organization and select-all bundles, not authorization identities. Keep group code, Arabic label, display order, and permission membership in the canonical Backend catalogue and return them to the permission-management UI if needed. Persist a `PermissionGroups` table only if non-developer users must localize, reorder, or configure groups at runtime.

No `UserPermissionGroup` or “manage all” grant should exist. Selecting a group grants each current permission code individually; the Backend continues checking the exact action code.

### 8.7 Migration and backward compatibility

A safe future migration must:

1. inventory all current users and their role names;
2. guarantee that the intended Owner row/user remains `Active` before protection is enabled;
3. create/seed the 19 permissions;
4. create direct-grant storage and audit fields;
5. clear or convert every Admin/Editor `RoleId`, then remove those two seed rows;
6. never auto-grant permissions from Admin/Editor names without an approved mapping;
7. update `/api/access/me` from `roleId`/`roleName`-only data to explicit Owner status plus permission codes;
8. update generated OpenAPI/frontend models together.

The current `/me` record and exported schema require `sub`, `email`, `displayName`, `status`, nullable `roleId`, and nullable `roleName` (`Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:37-43`, `Frontend/quran-dashboard-ui/openapi/swagger.json:5594-5628`). During a transition, `roleName` can remain `"Owner"` or null while `isOwner` and `permissions` are added; `roleId` should be removed from the public contract unless the UI has a proven need for an internal database key.

## 9. Proposed Backend authorization direction

### 9.1 Minimal architecture

Use four cohesive pieces:

1. **Request access snapshot.** One scoped service loads the current `User` by `sub`, its status, whether its sole role is Owner, and its direct permission codes. It memoizes the result for the request.
2. **Active-administrator requirement.** Every dashboard data route requires an existing `Active` application user.
3. **Permission requirement/handler.** A reusable `PermissionRequirement(code)` checks the request snapshot. It succeeds centrally for active Owner; otherwise it requires the exact code.
4. **Permission metadata.** A `RequirePermission` attribute or equivalent controller convention carries a constant from the canonical permission catalogue on every write action.

This replaces controller-level database calls, arbitrary string literals, and role-specific policies. `ICurrentUser` can remain the `sub` boundary; `IUserRoleResolver`/`RoleClaimsTransformation` should be removed or narrowed so application authorization reads one access snapshot rather than separately cached role claims (`Backend/application/QuranDashboard.Application.Abstractions/Security/ICurrentUser.cs:3-6`, `Backend/api/QuranDashboard.Api/Authentication/RoleClaimsTransformation.cs:7-42`).

### 9.2 Policies and endpoint conventions

Recommended endpoint posture:

- `GET /api/health`: explicit `[AllowAnonymous]`.
- `GET /api/access/me`: explicit authenticated-only policy so an unknown subject can be provisioned and learn its pending/disabled status.
- All other dashboard reads: active-administrator fallback/read policy.
- Every `POST`, `PUT`, `PATCH`, and `DELETE`: explicit permission metadata whose dynamic policy includes both active-administrator and exact-permission requirements.

The active-user requirement must be included in every explicit permission policy because an explicit policy does not inherit a fallback policy automatically. Owner bypass belongs only inside the permission handler and must execute **after** active status is established; disabled Owner must remain denied, matching the current no-auto-revival rule (`Backend/tests/QuranDashboard.Tests/Api/Access/UserProvisioningServiceTests.cs:82-110`).

Attributes fit the current attribute-routed controllers and make the security rule visible beside each action (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:13-25`, `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:9-17`). A dynamic policy provider or equivalent policy factory avoids registering 19 near-identical policies manually.

### 9.3 Preventing forgotten write protection

Fallback authentication alone is insufficient: an unannotated future write would otherwise become writable by every active user. Add both:

- a **runtime fail-closed convention/requirement** that does not authorize unsafe dashboard methods without permission metadata; and
- an `EndpointDataSource` test that enumerates every `POST`/`PUT`/`PATCH`/`DELETE` and fails by route/method when exact permission metadata is absent or unknown.

Extend `SmokeRouteCatalog` beyond its current two-value `Open`/`RequiresAuthentication` model to record anonymous, authenticated-provisioning, active-read, or exact permission access (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:5-9`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:67-81`). Existing parity checks route/method presence but does not inspect authorization metadata (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:41-68`).

### 9.4 Loading, caching, and invalidation

Start with request-scoped memoization and no cross-request permission cache. Administrative user volume is small, one access query per request is predictable, and revocation becomes immediately effective.

If measurement later justifies cross-request caching:

- cache the complete access snapshot, including negative/inactive results;
- key by `sub` plus an authorization version or equivalent;
- invalidate on permission grant/revoke, activation/deactivation, and Owner transfer;
- make invalidation work across multiple application instances, not only one process;
- never authorize from stale token role/permission claims.

The current role cache is an in-process 30-second `IMemoryCache` with manual eviction; tests demonstrate that direct database mutation leaves a negative result stale until `Evict` (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/CachedUserRoleResolver.cs:8-52`, `Backend/tests/QuranDashboard.Tests/Api/Access/CachedUserRoleResolverTests.cs:44-73`). Reusing that pattern without ensuring every mutation path and every instance invalidates it would create a revocation window.

Any database/cache failure must fail closed—no user is granted access because the resolver failed. Operationally, a controlled `500`/`503` may distinguish an outage from a genuine `403`, but the handler must never succeed on error.

### 9.5 `401`/`403` contract

Use one authorization middleware result handler or equivalent shared writer:

- no/invalid authentication → `401` with the existing shared envelope;
- valid authentication but no local user, `Pending`, `Disabled`, or missing exact permission → `403` with a shared Arabic failure envelope;
- `/api/access/me` remains the provisioning/status exception and can return the pending/disabled user as `200`.

The API guide already reserves `401` for missing/invalid authentication and `403` for authenticated-but-not-allowed, and requires consistent `ApiResponse` shapes (`Backend/.architecture/API_GUIDELINES.md:85-109`). Do not return a bare framework `403`.

## 10. Frontend integration direction

### 10.1 Current state

- Logto initializes at app startup; API URLs are secure routes for bearer attachment (`Frontend/quran-dashboard-ui/src/app/app.config.ts:22-48`).
- `CurrentUser` contains status plus nullable role ID/name, and still declares `Owner | Admin | Editor` (`Frontend/quran-dashboard-ui/src/app/core/auth/current-user.model.ts:1-12`).
- `CurrentUserStore` loads only `/api/access/me`, keeps a user/error signal, and treats HTTP failures generically (`Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.ts:8-79`).
- `roleGuard` redirects unauthenticated callers to Logto and permits only an active exact named role, but it is attached to no route (`Frontend/quran-dashboard-ui/src/app/core/auth/role.guard.ts:9-27`, `Frontend/quran-dashboard-ui/src/app/app.routes.spec.ts:57-66`).
- Neither root routes nor the two Abwab routes declare an activation guard (`Frontend/quran-dashboard-ui/src/app/app.routes.ts:22-69`, `Frontend/quran-dashboard-ui/src/app/features/abwab/abwab.routes.ts:12-23`).
- The Abwab data-access classes expose all four reads and 21 writes with no permission layer (`Frontend/quran-dashboard-ui/src/app/features/abwab/data-access/abwab.api.ts:39-104`, `Frontend/quran-dashboard-ui/src/app/features/abwab/data-access/abwab-templates.api.ts:23-63`).
- Abwab maps `409` to conflict and `400`/`404` to invalid; `401` and `403` fall through to a generic transport error (`Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-write.controller.ts:37-50`).

### 10.2 Current-user contract and reusable checks

Return from `/api/access/me`:

```text
status
isOwner
permissions: string[]
```

alongside the existing identity/profile fields. `permissions` should contain the caller’s direct grants; it may be empty for Owner because `isOwner` is authoritative and for read-only users because they hold no writes.

`CurrentUserStore` should derive:

- `isActive`;
- `isOwner`;
- a `ReadonlySet<string>`;
- `can(permissionCode) = isActive && (isOwner || permissionSet.has(code))`.

All frontend references must use one typed permission-code catalogue rather than scattering strings. The frontend catalogue must be generated from or contract-checked against the Backend source of truth.

### 10.3 Route behavior

All dashboard content routes—including Dashboard, Mushaf, Words, Abwab, templates, and placeholders—should require an authenticated active administrative user. The Logto callback remains public. A pending or disabled authenticated user should land on a dedicated access-status page rather than the current redirect to `/`, which itself redirects into an unguarded dashboard (`Frontend/quran-dashboard-ui/src/app/core/auth/role.guard.ts:21-27`, `Frontend/quran-dashboard-ui/src/app/app.routes.ts:22-29`).

Abwab and `/abwab/templates` remain accessible to every active administrative user because every read is universal. There is no permission route guard for Abwab and no feature-specific read permission.

On refresh or deep-link navigation:

1. initialize Logto;
2. load `/api/access/me`;
3. admit only an active user;
4. preserve the requested read URL/query state after admission.

An unauthenticated deep link should return through Logto to the same URL. A deep-linked write modal/action must not open when the matching permission is absent; the base read page remains available.

### 10.4 Abwab controls

The current page exposes create-root/section/template entry controls, single/bulk door operations, archive restore, relation creation/deletion, and section CRUD/reorder (`Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html:27-47`, `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html:153-200`, `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html:229-290`). The templates workshop exposes template create/apply/edit/delete and node add/edit/reorder/delete (`Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.html:42-80`, `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.html:109-187`, `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.html:195-251`).

Recommended behavior:

- Keep read navigation, tree/cards, archive view, relations viewing, template list, and template tree visible to all active users.
- Hide write-only toolbar and context-menu actions when the exact permission is absent; where a stable control must remain for layout/discoverability, disable it with an accessible Arabic explanation.
- Gate every event path, not only its button: context menu, keyboard reorder, quick-add, row `+`, bulk events, modal submit, and URL-restored modal.
- Show “Manage sections” only when the caller has at least one section write permission, and gate each create/edit/reorder/delete control independently.
- Keep the relation modal readable without permissions; hide/disable add UI unless `abwab.relations.create` and removal affordances unless `abwab.relations.delete` (`Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-relations-modal/abwab-relations-modal.component.html:54-81`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-relations-modal/abwab-relations-modal.component.html:83-164`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-relations-modal/abwab-relations-modal.component.html:173-190`).
- Offer bulk mode only if at least one bulk-capable action is allowed; independently gate bulk move, relation create, and archive (`Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-side-panel/abwab-side-panel.component.html:21-118`).
- Gate archive restore separately from archive (`Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.html:30-45`).
- Keep the template page read-only when no template/node write permission exists; apply, template CRUD, node CRUD, quick-add, and inline reorder each use their own codes.

Frontend checks are UX only. A handcrafted HTTP request must still receive Backend `403`.

### 10.5 Runtime authorization changes and errors

Add explicit frontend handling:

- `401`: begin the established reauthentication/session-expired flow without losing the intended route.
- `403`: show the Backend’s controlled Arabic message; refresh `/api/access/me` because status or grants may have changed; close or disable a stale write surface; do not retry the mutation automatically.

If a permission is revoked while a modal is open, Backend `403` is authoritative. The refreshed store then removes the action. A stale frontend grant may briefly display a control, but it must never make the write succeed.

## 11. Testing-debt and acceptance matrix

### 11.1 Existing coverage

Current authentication tests cover:

- `/api/access/me` provisioning, idempotence, invalid credential `401`, public-route behavior, and email collision (`Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:12-69`, `Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:71-164`);
- fixed seeded roles, Owner bootstrap, and cache eviction (`Backend/tests/QuranDashboard.Tests/Api/Access/AccessRolesTests.cs:12-100`);
- role transformation, including a token-borne role claim not bypassing the database load (`Backend/tests/QuranDashboard.Tests/Api/Access/RoleClaimsTransformationTests.cs:8-70`);
- current policy registration and the deliberate absence of a fallback policy (`Backend/tests/QuranDashboard.Tests/Api/Access/AuthorizationPolicyRegistrationTests.cs:11-39`);
- real pipeline `401`, unknown-sub provisioning, Owner bootstrap, invalid JWTs, and role-cache reset (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeAuthPipelineTests.cs:17-82`).

Current route tests provide bidirectional route/method parity, but the anonymous route sweep explicitly expects no `403` and skips `ParityOnly` writes (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:10-40`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRoutePipelineTests.cs:13-48`). `SmokeAbwabWriteTests` exercises many section/door behaviors through an uncredentialed client, demonstrating the current open posture rather than authorization (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs:13-25`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs:67-90`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs:176-185`).

Frontend auth tests cover bearer attachment, current-user loading, and the unattached named-role guard (`Frontend/quran-dashboard-ui/src/app/core/auth/auth-bearer-token.spec.ts:95-143`, `Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.spec.ts:57-117`, `Frontend/quran-dashboard-ui/src/app/core/auth/role.guard.spec.ts:55-87`). `app.routes.spec.ts` currently asserts the opposite of the target posture—no route has an activation guard—and must be replaced with active-administrator route coverage (`Frontend/quran-dashboard-ui/src/app/app.routes.spec.ts:57-66`).

The Abwab Playwright suite exercises real write flows against a disposable local database, stubs external Logto traffic, and is explicitly supplementary rather than the Backend route-smoke tier (`Frontend/quran-dashboard-ui/e2e/README.md:5-19`, `Frontend/quran-dashboard-ui/e2e/README.md:60-84`, `Frontend/quran-dashboard-ui/e2e/README.md:118-123`). It currently has no read-only/permission personas.

### 11.2 Existing debt that is mandatory for this feature

The ledger explicitly says the Abwab smoke rows are acceptance criteria of the auth feature (`docs/TESTING_DEBT.md:20-25`).

| Debt row | Mandatory acceptance obligation |
|---|---|
| `abwab-relations` row 3 | Dispatch and verify the three relation routes across their `200`/`201`/`204`/`400`/`404`/`409` envelopes, including archived-anchor `200 []`, plus auth personas (`docs/TESTING_DEBT.md:33-37`). |
| `abwab-templates` row 8 | Dispatch all nine template/template-node routes across their documented success/failure envelopes, plus auth personas (`docs/TESTING_DEBT.md:58-63`). |
| `ux-slice-f` F3 | Add section-reorder route smoke for `200`/`400`/`404`/`409`, plus auth personas (`docs/TESTING_DEBT.md:83-87`). |
| `ux-slice-g` G3 | Cover apply’s narrowed `400` and reshaped `409` contract; this narrows row 8 rather than replacing it (`docs/TESTING_DEBT.md:98-102`). |
| `ux-slice-i` I2 | Pay the remaining conditional-GET cases for all three reads: matching/mismatching/malformed validators, required headers/bodiless `304`, and zero-query list-read `304` paths (`docs/TESTING_DEBT.md:138-142`). |

`ux-slice-h` H1 is **conditional**, not automatically an auth-feature acceptance row: its trigger fires only if the implementation changes the navbar/nav model to add auth-gated entries (`docs/TESTING_DEBT.md:113-118`). The recommended route guard can protect routes without hiding read navigation, so H1 should not be claimed as mandatory unless those files actually change.

Writer-behavior debt rows 1, 2, 6, 7, F1/F2, G1/G2, and frontend workshop rows are not made due merely by adding authorization metadata; their own ledger triggers still govern unless implementation touches those writers/surfaces (`docs/TESTING_DEBT.md:33-42`, `docs/TESTING_DEBT.md:58-67`, `docs/TESTING_DEBT.md:83-102`).

### 11.3 Proposed acceptance matrix

| Scenario | Required evidence |
|---|---|
| Unauthenticated request | Every dashboard data/write route returns shared-envelope `401`; `/api/health` remains explicitly anonymous; `/api/access/me` returns `401`. |
| Invalid JWT | Wrong key, expiry, issuer/audience failures return shared-envelope `401`, retaining current access/smoke cases. |
| Authenticated unknown user | `/api/access/me` provisions/returns `Pending`; direct reads/writes outside `/me` return shared-envelope `403`. |
| Pending or disabled user | Every dashboard read and write returns `403`; disabled Owner is also denied. |
| Active read-only user | All four Abwab reads and all other dashboard reads succeed; all 21 Abwab writes return `403`. No named role is required. |
| Active user missing the precise write permission | The attempted action returns `403`, the shared envelope is correct, and database state/cache validators do not change. |
| Active user with the precise permission | The one mapped action succeeds and retains its existing status/envelope/domain behavior. |
| Owner with no direct grants | Every one of the 21 current Abwab writes succeeds through the central bypass; all reads succeed. |
| Permission isolation | Each permission is data-driven against neighboring writes: create does not edit, edit does not move, archive does not restore, template create does not apply, relation create does not delete, and so on. |
| Single/bulk equivalence | `abwab.doors.move` authorizes single and bulk move only; `abwab.doors.archive` authorizes single and bulk archive only. |
| Composite operations | Move/bulk move, archive/bulk archive, restore, section delete/reorder, relation multi-add, template create/apply, and node subtree delete require only the decisions in section 7 and remain all-or-nothing where currently designed. |
| Owner bypass isolation | Owner bypass applies centrally to permission requirements, not to inactive status, invalid authentication, model validation, concurrency, or domain refusals. |
| Token-claim smuggling | Token-borne role or permission-looking claims do not grant Owner or any direct permission; database access state wins. |
| Permission grant/revoke | Grant becomes effective on the next request; revoke and deactivation prevent the next request. If cross-request caching exists, test invalidation and concurrent stale-entry races. |
| Multi-instance/cache behavior | If a distributed deployment caches authorization, revoke/status/Owner changes are observed by every instance; no process-local stale window grants writes. |
| Route completeness | Every current Abwab route appears in `SmokeRouteCatalog`; every unsafe live route carries exactly one known permission code; every safe dashboard read carries active-read protection. |
| `401`/`403` response contract | Both statuses use the shared `ApiResponse` failure shape and correct Arabic messages; no bare framework body. |
| Conditional reads | Active read-only and Owner callers preserve current ETag/`304` semantics; unauthorized callers do not receive protected data or validators. Pay I2. |
| Frontend route guards | Anonymous redirects to Logto and returns to the original URL; pending/disabled reaches access-status UI; active read-only reaches all read pages. |
| Frontend controls | Owner, read-only, and single-permission personas verify visibility/disabled state for toolbar buttons, context menus, keyboard paths, bulk actions, modals/forms, relations add/delete, restore, and template/node operations. |
| Frontend stale permission | Backend `403` after an open modal refreshes current access, closes/disables the action, and does not auto-retry. |
| Handcrafted API request | Direct HTTP calls that bypass hidden frontend controls receive the same Backend `403`; this must be an API integration/smoke assertion, not only a browser assertion. |

Use data-driven matrices keyed by the section 5 route/permission pairs so every current endpoint is covered without copy-pasted tests.

### 11.4 Required verification lanes when implemented

Authentication/authorization changes require the `feature Access` lane plus the adjacent
`feature Middleware` / `feature ApiBehavior` lanes during development, and the `smoke` lane;
ordinary pre-PR is `access` + `smoke` + `tier-b` (`TESTING_STRATEGY.md` §3, §5, §6). Evidence must
name the lane it came from — the canonical Smoke data tier is the separate `canonical-data` lane,
where a missing resource fails the lane rather than skipping it (its §3.4).

Frontend routing/core changes require `npm run test:authorization` and `npm run test:feature:abwab`
during work, then `npm run test:pre-pr` at the review/pre-PR boundary (its §4, §5). Browser E2E may
add useful permission-persona evidence but remains opt-in and cannot substitute for the Backend
`smoke` lane (its §11, `Frontend/quran-dashboard-ui/e2e/README.md`).

There is no CI in the repository; every required lane is local and its exact output must be
recorded (its §8).

## 12. Risks and blockers

| Severity | Risk | Evidence / consequence | Mitigation direction |
|---|---|---|---|
| Critical | Unauthenticated Abwab writes are deployed. | All 21 write routes are open (`Backend/api/QuranDashboard.Api/Controllers/README.md:8-20`, `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:13-26`). | Make Backend protection the first releasable slice; do not rely on frontend hiding or rate limiting. |
| Critical | An active-user fallback without unsafe-method permission enforcement could grant every active user all writes. | Current fallback is null and current route access model has only open/authenticated values (`Backend/tests/QuranDashboard.Tests/Api/Access/AuthorizationPolicyRegistrationTests.cs:33-39`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:5-9`). | Permission metadata on every unsafe action, runtime fail-closed behavior, and endpoint metadata tests. |
| High | Owner lockout during role/data migration. | Owner creation depends on a seeded Owner role, configured email, and verification; disabled Owner is not revived (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:35-66`, `Backend/tests/QuranDashboard.Tests/Api/Access/UserProvisioningServiceTests.cs:82-110`). | Preflight Owner row/config, transactional migration, tested recovery/transfer procedure, and staged deployment order. |
| High | Admin/Editor data cannot be safely auto-converted. | Those roles are seeded, but named policies are unused and writes are open (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:28-31`, `Backend/api/QuranDashboard.Api/README.md:68-71`). | Inventory and require explicit grant decisions; default to no writes. |
| High | Stale permission or Owner state grants revoked access. | Current role resolver caches positive/negative results for 30 seconds and requires explicit eviction (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/CachedUserRoleResolver.cs:8-52`, `Backend/tests/QuranDashboard.Tests/Api/Access/CachedUserRoleResolverTests.cs:44-73`). | Request-scope only initially; otherwise versioned/distributed invalidation on every mutation. |
| High | Missing endpoint protection. | Route parity checks method/path but not authorization metadata; current write routes are `ParityOnly` and not dispatched by the generic sweep (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:37-68`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRoutePipelineTests.cs:13-18`). | Extend catalogue/access assertions and add exact permission metadata parity. |
| High | Explicit permission policies accidentally omit active-user enforcement. | Explicit policies are independently composed in current registration (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:50-58`). | Dynamic permission policy must always include authenticated + active + permission requirements. |
| Medium | Token or stale claims become authorization inputs. | Current transformation already defends its database role load from token role claims (`Backend/api/QuranDashboard.Api/Authentication/RoleClaimsTransformation.cs:18-40`, `Backend/tests/QuranDashboard.Tests/Api/Access/RoleClaimsTransformationTests.cs:20-46`). | Resolve Owner/permissions from application DB; ignore token role/permission claims. |
| Medium | `403` becomes inconsistent or bare. | Only the `401` challenge writer exists; global exception handling does not own forbidden results (`Backend/api/QuranDashboard.Api/Authentication/UnauthorizedRejectionWriter.cs:3-18`, `Backend/api/QuranDashboard.Api/Middleware/GlobalExceptionHandler.cs:24-58`). | Central authorization result handler for both controlled outcomes. |
| Medium | Frontend displays stale write affordances or mishandles auth errors. | Current Abwab error mapper handles only `400`/`404`/`409`; current-user HTTP errors are generic (`Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-write.controller.ts:37-50`, `Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.ts:54-78`). | Explicit `401`/`403` flow and store refresh; Backend remains authoritative. |
| Medium | Logto subject recreation cannot recover the local account. | New `sub` plus an existing email yields `409` and no relink (`Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:116-145`). | Product-approved, audited relink/recovery process. |
| Medium | Owner verification depends on a tenant-specific inference. | Verification is inferred from linked social/SSO identities (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/LogtoManagementApiUserProfileSource.cs:40-47`). | Revalidate against current Logto behavior/config before relying on it for Owner recovery. |
| Medium | Permission-code drift across DB, policies, frontend, and UI bundles. | Current frontend models are hand-authored from the `/me` shape and generated OpenAPI exists separately (`Frontend/quran-dashboard-ui/src/app/core/auth/current-user.model.ts:1-12`, `Frontend/quran-dashboard-ui/openapi/swagger.json:5594-5628`). | One Backend catalogue; seeded DB identities; generated/contract-checked frontend constants. |

## 13. Questions that genuinely require product-owner decisions

The locked product decisions answer the role/read/write model. These remaining questions do not:

1. **Owner cardinality and recovery:** must exactly one active Owner exist, may several Owners coexist, and what is the emergency transfer/recovery path when the Owner’s Logto account is unavailable?
2. **Administration authority:** who may accept/disable users and grant/revoke direct permissions? Is this Owner-only, or will future explicit permission-administration actions be delegated? No such endpoints currently exist, so no permission code is proposed in this report.
3. **Reactivation semantics:** when a disabled user becomes active again, do prior direct grants automatically resume, remain dormant pending review, or get cleared at disable time?
4. **Audit and retention:** must permission/activation history be append-only, how long is it retained, and must the dashboard expose who granted/revoked/approved/disabled?
5. **Identity relinking:** when Logto issues a new `sub` for the same verified email, who may relink the local application user and what proof/review is required?

## 14. Recommended next planning step

After the product owner answers section 13, create a focused permission-design decision record—not a final implementation plan—that freezes:

- Owner cardinality/recovery and activation semantics;
- the 19 permission codes and Arabic labels;
- `/api/access/me` response shape;
- database entities, grant/audit behavior, and Admin/Editor conversion rule;
- Backend active-read, exact-write, Owner-bypass, fail-closed, and `401`/`403` contracts;
- the section 5 endpoint mapping;
- the section 11 acceptance matrix and mandatory debt payoff.

Then validate that decision record against the live controller catalogue and only afterward open implementation planning.

At architecture level, the safe implementation/migration order is:

1. Resolve section 13 and freeze the catalogue, access rules, response contract, and acceptance matrix.
2. Add the authorization personas, route/permission metadata assertions, and debt-payoff tests before changing protection behavior.
3. Apply an additive schema change for permissions/grants/audit, seed the catalogue, convert data explicitly, and prove the intended Owner is active. Keep the old `/me` fields temporarily while adding `isOwner` and permission codes.
4. Build and verify the complete Backend authorization path—active-read fallback, exact write requirements, Owner bypass, fail-closed unsafe-route handling, and controlled `401`/`403`—against all current routes before deployment.
5. Activate Backend enforcement for the entire route inventory as one security boundary. Re-verify Owner, read-only, exact-permission, and denial personas immediately. If Backend and frontend deployments cannot be atomic, Backend enforcement goes first; a temporary denied UI is safer than a temporary unprotected API.
6. Deploy frontend active-user routing, permission-aware controls, deep-link behavior, and explicit `401`/`403` handling against the additive `/me` contract.
7. After data and runtime verification, remove legacy Admin/Editor policies/constants/seeds and any transitional `/me` fields; rerun the required local gates and record whether the smoke data tier ran or skipped.

This is sequencing guidance, not phase/task decomposition; the latter belongs to the later implementation plan.

## 15. Final readiness verdict

`READY_WITH_DECISIONS_REQUIRED`

The current code and route surface are sufficiently understood to design granular permissions. Implementation should not begin until the five product-owner questions in section 13—especially Owner recovery and permission-administration authority—are resolved. The unauthenticated Abwab write surface remains a critical production security blocker while those decisions are being closed.
