using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    // The pipeline calls UseHttpsRedirection, so an http base address answers 307 instead of the route.
    public HttpClient CreateClient()
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

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
                return _apiFactory ??= BuildApiFactory();
            }
        }
    }

    private WebApplicationFactory<HealthController> BuildApiFactory()
    {
        return new WebApplicationFactory<HealthController>()
            .WithWebHostBuilder(builder =>
            {
                // The reason this fixture exists: Testing loads base appsettings.json only — no
                // appsettings.Development.json, no user secrets, and no Swagger (UseApiPipeline
                // registers it under IsDevelopment()), so the composed route table is exactly the
                // controller endpoints.
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = ConnectionString,
                        // A valid-shaped https authority the options validator accepts. No metadata is ever
                        // fetched from it: TestJwtTokens.ConfigureOfflineValidation seeds
                        // JwtBearerOptions.Configuration instead.
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        // Required non-blank by the validator; inert otherwise, since the effective
                        // audience is pinned by TestJwtTokens.ConfigureOfflineValidation.
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        // Enables the Owner-bootstrap path for OwnerSub only (its fake profile email).
                        ["Auth:BootstrapOwnerEmail"] = FakeExternalUserProfileSource.EmailFor(SmokePersonas.OwnerSub),
                        // AddApiServices throws when the allowed-origins list is empty.
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                    }));

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<QuranDashboardDbContext>();
                    services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                    services.AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString));

                    // Replace the real Logto Management API boundary with the in-memory fake so no test
                    // ever calls out.
                    services.RemoveAll<IExternalUserProfileSource>();
                    services.AddSingleton<IExternalUserProfileSource>(ProfileSource);

                    TestJwtTokens.ConfigureOfflineValidation(services);

                    // Deliberately no health-check stub, unlike HealthApiFactory and RateLimitingApiFactory:
                    // the real AddDbContextCheck runs against the container, so a green /api/health proves
                    // the connection and the migrations.
                });
            });
    }

    private ServiceProvider BuildQueryProvider()
    {
        return new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString))
            .BuildServiceProvider();
    }
}
