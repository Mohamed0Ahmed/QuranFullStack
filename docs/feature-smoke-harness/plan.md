# API Smoke Harness — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: `superpowers:subagent-driven-development`
> (recommended) or `superpowers:executing-plans`. Steps use checkbox (`- [ ]`) syntax.
> This is a normal implementation plan, **not** a Spec Kit feature — no `specs/<feature>/`
> artifacts, no `spec.md`/`tasks.md`.

**Goal:** Restore the route-level backend smoke tier described in `TESTING_STRATEGY.md` §13 as a
real, running xUnit suite (`QuranDashboard.Tests.Smoke`), so that any change to `Backend/api/`
routes, contracts, auth, middleware, or model binding has a real-composition gate again.

**Architecture:** One `WebApplicationFactory<HealthController>` booted with
`ASPNETCORE_ENVIRONMENT=Testing` over a Testcontainers PostgreSQL, driving all 48 registered
routes through routing → authentication → authorization → model binding → handler →
serialization. A reflection parity gate over `EndpointDataSource` proves the catalog and the live
route table are the same set, in both directions. A separate, self-skipping data tier restores a
regenerated canonical `quran_*` dump and asserts the legacy read routes return real, non-empty
data.

---

## 0. Environment note — read before starting

Two facts about the working tree differ from the framing of this feature's brief; the plan
accounts for both rather than assuming them away.

| Assumed | Actual (verified 2026-07-28) | Consequence |
|---|---|---|
| Work starts from `dev`, which already contains the Playwright e2e layer | `dev` is at `ad898826`; the e2e layer lives on `e2e-bootstrap` (`d0631a14`) and **is not an ancestor of `dev`** | **Merge `e2e-bootstrap` into `dev` first.** Decision 4 ("no Kestrel sentinel — the e2e layer covers real-server boot") and the §13 rewrite in Phase 5 both edit text that only exists on `e2e-bootstrap`. Branching this feature off today's `dev` would silently drop that rationale and reintroduce merge conflicts in `TESTING_STRATEGY.md`. |
| `resources/db-dumps/quran-canonical/` is stale | Confirmed stale — `manifest.json` names `20260727120308_AddAbwabQuranLinks`, which is **not** one of the 19 migrations in `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/` (head is `20260718142612_AddAccessRoles`) | Phase 4 regenerates it. The old file is replaced, not patched. |

---

## Objective

`dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` boots the real API
composition once and proves, per route:

1. the route is registered and reachable (no 404-from-routing, no 405);
2. authentication and authorization behave as declared (anonymous vs authenticated vs owner);
3. model binding accepts the catalogued path and the action executes;
4. the response is the shared `ApiResponse<T>` envelope, correctly serialized;
5. **no route 500s**;

and separately, when the canonical dump is staged, that the legacy read routes return real,
non-empty Quran data rather than an empty-schema shape.

The catalog is bidirectionally locked to the live route table, so adding an endpoint without a
catalog entry — or deleting a catalog entry — fails the suite by name.

## Scope

- New namespace `QuranDashboard.Tests.Smoke` (+ `.Data`) inside the existing
  `Backend/tests/QuranDashboard.Tests/` project, under `Smoke/`.
- Testing-environment fixture with boot guards (environment, connection-string containment,
  authentication-scheme inventory, health).
- Three personas (anonymous / authenticated-unknown-sub / owner) over the **real** JwtBearer
  handler with RSA test tokens.
- A 48-entry route catalog + a bidirectional `EndpointDataSource` parity gate + a `[Theory]` pass
  over every entry.
- `Backend/scripts/create-smoke-dump` — a verified data-only `pg_dump` of the `quran_*` tables
  plus a sha256 + migration-head manifest; regeneration of
  `resources/db-dumps/quran-canonical/`.
- A data-smoke fixture (postgres:18-alpine, restore, fail-loud/skip gates) and real-data read
  assertions.
- **`TESTING_STRATEGY.md` §13 is consumed by this feature**: its content moves into §3–§5 and
  §10–§11 as an active tier, and §13 is retired. The instruction-file lines that currently assert
  "no route-parity gate exists" are inverted in the same change.
- `Backend/report/database-inventory/current-database-inventory.md` refresh (it is stale on the
  same ground this feature measures — see Phase 5).

## Non-goals

- **No production-code edits.** No `public partial class Program`, no `InternalsVisibleTo`, no
  test-only branch in `Program.cs`/`ServiceCollectionExtensions.cs`/`WebApplicationExtensions.cs`.
  If a test cannot be written without touching production code, stop and report.
- **No Kestrel sentinel.** TestServer only.
- **No browser work.** The Playwright e2e layer is untouched and stays an opt-in local gate.
- **No CI.** §8 ("there is no CI") remains true and is not weakened. §13.4 (the CI step that
  "will return") is deleted, not implemented.
- **No write flows.** Every catalogued route is a `GET`. The suite never mutates Quran data.
- No new xUnit traits/categories (§12 deferral stands — the suite uses namespace selection like
  every other family here).
- No new endpoints, no endpoint behavior changes, no response-shape changes.
- No `[Authorize(Policy = …)]` coverage: no endpoint in this tree carries one yet
  (`AuthenticationRegistration.cs:60-68` registers Owner/Admin/Editor policies but applies none).
  The persona matrix expands when the first policy-protected endpoint lands — that is a future
  change, recorded here as an obligation in Phase 5.

## Locked decisions

| # | Decision | Value | Verified basis |
|---|---|---|---|
| 1 | Entry type | `WebApplicationFactory<HealthController>` — the repo's established convention | `AccessTestFixture.cs:132` uses `WebApplicationFactory<AccessController>`; `HealthApiFactory.cs:11` uses `<HealthController>`; `ApiBehaviorTestFactory.cs:8` uses `<UniqueWordsController>`. No `Program` entry type exists or is needed. |
| 2 | Dump | Regenerated by a new `Backend/scripts/create-smoke-dump`; data-only `pg_dump` of `quran_*` + sha256/migration manifest | Current manifest names a deleted migration (§0). |
| 3 | Data-restore container | `postgres:18-alpine` | Host `pg_dump`/`pg_restore` are **18.4**; a pg16 `pg_restore` refuses a newer archive. Siblings stay on `postgres:16-alpine` (`AccessTestFixture.cs:16`) — divergence documented in the fixture and in `Backend/tests/QuranDashboard.Tests/README.md`. |
| 4 | No Kestrel sentinel | TestServer only | The Playwright e2e layer boots the real `https` Kestrel profile and gates on `GET /api/health` daily (`TESTING_STRATEGY.md` §6). A sentinel would duplicate it for no new signal. |
| 5 | Personas | Exactly 3: anonymous / authenticated-unknown-sub (provisions `Pending`) / owner (`Auth:BootstrapOwnerEmail` path). Real JwtBearer + RSA test tokens. | `TestJwtTokens.cs`, `AccessTestFixture.cs:158-176`. **Audience must be pinned via `PostConfigure<JwtBearerOptions>`, never via config** — `AuthenticationRegistration.cs:27-28` binds `Auth:Audience` eagerly at registration, before `ConfigureAppConfiguration` overrides apply. |
| 6 | Parity source | `EndpointDataSource` composed under `ASPNETCORE_ENVIRONMENT=Testing`; base `appsettings.json` only | `WebApplicationExtensions.cs:8-17` registers Swagger **only** when `IsDevelopment()`, so `Testing` yields exactly the 48 controller endpoints. No `appsettings.Testing.json` exists and none is added. |
| 7 | Test shape | `[Theory]` over a 48-entry catalog (auth kind, concrete path, expected-status contract). One failure per route. | — |
| 8 | Collection | Own `[CollectionDefinition(nameof(SmokeCollection))]`, never shared with `AccessCollection` or any pipeline collection | Personas mutate the `users` table and the shared role cache; sharing a collection would leak across families. |
| 9 | Role-cache eviction | Between persona switches, via the fixture's shared `IMemoryCache` + `IUserRoleResolver.Evict` | `CachedUserRoleResolver.cs:15` — `CacheTtl = TimeSpan.FromSeconds(30)`. Pattern copied from `AccessTestFixture.EvictRoleCache`. |
| 10 | Rate limiting | Disabled via configuration | Base `appsettings.json` already has `RateLimiting:Enabled=false`, and `RateLimitingRegistration.cs:43` short-circuits on it. Under `Testing` this is inherited, not overridden — the fixture asserts it rather than setting it. |
| 11 | Health check | **Not** stubbed — the real `AddDbContextCheck<QuranDashboardDbContext>` runs against the container | Real coverage; contrast `HealthApiFactory.cs:33-42`, which stubs it deliberately for its own status-mapping tests. |
| 12 | Filters | Dot-bounded `QuranDashboard.Tests.Smoke.` | Two legacy classes named `*SmokeTests` must stay outside it: `Quran.WordsWordTypes.WordTypesFixtureSmokeTests` and `Quran.WordsMorphologyExplorers.MorphologyExplorersFixtureSmokeTests`. Neither FQN contains `QuranDashboard.Tests.Smoke.`. Phase 3 proves this by count, not by assertion. |

---

## Verified repository facts this plan depends on

Re-verify any of these before relying on it; all were checked on 2026-07-28 against
`e2e-bootstrap` (`d0631a14`).

| Fact | Evidence |
|---|---|
| 48 registered routes, all `GET` | 18 controller files under `Backend/api/QuranDashboard.Api/Controllers/`; per-controller `[HttpGet]` counts sum to 48 (breakdown below). |
| Exactly one `[Authorize]` endpoint | `AccessController.cs:9` (class-level) → `GET /api/access/me`. No other `[Authorize]`, no fallback policy (`AuthenticationRegistration.cs:63` comment). |
| One authentication scheme | `AuthenticationRegistration.cs:31` — `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(…)`. Scheme inventory must equal `["Bearer"]`. |
| 401 carries the envelope | `AuthenticationRegistration.cs:41-48` (`OnChallenge` → `HandleResponse()`), `UnauthorizedRejectionWriter.cs:12-21` writes `ApiResponse<object>.Fail(ApiMessages.Unauthorized)` with status 401 and `application/json`. |
| Envelope shape | `Contracts/ApiResponse.cs` — `IsSuccess`, `Message`, `Data`, `Errors`. |
| Health returns 503 when unhealthy | `HealthController.cs:36-46`. A real 200 therefore proves the container connection actually works. |
| `Dashboard/info` echoes the environment name | `DashboardController.cs:17` → `AppInfoData(…, environment.EnvironmentName)`. This is the cleanest end-to-end proof that the host booted as `Testing`. |
| Auth options are validated on start | `JwtAuthenticationOptions.cs:14-36` — `Authority` must be an absolute **https** URI, `Audience` non-blank. Base `appsettings.json` placeholders satisfy both; the fixture overrides them anyway. |
| 19 EF migrations, head `20260718142612_AddAccessRoles` | `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/`. |
| Canonical row baselines | `resources/db-dumps/quran-canonical/manifest.json` already records roots 1,642 / lemmas 4,817 / stems 11,843 / morphology 77,432 / segments 128,219 — matching the accepted baseline. Its **migration id** is what is stale, not its counts. |
| Path parameter formats | verse key `^\d+:\d+$` (`GetAyahStudyHandler.cs:42`); word location `^\d+:\d+:\d+$` (`GetWordAnalysisHandler.cs:29`); word kinds `tashkeel`/`simple` (`UniqueWordKindKeys`); grouped dimension kinds `roots`/`stems`/`lemmas` (`WordTypeGroupedDimensionKind.cs`). |

### Route inventory (48)

| Controller | Base route | Endpoints |
|---|---|---:|
| `RootsController` (+ `.Details`) | `api/words/roots` | 8 |
| `LemmasController` | `api/words/lemmas` | 7 |
| `StemsController` | `api/words/stems` | 7 |
| `UniqueWordsController` | `api/words/unique` | 5 |
| `WordTypesController` (+ `.Details`) | `api/words/word-types` | 7 |
| `WordTypeGroupedDetailsController` | `api/words/word-types/table` | 4 |
| Mushaf (`Pages`, `SurahCatalog`, `StudySourceCatalog`, `AyahStudy`, `AyahMutashabihat`, `AyahSimilarities`, `WordAnalysis`) | `api/mushaf/*` | 7 |
| `HealthController` / `DashboardController` / `AccessController` | `api/health`, `api/dashboard`, `api/access` | 3 |
| **Total** | | **48** |

---

## Assertion contract

### The two-status model (this is the crux — read it before writing Phase 3)

The pipeline suite runs against a **migrated but empty** schema. Several catalogued routes are
id-scoped and their handlers correctly return `NotFound` on empty data
(`WordTypeGroupedDetailsController.cs:45-46`, `MushafPagesController.cs:29-30`,
`GetWordAnalysisOutcome.NotFound`, …). Asserting a blanket 200 would be wrong; asserting
"whatever it returned" would be worthless. So each catalog entry carries **two** expectations:

```csharp
internal sealed record SmokeRoute(
    SmokeAuthKind AuthKind,       // Anonymous | Authorized
    string Path,                  // concrete, bound path — no templates
    SmokeExpectation Pipeline,    // empty schema: proves routing/auth/binding/handler/serialization
    SmokeExpectation? Data);      // dump restored: 200 + non-empty payload, or null when not data-backed
```

- `Pipeline` is **derived from the handler's own outcome mapping**, by reading the controller's
  `switch` — not by running the test and recording whatever came back. List/collection routes
  derive `200` (empty page); id-scoped routes derive `404`; `api/health` derives `200`;
  `api/dashboard/info` derives `200`; `api/access/me` derives `401` anonymous / `200`
  authenticated.
- **If an observed status contradicts the derived one, that is a finding to report — never a
  catalog edit.** See *Risks*.
- `Data` is non-null only for the routes the canonical dump can actually populate. Anything the
  dump does not cover keeps `Data = null` and is exercised by the pipeline tier alone.

### Per auth kind

**Anonymous routes (47).**

- status equals the derived `Pipeline` expectation;
- `Content-Type` is `application/json`;
- the body deserializes into `ApiResponse<JsonElement>` and `IsSuccess` matches the status class
  (`true` for 2xx, `false` otherwise);
- `Message` is non-empty;
- **never 401, never 403, never 500**;
- in the data tier, for every entry with a non-null `Data`: status `200`, `IsSuccess == true`,
  and `Data` is present and non-empty (a non-empty array/`items` collection, or a populated
  object — asserted per route, not generically).

**`[Authorize]` route (`GET /api/access/me`).**

- anonymous → `401` **with the `ApiResponse` envelope**: `IsSuccess == false`,
  `Message == ApiMessages.Unauthorized`, `Errors` present and empty, `Content-Type`
  `application/json`. (This is the `UnauthorizedRejectionWriter` proof — a bare framework 401
  with an empty body fails the test.);
- authenticated-unknown-sub → `200`, `Data.status == "pending"`, `Data.roleName == null`;
- owner persona → `200`, `Data.status == "active"`, `Data.roleName == "Owner"`;
- a token signed with `TestJwtTokens.DifferentKey` → `401` with the same envelope;
- an expired token → `401` with the same envelope.

**Health.**

- real `200` against the Testcontainers database, with `Data.status == "healthy"` and a check
  named `database` present. No stub. A 503 here means the container/migration path is broken and
  the whole suite is invalid.

---

## Phases

Each phase is one commit, tree green at every commit. Every phase runs its own verification
before the commit; **fresh output, in-session, or it did not happen** (§2 of the strategy).

Common prerequisite for every backend verification below:

```bash
dotnet build Backend/QuranDashboard.sln
```

### Phase 1 — Testing-environment fixture + boot guards

**Files**

- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeApiFixture.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCollection.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeBootGuardTests.cs` (new)

**Behavior**

`SmokeApiFixture : IAsyncLifetime`, namespace `QuranDashboard.Tests.Smoke`:

- owns a `PostgreSqlContainer` on `postgres:16-alpine` (matches the sibling fixtures; only the
  Phase-4 data fixture needs 18);
- on `InitializeAsync`: start container, build an independent query provider, `MigrateAsync()`;
- lazily builds `WebApplicationFactory<HealthController>` behind a lock (the
  `AccessTestFixture.Factory` pattern, `AccessTestFixture.cs:76-88`) with:
  - `builder.UseEnvironment("Testing")` — **the one thing no existing fixture does**;
  - `ConfigureAppConfiguration` in-memory overrides: `ConnectionStrings:QuranDashboardDb` →
    the container, `Auth:Authority` → `https://test-issuer.example/oidc`,
    `Auth:BootstrapOwnerEmail` → the owner persona's fake email, `Cors:AllowedOrigins:0` →
    `https://localhost`;
  - `ConfigureTestServices`: re-register `QuranDashboardDbContext` against the container;
    replace `IExternalUserProfileSource` with the existing public
    `QuranDashboard.Tests.Api.Access.FakeExternalUserProfileSource` (same assembly, reused — do
    not clone it); `PostConfigure<JwtBearerOptions>` seeding issuer, signing key **and audience**
    (decision 5);
  - **no** health-check stub, **no** rate-limiting override;
- exposes `CreateClient(...)` with `BaseAddress = https://localhost` (required — the pipeline
  calls `UseHttpsRedirection()`, `WebApplicationExtensions.cs:19`), `ApiServices`,
  `EvictRoleCache(sub)`, and a `ResetAsync()` that truncates `users` and resets the fake profile
  source.

`SmokeBootGuardTests` (5 tests):

1. `GET /api/dashboard/info` → `200`, `Data.environment == "Testing"`.
2. The composed `IConfiguration`'s `ConnectionStrings:QuranDashboardDb` **contains the container
   host/port and does not contain `quran_dashboard`** — a fail-closed guard that the suite can
   never touch the developer's real local database.
3. `IAuthenticationSchemeProvider.GetAllSchemesAsync()` names exactly `["Bearer"]`.
4. `IOptions<RateLimitingOptions>.Value.Enabled == false` (inherited from base
   `appsettings.json`, asserted not set).
5. `GET /api/health` → `200`, `Data.status == "healthy"`, a check named `database` present.

**Verification**

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Expected: 5 passed, 0 failed, 0 skipped.

**Runtime budget:** ≤ 20 s (container start ~3 s + migrate ~4 s + 5 requests).

### Phase 2 — Personas, tokens, role-cache eviction

**Files**

- `Backend/tests/QuranDashboard.Tests/Smoke/SmokePersonas.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAuthPipelineTests.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeApiFixture.cs` (edit — add
  `CreateClientFor(persona)`)

**Behavior**

`SmokePersonas` defines three personas and their clients:

| Persona | Token | Expected `/api/access/me` |
|---|---|---|
| `Anonymous` | none | `401` + envelope |
| `AuthenticatedUnknown` | `TestJwtTokens.Mint("smoke-unknown")` | `200`, `status == "pending"`, `roleName == null` |
| `Owner` | `TestJwtTokens.Mint("smoke-owner")`, whose fake profile email equals the configured `Auth:BootstrapOwnerEmail` | `200`, `status == "active"`, `roleName == "Owner"` |

`CreateClientFor` sets the `Authorization: Bearer …` default header. Persona switches call
`ResetAsync()` (truncate + fake reset) **and** `EvictRoleCache(sub)` for every persona sub used so
far — the 30 s TTL otherwise leaks a role across a truncation (decision 9).

`SmokeAuthPipelineTests` (6 tests):

1. anonymous `/api/access/me` → `401`, `IsSuccess == false`, `Message == ApiMessages.Unauthorized`,
   `Errors` non-null and empty, `Content-Type: application/json` — the envelope proof;
2. unknown-sub → `200` + `pending`, and a `users` row exists with that sub;
3. owner → `200` + `active`/`Owner`;
4. token signed with `DifferentKey` → `401` + envelope;
5. expired token (`expires: DateTime.UtcNow.AddMinutes(-5)`) → `401` + envelope;
6. persona switch owner → unknown within one test observes the **unknown** role, not a cached
   `Owner` (the eviction regression guard).

**Verification**

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Expected: 11 passed (5 + 6), 0 failed, 0 skipped.

**Runtime budget:** ≤ 30 s cumulative.

### Phase 3 — Route catalog, bidirectional parity gate, the 48-route Theory

**Files**

- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRoutePipelineTests.cs` (new)

**Behavior**

`SmokeRouteCatalog` — a static `IReadOnlyList<SmokeRoute>` of 48 entries. Each entry carries the
**route template** (for parity) *and* a **concrete bound path** (for the request), because the
live `EndpointDataSource` exposes templates while the client needs bound values. Concrete values
are drawn from the verified formats above: ids `1`, word kind `tashkeel`, grouped kind `roots`,
page `1`, verse key `1:1`, word location `1:1:1`.

`SmokeCoverageParityTests` (2 tests), reading
`fixture.ApiServices.GetServices<EndpointDataSource>()` → `Endpoints.OfType<RouteEndpoint>()` →
`(HttpMethod, RoutePattern.RawText)`:

1. **live ⊇ catalog** — every catalogued template exists in the live set; failure message names
   each missing template ("catalog entry `api/words/roots/{id:int}/stems` is not a registered
   route").
2. **catalog ⊇ live** — every live route template has a catalog entry; failure message names each
   uncovered route ("registered route `api/foo` has no SmokeRouteCatalog entry — add one in the
   same change").

Both compare normalized `(method, template)` pairs, so a route whose constraint changes
(`{id:int}` → `{id}`) surfaces as a mismatch rather than silently passing.

`SmokeRoutePipelineTests` — one `[Theory]` with a `MemberData` source over the catalog, so a
failure names exactly one route. Per entry it asserts the anonymous-persona contract above
(status = derived `Pipeline`, envelope shape, `IsSuccess`/status agreement, non-empty `Message`,
never 401/403/500), and for the `Authorized` entry it runs the anonymous → `401` + authenticated
→ `200` pair.

**Verification**

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Expected: 61 passed (11 + 2 parity + 48 theory cases), 0 failed, 0 skipped.

Prove the legacy `*SmokeTests` classes are outside the filter (decision 12) — run both and check
the counts, do not eyeball the names:

```bash
# Must report 0 tests from the two legacy classes — i.e. the count above is unchanged by them:
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~SmokeTests" --list-tests

# The two legacy classes must still run under their own families:
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~.Quran.WordsWordTypes.|FullyQualifiedName~.Quran.WordsMorphologyExplorers."
```

**Deliberate failure drills** (run, observe, revert — do not commit either mutation):

1. Comment out one catalog entry → **test 1 (live ⊇ catalog)** still passes, **the Theory** loses a
   case, and **test 2 (catalog ⊇ live)** fails naming the now-uncovered registered route. Restore.
2. Add a throwaway `[HttpGet("smoke-parity-probe")]` to `DashboardController` → **test 2** fails
   naming `api/dashboard/smoke-parity-probe`. **Revert immediately** — this is the only moment
   production code is touched, and it is reverted before the commit (`git diff` over
   `Backend/api/` must be empty at commit time).

**Runtime budget:** ≤ 45 s cumulative (48 in-memory requests against an empty schema are cheap;
the container start dominates).

### Phase 4 — `create-smoke-dump`, dump regeneration, data-smoke tier

**Files**

- `Backend/scripts/create-smoke-dump` (new, executable)
- `Backend/scripts/README.md` (edit — add the command row + prerequisites)
- `resources/db-dumps/quran-canonical/quran-canonical.dump` + `manifest.json` (regenerated;
  local + gitignored, so this is an operator step, not a commit)
- `Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDumpManifest.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDumpGate.cs` (new — `SmokeDumpFactAttribute`
  / `SmokeDumpTheoryAttribute`, modelled on `CanonicalImportSourceTestGate.cs`)
- `Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDataFixture.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDataCollection.cs` (new)
- `Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDataReadTests.cs` (new)

**`create-smoke-dump` behavior** (bash, `set -euo pipefail`, `_preflight-sandbox.sh` sourced, same
shape as `reset-db`/`update-db`):

1. Resolve the connection string from `ConnectionStrings__QuranDashboardDb` if set, else from
   `dotnet user-secrets list --project api/QuranDashboard.Api --json` merged over
   `appsettings.Development.json`. **Refuse a non-`localhost`/`127.0.0.1` host unless
   `--allow-remote` is passed** — fail-closed against dumping a deployed database.
2. Refuse to run without `--yes`, printing what it will overwrite (the existing dump is 367 MB;
   silent replacement is not acceptable).
3. Read the applied migration head from `__EFMigrationsHistory` (`SELECT "MigrationId" … ORDER BY
   "MigrationId" DESC LIMIT 1`) and the applied count. **Assert the count equals the number of
   migration files** in `infrastructure/QuranDashboard.Infrastructure/Migrations/` — today 19 = 19.
   Mismatch → exit non-zero (the database is behind or ahead of the tree).
4. Count the five baseline tables and assert against the accepted baseline —
   `quran_roots` 1,642 / `quran_lemmas` 4,817 / `quran_stems` 11,843 / `quran_word_morphology`
   77,432 / `quran_word_morphology_segments` 128,219. **Mismatch → exit non-zero, dump nothing.**
5. `pg_dump --format=custom --data-only --table='public.quran_*'` to a temp file, then `sha256sum`,
   then atomic `mv` into place.
6. Write `manifest.json`: `name`, `createdUtc`, `migrationId`, `migrationCount`, `dumpSha256`,
   `pgDumpVersion`, and the full per-table row-count map (same shape as today's manifest — it is a
   good format, only its contents are stale).

**`SmokeDataFixture` behavior**

- `postgres:18-alpine` (decision 3), with the dump directory bind-mounted read-only at `/dump`
  (a 367 MB `CopyFileAsync` into the container is materially slower than a bind mount);
- gate order, before any container work:
  - dump file **absent** → the whole tier **skips** via `SmokeDumpFactAttribute`/`…Theory`
    (`CanonicalImportSourceTestGate` convention, `Skip` set in the attribute constructor);
  - dump file **present** → read `manifest.json`, recompute the sha256, and compare; compare
    `manifest.migrationId` against the assembly's last migration id
    (`dbContext.Database.GetMigrations().Last()`). Either mismatch → **throw, do not skip**
    (fail loud, decision 2). The exception message states which check failed and names
    `Backend/scripts/create-smoke-dump` as the fix;
- then: start container → `MigrateAsync()` (schema first; the dump is data-only) →
  `pg_restore --data-only --disable-triggers --single-transaction -j 4` executed **inside** the
  container against `/dump/quran-canonical.dump`;
- exposes its own `WebApplicationFactory<HealthController>` pointed at this container, with the
  same `Testing` environment and persona wiring as `SmokeApiFixture` (shared via a small internal
  helper, not by inheriting the other fixture's container).

`SmokeDataReadTests` — the real-data reads for the legacy routes, one test per assertion, all
`[SmokeDumpFact]`:

- `GET /api/words/roots` → `200`, `Data.totalCount == 1642`;
- `GET /api/words/lemmas` → `200`, `Data.totalCount == 4817`;
- `GET /api/words/stems` → `200`, `Data.totalCount == 11843`;
- `GET /api/mushaf/surahs` → `200`, 114 items;
- `GET /api/mushaf/pages/1` → `200`, non-empty lines;
- `GET /api/mushaf/ayahs/1:1/study` → `200`, non-empty payload;
- `GET /api/mushaf/words/1:1:1/analysis` → `200`, non-empty payload;
- `GET /api/words/unique/tashkeel` → `200`, `Data.totalCount == 21294`.

Every catalog entry that gained a non-null `Data` expectation must be covered here; the counts
come from the regenerated manifest, not from memory.

**Verification**

```bash
# Regenerate (operator step, once, on the machine with the seeded local DB):
Backend/scripts/create-smoke-dump --yes

# Full smoke tier with the dump present:
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."

# Skip path — temporarily move the dump aside, confirm the data tests report Skipped (not Failed),
# and that the pipeline tier still passes:
mv resources/db-dumps/quran-canonical resources/db-dumps/quran-canonical.off
dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
mv resources/db-dumps/quran-canonical.off resources/db-dumps/quran-canonical

# Fail-loud path — corrupt one byte of a COPY of the dump, point the fixture at it, confirm the
# manifest/sha mismatch throws with a message naming create-smoke-dump. Never corrupt the real dump.
```

Expected with dump present: 71 passed (61 + ~10 data tests), 0 failed, 0 skipped.
Expected with dump absent: 61 passed, ~10 skipped, 0 failed.

**Runtime budget:** the data tier is the expensive one — container start ~3 s, `pg_restore -j 4`
of a 367 MB archive **≈ 90–240 s** on the reference machine, reads ~5 s. Budget the tier at
**≤ 5 min**, and the whole `~QuranDashboard.Tests.Smoke.` filter at **≤ 5 m 30 s with the dump
present / ≤ 45 s without it**. Measure and record the real number in the commit message — the
number below feeds Phase 5's strategy text and must be the measured one, not this estimate.

This runtime is precisely why the namespace must be excluded from the fast tiers (Phase 5). If the
measured restore exceeds ~5 min, do **not** trim the dump silently: report it and propose either
directory-format parallel restore or a documented table subset as a follow-up decision.

### Phase 5 — Strategy activation and documentation inversion

No test code. This phase makes the tier real in the documents that agents actually read.

**`TESTING_STRATEGY.md`**

- **§3 Tier C** — replace the paragraph beginning "An API/auth/middleware/binding change currently
  has **no** real-composition route gate in this tree" with the active rule: such changes REQUIRE
  the Smoke suite, and the evidence must state whether the data tier ran or skipped.
- **§3 Tier D/E** — Tier E gains the Smoke suite with the dump staged (zero unexplained skips, as
  with the canonical families).
- **§4 matrix** — the row "API endpoint added/changed, or auth/middleware/binding/contract change"
  becomes `A + Smoke` / `C + Smoke`, pipeline tests still `No`. Delete the "No route-parity gate
  exists — state that in the evidence (§13)" cell text.
- **§5** — add the Smoke command; append `&FullyQualifiedName!~QuranDashboard.Tests.Smoke.` to
  **both** the Tier B/C no-pipeline filter occurrences (§3 Tier B and §5) **and** the Tier C block
  in §3; update the partition identity from two-way to three-way:
  `no-pipeline + all-pipeline + Smoke = full`, with the measured numbers from Phase 4
  (1,040 + 617 + N = 1,657 + N). State the measurement date.
- **§10** — pre-PR workflow adds the Smoke suite for route/contract/auth/middleware/binding
  changes; add the new implementer obligation: *anyone adding or changing an API route MUST add or
  update the matching `SmokeRouteCatalog` entry in the same change, because
  `SmokeCoverageParityTests` fails otherwise*; reviewer must block when a route changed and the
  Smoke suite did not run.
- **§11** — add a scope-example row for a route/contract change.
- **§13 — delete.** Its normative content now lives in §3–§5 and §10–§11. §13.4 (the CI step) is
  deleted outright: §8 still says there is no CI, and that stays true. Renumber nothing else; if a
  §13 remains, it is the E2E/opt-in note only if that is where it already lives — otherwise the
  section ends at §12.

**Instruction files (invert the "no gate exists" claims)**

| File | Current text | Becomes |
|---|---|---|
| `CLAUDE.md:174-182` | "There is no route-parity/smoke gate in this tree… never add a `Tests.Smoke` filter term" | the gate exists; route/contract/auth/middleware/binding changes run it; the namespace IS excluded from the fast-tier filters |
| `AGENTS.md:174-182` | same | same |
| `Backend/CLAUDE.md:30-33` | same | same |
| `Backend/AGENTS.md:30-33` | same | same |
| `.claude/skills/engineering-review/SKILL.md:393-398` | "expect the evidence to *say* no route-parity gate ran… treat any command carrying a `Tests.Smoke` filter term as stale" | block when a route changed and the Smoke suite did not run |
| `.claude/skills/test-guard/SKILL.md:132-133` | "No route-parity/smoke gate exists. Do not ask for one." | ask for it when test changes touch API routes |
| `.claude/skills/pr-context-prep/SKILL.md:104-107` | "say plainly that no route-parity gate ran" | require the Smoke evidence line, including whether the data tier ran or skipped |

Also check `.agents/skills/*` pointers for a mirrored copy of any inverted line before finishing
(`grep -rn "route-parity\|Tests.Smoke" .claude .agents` must return only the new, correct text).

**READMEs**

- `Backend/tests/QuranDashboard.Tests/README.md` — folder map gains `Smoke/` and `Smoke/Data/`;
  **replace** the "`resources/db-dumps/quran-canonical/` is an orphaned local artifact — no test or
  script reads it" paragraph (lines 47-50) with the truth: it is produced by
  `Backend/scripts/create-smoke-dump` and consumed by `Smoke/Data`; absent → skip, stale/corrupt →
  fail loud. Document the postgres **18** vs **16** image divergence and why.
- `Backend/scripts/README.md` — add the `create-smoke-dump` row, its `--yes` / `--allow-remote`
  flags, its baseline-count and migration-count guards, and its `pg_dump` ≥ 18 prerequisite.
- `Backend/api/QuranDashboard.Api/README.md` — one line: adding an endpoint requires a
  `SmokeRouteCatalog` entry in the same change.

**`Backend/report/database-inventory/current-database-inventory.md` refresh**

Folded in here because this feature measures exactly the same ground (row counts, migration head,
table set) and would otherwise leave a document that contradicts the freshly generated manifest.
Verified stale points to correct, re-measured against the live local database at refresh time:

- header date `2026-06-29` → the refresh date;
- `quran_lemmas` 4,790 → 4,817 and `quran_stems` 12,108 → 11,843 (lines 63, 73 and the per-table
  sections) — the recomputed lemma/stem values the manifest already carries;
- missing table `quran_lemma_analyses` (migration `20260704102858_AddQuranLemmaAnalyses`, 4,832
  rows) added to §2 and the per-table detail;
- missing access tables `users` and `roles` (migrations `20260718115014_AddAccessUsers`,
  `20260718142612_AddAccessRoles`) added;
- "Tables | 32" and "EF migrations applied | 15" corrected to the measured values (19 migrations);
- the growth paragraph extended past Feature 018.

Do **not** widen this into a full catalog regeneration — correct the stale facts, re-measure what
you touch, and say in the header what was re-measured and what was carried forward.

**Verification**

```bash
# Every doc claim about filters must be executable. Re-run the three catalog commands verbatim
# from the edited §5 and check the counts match the numbers written into §5:
dotnet test … --filter "<edited no-pipeline filter>"      # expect 1,040
dotnet test … --filter "<all-pipeline filter>"            # expect 617
dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."   # expect N
dotnet test …                                              # expect 1,657 + N

# No stale claim survives anywhere:
grep -rn "no route-parity\|route-parity gate\|planned only\|§13" --include="*.md" . | grep -v node_modules
```

**Runtime budget:** documentation only, but the verification above is a full-suite run —
≈ 5 min + the smoke tier.

---

## Risks and stop conditions

| Risk | Stop condition / response |
|---|---|
| **A real 500 on an existing endpoint** | **This is a product bug. Stop, report it, do not adapt the test, do not soften the expectation to `500`.** The suite exists to find exactly this. Report it separately from the feature's own results (§9). |
| **Observed status ≠ derived status** on a catalogued route | Do not edit the catalog to match. Re-read the handler's outcome mapping; if the code genuinely returns something the mapping does not explain, that is a finding. Only after confirming the handler's intent may the derived expectation change — and the commit message must say why. |
| **Dump regeneration count mismatch** (roots ≠ 1,642 / lemmas ≠ 4,817 / stems ≠ 11,843 / morphology ≠ 77,432 / segments ≠ 128,219) | `create-smoke-dump` exits non-zero and writes nothing. Stop and report — the local database is not the canonical state, and dumping it would poison the tier with wrong baselines. |
| **Migration count ≠ file count** (today 19 = 19) | Same: script refuses. The database is behind or ahead of the tree. |
| Phase 1 `Testing` boot fails in a way no existing fixture hits | Most likely causes, in order: `Auth` options validation (`ValidateOnStart`), the `Cors:AllowedOrigins` guard (`ServiceCollectionExtensions.cs:64-67` throws on an empty list), `UseHttpsRedirection` with an `http` BaseAddress. All three are fixture-side; **none justifies a production-code edit.** If one appears to, stop and report rather than adding a test-only branch to `Program`. |
| `pg_restore` version mismatch | The dump is produced by pg_dump 18.4; the restore container must be `postgres:18-alpine`. A 16-alpine restore fails with "unsupported version in file header". Do not "fix" it by downgrading the producer without re-deciding decision 3. |
| Restore time blows the budget | Report the measured number; propose directory-format `-j` restore or a documented table subset as a **separate** decision. Do not silently shrink the dump — the baseline counts are the tier's value. |
| Two legacy `*SmokeTests` classes get swept into the filter | Phase 3 proves otherwise by count. If a future class lands at `QuranDashboard.Tests.Smoke.*` by accident, the three-way partition identity in §5 breaks and the check catches it. |
| The e2e layer is not on `dev` when Phase 5 runs | §0. Merge first; otherwise the §13/§3-Tier-E edits conflict or lose the E2E rationale for decision 4. |

---

## Acceptance criteria

- [ ] `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` passes with the
      dump present: **0 failed, 0 skipped**, N tests, measured runtime recorded.
- [ ] The same filter with the dump absent: **0 failed**, the data tests **skipped** (not failed),
      pipeline tests all pass.
- [ ] A corrupted/stale dump **fails loud** with a message naming `Backend/scripts/create-smoke-dump`
      — never skips.
- [ ] **Deleting any single catalog entry** makes `SmokeCoverageParityTests` fail with a message
      naming that route template. Demonstrated, then reverted.
- [ ] **Temporarily adding an unregistered-in-catalog route** makes the other parity direction fail
      naming that route. Demonstrated, then reverted — and no production-code change is committed.
- [ ] All 48 routes are catalogued; the catalog count and the live `EndpointDataSource` route count
      are both 48 at commit time.
- [ ] `GET /api/access/me` anonymous returns `401` **with** the `ApiResponse` envelope
      (`IsSuccess == false`, `Message == ApiMessages.Unauthorized`) — asserted, not assumed.
- [ ] `GET /api/health` returns a real `200` from the container-backed health check (not stubbed).
- [ ] Backend no-pipeline count **unchanged at 1,040**; all-pipeline count **unchanged at 617**;
      full suite **= 1,657 + N**; the three-way partition identity holds and is written into §5
      with the measurement date.
- [ ] Frontend Vitest counts **unchanged at 1,938 across 169 spec files** (this feature touches no
      frontend code — assert by not running it unless something frontend-adjacent changed, and say
      so in the evidence).
- [ ] **Zero occurrences of `QuranDashboard.Tests.Smoke` in the fast tiers' results**: running the
      edited no-pipeline filter with `--list-tests` returns no test whose FQN contains
      `QuranDashboard.Tests.Smoke.`.
- [ ] The two legacy `*SmokeTests` classes still run under their own families and are absent from
      the smoke filter.
- [ ] `grep -rn "no route-parity\|planned only\|§13"` over the repo's `*.md` returns nothing stale;
      every inverted line in the seven instruction/skill files is updated.
- [ ] `TESTING_STRATEGY.md` §13 is gone and its rules live in §3/§4/§5/§10/§11.
- [ ] `Backend/report/database-inventory/current-database-inventory.md` no longer contradicts the
      regenerated manifest (lemmas 4,817, stems 11,843, `quran_lemma_analyses` present, access
      tables present, 19 migrations).
- [ ] No production code changed. `git diff --stat dev -- Backend/api Backend/application
      Backend/domain Backend/infrastructure Backend/shared` is empty.

---

## Branch decision

**One feature branch, `smoke-harness`, cut from `dev` after `e2e-bootstrap` is merged into `dev`.
Five commits, one per phase. PR into `dev`.**

Reasons, in order of weight:

1. **`CLAUDE.md` requires it.** "ALL new work branches off `dev`… Feature branches open pull
   requests into `dev`." The e2e work followed exactly this (`e2e-bootstrap`). Direct-to-`dev`
   would be the deviation, not the convention.
2. **Phase 1 is genuinely experimental.** No fixture in this tree has ever booted the API host
   under `ASPNETCORE_ENVIRONMENT=Testing`; all three existing factories inherit
   `WebApplicationFactory`'s default. The dual-server e2e bring-up is the precedent for how much
   an unproven host-boot path can churn. That churn belongs on a branch.
3. **Phase 5 is high-blast-radius documentation.** It rewrites the file that governs every other
   agent's test selection, across seven instruction/skill files. A reviewable diff matters more
   here than in ordinary feature work.
4. Splitting Phase 1 onto a "light branch" and the rest onto `dev` would give the worst of both:
   two integration points, and the risky phase merged before the phases that prove it was right.

The one thing that must happen **before** the branch is cut: merge `e2e-bootstrap` → `dev`
(§0). That is a separate, already-complete piece of work and is not part of this plan's commits.
