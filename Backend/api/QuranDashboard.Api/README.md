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
