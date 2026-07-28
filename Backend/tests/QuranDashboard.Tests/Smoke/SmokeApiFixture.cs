using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.Api.Access;

namespace QuranDashboard.Tests.Smoke;

public sealed class SmokeApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly object _apiFactoryLock = new();
    private WebApplicationFactory<HealthController>? _apiFactory;
    private ServiceProvider? _queryProvider;

    // Shared as a singleton so its call counters survive across requests.
    public FakeExternalUserProfileSource ProfileSource { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        _queryProvider = BuildQueryProvider();

        await using var scope = _queryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _apiFactory?.Dispose();
        _apiFactory = null;

        if (_queryProvider is not null)
        {
            await _queryProvider.DisposeAsync();
            _queryProvider = null;
        }

        await _container.DisposeAsync();
    }

    public HttpClient CreateClient() => SmokeApiHost.CreateClient(Factory);

    internal HttpClient CreateClientFor(SmokePersona persona)
    {
        var client = CreateClient();
        if (SmokePersonas.SubFor(persona) is { } sub)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(sub));
        }

        return client;
    }

    // The host's own container, not a parallel one: a test resolving through this sees exactly the
    // service instances the request pipeline used.
    public IServiceProvider ApiServices => Factory.Services;

    // The persona subs are fixed across tests, so every one of them is evicted from the shared role
    // cache as well: CachedUserRoleResolver holds a resolved role for 30 s and a TRUNCATE does not
    // touch it, so a prior test's role would otherwise leak past the reset.
    public async Task ResetAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE users RESTART IDENTITY CASCADE;");
        ProfileSource.Reset();

        foreach (var sub in SmokePersonas.TokenBearingSubs)
        {
            EvictRoleCache(sub);
        }
    }

    private void EvictRoleCache(string logtoSub)
    {
        using var scope = ApiServices.CreateScope();
        scope.ServiceProvider.GetRequiredService<IUserRoleResolver>().Evict(logtoSub);
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
