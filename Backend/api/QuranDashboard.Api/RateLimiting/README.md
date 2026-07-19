# Rate limiting

Per-client-IP request throttling for `QuranDashboard.Api`. This folder owns the limiter
partitioning, client-IP resolution, and the `429` rejection response; it is wired into DI by
`AddRateLimiting` and into the pipeline by `UseRateLimiter` (see `../Extensions/`).

## Profiles

The global limiter partitions each request into one of two per-IP profiles (`RateLimitingRegistration`):

- **General (token bucket)** — every non-exempt request except `/api/health*`. Keyed `general:{ip}`;
  `TokenLimit`/`TokensPerPeriod`/`ReplenishmentPeriodSeconds`/`QueueLimit` from options
  (defaults 30 / 30 / 15s / 0), with `AutoReplenishment=false`.
- **Health (fixed window)** — `/api/health*` only. Keyed `health:{ip}`; `HealthPermitLimit` per
  `HealthWindowSeconds` window (defaults 300 / 60s), `QueueLimit=0`.

Keys are namespaced (`general:` / `health:`) so the two profiles never share a materialized limiter
for the same IP. Health-vs-general classification is `RateLimitRequestClassifier.IsHealthRequest`
(path `StartsWithSegments("/api/health")`), the single rule shared by the partitioner and the
rejection writer.

## Exemptions

The partitioner returns a no-op limiter (no throttling) when:

- `Enabled` is `false` — the secure-by-default state; the feature ships disabled and is turned on
  via configuration override only.
- the request method is `OPTIONS` (CORS preflight).
- the environment is Development and the path starts with `/swagger`.

## Client IP resolution

`ClientIpResolver` reads the configured `ClientIpHeaderName` header (default `X-Real-IP`, treated as
a single value — no comma-split list parsing) and returns it when it parses as an `IPAddress`.
Otherwise it falls back to `HttpContext.Connection.RemoteIpAddress`, or the literal `unknown`.

## Rejection response

`RateLimitRejectionWriter` writes throttled responses: HTTP `429`, `application/json`, a `Retry-After`
header (seconds), and the shared `ApiResponse<object>.Fail(ApiMessages.TooManyRequests)` envelope.
`Retry-After` comes from the lease's `RetryAfter` metadata (ceiling, minimum 1s), falling back to the
relevant profile window (`HealthWindowSeconds` or `ReplenishmentPeriodSeconds`). It is a no-op if the
response has already started.

## Options and validation

`RateLimitingOptions` binds from the `RateLimiting` configuration section.
`RateLimitingOptionsValidator` runs with `ValidateOnStart()`, so invalid configuration (blank header
name, non-positive limits/periods, or negative `QueueLimit`) throws at startup rather than as runtime
limiter errors.

## Boundary

- Runs as middleware **before** controllers and **before** authentication (keyed per-IP, not per-user),
  and **after** CORS so preflight is handled and `429` responses carry CORS headers.
- Keying is per-client-IP only; it does not read user claims or authorization state.
- The envelope and status contract are shared with the rest of the API — see `../Contracts/ApiResponse.cs`,
  `../README.md`, and `../../../.architecture/API_GUIDELINES.md`.
