# Quran Dashboard API

**Adding or changing an endpoint requires adding or updating its `SmokeRouteCatalog` entry in
the same change** (`../../tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`). The catalog
is bidirectionally locked to the live `EndpointDataSource`, so an uncatalogued route — or a
catalog entry whose route no longer exists — fails `SmokeCoverageParityTests` by route name.
Run it with `../../scripts/test-backend smoke --no-build` after any route, contract, auth,
middleware, model-binding, startup, DI, or configuration change. See
`../../../TESTING_STRATEGY.md` §6 and §10.

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
**get-or-create provisions** the local user keyed by the Logto `sub`. Owner bootstrap email evidence
is **verified server-side through the already-validated OIDC claims**: matching `sub`, present
`email`, and `email_verified=true`. Logto Management API `primaryEmail` is used only to match provider
identity data and is never email-verification authority. A new user starts `Pending` with no role.
`/api/access/me` is the only generic authenticated-only endpoint. The twelve administration routes for
users, direct grants, audit history, relink preview/confirm, catalogue, and reconciliation status carry
`[RequireOwner]`; they never accept a direct permission as equivalent and never mutate Owner membership or
Owner configuration. The twenty-one Abwab write endpoints each carry an exact granular permission
requirement; there is **no global fallback policy**, so public content GETs remain anonymous.

The API has a database-backed authorization core:
`[RequirePermission(...)]` checks an active local user's exact direct grant, while an active local Owner
bypasses that exact check; `[RequireOwner]` accepts only an active local Owner. Both resolve the `sub`
through `ICurrentUser` and ignore token-borne role or permission claims. Every Abwab write has exactly one
`[RequirePermission(...)]` matching the route's required catalogue code. `UnsafeEndpointMetadataValidator`
runs immediately after controller mapping and refuses any unsafe endpoint with missing, unknown, or
conflicting metadata; the requirement handlers repeat that validation fail-closed. It also validates the
Owner-only classification of security-administration writes. This does not change public `GET` endpoints
or `/api/access/me`, and production activation remains a separate deployment gate.

### Roles (Phase 2 — infrastructure only)

A fixed, seeded role set (`Owner` / `Admin` / `Editor`, seeded with Arabic display names via the
`AddAccessRoles` migration) backs authorization; `Users.RoleId` is a nullable FK → `roles`. Capabilities
are enforced from active direct grants, with a separate active-Owner bypass; `Admin` and `Editor` are
transitional role data, not a capability source. Roles are never created from the UI.

- **Owner bootstrap** (`OwnerBootstrap:Emails`): the normalized, validated desired Owner list is
  reconciled for additions only after a configured identity provisions through `/api/access/me` with
  verified interactive OIDC email evidence. The operator tool can report
  `AwaitingVerifiedSignIn`, remove safely resolved stale Owners, and clean Owner direct grants, but
  cannot promote an Owner from M2M data. Each promotion revokes direct grants and appends audit
  history in the same transaction. Empty, invalid, or duplicate normalized lists fail startup
  validation; a Disabled configured user is never reactivated.
- **Authorization state:** a scoped `IAuthorizationStateResolver` projects status, the local Owner
  relation, and active non-Owner direct grant codes once per protected request. It never provisions a
  user and rejects a second distinct `sub` in its request scope. The old role-claim transformation is
  no longer registered; `IUserRoleResolver` remains transitional for existing reconciliation/cache
  invalidation work and is not consulted by the new requirement handlers.
- **`GET /api/access/me`** returns `sub`, `email`, `displayName`, `status`, `isOwner`, ordered
  active direct `permissions`, and transitional `roleName`; it does not expose `roleId`.
  Owners, Pending users, and Disabled users receive an empty permission list. `isOwner` remains
  true for a Disabled configured Owner while every authorization handler still fails closed on status.
- **Named policies registered, applied to nothing:** one policy per role
  (`AuthorizationPolicyNames.Owner`/`Admin`/`Editor`, each `RequireAuthenticatedUser().RequireRole(name)`)
  is registered ready for future admin surfaces. **No `[Authorize(Policy = …)]` is applied to any
  endpoint**, and there is still no global fallback policy. Granular Abwab authorization metadata does not
  use these policies or transformed/token role claims.

### Configuration (`Auth` section)

| Key | Meaning |
|---|---|
| `Authority` | Logto issuer, e.g. `https://<tenant>.logto.app/oidc`. Used for OIDC metadata/JWKS discovery. |
| `Audience` | The exact Logto API resource indicator every access token must target. |
| `ManagementApi:Endpoint` | Logto tenant endpoint, e.g. `https://<tenant>.logto.app`. |
| `ManagementApi:Resource` | Management API resource indicator, typically `https://<tenant-id>.logto.app/api`. |
| `ManagementApi:AppId` | Machine-to-machine application id for the client-credentials token. |
| `ManagementApi:AppSecret` | Machine-to-machine application secret. **Secret — never commit; set via user-secrets/env.** |

`Authority`/`Audience` and the `ManagementApi` endpoint/resource ship as **placeholder values**
(`REPLACE-WITH-YOUR-…`) in `appsettings*.json`; the deployment owner replaces them with real Logto
tenant values. Production supplies one or more Owner identities as
`OwnerBootstrap__Emails__0`, `OwnerBootstrap__Emails__1`, and so on. Their normalized values must
be unique; an empty list fails startup validation. Invalid `Auth` values (blank `Authority`/
`Audience`, or an `Authority` that is not an absolute `https` URI) **fail fast** at startup. The `ManagementApi` credentials are **not** validated at startup (the secret
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
