using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Smoke;

public sealed class SmokeApiFixture : IAsyncLifetime
{
    private readonly object _apiFactoryLock = new();
    private PostgreSqlDatabaseLease? _databaseLease;
    private WebApplicationFactory<HealthController>? _apiFactory;
    private ServiceProvider? _queryProvider;

    // Shared as a singleton so its call counters survive across requests.
    public FakeExternalUserProfileSource ProfileSource { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _databaseLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(nameof(SmokeApiFixture));
        ConnectionString = _databaseLease.ConnectionString;

        try
        {
            _queryProvider = BuildQueryProvider();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        // The lease is released in the finally so a failing host or provider disposal cannot strand a
        // leased database on the shared server for the rest of the test process.
        try
        {
            if (_apiFactory is not null)
            {
                await _apiFactory.DisposeAsync();
                _apiFactory = null;
            }

            if (_queryProvider is not null)
            {
                await _queryProvider.DisposeAsync();
                _queryProvider = null;
            }
        }
        finally
        {
            if (_databaseLease is not null)
            {
                await _databaseLease.DisposeAsync();
                _databaseLease = null;
            }
        }
    }

    public HttpClient CreateClient() => SmokeApiHost.CreateClient(Factory);

    internal HttpClient CreateClientFor(SmokePersona persona)
    {
        var client = CreateClient();
        if (SmokePersonas.SubFor(persona) is { } sub)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    TestJwtTokens.Mint(sub, additionalClaims: SmokePersonas.ClaimsFor(persona)));
        }

        return client;
    }

    // The host's own container, not a parallel one: a scope taken here shares the host's singletons —
    // the IMemoryCache CachedUserRoleResolver writes through, so an Evict from a test invalidates what
    // the request pipeline cached. Scoped services (DbContext, IUserRoleResolver) are still fresh
    // instances; only the singleton state is shared.
    public IServiceProvider ApiServices => Factory.Services;

    // The whole ResetPerTest contract the SmokeCollection row of TestSupport/Execution/test-resources.tsv
    // declares, in one entry point: every table this collection mutates, restored before a case runs.
    // users reaches user_permissions and access_audit_events by cascade; the six abwab tables are the
    // write routes' surface. Two pieces of state a TRUNCATE cannot reach are restored with them — the
    // shared role cache (CachedUserRoleResolver holds a resolved role for 30 s, the persona subs are
    // fixed across tests, and a truncate does not touch it) and the abwab read caches (their generation
    // counter moves on writes, never on raw SQL, so a truncated tree stays served from IMemoryCache).
    public async Task ResetAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE users, abwab_sections, abwab_doors, abwab_door_aliases, abwab_door_relations, "
            + "abwab_templates, abwab_template_nodes RESTART IDENTITY CASCADE;");
        ProfileSource.Reset();

        foreach (var sub in SmokePersonas.TokenBearingSubs)
        {
            EvictRoleCache(sub);
        }

        InvalidateAbwabCaches();
    }

    private void EvictRoleCache(string logtoSub)
    {
        using var scope = ApiServices.CreateScope();
        scope.ServiceProvider.GetRequiredService<IUserRoleResolver>().Evict(logtoSub);
    }

    // Through the host's own services, never a second provider: the generation counter that decides
    // whether a cached tree is stale is a singleton of the container serving the requests.
    private void InvalidateAbwabCaches()
    {
        using var scope = ApiServices.CreateScope();
        var caches = scope.ServiceProvider.GetRequiredService<IAbwabCacheInvalidator>();
        caches.InvalidateTree();
        caches.InvalidateTemplates();
    }

    // Reads via an independent DbContext, never the pipeline's own instance (test isolation).
    public async Task<User?> GetUserBySubAsync(string logtoSub)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().SingleOrDefaultAsync(u => u.LogtoSub == logtoSub);
    }

    private ServiceProvider QueryProvider => _queryProvider
        ?? throw new InvalidOperationException(
            $"{nameof(SmokeApiFixture)} has not been initialized. Ensure it is used as an ICollectionFixture.");

    private WebApplicationFactory<HealthController> Factory
    {
        get
        {
            // Guard the lazy init so concurrent callers reuse the single factory instead of racing to
            // construct (and leak) multiple WebApplicationFactory instances.
            lock (_apiFactoryLock)
            {
                return _apiFactory ??= SmokeApiHost.Build(ConnectionString, ProfileSource);
            }
        }
    }

    private ServiceProvider BuildQueryProvider()
    {
        return new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString))
            .BuildServiceProvider();
    }
}
