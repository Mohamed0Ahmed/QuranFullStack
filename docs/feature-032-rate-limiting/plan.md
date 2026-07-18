# Feature 032 — API Rate Limiting (Implementation Plan)

> **Type:** Normal implementation plan (not Spec Kit). **Mode of this document:** plan only — no code, no config, no packages, no git.
> **Feature number:** `032`. Chosen because `docs/` tops out at `feature-031-words-explainers`; `specs/` at `026`; `Backend/report/` at `026`. The PR numbers `#32/#33` in recent git history are unrelated to feature numbering. `032` is the next free feature slot.
> **Scope boundary:** `Backend/api/QuranDashboard.Api` + its `appsettings.*` + tests + docs. No schema, no migration, no Quran-data, not cross-stack.

## Adopted shape (authoritative summary)

- **General limiter:** Token Bucket — `TokenLimit=30` (burst) + `TokensPerPeriod=30` every `ReplenishmentPeriod=15s` → sustained **120 req/min/IP**, `QueueLimit=0`. Covers **every non-exempt request except `/api/health*`**.
- **Health limiter:** **separate** Fixed Window, **per-IP**, `QueueLimit=0`, **generous env-configurable** permit — an initial default subject to live deploy verification (`/api/health` is NOT fully exempt: it runs a DB health check, so unlimited public access is a DB-amplification vector).
- **Partition keys are namespaced** — `general:{ip}` vs `health:{ip}` — so the two policies for the same IP never collide (see §5.2).
- **Client IP:** Railway **`X-Real-IP`** (single-valued; **configurable** header name) → fallback `HttpContext.Connection.RemoteIpAddress` → `"unknown"` sentinel.
- **Exempt (NoLimiter):** `OPTIONS` preflight + Development Swagger **only**.
- **429 response:** `ApiResponse` envelope + `Retry-After` header.
- **Secure by default:** `Enabled=false` in **all** shipped `appsettings` (base, Development, Production). Enabled via env var only after the verification gates.
- **Rollout:** verify `X-Real-IP` (not spoofable) with `Enabled=false` → verify probe-not-throttled + Angular smoke with `Enabled=true` on staging/monitored deploy → enable Production.

---

## 0. Fact re-verification (repo + platform grounding)

All repo facts re-confirmed against the current tree; each is cited. No locked decision conflicts with repo reality. Platform facts are grounded in official docs.

**Repo facts**

| Fact | Location | Status |
|---|---|---|
| TFM `net10.0` (all projects) → built-in limiter is in-framework, **no NuGet** | every `*.csproj`; API refs Swashbuckle 10.2.3 / HealthChecks.EFCore 10.0.0, no rate-limit pkg | ✅ |
| Pipeline: `ExceptionHandler → (dev)Swagger → HttpsRedirection → Cors("AngularDev") → MapControllers` | `Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:7,9–17,19,20,21` | ✅ |
| Registrations: controllers, Swagger, HealthChecks(+`AddDbContextCheck`), ProblemDetails, `AddExceptionHandler<GlobalExceptionHandler>`, `AddCors` | `Backend/api/QuranDashboard.Api/Extensions/ServiceCollectionExtensions.cs:16–80` | ✅ |
| **CORS fail-fast precedent** (`throw new InvalidOperationException`) | `…/Extensions/ServiceCollectionExtensions.cs:68–71` | ✅ |
| Envelope `ApiResponse<T>` with `Fail(message, errors)` (errors default `[]`) | `Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs:20–25` | ✅ |
| `GlobalExceptionHandler` writes **ApiResponse, not ProblemDetails** (`WriteAsJsonAsync`, `application/json`) | `Backend/api/QuranDashboard.Api/Middleware/GlobalExceptionHandler.cs:31,33–37` | ✅ |
| Central Arabic messages; **no 429 constant yet** | `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs` | ✅ |
| Health route `api/health`; **controller always returns HTTP 200** and **runs a DB health check** (`AddDbContextCheck<QuranDashboardDbContext>`) | `Controllers/System/HealthController.cs:6,15,17,32`; `ServiceCollectionExtensions.cs:57–58` | ✅ |
| Railway healthcheck on `/api/health`, `healthcheckTimeout 120`, `ON_FAILURE`, `maxRetries 10`; edge-TLS, `$PORT` | `Backend/railway.json`; `Backend/Dockerfile` | ✅ |
| Options-binding precedent `Configure<T>(GetSection(...))` | `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/MushafReaderDependencyInjection.cs:12` | ✅ |
| `ForwardedHeaders`/`KnownProxies` **not configured anywhere** | repo-wide grep empty | ✅ |
| Unit test pattern: `DefaultHttpContext` + `MemoryStream` | `Backend/tests/QuranDashboard.Tests/Api/Middleware/GlobalExceptionHandlerTests.cs:19–37` | ✅ |
| HTTP integration precedent: `WebApplicationFactory<WordTypeGroupedDetailsController>` boots real `UseApiPipeline`; `ConfigureTestServices` swaps DbContext | `…/Quran/WordsWordTypes/WordTypesTestFixture.cs:91–119`; `…/WordTypesGroupedDetailsControllerTests.cs:46–72` | ✅ |
| **Fixture caches a singleton factory** → stateful limiter would bleed across theory cases | `WordTypesTestFixture.cs:95–98` (`_apiFactory ??= …`) | ✅ (addressed in §8) |
| `GET /api/dashboard/info` needs **no DB** | `Controllers/Dashboard/DashboardController.cs:13–22` | ✅ (DB-free general-limiter target) |

**Platform / framework facts**

| Fact | Source |
|---|---|
| The header **"for identifying the client's remote IP" is `X-Real-IP`**. `X-Forwarded-For` is **not** listed for this purpose. | Railway — *Public Networking → Specs & Limits* (`https://docs.railway.com/networking/public-networking/specs-and-limits`) |
| The deployment **health check runs only at deployment start**, repeating **until it gets `200` or the timeout (default 300s)**, and is **"not used for continuous monitoring"** — Railway does **not** poll the endpoint after the deployment is live, and it does **not** restart a running app. **Probe cadence is not documented.** | Railway — *Deployments → Healthchecks* (`https://docs.railway.com/deployments/healthchecks`) |
| The deploy health probe originates from hostname `healthcheck.railway.app` (internal source), so probe requests likely share **one** partition key. The `Host` header is **client-spoofable** and must not be used as a security exemption. | Railway — *Deployments → Healthchecks* (as above) |
| **`PartitionedRateLimiter` caches the materialized inner limiter by partition key (first-wins):** if the key already exists, the cached limiter is reused and the factory is **not** called again. Distinct policies for the **same** IP therefore require **namespaced keys** or they collide. | Microsoft Learn — *Rate limiting middleware in ASP.NET Core*; dotnet/runtime #71352 |
| `RateLimitPartition.GetTokenBucketLimiter`/`GetFixedWindowLimiter` **force `AutoReplenishment=false`** on each inner limiter and drive replenishment from the `PartitionedRateLimiter`'s **own single timer** — so the flag is not a user-meaningful knob, and tests cannot freeze a bucket by setting it. | .NET source `RateLimitPartition.cs`; dotnet/runtime #114151 |

**Design refinements (not conflicts with any locked decision):**
- Exemptions are **partitioner-level** (method/env) via `GetNoLimiter`, not endpoint attributes → **no explicit `UseRouting`** required; `UseRateLimiter` sits right after `UseCors`.
- `RateLimiterOptions.RejectionStatusCode` **defaults to 503** → it **must be set to `429`** explicitly.
- `/api/health` is routed to its **own** per-IP limiter (not `NoLimiter`), with a **namespaced** key so it never shares a bucket with the general limiter — see §5.

---

## 1. Objective & exact final behavior

Add a global, per-client-IP rate limiter to every non-exempt endpoint, configured from `appsettings`, so the API sheds abusive burst/sustained traffic while leaving legitimate reads untouched and never blocking Railway deployments.

**Two limiters, selected by the partitioner, keyed on namespaced partitions:**
- **General** (Token Bucket) for **all non-exempt requests except `/api/health*`**: `TokenLimit=30`, `TokensPerPeriod=30`, `ReplenishmentPeriod=15s`, `QueueLimit=0` → sustained **120 req/min/IP**, burst **30**. Key `general:{ip}`.
- **Health** (Fixed Window, per-IP) for `/api/health*`: `QueueLimit=0`, generous env-configurable permit (§5.5). Key `health:{ip}`.

**Admitted request:** passes through unchanged (one in-memory lease acquisition).

**Rejected request (over limit):**
- HTTP **`429 Too Many Requests`**, `Content-Type: application/json`.
- Body = shared envelope `ApiResponse<object>.Fail(ApiMessages.TooManyRequests)`:
  ```json
  { "isSuccess": false, "message": "<Arabic 429 message>", "data": null, "errors": [] }
  ```
- Header **`Retry-After: <seconds>`** from lease `MetadataName.RetryAfter` (fallback: the limiter's window/period seconds).

**Client-IP source:** the single-valued **`X-Real-IP`** header (Railway), via a small unit-testable resolver; fall back to `RemoteIpAddress` when missing/empty/malformed, then `"unknown"`. Header name is configurable (so a fronting CDN/Cloudflare change can be absorbed without code edits).

**Exemptions (NoLimiter):** any `OPTIONS`, and `/swagger*` in Development only.

**Kill switch / secure default:** `RateLimiting:Enabled=false` makes the partitioner return a no-op limiter for every request. **Every shipped `appsettings` sets `Enabled=false`** (base, Development, Production) so no environment inherits throttling before header verification; it is turned on via the Railway env var only after the gates in §4/§11.

---

## 2. Scope & explicit non-goals

**In scope:** Api project limiter registration + pipeline wiring, options + IP resolver + rejection writer, one new Arabic message constant, `appsettings.*` sections (general **and** health limits), unit + integration tests, docs.

**Non-goals (explicit):**
- ❌ Per-user / API-key partitioning (design for it; do not build it).
- ❌ `RateLimit-*` draft headers (only `Retry-After`).
- ❌ Distributed / Redis-backed store (in-memory, per-instance).
- ❌ Authentication / authorization middleware.
- ❌ `ForwardedHeaders` middleware + `KnownProxies`/`KnownNetworks`. **Justification:** Railway proxy IPs are dynamic (allowlist impractical); the app-level single-header `X-Real-IP` resolver achieves per-IP partitioning without it.
- ❌ Sliding-window / concurrency algorithms for the general limiter; per-route tiered policies beyond the general/health split.
- ❌ **Caching the DB health-check result** to make `/api/health` cheap regardless of rate — noted as **future hardening** (a `HealthController`/health-registration change, not this feature). Until then, the health limiter is the guard against DB amplification.
- ❌ Any change to controllers, handlers, EF, cache, or Quran data.

---

## 3. Affected layers & files

**New files (under `Backend/api/QuranDashboard.Api/RateLimiting/`):**
- `RateLimitingOptions.cs` — bound options (general + health) + validation.
- `IClientIpResolver.cs` — resolver abstraction.
- `ClientIpResolver.cs` — single `X-Real-IP` + fallback implementation (returns the **raw** IP; namespacing happens in the partitioner).
- `RateLimitRequestClassifier.cs` — pure `IsHealthRequest(PathString)` helper; the **single source** of the health-path rule, used by both the partitioner and the rejection writer (DRY).
- `RateLimitRejectionWriter.cs` — `OnRejected` body/header writer (mirrors `GlobalExceptionHandler` shape); **DI-registered, options-aware** (reads `RateLimitingOptions` for the `Retry-After` fallback).
- `RateLimitingRegistration.cs` — `internal static AddRateLimiting(this IServiceCollection, IConfiguration)` extension (partitioner selecting general/health/NoLimiter with namespaced keys + fail-fast validation).

**Changed files:**
- `Extensions/ServiceCollectionExtensions.cs` — call `services.AddRateLimiting(configuration)` in `AddApiServices` (near `AddCors`).
- `Extensions/WebApplicationExtensions.cs` — insert `app.UseRateLimiter()` (exact slot + reserved post-auth comment, §5.7).
- `Common/ApiMessages.cs` — add `TooManyRequests` Arabic constant.
- `appsettings.json` — base `RateLimiting` section (general **and** health defaults, **`Enabled: false`**).
- `appsettings.Development.json` — `RateLimiting:Enabled: false`.
- `appsettings.Production.json` — explicit values, **`Enabled: false` at ship** (flip to `true` via env var post-verification).

**New test files (under `Backend/tests/QuranDashboard.Tests/`):**
- `Api/RateLimiting/ClientIpResolverTests.cs` (unit).
- `Api/RateLimiting/RateLimitRejectionWriterTests.cs` (unit).
- `Api/RateLimiting/RateLimitingIntegrationTests.cs` + a **dedicated** `RateLimitingApiFactory.cs` (integration; not the shared singleton fixture; **mandatorily** overrides the DB health check).

**Docs (same change):**
- `Backend/.architecture/API_GUIDELINES.md` §14 (concrete general/health rate-limit rule) + §5 (429 envelope note).
- `Backend/api/QuranDashboard.Api/README.md` and/or `Controllers/README.md` — envelope now covers 429, the general/health split, exemptions, config section + `Enabled` switch.

---

## 4. Ordered phases, dependencies & checkpoints

Single controlled pass. Phases 1–6 are code/docs; Phases 7–9 are **deploy-time gates** (note the ordering fix: the spoof check runs with the limiter **off**; the probe-not-throttled + smoke checks require it **on**).

| Phase | Work | Depends on | Checkpoint (must pass before next) |
|---|---|---|---|
| **1. Options + resolver** | `RateLimitingOptions` (general+health), `IClientIpResolver`/`ClientIpResolver` + unit tests | — | Resolver unit tests green (`X-Real-IP`, fallback, malformed, IPv6, custom name, `"unknown"`) |
| **2. Rejection writer + message** | `RateLimitRejectionWriter`, `ApiMessages.TooManyRequests` + unit tests | — | Writer emits 429 + envelope + `Retry-After`; unit tests green |
| **3. Limiter registration** | `AddRateLimiting`: general (token bucket) + health (fixed window) with **namespaced keys** + partitioner selection + `RejectionStatusCode=429` + `OnRejected` + fail-fast validation; call from `AddApiServices` | 1, 2 | `dotnet build` green; invalid config throws at startup |
| **4. Pipeline wiring + config** | `app.UseRateLimiter()` slot + reserved post-auth comment; `appsettings.*` sections (all `Enabled=false`) | 3 | App boots in Dev (Enabled=false → no throttle) |
| **5. Integration tests** | dedicated factory (DB health check overridden); general throttle, **per-IP isolation**, **same-IP cross-policy isolation**, health-limiter, exemptions, disabled | 4 | Integration tests green, no state bleed |
| **6. Docs** | API_GUIDELINES §14/§5, READMEs | 4 | Docs match implemented behavior |
| **7. `X-Real-IP` verification (GATE, Enabled=false)** | On a **staging** deploy with the limiter **off**, add a **temporary** diagnostic logging **only three fields** — the raw configured IP-header value, `RemoteIpAddress`, and the resolved client IP (**never** all inbound headers; they may carry `Authorization`/`Cookie`). Confirm **`X-Real-IP` carries the true client IP** AND that a client **cannot override/spoof** it (send a forged `X-Real-IP`; verify the server-seen value differs). **Remove/disable the diagnostic after verification.** | deploy | `X-Real-IP` trustworthy → proceed to enable on staging |
| **8. Probe + smoke verification (GATE, Enabled=true on staging/monitored deploy)** | With the limiter **on** (preview/staging if available, else a monitored Production deploy with rollback ready): confirm the **Railway healthcheck succeeds** (deploy probe not throttled by the health limiter) AND run the **Angular smoke test** — open the main pages, navigate normally, confirm **no `429`** from parallel page-load requests (validates burst=30) | 7 | Probe green + zero 429 on normal navigation |
| **9. Enable Production** | Set Production `Enabled=true` (if Phase 8 ran on staging) or confirm the monitored Production deploy from Phase 8 is stable | 8 | Production stable with limiter on |

Phases 1 and 2 are independent. Phase 6 may run alongside Phase 5. If no preview/staging environment exists, Phases 8–9 collapse into **one monitored Production deployment with a clear rollback** — do **not** claim the probe was pre-verified in that case; it is verified live under monitoring.

---

## 5. Backend design

### 5.1 Limiter shape — global limiter with partitioner selection (chosen)
Use `RateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(partitioner)`.
**Justification:** scope is "all non-exempt endpoints" with two profiles (general + health) and a couple of NoLimiter carve-outs; a single global limiter whose **partitioner selects the profile** covers this with zero per-endpoint annotation and needs **no endpoint metadata** (so no `UseRouting`).

### 5.2 Partitioner selection with namespaced keys (contract — implement in `RateLimitingRegistration`)
```
partitioner(HttpContext ctx):
    if not options.Enabled:                                   return NoLimiter("__disabled__")
    if HttpMethods.IsOptions(ctx.Request.Method):             return NoLimiter("__options__")
    if env.IsDevelopment()
       and ctx.Request.Path.StartsWithSegments("/swagger"):   return NoLimiter("__swagger__")

    ip = ipResolver.Resolve(ctx)                              // RAW client IP string

    if RateLimitRequestClassifier.IsHealthRequest(ctx.Request.Path):
        return RateLimitPartition.GetFixedWindowLimiter($"health:{ip}",  _ => healthOptions)
    else:
        return RateLimitPartition.GetTokenBucketLimiter($"general:{ip}", _ => generalOptions)
```
- **CRITICAL — namespaced keys.** `PartitionedRateLimiter` caches the materialized limiter **by key (first-wins)**; a raw `ip` key shared by both policies would make whichever limiter is created first for that IP serve **both** health and general requests. The `general:` / `health:` prefixes keep the two buckets independent for the same IP. The resolver returns the raw IP; the partitioner does the prefixing. Exempt `NoLimiter` partitions keep their constant keys.
- `NoLimiter` = `RateLimitPartition.GetNoLimiter(sharedKey)`.
- `generalOptions` = `new TokenBucketRateLimiterOptions { TokenLimit, TokensPerPeriod, ReplenishmentPeriod = TimeSpan.FromSeconds(ReplenishmentPeriodSeconds), QueueLimit, AutoReplenishment = false }` (the partition forces `false` regardless; setting it explicitly also saves a per-limiter timer allocation per the .NET docs).
- `healthOptions` = `new FixedWindowRateLimiterOptions { PermitLimit = HealthPermitLimit, Window = TimeSpan.FromSeconds(HealthWindowSeconds), QueueLimit = 0 }`.

**Path matching:** the health rule lives once in `RateLimitRequestClassifier.IsHealthRequest` (`StartsWithSegments("/api/health", OrdinalIgnoreCase)`); the Dev-Swagger check uses `StartsWithSegments("/swagger", OrdinalIgnoreCase)`. **Do not** trust the `Host`/`healthcheck.railway.app` hostname for any decision — it is client-spoofable.

### 5.3 IP resolver contract (`IClientIpResolver`)
```
string Resolve(HttpContext ctx)
```
Algorithm (single-valued header — **no comma split, no leftmost logic**):
1. Read header `options.ClientIpHeaderName` (default `X-Real-IP`).
2. If present and non-blank: **trim**; if `IPAddress.TryParse` succeeds, return its normalized string.
3. Else fall back to `ctx.Connection.RemoteIpAddress?.ToString()`.
4. Else return `"unknown"` (all unresolved clients share one conservative bucket — documented).
Registered as a stateless singleton. Returns the **raw** IP (no policy prefix). Unit-testable via `DefaultHttpContext`.

### 5.4 Rejection wiring (in `AddRateLimiting`)
- `options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;` (override the 503 default).
- `options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext,string>(partitioner)` (resolver + options via closure/`sp`).
- `options.OnRejected = (context, ct) => RateLimitRejectionWriter.WriteAsync(context, ct);` — the writer resolves `RateLimitingOptions` (via `context.HttpContext.RequestServices`/`IOptions`, or captured in the `AddRateLimiting` closure) and uses `RateLimitRequestClassifier` to choose the `Retry-After` fallback (§6).

### 5.5 Options (`RateLimitingOptions`) + fail-fast
Fields:
- `Enabled (bool = false)`, `ClientIpHeaderName (string = "X-Real-IP")`.
- **General:** `TokenLimit (int = 30)`, `TokensPerPeriod (int = 30)`, `ReplenishmentPeriodSeconds (int = 15)`, `QueueLimit (int = 0)`. *(No `AutoReplenishment` knob — the partition forces it `false` and drives replenishment from its own timer; §0.)*
- **Health:** `HealthPermitLimit (int = 300)`, `HealthWindowSeconds (int = 60)`.

**Health default rationale (deploy safety):** Railway's deploy probe polls `/api/health` repeatedly until `200` or the configured healthcheck timeout (`healthcheckTimeout=120` in `railway.json`; 300s is only Railway's default), all sharing one partition key (internal source, no `X-Real-IP` → `RemoteIpAddress`). **Railway does not document the probe cadence**, so the `300 / 60s` default is an **initial, generous, env-configurable** starting value chosen to bound public DB-amplification abuse while leaving ample headroom for the probe; it is **subject to live deployment verification** (Phase 8): confirm on a real deploy that the probe is never throttled and tune the value if the observed cadence requires it.

Bind via `services.AddOptions<RateLimitingOptions>().Bind(configuration.GetSection("RateLimiting")).Validate(...).ValidateOnStart()` (or eager read+validate throwing `InvalidOperationException`, mirroring CORS `ServiceCollectionExtensions.cs:68–71`).
Validation (throw on violation): `TokenLimit > 0`, `TokensPerPeriod > 0`, `ReplenishmentPeriodSeconds > 0`, `QueueLimit >= 0`, `HealthPermitLimit > 0`, `HealthWindowSeconds > 0`, `ClientIpHeaderName` non-blank.

### 5.6 `appsettings` shape
```
"RateLimiting": {
  "Enabled": false,                        // base/Development/Production all false at ship; env var → true after gates
  "ClientIpHeaderName": "X-Real-IP",
  "TokenLimit": 30,
  "TokensPerPeriod": 30,
  "ReplenishmentPeriodSeconds": 15,
  "QueueLimit": 0,
  "HealthPermitLimit": 300,
  "HealthWindowSeconds": 60
}
```

### 5.7 Pipeline insertion (exact) — `WebApplicationExtensions.cs`
Insert **after `app.UseCors("AngularDev")` (line 20) and before `app.MapControllers()` (line 21):**
```
app.UseExceptionHandler();
… (dev) Swagger …
app.UseHttpsRedirection();
app.UseCors("AngularDev");
// ── future: app.UseAuthentication(); app.UseAuthorization();  (reserve this slot) ──
app.UseRateLimiter();          // NEW — after CORS (preflight + 429 CORS headers), after future auth
app.MapControllers();
```
**Rationale:** CORS before limiter ⇒ preflight handled by CORS and 429 carries CORS headers; the reserved comment fixes the future per-user slot (`UseRateLimiter` after `UseAuthentication` so claims are available). Global limiter needs no endpoint metadata → no `UseRouting` added.

---

## 6. 429 behavior (`RateLimitRejectionWriter`)

The writer is **DI-registered and options-aware**: it reads `RateLimitingOptions` (via `context.HttpContext.RequestServices` / `IOptions`, or captured in the `AddRateLimiting` closure) and shares `RateLimitRequestClassifier` with the partitioner so the health-path rule is defined once.

`WriteAsync(OnRejectedContext context, CancellationToken ct)`:
1. If `context.HttpContext.Response.HasStarted` → return (mirror `GlobalExceptionHandler:13`).
2. `Retry-After`: `if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))` → `Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString()`. On **missing** metadata, pick the fallback via the shared classifier: `RateLimitRequestClassifier.IsHealthRequest(context.HttpContext.Request.Path)` → `HealthWindowSeconds`, else → `ReplenishmentPeriodSeconds`. Both token-bucket and fixed-window limiters normally supply `RetryAfter` metadata.
3. `Response.StatusCode = StatusCodes.Status429TooManyRequests;` `ContentType = "application/json";`
4. `await Response.WriteAsJsonAsync(ApiResponse<object>.Fail(ApiMessages.TooManyRequests), ct);`

New constant `ApiMessages.TooManyRequests` — Arabic, generic (no Quranic content), "too many requests, try again shortly". API property names stay English (envelope unchanged).

---

## 7. Failure / edge behavior (explicit)

| Condition | Behavior |
|---|---|
| `X-Real-IP` **absent/empty/whitespace** | Fall back to `RemoteIpAddress`; if null → `"unknown"` bucket. |
| `X-Real-IP` **malformed** (non-IP) | Same fallback path (no throw). |
| `X-Real-IP` **spoofed by client** | **Unverified until Phase 7.** If Railway does not overwrite a client-sent `X-Real-IP`, partitioning is spoofable — Phase 7 (limiter off) is the trust gate (§11 stop condition). |
| Custom `ClientIpHeaderName` (CDN in front) | Honored via config; no code change. |
| **IPv6** client | `IPAddress.TryParse` handles it; normalized string is the key. |
| `OPTIONS` preflight | `NoLimiter` (never throttled). |
| `/swagger*` in Development | `NoLimiter`; in non-Dev Swagger isn't mapped anyway. |
| `/api/health*` | **Health fixed-window limiter, per-IP**, key `health:{ip}` (not exempt). Deploy probe (single key) stays under the generous default. |
| General bucket exhausted for IP A | IP A → `429`; **IP B (different `X-Real-IP`) still admitted**. |
| Health bucket exhausted for one IP | That IP's `/api/health` → `429`; a different health IP still admitted. |
| **Same IP, general exhausted** | `/api/health` for that **same** IP is **still admitted** — separate `health:{ip}` bucket (proves namespacing; no key collision). |
| **Same IP, health exhausted** | A general endpoint for that **same** IP is **still admitted** — separate `general:{ip}` bucket. |
| Keys **not** namespaced (defect guard) | Would collide: first-created limiter for the IP serves both policies. Prevented by the `general:`/`health:` prefixes (§5.2). |
| `RateLimiting:Enabled=false` | Partitioner returns `NoLimiter` for all requests → zero throttling (shipped default). |
| Invalid config values | Startup throws `InvalidOperationException` (fail fast). |
| `Retry-After` metadata missing | Fall back to the relevant window/period seconds. |

---

## 8. Tests per phase

### Phase 1 — `ClientIpResolverTests` (unit, `DefaultHttpContext`)
- `X-Real-IP` present → returns that IP.
- `X-Real-IP` absent → returns `RemoteIpAddress`.
- `X-Real-IP` malformed → falls back to `RemoteIpAddress`.
- Custom `ClientIpHeaderName` honored.
- IPv6 parsed; whitespace trimmed.
- Null everything → `"unknown"`.
- *(No multiple-value / leftmost test — `X-Real-IP` is single-valued.)*

### Phase 2 — `RateLimitRejectionWriterTests` (unit, `DefaultHttpContext` + `MemoryStream`, mirrors `GlobalExceptionHandlerTests`)
- Status `429`; `Content-Type application/json`.
- Envelope: `isSuccess=false`, `message=ApiMessages.TooManyRequests`, `data=null`, `errors=[]`.
- `Retry-After` present when a lease exposes `RetryAfter` metadata; fallback value when absent.
- `HasStarted` short-circuit path.

### Phase 5 — `RateLimitingIntegrationTests` (real HTTP via dedicated `WebApplicationFactory`)
**State-bleed caveat (locked):** do **not** reuse the shared singleton `WordTypesTestFixture`. Use a **dedicated `RateLimitingApiFactory`** per test class that overrides `RateLimiting` and `Enabled=true`, and **mandatorily overrides the DB health check** in `ConfigureTestServices` (replace `AddDbContextCheck` with a stub healthy check) so tests never touch a real DB or hang.

**Deterministic counting (locked):** the inner `AutoReplenishment` flag is not a freeze lever — the partition forces it `false` and drives replenishment from its own timer (§0). Instead use a **very long `ReplenishmentPeriodSeconds`** (general) and a **long `HealthWindowSeconds`** (health) so no replenishment occurs during the test, **tiny permit limits** (e.g. general `TokenLimit=2, TokensPerPeriod=2`; health `HealthPermitLimit=2`), and fire requests **immediately/rapidly**. Isolate each simulated client by sending a **distinct `X-Real-IP`** per test.

- **General throttle:** `GET /api/dashboard/info` (no DB) with IP A → `TokenLimit` admitted; next → `429` + envelope + `Retry-After`.
- **Per-IP isolation (general):** IP A exhausts its bucket → `429`; **IP B → `200`** (proves per-IP, not accidental global).
- **Cross-policy isolation A (namespacing):** IP A — exhaust the **general** bucket → `/api/health` for IP A still `200` (its `health:{A}` bucket untouched).
- **Cross-policy isolation B (namespacing):** IP B — exhaust the **health** bucket → a general endpoint for IP B still `200` (its `general:{B}` bucket untouched). *(Two separate IPs on purpose: exhausting a bucket leaves it exhausted, so a single-IP combined assertion would be self-defeating.)*
- **Health limiter throttles + isolates by IP:** hammering `/api/health` with one IP past `HealthPermitLimit` → eventually `429`; a **different** health IP still admitted.
- **Exemptions:** many `OPTIONS` → never `429`; Dev Swagger → never `429`.
- **Disabled:** with `Enabled=false`, exceed nominal limits on `/api/dashboard/info` → never `429`.

---

## 9. Docs to update (same change)
- `Backend/.architecture/API_GUIDELINES.md` — replace §14's generic "rate limiting should be configured centrally" with the concrete contract (general token bucket 120/min/IP over **all non-exempt requests** + separate per-IP health fixed window with **namespaced keys**, `X-Real-IP` partition, `OPTIONS`/Dev-Swagger exemptions, env-configurable, secure-default-off-until-verified); add a one-line 429-envelope note to §5.
- `Backend/api/QuranDashboard.Api/README.md` and/or `Controllers/README.md` — record that the shared `ApiResponse` envelope now covers `429`, the general/health split, exemptions, and the `RateLimiting` config section + `Enabled` switch.

---

## 10. Data validation & performance
- **Integrity: LOW.** No read path, EF query, cache (`MushafReader*` cached readers), or Quran text is touched. The limiter admits requests unchanged or short-circuits with 429 before the controller. Confirm by diff review: only Api-project middleware/registration/config/tests/docs change.
- **DB-amplification guard:** `/api/health` (which runs a DB check) is now bounded per-IP by the health limiter instead of being unlimited — the reason it is not fully exempt.
- **Performance:** admitted-request cost = one in-memory lease acquisition; negligible. No added DB/IO on the hot path.
- **Per-instance limiter (documented out-of-scope):** the in-memory limiter is per process; if Railway scales to N instances the effective limits become N× the configured values. Acceptable now (single instance); a distributed store is future work. Recorded in API_GUIDELINES §14.

---

## 11. Risks, rollback, stop conditions
**Risks**
- **Partition-key collision (HIGH if not namespaced):** sharing a raw-IP key between the general and health limiters would make the first-created limiter serve both policies (`PartitionedRateLimiter` caches by key, first-wins). Mitigated by `general:`/`health:` namespacing (§5.2) and proven by the same-IP cross-policy test (§8).
- **`X-Real-IP` trust (HIGH until Phase 7):** if Railway does not overwrite a client-supplied `X-Real-IP`, a client could spoof the partition key and evade/poison per-IP limiting. Verified with the limiter **off** in Phase 7; Production stays `Enabled=false` until it passes.
- **Health-limiter misconfiguration (HIGH, deployment-time):** the deploy health probe polls `/api/health` until `200` or the configured healthcheck timeout (`healthcheckTimeout=120` in `railway.json`; 300s is Railway's default). A **tight** health limit could throttle the probe and **DELAY or FAIL a NEW Railway deployment**. Railway's health check is **deployment-time only, not continuous — it does not restart a running app.** Mitigated by the generous default (§5.5) + Phase 8 probe verification (which requires the limiter **on**). Do **not** rely on the `healthcheck.railway.app` hostname as an exemption (spoofable `Host`).
- **429 without envelope (MED):** if `OnRejected` is skipped, clients get a bare 429 breaking the envelope contract. Covered by unit + integration tests.
- **Accidental global instead of per-IP (MED):** covered by the Phase-5 per-IP + same-IP cross-policy isolation tests.
- **Diagnostic header logging (MED, operational):** the Phase-7 IP check must log **only** the configured IP-header value, `RemoteIpAddress`, and the resolved IP — **never** full inbound headers (`Authorization`/`Cookie` leak); run on **staging**, temporary, removed after verification.
- **Test state bleed / non-determinism (MED):** shared factory + limiter's own replenishment timer — mitigated by dedicated factory, per-test unique `X-Real-IP`, overridden DB health check, and long windows (not the inner `AutoReplenishment` flag).

**Rollback:** flip `RateLimiting:Enabled=false`. **Accurate caveat:** the app reads config at startup, so changing a Railway environment variable (or `appsettings`) **requires a service restart/redeploy to take effect — it is NOT an instant, no-redeploy toggle.** Alternatively revert the single feature commit and redeploy.

**Rollout (corrected sequence — no circular verification):**
1. **`Enabled=false`** (prod or preview): verify `X-Real-IP` carries the true client IP **and is not client-spoofable** (Phase 7).
2. **`Enabled=true` on a preview/staging deployment:** verify the Railway healthcheck **succeeds** (probe not throttled) **and** run the Angular smoke test (Phase 8).
3. **Enable Production** (Phase 9).
   - If no preview/staging exists, step 2 is performed as a **monitored Production deployment with a clear rollback**; do **not** claim the probe was pre-verified — it is verified live under monitoring.

**Stop conditions (halt and report):**
- Same-IP cross-policy test fails (keys collide / not namespaced).
- Health, `OPTIONS`, or (Dev) Swagger get throttled unexpectedly in tests.
- Per-IP isolation tests fail (limiting is effectively global).
- Phase 7 shows `X-Real-IP` is client-overridable (spoofable) → do **not** enable in Production; reassess (alternate header, or `ForwardedHeaders`+trust config).
- Phase 8 shows the deploy probe is throttled by the health limiter → raise `HealthPermitLimit` and re-verify.
- Any locked value cannot be satisfied by the built-in token-bucket / fixed-window limiters as specified.

---

## 12. Acceptance criteria (testable)
- [ ] No NuGet package added; builds on `net10.0`.
- [ ] Exceeding the general burst on a normal endpoint returns **429** with `{isSuccess:false, message:<Arabic>, data:null, errors:[]}` and a `Retry-After` header.
- [ ] **Per-IP isolation proven:** one `X-Real-IP` being throttled does not throttle a different `X-Real-IP` (general limiter).
- [ ] **Cross-policy isolation proven (namespacing):** on IP A, exhausting the general bucket leaves `/api/health` admitted; on IP B, exhausting the health bucket leaves general endpoints admitted.
- [ ] `/api/health` has its **own** per-IP fixed-window limiter (namespaced key), not full exemption; the health limiter isolates per IP.
- [ ] The health-path rule lives once in `RateLimitRequestClassifier`, shared by the partitioner and the rejection writer (DRY).
- [ ] `OPTIONS` and Dev `/swagger` are never throttled.
- [ ] Limits (general **and** health) are read from `appsettings` and change behavior without code edits; `Enabled=false` disables throttling.
- [ ] **Secure default:** base, Development, and Production `appsettings` all ship `Enabled=false`.
- [ ] Invalid config throws at startup (fail fast).
- [ ] Client IP resolves from single `X-Real-IP`, else `RemoteIpAddress`, else `"unknown"`; header name configurable; unit-tested.
- [ ] Client identity resolution is centralized in `IClientIpResolver`; policy-key composition (`general:`/`health:`) is centralized in the partitioner — so a future switch to per-user keying is a localized change; `UseRateLimiter` sits after the reserved auth slot.
- [ ] Integration tests use a dedicated factory with the **DB health check overridden** and long windows (not the inner `AutoReplenishment` flag); no shared-fixture state bleed.
- [ ] `API_GUIDELINES.md` and the API README(s) updated in the same change.
- [ ] **Phase 7 gate (limiter off):** on a deployed service, `X-Real-IP` confirmed to carry the true client IP AND confirmed not client-spoofable.
- [ ] **Phase 8 gate (limiter on, staging/monitored):** Railway deploy probe confirmed not throttled AND normal Angular navigation (parallel page requests) produces zero `429`.
- [ ] Production is enabled (Phase 9) only after Phases 7–8 pass.

---

## 13. Commit boundary
Single final commit for this coherent, self-contained feature (Api + config + tests + docs). **Do not commit as part of this plan** — commit only on explicit user request, following `commit-workflow`.
