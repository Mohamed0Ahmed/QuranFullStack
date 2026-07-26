# Mini Real-Run Smoke Harness — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan phase-by-phase. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Boot the real ASP.NET Core API (full MVC pipeline: routing, model binding, auth policies, middleware, serialization) against a test PostgreSQL with real-JwtBearer test tokens, smoke every registered route, and gate CI so no future endpoint ships without a smoke entry.

**Architecture:** One xunit collection hosts the real `Program` composition twice (in-memory TestServer + Kestrel-on-port) over a Testcontainers PostgreSQL in a dedicated `Testing` environment. A route catalog + reflection parity test (reusing `ApiContractSources`) forces catalog completeness. Quran data arrives only via a verified canonical `pg_dump` restore, skip-gated exactly like canonical-source tests.

**Tech Stack:** net10.0, xunit 2.9.3, `Microsoft.AspNetCore.Mvc.Testing` 10.0.8 (`WebApplicationFactory<Program>.UseKestrel`), Testcontainers.PostgreSql 4.4.0 (`postgres:16-alpine`), FluentAssertions (repo standard), bash + `pg_dump --format=custom`.

**Motivating defect (what this harness must catch):** `[property: Required]` on positional-record contracts made every template POST return 500 at model binding while 2000+ unit tests stayed green (fixed in `1bb340f6`; regression test `Backend/tests/QuranDashboard.Tests/Api/ApiBehavior/AbwabTemplateRequestBindingTests.cs`). Phase 3's binding-rejection pass generalizes that regression class to every route.

---

## Locked decisions (do not re-litigate during implementation)

| # | Decision |
|---|---|
| L1 | Auth = behavioral parity. Keep the REAL JwtBearer handler; RSA test tokens via the `AccessTestFixture` `PostConfigure<JwtBearerOptions>` pattern + `TestJwtTokens.Mint`. No replacement auth scheme. No literal Logto token shape (`at+jwt`/`iat`/`jti`/`client_id`/`scope` not needed — production reads only raw `sub`). |
| L2 | Personas get DB rows via `SecurityTestHarness` real handlers; roles/permissions NEVER injected as claims. `IExternalUserProfileSource` faked everywhere — zero real Logto calls. |
| L3 | Selection = namespace `QuranDashboard.Tests.Smoke`. No `[Trait]`s. EXCLUDED from Tier B/C envelope (filters amended in Phase 5); own pre-PR command + own CI step. |
| L4 | Parity split: **pipeline-smoke** (CI-enforced, ALL routes through routing/auth/binding/serialization, no Quran data) + **data-smoke** (legacy Quran reads with real data, staged machines only, skip-gated via the `CanonicalImportSourceTestGate` convention). |
| L5 | Dump = `resources/db-dumps/quran-canonical/` + sha256 manifest; documented as a *derived cache of the verified canonical import*; restore requires migrated schema incl. `__EFMigrationsHistory` (dump is data-only); never synthetic Quran data; measure size/time during Phase 4. |
| L6 | Credential rotation for the committed Railway password is a SEPARATE immediate action. This feature must NOT touch `appsettings.Production.json`. |
| L7 | Dedicated `ASPNETCORE_ENVIRONMENT=Testing` (never Production/Development defaults). Only production-code edit in the whole feature = `public partial class Program;` in `Program.cs`. |
| L8 | Smoke fixtures get their OWN xunit collections — never `AbwabDbCollection` (its 76 classes full-reset per test and would wipe smoke state). |
| L9 | Dual host (TestServer + Kestrel-on-port, http-only binding) from day one. Verified available: `WebApplicationFactory<T>.UseKestrel(int port)` ships in Mvc.Testing 10.0.0; call before `CreateClient()`. CRITICAL: call it on a factory SUBCLASS instance that overrides `ConfigureWebHost` — calling it on a `WithWebHostBuilder(...)`-derived wrapper silently drops the port override (the parent factory's `CreateHost` reads its own null `_kestrelPort`) and Kestrel binds default localhost:5000/5001 instead. |
| L10 | Test config raises rate limits: `RateLimiting:Enabled=false` (disables the global partition) AND `RateLimiting:PermissionAdminPermitLimit=100000` (the named `PermissionAdmin` policy ignores `Enabled` — `RateLimiting/RateLimitingRegistration.cs:46-64`). All `ValidateOnStart` chains satisfied: absolute-https `Auth:Authority`, non-blank `Auth:Audience`, non-empty `Cors:AllowedOrigins`, rate quotas > 0, present connection string. |

## Objective + final behavior

After this feature:

- A developer runs `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"` and gets, in under ~2 minutes: every registered API route exercised through the real pipeline (401/403 envelope checks, valid-body acceptance, invalid-body 400-not-500), host-integrity guards, and — on a machine with a staged canonical dump — real-data reads for the legacy Quran surfaces.
- CI runs the same pipeline-smoke suite as a dedicated step on every push/PR to `main`/`dev`. Adding a controller action without a smoke catalog entry fails CI with a message naming the missing route.
- The production DI container is guarded: a test asserts the authentication scheme inventory is exactly `["Bearer"]` with the JwtBearer handler, and that the smoke hosts run in `Testing` environment against the test container (never the Production/Development connection strings).

## Scope + explicit non-goals

In scope: everything under "File map" below — one production line (`Program.cs` partial class), the `Smoke/` test namespace, the dump script, `TESTING_STRATEGY.md` §3/§4/§5/§9/§11, one `ci.yml` job amendment, listed READMEs.

Non-goals (do not do these):

- NO browser/Playwright work — deferred to feature 034.
- NO `TESTING_STRATEGY.md` changes beyond §3/§4/§5/§9/§11.
- NO importer, migration, or schema changes; no changes to the DataImporter chain.
- NO edits to `appsettings.Production.json` (L6) or any credential handling.
- NO `[Trait]` attributes, no xunit.runner.json (L3).
- NO replacement/test auth scheme (L1). The existing `SubjectAuthenticationHandler` inside `AbwabTemplateRequestBindingTests.cs` stays untouched (it is a pipeline-only regression test with no DB; migrating it is out of scope).
- NO changes to existing fixtures (`AccessTestFixture`, `SecurityApiFixture`, `PostgresFixture`) except the additive connection-string overloads on `SecurityTestHarness` and `AbwabTreeSeeding.InsertAsync` in Phase 2 (existing signatures stay byte-compatible; the fixture-based methods delegate to the new overloads).

## File map (who owns what)

| File | Phase | Responsibility |
|---|---|---|
| `Backend/api/QuranDashboard.Api/Program.cs` | 1 (modify) | append `public partial class Program;` — entry-point marker for `WebApplicationFactory<Program>` |
| `Backend/tests/QuranDashboard.Tests/Smoke/_Fixtures/SmokeHostConfigurator.cs` | 1 | single source of truth for Testing-env host config (config dict, DbContext swap, JwtBearer PostConfigure, profile-source fake) — shared by both smoke fixtures |
| `Backend/tests/QuranDashboard.Tests/Smoke/_Fixtures/SmokeApiFixture.cs` | 1 | Testcontainers PG + migrate + dual factories (in-memory + Kestrel) + clients + reset/seed entry points |
| `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCollection.cs` | 1 | `ICollectionFixture<SmokeApiFixture>` collection definition |
| `Backend/tests/QuranDashboard.Tests/Smoke/Guards/SmokeHostGuardTests.cs` | 1 | environment/scheme/connection-string/dual-host guards (D6-class) |
| `Backend/tests/QuranDashboard.Tests/Abwab/_Support/SecurityTestHarness.cs` | 2 (modify, additive) | connection-string overloads so smoke can reuse the real bootstrap/grant handlers |
| `Backend/tests/QuranDashboard.Tests/Abwab/_Support/AbwabTreeSeeding.cs` | 2 (modify, additive) | connection-string overload of `InsertAsync` so smoke can insert Abwab entities through the guarded production context |
| `Backend/tests/QuranDashboard.Tests/Smoke/_Support/SmokePersonas.cs` | 2 | persona constants (subs + emails) |
| `Backend/tests/QuranDashboard.Tests/Smoke/_Support/SmokeSeed.cs` | 2 | reset + persona rows + minimal Abwab data, one entry point |
| `Backend/tests/QuranDashboard.Tests/Smoke/_Support/SmokeTokens.cs` | 2 | persona → `TestJwtTokens.Mint` bearer header helper |
| `Backend/tests/QuranDashboard.Tests/Smoke/_Support/ApiEnvelopeAssertions.cs` | 2 | shared `ApiResponse` envelope assertions (`isSuccess` false, non-empty `message`, JSON content type) |
| `Backend/tests/QuranDashboard.Tests/Smoke/Personas/SmokePersonaTests.cs` | 2 | persona behavior through the full pipeline |
| `Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/SmokeEndpointInventory.cs` | 3 | live route enumeration (reuses `Abwab/Ci/ApiContractSources`) + key normalization |
| `Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/SmokeRouteCatalog.cs` | 3 | the 92-route catalog: auth kind, concrete path builder, valid/invalid bodies |
| `Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/SmokeCoverageParityTests.cs` | 3 | catalog ⊇ live routes AND live routes ⊇ catalog (the CI gate for future endpoints) |
| `Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/PipelineSmokeTests.cs` | 3 | the assertion-contract passes over every catalog entry + Kestrel sentinel subset |
| `Backend/scripts/create-smoke-dump` | 4 | bash: verified data-only pg_dump of `quran_*` tables + manifest.json |
| `Backend/tests/QuranDashboard.Tests/Smoke/_Support/QuranDumpGate.cs` | 4 | skip-gate attributes when `resources/db-dumps/quran-canonical/` absent |
| `Backend/tests/QuranDashboard.Tests/Smoke/_Fixtures/QuranDataSmokeFixture.cs` | 4 | own container: migrate → verify manifest → pg_restore → host |
| `Backend/tests/QuranDashboard.Tests/Smoke/Data/QuranDataSmokeTests.cs` | 4 | real-data reads across all 13 legacy controllers |
| `TESTING_STRATEGY.md` | 5 (modify) | §3 smoke commands, §4 matrix row, §5 catalog + filter amendments, §9 responsibility, §11 clarification |
| `.github/workflows/ci.yml` | 5 (modify) | exclude Smoke from main test step, add dedicated smoke step |
| `Backend/tests/QuranDashboard.Tests/README.md` | 5 (modify) | Smoke cluster description |
| `Backend/api/QuranDashboard.Api/Authentication/README.md` | 5 (modify) | fix stale "policies not applied to any endpoint" claim (lines 50–52); document the smoke-auth contract |
| `Backend/tests/QuranDashboard.Tests/Smoke/README.md` | 5 | Smoke area truth: coverage rule, personas, dump provenance, collection isolation, Testing-env invariant |

Branching: feature branch off `dev` (next free feature number per repo sequence). One commit per phase. Push/PR only on explicit user request.

---

## Phase 1 — Testing-environment boot, `partial Program`, dual-host fixture, guard tests

Depends on: nothing.

**Files:**
- Modify: `Backend/api/QuranDashboard.Api/Program.cs` (append 1 line after `app.Run();`)
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/_Fixtures/SmokeHostConfigurator.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/_Fixtures/SmokeApiFixture.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCollection.cs`
- Test: `Backend/tests/QuranDashboard.Tests/Smoke/Guards/SmokeHostGuardTests.cs`

**Intended behavior:** `WebApplicationFactory<Program>` boots the REAL composition (`AddApplication` + `AddInfrastructure` + `AddAbwabStabilization` + `AddApiServices` + `UseApiPipeline`) in environment `Testing` — so only base `appsettings.json` loads (no `appsettings.Testing.json` exists; no user secrets; no Development/Production files). All required config is injected in-memory. The same configurator produces an in-memory TestServer host and a Kestrel host on a dynamic loopback http port.

- [ ] **Step 1: Write the failing guard tests**

`Backend/tests/QuranDashboard.Tests/Smoke/Guards/SmokeHostGuardTests.cs`:

```csharp
namespace QuranDashboard.Tests.Smoke.Guards;

[Collection(nameof(SmokeCollection))]
public sealed class SmokeHostGuardTests(SmokeApiFixture fixture)
{
    [Fact]
    public void Host_runs_in_Testing_environment()
        => fixture.InMemoryServices.GetRequiredService<IHostEnvironment>()
            .EnvironmentName.Should().Be("Testing");

    [Fact]
    public void Active_connection_string_is_the_test_container()
    {
        var config = fixture.InMemoryServices.GetRequiredService<IConfiguration>();
        var active = config.GetConnectionString("QuranDashboardDb");
        active.Should().Be(fixture.ConnectionString);
        active.Should().NotContain("rlwy.net").And.NotContain("railway");
    }

    [Fact]
    public async Task Authentication_scheme_inventory_is_exactly_Bearer()
    {
        var schemes = await fixture.InMemoryServices
            .GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();
        var scheme = schemes.Should().ContainSingle().Subject;
        scheme.Name.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        scheme.HandlerType.Should().Be(typeof(JwtBearerHandler));
    }

    [Fact]
    public async Task Health_returns_200_through_in_memory_host()
        => (await fixture.InMemoryClient.GetAsync("api/health"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public async Task Health_returns_200_through_kestrel_host()
        => (await fixture.KestrelClient.GetAsync("api/health"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public void Kestrel_host_listens_on_real_loopback_http_port()
    {
        fixture.KestrelClient.BaseAddress!.Scheme.Should().Be("http");
        fixture.KestrelClient.BaseAddress.Port.Should().NotBe(0).And.NotBe(80);
    }
}
```

D6 rationale: the smoke fixture never touches authentication registration (it only `PostConfigure`s JwtBearer *options*), so the scheme inventory this test sees is exactly what the production `AddApiAuthentication` registers (`Authentication/AuthenticationRegistration.cs:28`). A future accidental `AddAuthentication(...).AddScheme<...>` in production code — or a test scheme leaking into the shared composition — fails this test.

- [ ] **Step 2: Run tests, verify they fail to compile** (fixture doesn't exist yet)

Run: `dotnet build Backend/QuranDashboard.sln`
Expected: build errors referencing `SmokeApiFixture`, `SmokeCollection`.

- [ ] **Step 3: Add the entry-point marker to Program.cs**

Append to `Backend/api/QuranDashboard.Api/Program.cs` (after `app.Run();`):

```csharp
public partial class Program;
```

This is the standard marker making the top-level-statements `Program` class public for `WebApplicationFactory<Program>` (Microsoft-documented pattern). It changes no runtime behavior. This is the ONLY production-code edit in the feature (L7).

- [ ] **Step 4: Implement the shared configurator**

`Backend/tests/QuranDashboard.Tests/Smoke/_Fixtures/SmokeHostConfigurator.cs`:

```csharp
namespace QuranDashboard.Tests.Smoke._Fixtures;

internal static class SmokeHostConfigurator
{
    internal static void Configure(IWebHostBuilder builder, string connectionString)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = connectionString,
                ["Auth:Authority"] = TestJwtTokens.TestIssuer,
                ["Auth:Audience"] = TestJwtTokens.TestAudience,
                ["Auth:BootstrapOwnerEmail"] = SmokePersonas.OwnerEmail,
                ["Cors:AllowedOrigins:0"] = "https://localhost",
                ["RateLimiting:Enabled"] = "false",
                ["RateLimiting:PermissionAdminPermitLimit"] = "100000",
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<QuranDashboardDbContext>();
            services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
            services.AddDbContext<QuranDashboardDbContext>(o => o.UseNpgsql(connectionString));

            services.RemoveAll<IExternalUserProfileSource>();
            services.AddSingleton<IExternalUserProfileSource, FakeExternalUserProfileSource>();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Configuration = new OpenIdConnectConfiguration { Issuer = TestJwtTokens.TestIssuer };
                options.Configuration.SigningKeys.Add(TestJwtTokens.SigningKey);
                options.TokenValidationParameters.ValidIssuer = TestJwtTokens.TestIssuer;
                options.TokenValidationParameters.IssuerSigningKey = TestJwtTokens.SigningKey;
                options.TokenValidationParameters.ValidAudience = TestJwtTokens.TestAudience;
            });
        });
    }
}
```

Notes for the implementer:
- `TestJwtTokens` and `FakeExternalUserProfileSource` already exist at `Backend/tests/QuranDashboard.Tests/Api/Access/` — reuse, do not copy. If `TestJwtTokens.TestIssuer` is not an absolute https URI usable as `Auth:Authority`, use the literal `"https://test-issuer.example/oidc"` (its current value) for the Authority key.
- `SmokePersonas.OwnerEmail` arrives in Phase 2; for Phase 1 compilation use the literal `"smoke-owner@example.test"` and swap to the constant in Phase 2.
- The JwtBearer block is copied from the proven pattern at `AccessTestFixture.cs:149-156` / `SecurityApiFixture.cs:147-154` (L1).

- [ ] **Step 5: Implement the dual-host fixture**

`Backend/tests/QuranDashboard.Tests/Smoke/_Fixtures/SmokeApiFixture.cs`:

```csharp
namespace QuranDashboard.Tests.Smoke._Fixtures;

public sealed class SmokeApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    private SmokeWebApplicationFactory? _inMemoryFactory;
    private SmokeWebApplicationFactory? _kestrelFactory;

    public string ConnectionString { get; private set; } = null!;
    public HttpClient InMemoryClient { get; private set; } = null!;
    public HttpClient KestrelClient { get; private set; } = null!;
    public IServiceProvider InMemoryServices => _inMemoryFactory!.Services;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        await MigrateAsync();

        _inMemoryFactory = new SmokeWebApplicationFactory(ConnectionString);
        InMemoryClient = _inMemoryFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        _kestrelFactory = new SmokeWebApplicationFactory(ConnectionString);
        _kestrelFactory.UseKestrel(0);
        KestrelClient = _kestrelFactory.CreateClient();
    }

    private sealed class SmokeWebApplicationFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => SmokeHostConfigurator.Configure(builder, connectionString);
    }

    private async Task MigrateAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<QuranDashboardDbContext>(o => o.UseNpgsql(ConnectionString));
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>()
            .Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_kestrelFactory is not null) await _kestrelFactory.DisposeAsync();
        if (_inMemoryFactory is not null) await _inMemoryFactory.DisposeAsync();
        await _container.DisposeAsync();
    }
}
```

`Backend/tests/QuranDashboard.Tests/Smoke/SmokeCollection.cs`:

```csharp
namespace QuranDashboard.Tests.Smoke;

[CollectionDefinition(nameof(SmokeCollection))]
public sealed class SmokeCollection : ICollectionFixture<SmokeApiFixture>;
```

In-memory client uses `BaseAddress = https://localhost` (repo precedent — dodges `UseHttpsRedirection`). The Kestrel client is http-only (L9): with no https address configured, `UseHttpsRedirection` no-ops (it logs a warning; that is expected and harmless).

Why the nested subclass instead of `WithWebHostBuilder`: `UseKestrel(0)` stores the port on the instance it is called on, but a `WithWebHostBuilder`-derived factory delegates `CreateHost` to its PARENT, which reads its own (null) port field — the override is silently dropped and Kestrel binds default addresses (L9). The subclass has no parent/derived split, so the port-0 request is honored; it also avoids leaking undisposed parent factories.

- [ ] **Step 6: Run the guard tests**

Run: `dotnet build Backend/QuranDashboard.sln && dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"`
Expected: 6 passed, 0 failed, 0 skipped. If `Testing`-env boot fails a `ValidateOnStart` chain, the failure message names the offending option — fix the config dict, do not weaken the validator.

- [ ] **Step 7: Verify no regression in the fast suite (interim state check)**

Run (Tier B no-pipeline command from `TESTING_STRATEGY.md` §3): the ~45 s no-pipeline filter. Until Phase 5 amends the filters, the new Smoke namespace IS swept into this run — that is a known interim state; it must still pass. Expected: all green, total count = previous + 6.

- [ ] **Step 8: Commit**

```bash
git add Backend/api/QuranDashboard.Api/Program.cs Backend/tests/QuranDashboard.Tests/Smoke/
git commit -m "feat(smoke): phase 1 — Testing-env dual-host fixture, partial Program, boot guards"
```

---

## Phase 2 — shared seed module (personas via real handlers) + token helper

Depends on: Phase 1.

**Files:**
- Modify: `Backend/tests/QuranDashboard.Tests/Abwab/_Support/SecurityTestHarness.cs` (additive overloads only)
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/_Support/SmokePersonas.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/_Support/SmokeSeed.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/_Support/SmokeTokens.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/_Support/ApiEnvelopeAssertions.cs`
- Test: `Backend/tests/QuranDashboard.Tests/Smoke/Personas/SmokePersonaTests.cs`

**Intended behavior:** one call — `SmokeSeed.EnsureSeededAsync(fixture)` — resets smoke state and provisions three personas plus minimal Abwab data, all through REAL handlers (audited writes), idempotently (safe to call from multiple test classes; seeds once per fixture instance). Personas (L2 — DB rows, never claims):

| Persona | Sub | DB rows | Expected authz behavior |
|---|---|---|---|
| `Owner` | `smoke-owner` | active `SystemOwnerMembership` (via real `BootstrapOwnerAsync`) + `users` row (LogtoSub=smoke-owner, role **Owner**, Active) | passes `SystemOwner` policy AND the three SystemOwner-only code policies (`permission.administer`, `audit.restore`, `safetyPoint.manage`) via `SystemOwnerOnlyCodes` resolution |
| `Granted` | `smoke-granted` | `users` row (Active, no role) + one `permission_assignments` subject-grant per NON-SystemOwner-only code — loop `PermissionCatalogue.All.Where(e => !e.SystemOwnerOnly)` (via real `GrantAsync`). Granting a SystemOwner-only code THROWS `PermissionBaselineLockedException` (`PermissionAdministrationHandler.cs:23-27`) — never include those three | passes every permission policy used by a catalogued route (all catalogued routes use non-SystemOwner-only codes) |
| `NoPermissions` | `smoke-nopermissions` | `users` row (Active, no role), zero grants | authenticates fine; 403 on every permission/SystemOwner policy |

Minimal Abwab data (for concrete route paths in Phase 3): one NEW section + one root category in it, one child category under the root, one alias on the root, one mutual relationship between root and child, one door template with one node and one node alias. The migration-seeded default section (`…0001`) is left untouched; `SmokeSeedContext.SectionId` is the NEW section's id (so PUT/DELETE section cases never target the permanent default).

Build entities with the existing PURE builders and insert them through the new `AbwabTreeSeeding.InsertAsync(connectionString, …)` overload (which routes through the production context + `AbwabWriteGuardInterceptor` + a fixture ChangeSet):
- `AbwabTreeSeeding.NewSection` / `NewRootCategory` / `NewChildCategory` / `NewAlias` (`Abwab/_Support/AbwabTreeSeeding.cs:23-81`)
- `AbwabRelationshipTemplateSeeding.NewMutualRelationship` / `NewDoorTemplate` / `NewTemplateNode` / `NewTemplateNodeAlias` (`Abwab/_Support/AbwabRelationshipTemplateSeeding.cs:13-30,95-127`)

Do NOT use the composite seeders (`TwoCategoryEndpointsAsync` etc.) — they are `PostgresFixture`-bound and create a different shape (two root categories, no child, no aliases). Read both files first per repo README rule.

Additionally seed three EXPENDABLE entities reserved exclusively for the destructive pipeline cases (Phase 3's order-independence rule): one extra section, one extra root category, one extra door template — nothing else references them, so DELETE/subtree-delete cases can destroy them without breaking other cases.

Captured ids exposed as `SmokeSeedContext` (record: `SectionId`, `RootCategoryId`, `ChildCategoryId`, `AliasId`, `RelationshipId`, `TemplateId`, `NodeId`, `NodeAliasId`, `ExpendableSectionId`, `ExpendableCategoryId`, `ExpendableTemplateId`).

- [ ] **Step 1: Write the failing persona tests**

`Backend/tests/QuranDashboard.Tests/Smoke/Personas/SmokePersonaTests.cs`:

```csharp
namespace QuranDashboard.Tests.Smoke.Personas;

[Collection(nameof(SmokeCollection))]
public sealed class SmokePersonaTests(SmokeApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => SmokeSeed.EnsureSeededAsync(fixture);
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Missing_token_returns_401_ApiResponse_envelope()
    {
        var response = await fixture.InMemoryClient.GetAsync("api/abwab/templates");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await ApiEnvelopeAssertions.AssertFailureEnvelopeAsync(response);
    }

    [Fact]
    public async Task NoPermissions_persona_gets_403_on_permission_policy()
    {
        var response = await fixture.InMemoryClient.SendAsync(
            SmokeTokens.Get("api/abwab/templates", SmokePersonas.NoPermissions));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        // no envelope assert: policy 403s have an empty body (no OnForbidden writer)
    }

    [Fact]
    public async Task Granted_persona_passes_permission_policy()
        => (await fixture.InMemoryClient.SendAsync(
                SmokeTokens.Get("api/abwab/templates", SmokePersonas.Granted)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public async Task Owner_persona_passes_SystemOwner_policy()
        => (await fixture.InMemoryClient.SendAsync(
                SmokeTokens.Get("api/security/permissions", SmokePersonas.Owner)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public async Task Access_me_provisions_a_fresh_sub_via_fake_profile_source()
        => (await fixture.InMemoryClient.SendAsync(
                SmokeTokens.Get("api/access/me", SmokePersonas.Fresh)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
}
```

The provisioning test uses the UNSEEDED `Fresh` sub deliberately: a pre-seeded Active user short-circuits `UserProvisioningService` before it ever calls `IExternalUserProfileSource` (`UserProvisioningService.cs:15-21,35-51`) — only a fresh sub proves the fake-profile-source provisioning path end to end (new Pending user, 200).

The 401 test is the L1 payoff: it proves the REAL JwtBearer `OnChallenge` → `UnauthorizedRejectionWriter` envelope path runs (a replacement scheme would bypass it).

- [ ] **Step 2: Run, verify compile failure** (`SmokeSeed`, `SmokeTokens`, `SmokePersonas`, `ApiEnvelopeAssertions` missing)

- [ ] **Step 3: Implement support types**

`SmokePersonas.cs`:

```csharp
namespace QuranDashboard.Tests.Smoke._Support;

public static class SmokePersonas
{
    public const string Owner = "smoke-owner";
    public const string Granted = "smoke-granted";
    public const string NoPermissions = "smoke-nopermissions";
    public const string Fresh = "smoke-fresh";   // never seeded — exercises real provisioning
    public const string OwnerEmail = "smoke-owner@example.test";
    public const string GrantedEmail = "smoke-granted@example.test";
    public const string NoPermissionsEmail = "smoke-nopermissions@example.test";
}
// users.Email is required + unique (UserConfiguration.cs:20-22,54) — every persona row
// needs its own distinct email. `Fresh` deliberately gets NO users row and NO email constant:
// its email comes from FakeExternalUserProfileSource ("{sub}@example.test").
```

`SmokeTokens.cs`:

```csharp
namespace QuranDashboard.Tests.Smoke._Support;

public static class SmokeTokens
{
    public static HttpRequestMessage Get(string path, string sub) => Build(HttpMethod.Get, path, sub, null);
    public static HttpRequestMessage Build(HttpMethod method, string path, string? sub, string? jsonBody)
    {
        var request = new HttpRequestMessage(method, path);
        if (sub is not null)
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(sub));
        if (jsonBody is not null)
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return request;
    }
}
```

(Adjust to `TestJwtTokens.Mint`'s actual signature — it exists at `Api/Access/TestJwtTokens.cs:18-34` and takes the subject as first parameter.)

`ApiEnvelopeAssertions.cs`: parse the response body as JSON; assert `Content-Type` is `application/json`; assert `isSuccess == false` and non-empty `message`. The envelope type is `Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs:3-11` (properties `IsSuccess`/`Message`/`Data`/`Errors`; no custom JsonOptions in the Api project ⇒ default camelCase on the wire). Scope note: only 401 (via `OnChallenge` → `UnauthorizedRejectionWriter`), 400 (invalid-model-state factory), and in-body controller failures carry the envelope — policy-based 403s have an EMPTY body (no `OnForbidden` handler exists, `AuthenticationRegistration.cs:37-45`); never assert an envelope on a bare policy 403.

- [ ] **Step 4: Add additive `SecurityTestHarness` overloads**

In `Abwab/_Support/SecurityTestHarness.cs`: the existing methods take the Abwab `PostgresFixture` plus COMMAND RECORDS (`GrantAsync(PostgresFixture, GrantPermissionCommand, IEffectivePermissionCache?)`, `BootstrapOwnerAsync(PostgresFixture, BootstrapSystemOwnerCommand)` — `SecurityTestHarness.cs:36-41,62-66`). Add parallel overloads where ONLY the `PostgresFixture` parameter becomes `string connectionString` (the command-record parameters stay); existing fixture-based methods delegate to them (extract the body; keep every existing public signature byte-compatible). Only `CreateContext`, `BootstrapOwnerAsync`, `GrantAsync`, `RevokeAsync` (and any private builders they use) need the overload.

Same pattern in `Abwab/_Support/AbwabTreeSeeding.cs`: add `InsertAsync(string connectionString, params object[] entities)` and delegate the existing `InsertAsync(PostgresFixture, …)` (`AbwabTreeSeeding.cs:83`) to it.

- [ ] **Step 5: Implement `SmokeSeed`**

```csharp
namespace QuranDashboard.Tests.Smoke._Support;

public static class SmokeSeed
{
    public static SmokeSeedContext Context { get; private set; } = null!;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string? _seededFor;

    public static async Task EnsureSeededAsync(SmokeApiFixture fixture)
    {
        await Gate.WaitAsync();
        try
        {
            if (_seededFor == fixture.ConnectionString) return;
            await ResetAsync(fixture.ConnectionString);
            Context = await SeedAsync(fixture.ConnectionString);
            _seededFor = fixture.ConnectionString;
        }
        finally { Gate.Release(); }
    }
    // ResetAsync: AbwabSubstrateReset.FullResetAsync(connectionString) — the string overload
    //   ALREADY EXISTS (AbwabSubstrateReset.cs:7-9) and already clears permission_assignments +
    //   system_owner_memberships (:27-28). The ONLY additional statement smoke needs:
    //   TRUNCATE users RESTART IDENTITY CASCADE (precedent: AccessTestFixture.cs:80).
    // SeedAsync — the harness methods take COMMAND RECORDS, not bare strings; construct them:
    //   1. SecurityTestHarness.BootstrapOwnerAsync(connectionString, new BootstrapSystemOwnerCommand(
    //        Issuer: TestJwtTokens.TestIssuer, Subject: SmokePersonas.Owner,
    //        ExpectedIssuer: TestJwtTokens.TestIssuer, EmailVerified: true, AccountEnabled: true,
    //        ExpectedTimelineGeneration: 0, ActorSubject: SmokePersonas.Owner))
    //      (adjust member names to BootstrapSystemOwnerCommand.cs:5-12; authorization matches
    //       subject only, so the issuer value just needs internal consistency)
    //   2. users rows: Owner (role Owner, Active, OwnerEmail), Granted (Active, no role,
    //      GrantedEmail), NoPermissions (Active, no role, NoPermissionsEmail) — direct inserts
    //      mirroring AccessTestFixture.InsertUserAsync (AccessTestFixture.cs:112-119)
    //   3. foreach entry in PermissionCatalogue.All.Where(e => !e.SystemOwnerOnly):
    //        SecurityTestHarness.GrantAsync(connectionString, new GrantPermissionCommand(
    //          TargetKind: Subject, TargetKey: SmokePersonas.Granted, PermissionCode: entry.Code,
    //          ExpectedTimelineGeneration: 0, ExpectedVersion: 0, ActorSubject: SmokePersonas.Owner))
    //        (adjust to GrantPermissionCommand.cs:6-12; SystemOwner-only codes THROW — see persona table)
    //   4. Abwab minimal data: pure builders + AbwabTreeSeeding.InsertAsync(connectionString, …)
    //      per the "Minimal Abwab data" block above — capture ids into SmokeSeedContext
}
```

Seed-once-per-fixture (not per test) is deliberate: Phase 3's assertions are tolerant of accumulated writes (see Phase 3 assertion contract), and per-test reseeding of 31 grants × 92 routes would blow the runtime budget.

- [ ] **Step 6: Run the persona tests**

Run: `dotnet build Backend/QuranDashboard.sln && dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"`
Expected: 11 passed (6 guards + 5 personas), 0 failed, 0 skipped.

- [ ] **Step 7: Verify the Abwab suite still passes** (SecurityTestHarness touched)

Run: `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "(FullyQualifiedName~QuranDashboard.Tests.Abwab)|(FullyQualifiedName~QuranDashboard.Tests.Api)"`
Expected: all green (~35 s per `TESTING_STRATEGY.md` §3 Tier A).

- [ ] **Step 8: Commit**

```bash
git add Backend/tests/QuranDashboard.Tests/Smoke/ Backend/tests/QuranDashboard.Tests/Abwab/_Support/SecurityTestHarness.cs Backend/tests/QuranDashboard.Tests/Abwab/_Support/AbwabTreeSeeding.cs
git commit -m "feat(smoke): phase 2 — persona seed module via real security handlers"
```

---

## Phase 3 — pipeline-smoke: route inventory, parity gate, 92-route pass

Depends on: Phase 2.

**Files:**
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/SmokeEndpointInventory.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/SmokeRouteCatalog.cs`
- Test: `Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/SmokeCoverageParityTests.cs`
- Test: `Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/PipelineSmokeTests.cs`

**Intended behavior:** this phase is what catches the 030-class bug for every current AND future route.

### Route inventory (parity source)

`SmokeEndpointInventory` reuses the proven enumeration in `Abwab/Ci/ApiContractSources.cs:10-24` (`AddControllers().AddApplicationPart(Api assembly)` + `IApiDescriptionGroupCollectionProvider` — no DB, no host). It yields normalized keys:

- Key = `"{HTTPMETHOD} {relative-path}"`, lowercase path, route-constraint suffixes stripped (`{id:guid}` → `{id}`), no leading slash. Example: `POST api/abwab/templates/{doortemplateid}/nodes`.
- Exclusions: none. Swagger is Development-only middleware and never appears in `ApiExplorer` output — nothing to exclude.

### Catalog

`SmokeRouteCatalog` is a static dictionary from normalized key to:

```csharp
public sealed record SmokeRouteCase(
    HttpMethod Method,
    string PathTemplate,                              // normalized, matches inventory key
    Func<SmokeSeedContext, string> ConcretePath,      // real seeded ids where they exist
    SmokeAuth Auth,       // Anonymous | TreeRedacted | Authenticated | Permission(code) | SystemOwner
    Func<SmokeSeedContext, string>? ValidBody,        // JSON built at request time; null when no body
    Func<SmokeSeedContext, string>? InvalidBody);     // JSON that must yield 400; null when N/A
```

Bodies are `Func<SmokeSeedContext, string>` (not static strings) because seeded ids are `Guid.NewGuid()` values that exist only at seed time. HARD RULE: the catalog's static initializer must never touch `SmokeSeed.Context` (it is `null!` at discovery time) — lambdas run only inside `PipelineSmokeTests` after seeding. `SmokeCoverageParityTests` only enumerates `Keys`, so the D5 gate stays zero-prerequisite in CI.

Catalog completeness is enforced mechanically by the parity test — the appendix table at the end of this plan lists all 92 routes with their auth kind (taken from the route inspection; every row cites its controller). Rules for filling entries — these are exact, not discretionary:

1. `ConcretePath`: substitute path parameters from `SmokeSeedContext` where a seeded entity exists (categoryId → `RootCategoryId`, doorTemplateId → `TemplateId`, templateNodeId → `NodeId`, aliasId → `AliasId`, templateNodeSearchAliasId → `NodeAliasId`, relationshipId → `RelationshipId`, sectionId → `SectionId`). Where no seeded entity exists by design (`deletionOperationId`, numeric `{id}` on Quran routes), use a syntactically valid literal (`Guid.NewGuid()`, `1`) — the assertion contract tolerates 404. Enum/format-bound path params get VALID literals so binding succeeds and the pass is not vacuous: `{verseKey}` → `"1:1"`, `{wordLocation}` → `"1:1:1"`, `{protectionType}` → `"QuranContent"` (a `ManualProtectionType` name — `Domain/Abwab/Protection/ManualProtectionType.cs:4-11`), `{kind}` on `api/words/unique/*` → `"tashkeel"`, `{kind}` on `api/words/word-types/table/*` and `{wordKind}` → the first member name of the enum each controller binds (read the controller signature when filling).
2. `ValidBody`: the minimal JSON accepted by the route's request contract. Source of truth per area: `Backend/api/QuranDashboard.Api/Abwab/Templates/TemplateContracts.cs`, `.../Relationships/`, `.../Categories/`, `.../Sections/`, `.../Protection/` contracts, and `Security/Permissions/` request types. Read the contract record, include every member with a plausible value (seeded ids via the lambda's `SmokeSeedContext`). For the 7 template write routes, reuse the exact bodies already proven in `AbwabTemplateRequestBindingTests.cs`. SPECIAL RULE for `POST api/security/permissions/grant` and `/revoke`: target a NON-seeded subject (`"smoke-unused"`) with `expectedVersion: 0` — revoke of a nonexistent assignment is a NoOp success (`PermissionAdministrationHandler.cs:76-85`); targeting `smoke-granted` would strip the Granted persona's permissions mid-run and cause order-dependent 403s across the suite.
3. `InvalidBody`: "required member" means a NON-NULLABLE REFERENCE-TYPE member (implicit `[Required]` applies only to those; a missing value-type member default-binds and reaches the handler — no 400). For contracts with at least one such member — `ValidBody` minus the FIRST one. For body-bearing contracts with none (e.g. `DeleteDoorTemplateRequest(uint, long)`, the Protection contracts) — the malformed literal `"{"` (JSON formatter → 400). For GET — `null`.
4. Body-bearing DELETEs: the six Abwab DELETE routes bind `[FromBody]` request records (`TemplatesController.cs:81-95,178-193,225-240`, `SectionsController.cs:51-60`, `CategoriesController.cs:137-146`, `RelationshipsController.cs:66-82`) — a bodiless DELETE gets 415, which the Authorized pass forbids. Every Abwab DELETE gets a `ValidBody` (its `expectedVersion`/`expectedTimelineGeneration` record) and an `InvalidBody` per rule 3. Only GETs are body-free.
5. `Auth`: from the appendix table (which mirrors the `[Authorize]` attributes in code — verify against the controller when filling). The two tree routes use `TreeRedacted` (see assertion contract).

### Assertion contract (the 4 passes in `PipelineSmokeTests`)

All via `[Theory]` + `MemberData` over the catalog, against the in-memory host, after `SmokeSeed.EnsureSeededAsync`:

| Pass | Applies to | Request | Assertion |
|---|---|---|---|
| Unauthenticated | Auth is Authenticated / Permission / SystemOwner | no token, ValidBody if any | `401` + failure envelope (the JwtBearer `OnChallenge` writer — L1 payoff) |
| Unauthenticated-open | Auth == Anonymous | no token | status is NOT 401, NOT 403, NOT ≥500 |
| Unauthenticated-tree | Auth == TreeRedacted (the 2 tree GETs) | no token | `403` + failure envelope — anonymous tree callers get an empty permission set and an IN-BODY `ApiResponse.Fail` 403 (`AbwabTreeController.cs:26-28,42-45,50-60`); these two routes DO have a 403 body |
| Forbidden | Auth is Permission / SystemOwner / TreeRedacted | `NoPermissions` token, ValidBody | `403` status ONLY — policy-based 403s have an EMPTY body (no `OnForbidden` handler, `AuthenticationRegistration.cs:37-45`); no envelope assert except TreeRedacted (in-body writer) |
| Authorized pipeline | all | `Granted` token (`Owner` for SystemOwner routes; any persona for plain Authenticated; `Granted` gives TreeRedacted a 200), ValidBody | status NOT 401, NOT 403, NOT 415, NOT ≥500 (2xx/404/409/400-domain all acceptable — the pass proves routing+binding+authz+serialization, not business outcomes) |
| Binding rejection | InvalidBody != null | authorized token, InvalidBody | `400` + failure envelope, explicitly NOT 500 — **this is the 030-bug class** |

Tolerance rationale: seed-once + accumulated writes means a second run of `POST api/abwab/sections` may 409; that still proves the pipeline. Auth rejections and 5xx are never tolerated. Order-independence rule: no ValidBody may destroy state another case depends on — DELETE/subtree-delete/revoke cases must target expendable entities (rule 2's `smoke-unused` for security routes; for Abwab DELETEs the seeded alias/node/relationship are acceptable casualties ONLY because their other cases tolerate 404 — but never delete `RootCategoryId`, `TemplateId`, or `SectionId` themselves: point destructive category/template/section cases at ids of additional expendable entities seeded for exactly this purpose (extend `SmokeSeedContext` with `ExpendableCategoryId`, `ExpendableTemplateId`, `ExpendableSectionId`).

Kestrel sentinel (L9): the same assertion-contract passes for exactly these routes through `KestrelClient`: `GET api/health`, `GET api/mushaf/surahs`, `GET api/abwab/templates` (401 + granted-200), `POST api/abwab/templates` (valid + invalid body). Full 92×Kestrel duplication is deliberately NOT run — the middleware pipeline is identical; only transport differs; budget stays intact.

- [ ] **Step 1: Write `SmokeEndpointInventory` + the parity test first**

`SmokeCoverageParityTests.cs`:

```csharp
namespace QuranDashboard.Tests.Smoke.Pipeline;

public sealed class SmokeCoverageParityTests
{
    [Fact]
    public void Every_registered_route_has_a_smoke_catalog_entry()
    {
        var live = SmokeEndpointInventory.ReadNormalizedKeys();
        var missing = live.Except(SmokeRouteCatalog.Keys).OrderBy(k => k).ToList();
        missing.Should().BeEmpty(
            "every API route needs a SmokeRouteCatalog entry; add entries for: {0}",
            string.Join(", ", missing));
    }

    [Fact]
    public void Every_catalog_entry_maps_to_a_registered_route()
    {
        var live = SmokeEndpointInventory.ReadNormalizedKeys();
        var orphans = SmokeRouteCatalog.Keys.Except(live).OrderBy(k => k).ToList();
        orphans.Should().BeEmpty(
            "catalog entries must match live routes (stale after a route rename?): {0}",
            string.Join(", ", orphans));
    }
}
```

No collection attribute — it needs no DB and no host; it must run everywhere including CI with zero prerequisites (this is the D5 gate).

- [ ] **Step 2: Run parity test with an empty catalog — expect failure listing all 92 routes** (proves the gate's failure message is actionable)

- [ ] **Step 3: Fill the catalog** (appendix table = checklist; 92 entries; follow rules 1–5 above)

- [ ] **Step 4: Run parity — expect 2 passed**

- [ ] **Step 5: Write `PipelineSmokeTests` implementing the assertion contract verbatim** (one `[Theory]` per pass + the Kestrel sentinel facts; `[Collection(nameof(SmokeCollection))]`; `InitializeAsync => SmokeSeed.EnsureSeededAsync`)

- [ ] **Step 6: Run the full smoke namespace**

Run: `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"`
Expected: all passed, 0 skipped; wall clock ≤ 90 s (budget; record actual). Any 500 surfaced by the Authorized or Binding pass is a REAL API defect — file it, do not weaken the assertion (stop condition if it blocks: report to user).

- [ ] **Step 7: Commit**

```bash
git add Backend/tests/QuranDashboard.Tests/Smoke/Pipeline/
git commit -m "feat(smoke): phase 3 — route catalog, parity gate, full pipeline pass"
```

---

## Phase 4 — canonical dump tooling + data-smoke (staged machines only)

Depends on: Phases 1–2 (`SmokeHostConfigurator` references `SmokePersonas`). Independent of Phase 3.

**Files:**
- Create: `Backend/scripts/create-smoke-dump` (chmod +x, bash, mirrors `Backend/scripts/reset-db` style)
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/_Support/QuranDumpGate.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Smoke/_Fixtures/QuranDataSmokeFixture.cs` (+ `QuranDataSmokeCollection` definition in the same file)
- Test: `Backend/tests/QuranDashboard.Tests/Smoke/Data/QuranDataSmokeTests.cs`

**Intended behavior (L5):** the dump is a *derived cache of the verified canonical import* — produced ONLY from a local DB that was seeded by the documented DataImporter chain, never committed (lives under gitignored `resources/`), verified fail-closed before every restore.

### `create-smoke-dump` exact behavior

1. Inputs: `--source` connection URI (default `postgresql://localhost:5432/quran_dashboard`), output fixed to `resources/db-dumps/quran-canonical/`.
2. Preflight (hard fail with message on any miss): source reachable; latest row of `__EFMigrationsHistory` equals the latest migration filename in `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/`; `quran_surahs` count == 114 and `quran_ayahs` count == 6236 (canonical invariants).
3. Dump: `pg_dump --data-only --format=custom --table='quran_*'` → `quran-canonical.dump` (data-only: restore target schema comes from EF migrations, satisfying "migrated schema incl. `__EFMigrationsHistory`").
4. Manifest `manifest.json`: `{ "name": "quran-canonical", "createdUtc", "migrationId", "dumpSha256", "tables": { "<table>": <rowcount>, ... } }` — row count per dumped table via `psql -tA -c "SELECT count(*) …"`, sha256 via `sha256sum`.
5. Prints dump size and elapsed time (L5: measured here — record both in the phase commit message body).

### `QuranDataSmokeFixture` exact behavior

1. Skip layer: `QuranDumpGate` mirrors `Quran/CanonicalImportSourceTestGate.cs:5-38` — root `resources/db-dumps/quran-canonical/` resolved relative to `AppContext.BaseDirectory` the same way; `QuranDumpFactAttribute` sets `Skip` in the constructor when the dump or manifest is missing. Missing dump ⇒ SKIP (CI-normal).
2. Fail-loud layer (corrupt ≠ skip): once the dump EXISTS — sha256 mismatch vs manifest ⇒ throw; `manifest.migrationId` != repo's latest migration ⇒ throw ("stale dump — re-run Backend/scripts/create-smoke-dump").
3. Own `postgres:16-alpine` container → `MigrateAsync` → copy dump into container (Testcontainers `CopyAsync`) → `container.ExecAsync` running `pg_restore --data-only --disable-triggers -U <user> -d <db> <path>` (assert exit code 0; `--disable-triggers` because FK order inside `quran_*` is not topologically dumped) → spot-verify: for `quran_surahs`, `quran_ayahs`, `quran_words`, actual `count(*)` equals the manifest value ⇒ else throw.
4. Boot ONE in-memory host via `SmokeHostConfigurator.Configure` (data-smoke needs no Kestrel and no personas — all legacy reads are anonymous).
5. Own collection `QuranDataSmokeCollection` (L8) — never shares the pipeline container.

### `QuranDataSmokeTests` — one real-data read per legacy controller (13), all `[QuranDumpFact]`

| Route (concrete) | Assertion beyond 200 |
|---|---|
| `api/mushaf/pages/1` | non-empty lines collection |
| `api/mushaf/surahs` | exactly 114 items |
| `api/mushaf/study-sources` | non-empty |
| `api/mushaf/ayahs/1:1/study` | non-empty entries |
| `api/mushaf/ayahs/1:1/similar-ayahs` | 200 (empty allowed — data-dependent) |
| `api/mushaf/ayahs/2:6/mutashabihat` | 200 (empty allowed) |
| `api/mushaf/words/1:1:1/analysis` | non-empty analysis |
| `api/words/roots` (first page) | non-empty items; total > 1600 |
| `api/words/lemmas` (first page) | non-empty; total > 4700 |
| `api/words/stems` (first page) | non-empty; total > 12000 |
| `api/words/unique/tashkeel` (first page) | non-empty; total > 21000 |
| `api/words/word-types/tree` | non-empty |
| `api/words/word-types/table/{kind}/{dimensionId}` with a kind+id read from the tree response | 200, non-empty |

Totals-thresholds come from the documented inventory (`Backend/report/database-inventory/current-database-inventory.md`) — assert `>` thresholds, not exact counts (exact counts live in the manifest check).

- [ ] **Step 1: Write `QuranDumpGate` + a gate self-test** (fact skips when dir absent — assert via `QuranDumpFactAttribute.Skip` non-null when `QuranDumpGate.IsMissing`)
- [ ] **Step 2: Write `create-smoke-dump`; run it against the local canonical DB**; expected output: dump + manifest under `resources/db-dumps/quran-canonical/`, printed size + elapsed. Record both.
- [ ] **Step 3: Write the failing data-smoke tests, then `QuranDataSmokeFixture`**
- [ ] **Step 4: Run data-smoke locally (staged machine)**

Run: `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke.Data"`
Expected: 13 passed, 0 skipped locally. Then rename `resources/db-dumps` temporarily and re-run: expected 13 SKIPPED with the gate's reason string; rename back.

- [ ] **Step 5: Commit** (dump itself is under gitignored `resources/` — verify `git status` shows only the script + tests)

```bash
git add Backend/scripts/create-smoke-dump Backend/tests/QuranDashboard.Tests/Smoke/
git commit -m "feat(smoke): phase 4 — canonical dump tooling and skip-gated data-smoke"
```

---

## Phase 5 — strategy wiring, CI step, READMEs

Depends on: Phases 3 + 4.

**Files:** modify `TESTING_STRATEGY.md`, `.github/workflows/ci.yml`, `Backend/tests/QuranDashboard.Tests/README.md`, `Backend/api/QuranDashboard.Api/Authentication/README.md`; create `Backend/tests/QuranDashboard.Tests/Smoke/README.md`.

- [ ] **Step 1: `TESTING_STRATEGY.md`** (§ numbers per current file; only these sections — non-goal guard):
  - §3: add a "Smoke suite" block: command `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"`; state data-smoke self-skips without `resources/db-dumps/quran-canonical/`; state the suite is REQUIRED pre-PR for any change touching `Backend/api/` routes, contracts, auth, or middleware.
  - §3 Tier B + §3 Tier C + §5 catalog: append `&FullyQualifiedName!~QuranDashboard.Tests.Smoke` to every occurrence of the no-pipeline exclusion filter (L3 — Smoke leaves the ~45 s envelope). Grep the file for the filter string and amend every copy.
  - §4 decision matrix: new row — "API endpoint added/changed, auth/middleware/binding change → Smoke suite (plus its tier row)".
  - §5: re-state the partition note: the no-pipeline/all-pipeline partition now excludes the Smoke namespace by construction; update the stale counts sentence to name the mechanism instead of frozen numbers.
  - §9: add responsibility line: "Any new API route requires a `SmokeRouteCatalog` entry in the same change — `SmokeCoverageParityTests` fails CI otherwise."
  - §11: clarify traits remain deferred; Smoke uses namespace selection, consistent with this section (no contradiction to resolve).
- [ ] **Step 2: `ci.yml`** — in the `backend-tests` job: amend the existing test step to `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj -c Release --no-build --filter "FullyQualifiedName!~QuranDashboard.Tests.Smoke"` and add immediately after:

```yaml
      - name: Smoke suite (real pipeline)
        run: dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"
```

  (Same job — reuses the Release build; Testcontainers already works there; data-smoke rows skip because `resources/` is absent in CI. Expected CI smoke-step wall clock ≤ 2 min.)
- [ ] **Step 3: READMEs** — `tests/README.md`: add the Smoke cluster to the folder map. `Authentication/README.md`: replace the stale lines 50–52 claim ("policies are not applied to any endpoint") with the current truth (per-action permission policies on all Abwab write surfaces, `SystemOwner` on PermissionsController, plain `[Authorize]` on AccessController) and add a short "Testing the pipeline" note pointing at the smoke harness pattern (real JwtBearer + `PostConfigure` + `TestJwtTokens`). New `Smoke/README.md`: coverage rule (catalog+parity), personas table, seed-once tolerance contract, dump provenance rule (derived cache; `create-smoke-dump`; fail-loud vs skip semantics), collection isolation (L8), Testing-env invariant (L7).
- [ ] **Step 4: Full verification battery**

Run, in order, expecting all green:
1. `dotnet build Backend/QuranDashboard.sln`
2. Amended Tier B no-pipeline command from §3 — expected: passes AND total test count returns to the pre-feature baseline (Smoke excluded).
3. Smoke command — all pipeline tests pass; data-smoke passes locally (13) / would skip in CI.
4. `Backend/scripts/check-api-contract` (CI parity for the contract-drift job — no API contract changed, must be clean).
- [ ] **Step 5: Commit**

```bash
git add TESTING_STRATEGY.md .github/workflows/ci.yml Backend/tests/QuranDashboard.Tests/README.md Backend/tests/QuranDashboard.Tests/Smoke/README.md Backend/api/QuranDashboard.Api/Authentication/README.md
git commit -m "feat(smoke): phase 5 — strategy tiers, CI smoke step, README truth updates"
```

---

## Runtime budget

| Suite | Budget | Notes |
|---|---|---|
| Guards + personas (phases 1–2) | ≤ 30 s | one container + migrate amortized per collection |
| Pipeline-smoke (phase 3, in-memory full + Kestrel sentinel) | ≤ 90 s | ~92×4 asserts, seed-once |
| Data-smoke (phase 4, staged machine) | ≤ 3 min incl. restore | restore time measured in phase 4; renegotiate budget with the user if restore alone exceeds ~2 min |
| CI smoke step | ≤ 2 min | data-smoke skips |

## Risks, rollback, stop conditions

- **Stop + report (do not improvise) if:** (a) `WebApplicationFactory<Program>` + `UseKestrel(0)` fails against this app shape at runtime; (b) any Authorized/Binding pass surfaces a real 500 in an existing endpoint (that is a product bug — report before "fixing" the test); (c) restore time or dump size makes the ≤3 min budget unreachable (D3 forbids trimming to synthetic subsets — the user must re-scope); (d) `Testing`-env boot cannot satisfy a `ValidateOnStart` chain without touching production config files.
- **Risk: `SecurityTestHarness` refactor breaks the 76-class Abwab collection.** Mitigation: additive overloads only + the Phase 2 Step 7 Abwab/Api run.
- **Risk: catalog drift from route renames.** Covered: the orphan-direction parity test fails with the stale key named.
- **Risk: interim phases 1–4 leak Smoke into the 45 s Tier B envelope.** Accepted, known, bounded (< 2 min added worst case); resolved in Phase 5. Do not reorder Phase 5 earlier — filters must land with the CI step.
- **Rollback:** one commit per phase; `git revert <phase-commit>` restores any prior state. The `Program.cs` partial-class line is behavior-neutral and safe to leave even under partial rollback.

## Acceptance criteria (feature exit)

1. `--filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"` green locally: guards + personas + parity + the full assertion-contract pipeline over all 92 routes + Kestrel sentinel + 13 data-smoke (staged machine).
2. Deleting any single `SmokeRouteCatalog` entry makes `SmokeCoverageParityTests` fail naming that route (spot-check one).
3. Temporarily adding `[property: Required]` back onto a positional-record member of `TemplateContracts.cs` makes the Binding pass fail with a 500-not-400 assertion (the 030 regression, generalized) — verify once locally, then revert the temporary change.
4. Scheme-inventory guard green; Testing-env guard green; no smoke test can reach a non-container connection string.
5. CI: main backend step excludes Smoke; dedicated smoke step runs it; data-smoke skips with the gate's reason; parity gate active.
6. Tier B/C runtime restored to its pre-feature envelope; `TESTING_STRATEGY.md` sections §3/§4/§5/§9/§11 updated; all listed READMEs updated; `appsettings.Production.json` untouched (`git log --stat` proof).

---

## Appendix — full route checklist (92 actions; parity baseline; auth from code)

Legend: A = Anonymous, TR = TreeRedacted (no attribute; in-body 403 for anonymous/unpermissioned), AU = Authenticated (plain `[Authorize]`), P(code) = permission policy, SO = SystemOwner (+ `PermissionAdmin` rate-limit policy).

### Legacy Quran reads (45 — all GET, all A)

Mushaf (7): `api/mushaf/pages/{pageNumber}` · `api/mushaf/surahs` · `api/mushaf/study-sources` · `api/mushaf/ayahs/{verseKey}/study` · `api/mushaf/ayahs/{verseKey}/similar-ayahs` · `api/mushaf/ayahs/{verseKey}/mutashabihat` · `api/mushaf/words/{wordLocation}/analysis`

Roots (8): `api/words/roots` · `api/words/roots/{id}` · `…/{id}/ayahs` · `…/{id}/words/{wordKind}` · `…/{id}/surahs` · `…/{id}/missing-surahs` · `…/{id}/lemmas` · `…/{id}/stems`

Lemmas (7): `api/words/lemmas` · `…/{id}` · `…/{id}/words/{wordKind}` · `…/{id}/ayahs` · `…/{id}/surahs` · `…/{id}/missing-surahs` · `…/{id}/stems`

Stems (7): `api/words/stems` · `…/{id}` · `…/{id}/words/{wordKind}` · `…/{id}/ayahs` · `…/{id}/surahs` · `…/{id}/missing-surahs` · `…/{id}/lemmas`

Unique words (5): `api/words/unique/{kind}` · `…/{kind}/{id}` · `…/{kind}/{id}/surahs` · `…/{kind}/{id}/missing-surahs` · `…/{kind}/{id}/ayahs`

Word types (7): `api/words/word-types/tree` · `…/words` · `…/table` · `…/scope-counts` · `…/words/{tashkeelWordId}` · `…/words/{tashkeelWordId}/ayahs` · `…/words/{tashkeelWordId}/surahs`

Word-type grouped details (4): `api/words/word-types/table/{kind}/{dimensionId}` · `…/words` · `…/ayahs` · `…/surahs`

### Abwab (41)

Tree (2, **TreeRedacted** — no `[Authorize]`, but anonymous/unpermissioned callers get an in-body 403 via permission redaction, NOT a 200): GET `api/abwab/tree` · GET `api/abwab/tree/search`

Sections (4): POST `api/abwab/sections` P(section.add) · PUT `…/{sectionId}` P(section.edit) · POST `…/reorder` P(section.reorder) · DELETE `…/{sectionId}` P(section.delete)

Categories (9): POST `api/abwab/categories` P(category.add) · PUT `…/{categoryId}` P(category.edit) · POST `…/move` P(category.move) · POST `…/reorder` P(category.reorder) · POST `…/{categoryId}/subtree-delete` P(category.delete) · POST `…/operation-restore/{deletionOperationId}` P(category.delete) · POST `…/{categoryId}/aliases` P(category.edit) · PUT `…/aliases/{aliasId}` P(category.edit) · DELETE `…/aliases/{aliasId}` P(category.edit)

Relationships (5): GET `api/abwab/relationships/{categoryId}` P(relationship.view) · POST `api/abwab/relationships` P(relationship.add) · PUT `…/{relationshipId}` P(relationship.edit) · DELETE `…/{relationshipId}` P(relationship.delete) · POST `…/{relationshipId}/restore` P(relationship.restore)

Templates (17): GET `api/abwab/templates` P(template.view) · GET `…/{doorTemplateId}` P(template.view) · GET `…/{doorTemplateId}/history` P(template.view) · POST `api/abwab/templates` P(template.add) · PUT `…/{doorTemplateId}` P(template.edit) · DELETE `…/{doorTemplateId}` P(template.delete) · POST `…/{doorTemplateId}/restore` P(template.restore) · POST `…/{doorTemplateId}/nodes` P(template.edit) · PUT `…/nodes/{templateNodeId}` P(template.edit) · POST `…/nodes/{templateNodeId}/reparent` P(template.edit) · POST `…/{doorTemplateId}/nodes/reorder` P(template.edit) · DELETE `…/nodes/{templateNodeId}` P(template.edit) · POST `…/nodes/{templateNodeId}/aliases` P(template.edit) · PUT `…/aliases/{templateNodeSearchAliasId}` P(template.edit) · DELETE `…/aliases/{templateNodeSearchAliasId}` P(template.edit) · POST `…/aliases/{templateNodeSearchAliasId}/restore` P(template.edit) · POST `…/{doorTemplateId}/apply` P(template.apply)

Protection (4): GET `api/abwab/protection/{categoryId}` P(protection.view) · POST `…/{categoryId}/{protectionType}/apply` P(protection.apply) · POST `…/{categoryId}/{protectionType}/lift` P(protection.lift) · POST `…/{categoryId}/full-preset` P(protection.apply)

### Security / access (4)

GET `api/access/me` AU · GET `api/security/permissions` SO · POST `api/security/permissions/grant` SO · POST `api/security/permissions/revoke` SO

### Misc (2)

GET `api/health` A · GET `api/dashboard/info` A
