# Quran Dashboard API

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
`AddApiAuthentication` (DI) + `UseAuthentication` / `UseAuthorization` (pipeline, after CORS,
before the rate limiter). It uses the standard `JwtBearer` handler to validate a Logto **access
token** (not the ID token):

- **Issuer & signing keys** are auto-discovered from the `Auth:Authority` OIDC metadata
  (`jwks_uri`) — no manual key handling.
- **Audience** must equal `Auth:Audience`, the registered Logto **API resource** indicator.
- Raw claims are preserved (`MapInboundClaims = false`) so the Logto `sub` — the identity key —
  survives unchanged.
- A missing/invalid token yields `401 Unauthorized` with the shared `ApiResponse` failure
  envelope (`isSuccess:false`, Arabic `message`, `errors:[]`) instead of the framework's default
  empty body.

`GET /api/access/me` carries `[Authorize]` (authenticated-only) and, on first login,
**get-or-create provisions** the local user keyed by the Logto `sub`. The user's email is **verified
server-side via the Logto Management API** (the inbound access token cannot call userinfo), never taken
from the client; a new user starts `Pending` with no role. This is the only endpoint that requires
authentication — there is **no global fallback policy**, so every other endpoint stays anonymous.

### Roles (Phase 2 — infrastructure only)

A fixed, seeded role set (`Owner` / `Admin` / `Editor`, seeded with Arabic display names via the
`AddAccessRoles` migration) backs authorization; `Users.RoleId` is a nullable FK → `roles`. Capabilities
are enforced in code keyed by the role **name** (`RoleNames`); roles are never created from the UI.

- **Owner bootstrap** (`Auth:BootstrapOwnerEmail`): on first login, a user whose identity-verified email
  equals this value is provisioned directly as `Owner`/`Active` instead of `Pending`/no-role; an existing
  matching user below `Owner`/`Active` is upgraded (idempotent). An **empty** value disables bootstrap.
- **Role loading:** `RoleClaimsTransformation` (`IClaimsTransformation`) loads the caller's active role
  into a `ClaimTypes.Role` claim, resolved by `sub` via a short-TTL (`30s`) cached `IUserRoleResolver`.
  It is idempotent (never duplicates the claim) and the role/status write path evicts the subject's cache
  entry so a change is observed immediately, not after the TTL.
- **`GET /api/access/me`** returns `roleName` (null when no role) alongside `roleId`/`status`.
- **Named policies registered, applied to nothing:** one policy per role
  (`AuthorizationPolicyNames.Owner`/`Admin`/`Editor`, each `RequireAuthenticatedUser().RequireRole(name)`)
  is registered ready for future admin surfaces. **No `[Authorize(Policy = …)]` is applied to any
  endpoint**, and there is still no global fallback policy — the whole product remains publicly browsable.

### Configuration (`Auth` section)

| Key | Meaning |
|---|---|
| `Authority` | Logto issuer, e.g. `https://<tenant>.logto.app/oidc`. Used for OIDC metadata/JWKS discovery. |
| `Audience` | The exact Logto API resource indicator every access token must target. |
| `BootstrapOwnerEmail` | Email bootstrapped to `Owner`/`Active` on login. **Empty disables bootstrap** (valid, no startup failure); a non-empty value is format-validated fail-fast. |
| `ManagementApi:Endpoint` | Logto tenant endpoint, e.g. `https://<tenant>.logto.app`. |
| `ManagementApi:Resource` | Management API resource indicator, typically `https://<tenant-id>.logto.app/api`. |
| `ManagementApi:AppId` | Machine-to-machine application id for the client-credentials token. |
| `ManagementApi:AppSecret` | Machine-to-machine application secret. **Secret — never commit; set via user-secrets/env.** |

`Authority`/`Audience` and the `ManagementApi` endpoint/resource ship as **placeholder values**
(`REPLACE-WITH-YOUR-…`) in `appsettings*.json`; the deployment owner replaces them with real Logto
tenant values. `BootstrapOwnerEmail` ships **empty** in `appsettings.json` (bootstrap disabled by
default); production must supply the owner address via environment configuration
(`Auth__BootstrapOwnerEmail`) to enable owner bootstrap. Invalid `Auth` values (blank `Authority`/
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
