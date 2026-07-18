using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QuranDashboard.Api.Controllers.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

/// <summary>
/// Shared fixture for the <c>/api/access/me</c> integration tests. Spins up a real
/// <c>postgres:16-alpine</c> container and applies the REAL EF Core migrations against a fresh database,
/// which proves the <c>AddAccessUsers</c> migration applies cleanly. A single
/// <see cref="WebApplicationFactory{TEntryPoint}"/> hosts the real API pipeline pointed at that
/// container, with exactly two swaps: the DbContext is repointed at the container, and the external
/// identity boundary (<see cref="IExternalUserProfileSource"/>) is replaced by the in-memory
/// <see cref="FakeExternalUserProfileSource"/>. Token validation is made fully offline by seeding the
/// trusted signing key and issuer into the JwtBearer options, so no metadata is ever fetched from the
/// configured (fake) authority.
/// </summary>
public sealed class AccessTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly object _apiFactoryLock = new();
    private WebApplicationFactory<AccessController>? _apiFactory;
    private ServiceProvider? _queryProvider;

    /// <summary>The fake identity boundary, shared as a singleton so its call counters survive across requests.</summary>
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

    public HttpClient CreateApiClient()
    {
        // Guard the lazy init so concurrent callers reuse the single factory instead of racing to
        // construct (and leak) multiple WebApplicationFactory instances.
        WebApplicationFactory<AccessController> factory;
        lock (_apiFactoryLock)
        {
            factory = _apiFactory ??= BuildApiFactory();
        }

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    /// <summary>Removes every provisioned user and resets the fake, giving each test a clean slate.</summary>
    public async Task ResetAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE users RESTART IDENTITY CASCADE;");
        ProfileSource.Reset();
    }

    /// <summary>Reads the persisted users via an independent DbContext (never the pipeline's own instance).</summary>
    public async Task<IReadOnlyList<User>> GetUsersAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
    }

    private ServiceProvider QueryProvider => _queryProvider
        ?? throw new InvalidOperationException(
            $"{nameof(AccessTestFixture)} has not been initialized. Ensure it is used as an ICollectionFixture.");

    private WebApplicationFactory<AccessController> BuildApiFactory()
    {
        return new WebApplicationFactory<AccessController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = ConnectionString,
                        // A valid-shaped https authority the options validator accepts. No metadata is ever
                        // fetched from it because JwtBearerOptions.Configuration is seeded below.
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                    }));

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<QuranDashboardDbContext>();
                    services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                    services.AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString));

                    // Replace the real Logto Management API boundary with the in-memory fake, shared as a
                    // singleton so its call counters accumulate across requests.
                    services.RemoveAll<IExternalUserProfileSource>();
                    services.AddSingleton<IExternalUserProfileSource>(ProfileSource);

                    // Make token validation fully offline: seed the trusted signing key + issuer directly.
                    // Setting Configuration short-circuits the metadata fetch to the (fake) authority.
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.Configuration = new OpenIdConnectConfiguration { Issuer = TestJwtTokens.TestIssuer };
                        options.Configuration.SigningKeys.Add(TestJwtTokens.SigningKey);
                        options.TokenValidationParameters.ValidIssuer = TestJwtTokens.TestIssuer;
                        options.TokenValidationParameters.IssuerSigningKey = TestJwtTokens.SigningKey;
                        // Pin the audience here rather than via in-memory config: production
                        // (AddApiAuthentication) binds Auth:Audience eagerly during service registration,
                        // which runs before WebApplicationFactory applies its ConfigureAppConfiguration
                        // override. PostConfigure materializes when the handler resolves the options, so it
                        // authoritatively sets the audience the minted tokens target.
                        options.TokenValidationParameters.ValidAudience = TestJwtTokens.TestAudience;
                    });
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

[CollectionDefinition(nameof(AccessCollection))]
public sealed class AccessCollection : ICollectionFixture<AccessTestFixture>;
