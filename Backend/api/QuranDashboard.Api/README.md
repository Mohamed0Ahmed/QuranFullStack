# Quran Dashboard API

**Adding or changing an endpoint requires adding or updating its `SmokeRouteCatalog` entry in
the same change** (`../../tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`). The catalog
is bidirectionally locked to the live `EndpointDataSource`, so an uncatalogued route — or a
catalog entry whose route no longer exists — fails `SmokeCoverageParityTests` by route name.
When `../../../TESTING_CONSTITUTION.md` and the active plan's `Testing Decision` select route Smoke,
run `../../scripts/test-backend smoke --no-build`. See
`../../tests/QuranDashboard.Tests/README.md` and `../../scripts/README.md` for the selected lane's
coverage and mechanics.

## Connection String Setup

The database password is **not** committed to source control. Set it via .NET User Secrets or an environment variable.

### Option 1: User Secrets (recommended for local development)

```sh
cd api/QuranDashboard.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:QuranDashboardDb" "Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=<your-password>"
```

### Option 2: Environment Variable

```sh
export ConnectionStrings__QuranDashboardDb="Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=<your-password>"
```

## Authentication

Logto access-token authentication lives in `Authentication/` and is wired via
`AddApiAuthentication` (DI) + `UseAuthentication` / `UseAuthorization` (pipeline, after CORS and
**after the rate limiter** — the limiter keys per client IP, not per user, so it deliberately
throttles unauthenticated traffic before authentication ever runs; see `RateLimiting/README.md`).
It uses the standard `JwtBearer` handler to validate a Logto **access token** (not the ID token):

- **Issuer & signing keys** are auto-discovered from the `Auth:Authority` OIDC metadata
  (`jwks_uri`) — no manual key handling.
- **Audience** must equal `Auth:Audience`, the registered Logto **API resource** indicator.
- Raw claims are preserved (`MapInboundClaims = false`) so the Logto `sub` — the identity key —
  survives unchanged.
- A missing/invalid token yields `401 Unauthorized` with the shared `ApiResponse` failure
  envelope (`isSuccess:false`, Arabic `message`, `errors:[]`) instead of the framework's default
  empty body. `ApiAuthorizationMiddlewareResultHandler` owns that challenge body and every future
  authorization forbid body, so the JWT bearer handler does not write a competing response.
- `GET api/access/me` provisions the caller on first sight, so it carries one status the other
  authenticated paths do not: an email already registered to a different `sub` is a
  `409 Conflict` in the same failure envelope (`UserProvisioningEmailConflictException` →
  `Middleware/GlobalExceptionHandler.cs`, the only non-`500` that handler produces).

`GET /api/access/me` carries `[Authorize]` (authenticated-only) and, on first login,
**get-or-create provisions** the local user keyed by the API access token's Logto `sub`. The SPA keeps
that API resource token in `Authorization` and supplies the raw signed Logto ID token separately in
`X-Interactive-Identity-Evidence`. A dedicated named JwtBearer scheme validates the ID-token signature,
issuer, lifetime, and SPA-client audience before the Backend accepts its matching `sub`, present
`email`, and `email_verified=true`. Logto Management API `primaryEmail` is used only to match provider
identity data and is never email-verification authority. A new user starts `Pending` with no role.
`/api/access/me` is the only generic authenticated-only endpoint. The twelve administration routes for
users, direct grants, audit history, relink preview/confirm, catalogue, and reconciliation status carry
`[RequireOwner]`; they never accept a direct permission as equivalent and never mutate Owner membership or
Owner configuration. The twenty-one Abwab write endpoints each carry an exact granular permission
requirement; there is **no global fallback policy**, so public content GETs remain anonymous.

`POST /api/linking/sources/resolve` (`Controllers/Linking/LinkingSourcesController.cs`) carries exactly
one `[RequireOwner]` and is the single boundary that resolves all six Abwab linking source families into
their complete ayah set. It is a **POST used as a read** — deliberate, because the request body is a
discriminated descriptor union too large and too structured for a query string, and the route is
Owner-only so it is metadata-valid. It never pages: the whole set comes back in one response, bounded by
`LinkingLimits.MaxResolvedAyahs`. Status mapping is `200` resolved, `400` invalid descriptor / resolved-ayah
cap exceeded / manual completeness failure naming the offending verse, `404` referenced dimension id not
found. **Both failure statuses name the offence, and both do it in Arabic.** The `400` is produced from the
structured `LinkingDescriptorViolation` via `ApiMessages.LinkingDescriptorViolationMessage`; the `404` is
produced from `ResolveLinkingSourceOutcome.NotFound.Reference` via `ApiMessages.LinkingSourceNotFoundMessage`,
which renders the reference the reader built (`rootId=999999`, `tashkeelWordId=…`) into the Arabic constant —
`المصدر المشار إليه غير موجود «rootId=999999»`. That `Reference` is a structured `field=id` pair assembled by
`EfLinkingSourceResolutionReader.NotFound`, never an exception `.Message`: the English text carried by
`LinkingSourceDescriptorValidation` and by the exception types is developer diagnostics for logs and must
never reach the envelope. A blank reference falls back to the bare `ApiMessages.LinkingSourceNotFound`
constant, so the envelope is never left with a dangling quotation. The request body is validated field-by-field in
`Contracts/Linking/LinkingSourceDescriptorBodyMapper.cs` **before** the Domain descriptor is constructed,
so a malformed body is a controlled `400` naming the field rather than a caught `ArgumentException`.

`Controllers/Linking/LinkingWorkspaceController.cs` carries the six per-user workspace routes — `GET
/api/linking/workspace`, `POST|DELETE /api/linking/workspace/sources`, `DELETE
/api/linking/workspace/sources/{id}`, `PUT /api/linking/workspace/sources/order`, and `PUT
/api/linking/workspace/sources/{id}/configuration` — each with exactly one `[RequireOwner]`. Four rules
govern this controller and none of them is optional:

- **The workspace is always the caller's own.** The owning user is `AuthorizationState.UserId`, re-resolved
  through `AuthorizationStateAccessEvaluator.ResolveActiveStateAsync` — the same seam `[RequireOwner]` just
  used, so the two can never disagree, and the per-scope memoization in `AuthorizationStateResolver` makes
  the second call free rather than a second query. There is no `?userId=`, no admin view, and no body field
  naming a user (spec FR-026). A request-supplied user id would be a cross-user leak; the id is never read
  from the wire.
- **`GET` is strictly read-only** (spec FR-019, research R21). No workspace row means an empty
  representation — `workspaceVersion: null`, empty `sources` — and **zero inserts**. The row is created by
  the first mutation, not by loading.
- **Every modifying route carries the version the client last read** (FR-027). Structural routes (add,
  remove, reorder, clear) carry `workspaceVersion`; the configuration route carries `sourceVersion`, so
  edits to two different sources never falsely conflict. On the two `DELETE`s the token is a **required
  query parameter** bound as `uint?` and refused with a `400` when absent — deliberately nullable, because a
  non-nullable `uint` would silently bind a missing token as `0`, a real-looking version that would then be
  compared and answered `409` instead of the honest "you sent no version".
  **The controller enforces this on four of the five modifying routes, not five.** `RemoveSource`,
  `ReorderSources`, `ReplaceSourceConfiguration` and `ClearSources` each call `VersionRequired()` up front;
  `AddSource`
  deliberately does not, because `workspaceVersion` is legitimately `null` on the very first add, when no
  workspace row exists yet and the client has no token to send. The rule is not weaker there, only enforced
  one layer down: `EfLinkingWorkspaceWriter.AddSourceAsync` refuses a token when no workspace exists and
  `ApplyWorkspaceVersion` refuses a missing token when one does, both with `LinkingStaleVersionException`
  → `409`. So an add that omits the token against an existing workspace is still refused; read the
  asymmetry as controller-plus-writer, not as a gap.
- **Status mapping**: `200` success, `400` validation failure, `404` a source id absent from *the caller's
  own* workspace, `409` stale version or duplicate identity. A descriptor naming a **non-existent dimension
  id** is `400` here (`LinkingWorkspaceViolationCode.ReferenceUnknown`), not the `404` the resolve route
  returns for the same id — on resolve the descriptor is the addressed resource, on add it is a field of a
  create request whose addressed resource exists. The reasoning is recorded in
  `infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/README.md`. Every message is composed in Arabic at this
  boundary from the structured `LinkingWorkspaceViolation.Code` via
  `ApiMessages.LinkingWorkspaceViolationMessage` (or from `LinkingDescriptorViolation` for descriptor and
  body-shape failures). **No exception `.Message` ever reaches the envelope** — the English text on
  `LinkingStaleVersionException`, `LinkingWorkspaceViolationException`, and the rest is developer
  diagnostics for logs only.

The wire response is `Contracts/Linking/LinkingWorkspaceResponse`, mapped from the Application
`LinkingWorkspaceDto` by `LinkingWorkspaceResponseMapper`. The mapping exists because the DTO carries the
**typed Domain `LinkingSourceDescriptor`** while the wire carries `LinkingSourceDescriptorBody` — the same
schema the resolve route accepts, so a client round-trips a prepared source straight back into
`POST /api/linking/sources/resolve` without a second descriptor shape. `descriptions` is carried on both
sides of the configuration route — a flat `(ayahId, orderValue, body)` list on the request body and on
each response source — and **replacement is wholesale**: the writer stores exactly the submitted set, so
an omitted `descriptions` field deletes every description that source had. A client that edits any other
part of the configuration must echo the descriptions it loaded. Body-shape failures (`descriptions.ayahId`,
`descriptions.orderValue` non-positive) are refused by `LinkingWorkspaceConfigurationBodyMapper` as `400`
naming the field; the four content rules — per-ayah count, order uniqueness, body length, and the ayah
belonging to that source's own set — are refused deeper, as `LinkingWorkspaceViolationCode`
`DescriptionLimitExceeded` / `DescriptionOrderConflict` / `DescriptionBodyInvalid` /
`DescriptionAyahOutsideSource`, each rendering an Arabic message that names the offending **ayah id**.

The API has a database-backed authorization core:
`[RequirePermission(...)]` checks an active local user's exact direct grant, while an active local Owner
bypasses that exact check; `[RequireOwner]` accepts only an active local Owner. Both resolve the `sub`
through `ICurrentUser` and ignore token-borne role or permission claims. Every Abwab write has exactly one
`[RequirePermission(...)]` matching the route's required catalogue code. `UnsafeEndpointMetadataValidator`
runs immediately after controller mapping and refuses any unsafe endpoint with missing, unknown, or
conflicting metadata; the requirement handlers repeat that validation fail-closed. It also validates the
Owner-only classification of security-administration writes. This does not change public `GET` endpoints
or `/api/access/me`, and production activation remains a separate deployment gate.

### Owner role

Only the seeded `Owner` role remains. `Users.RoleId` is a nullable FK to `roles`, used exclusively for
the local Owner relation; capabilities are enforced from active direct grants with a separate active-Owner
bypass. `Admin` and `Editor` have no remaining role, policy, or claim-based authorization path. Roles are
never created from the UI.

- **Owner bootstrap** (`OwnerBootstrap:Emails`): the normalized, validated desired Owner list is
  reconciled for additions only after a configured identity provisions through `/api/access/me` with
  verified interactive OIDC email evidence. The operator tool can report
  `AwaitingVerifiedSignIn`, remove safely resolved stale Owners, and clean Owner direct grants, but
  cannot promote an Owner from M2M data. Each promotion revokes direct grants and appends audit
  history in the same transaction. Empty, invalid, or duplicate normalized lists fail startup
  validation; a Disabled configured user is never reactivated.
- **Normalized identity:** `Users.NormalizedEmail` is the required, unique comparison key for local
  email identity. Provisioning normalizes provider email before persistence and rejects a
  collision rather than merging or relinking users.
- **Authorization state:** a scoped `IAuthorizationStateResolver` projects status, the local Owner
  relation, and active non-Owner direct grant codes once per protected request. It never provisions a
  user and rejects a second distinct `sub` in its request scope. No role-claim transformation or role
  resolver participates in authorization.
- **`GET /api/access/me`** returns `sub`, `email`, `displayName`, `status`, `isOwner`, ordered
  active direct `permissions`.
  Owners, Pending users, and Disabled users receive an empty permission list. `isOwner` remains
  true for a Disabled configured Owner while every authorization handler still fails closed on status.
- **No named role policies:** authorization uses the exact permission and Owner requirements above; there
  is no global fallback policy. Granular Abwab authorization metadata does not use token role claims.

`authorization preflight` is a readiness gate for the already-deployed schema and authorization
data. A clean result neither deploys an artifact nor activates authorization; those operational
actions remain outside the executable and the API process.

### Permission-catalogue startup synchronization

`Program.cs` calls `SynchronizePermissionCatalogueAsync` **between `UseApiPipeline()` and
`app.Run()`** — deliberately not an `IHostedService`, which would start after Kestrel and could
serve `GET /api/access/permissions` against an empty table. It runs inside its own async scope under
a 15-second budget: if `GetPendingMigrationsAsync` reports pending migrations it logs a warning and
returns without writing; otherwise it synchronizes the canonical catalogue and logs the
added/updated/unknown/retired counts. `PermissionCatalogueSynchronizer` takes a blocking
transaction-scoped advisory lock, so concurrent instances serialize instead of racing.

**A failure here never refuses the start.** Migrations are applied by a human running
`scripts/update-db` and `railway.json` carries no pre-deploy hook, so a deploy can legitimately
precede its migration. The operational exception set is caught, logged at `Error`, and the
application starts degraded — taking down every anonymous public `GET` over one Owner-only endpoint
would be the worse failure. Degraded startup is not permission to write: `GET /api/access/permissions`
returns `assignmentReady:false` in exactly that state and the editor fails closed.

The `permission_catalogue` health check reports the same readiness and is registered with
**`failureStatus: Degraded`, which is mandatory** — `HealthController` answers `503` only for
`Unhealthy` and `railway.json` gates the deploy on `/api/health`, so an `Unhealthy` catalogue check
would make the application permanently undeployable.

| Key | Default | Meaning |
|---|---|---|
| `Access:PermissionCatalogueStartupSync:Enabled` | `true` (in code; no `appsettings` entry) | Master switch for the boot-time sync. Production disables it with `Access__PermissionCatalogueStartupSync__Enabled=false`. `scripts/export-swagger` sets it `false` so generating a spec never writes to a database. |

### Configuration (`Auth` section)

| Key | Meaning |
|---|---|
| `Authority` | Logto issuer, e.g. `https://<tenant>.logto.app/oidc`. Used for OIDC metadata/JWKS discovery. |
| `Audience` | The exact Logto API resource indicator every access token must target. |
| `InteractiveClientId` | The exact Logto SPA application id every interactive ID-token evidence token must target. |
| `ManagementApi:Endpoint` | Logto tenant endpoint, e.g. `https://<tenant>.logto.app`. |
| `ManagementApi:Resource` | Management API resource indicator, typically `https://<tenant-id>.logto.app/api`. |
| `ManagementApi:AppId` | Machine-to-machine application id for the client-credentials token. |
| `ManagementApi:AppSecret` | Machine-to-machine application secret. **Secret — never commit; set via user-secrets/env.** |

`Authority`/`Audience`, `InteractiveClientId`, and the `ManagementApi` endpoint/resource ship as
**placeholder values** (`REPLACE-WITH-YOUR-…`) in base configuration; the deployment owner replaces
them with real Logto tenant values. Production supplies one or more Owner identities as
`OwnerBootstrap__Emails__0`, `OwnerBootstrap__Emails__1`, and so on. Their normalized values must
be unique; an empty list fails startup validation. Invalid `Auth` values (blank `Authority`,
`Audience`, or `InteractiveClientId`, or an `Authority` that is not an absolute `https` URI)
**fail fast** at startup. The `ManagementApi` credentials are **not** validated at startup (the secret
is legitimately absent on a fresh clone); they are validated on first use of `/api/access/me` with an
actionable error naming any missing keys.

Set the machine-to-machine credentials via User Secrets (do not put the secret in `appsettings*.json`):

```sh
cd api/QuranDashboard.Api
dotnet user-secrets set "Auth:ManagementApi:AppId" "<your-m2m-app-id>"
dotnet user-secrets set "Auth:ManagementApi:AppSecret" "<your-m2m-app-secret>"
```

## Rate Limiting

A global, per-client-IP rate limiter lives in `RateLimiting/` and is wired via
`AddRateLimiting` (DI) + `UseRateLimiter` (pipeline, after CORS). It exposes two profiles,
selected by one partitioner with **namespaced keys** (`general:{ip}` / `health:{ip}`) so the
two profiles for the same IP never collide:

- **General** — token bucket over all non-exempt requests except `/api/health*`
  (default sustained **120 req/min/IP**, burst 30).
- **Health** — a separate fixed-window limiter for `/api/health*` (it runs a DB health
  check, so it is bounded per-IP rather than fully exempt).

Behavior:

- **Client IP** resolves from the configurable single-valued header `ClientIpHeaderName`
  (default `X-Real-IP`) → `RemoteIpAddress` → `"unknown"`.
- **Exemptions:** any `OPTIONS`, and `/swagger*` in Development only.
- **Over-limit** requests get `429 Too Many Requests` with the shared `ApiResponse`
  failure envelope (`isSuccess:false`, Arabic `message`, `errors:[]`) and a `Retry-After`
  header.

### Configuration (`RateLimiting` section)

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | Master switch. **Ships `false` everywhere**; enable via env var only after the deploy-time verification gates. |
| `ClientIpHeaderName` | `X-Real-IP` | Single-valued client-IP header set by the edge proxy. |
| `TokenLimit` / `TokensPerPeriod` / `ReplenishmentPeriodSeconds` / `QueueLimit` | `30` / `30` / `15` / `0` | General token-bucket profile. |
| `HealthPermitLimit` / `HealthWindowSeconds` | `300` / `60` | Health fixed-window profile. |

Limits are read from `appsettings` and change behavior without code edits. Invalid values
**fail fast** at startup. To roll back, set `RateLimiting:Enabled=false` — note this reads at
startup, so it requires a service restart/redeploy (not an instant toggle).
