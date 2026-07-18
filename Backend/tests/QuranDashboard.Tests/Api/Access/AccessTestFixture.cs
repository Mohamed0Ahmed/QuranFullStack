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

    /// <summary>The subject whose fake profile email matches the configured Owner-bootstrap email.</summary>
    public const string OwnerSub = "logto-owner";

    /// <summary>The configured Owner-bootstrap email (the fake profile email for <see cref="OwnerSub"/>).</summary>
    public static string OwnerEmail => FakeExternalUserProfileSource.EmailFor(OwnerSub);

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
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    /// <summary>
    /// The API host's root service provider — for resolving pipeline services (e.g.
    /// <c>IUserRoleResolver</c> or <c>IAuthorizationPolicyProvider</c>) in tests. Services resolved here
    /// share the host's singletons (notably the one <c>IMemoryCache</c> the request pipeline uses), so the
    /// cache primed/evicted through the pipeline is the same instance a test observes.
    /// </summary>
    public IServiceProvider ApiServices => Factory.Services;

    private WebApplicationFactory<AccessController> Factory
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

    /// <summary>
    /// Removes every provisioned user and resets the fake, giving each test a clean slate. The seeded
    /// roles table is left intact. The owner subject reuses a fixed <c>sub</c> across tests (only its email
    /// triggers bootstrap), so its entry in the pipeline's shared role cache is evicted too — otherwise a
    /// prior test's cached role would leak past the truncation.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE users RESTART IDENTITY CASCADE;");
        ProfileSource.Reset();
        EvictRoleCache(OwnerSub);
    }

    /// <summary>Evicts a subject's cached role from the pipeline's shared cache (role-cache test isolation).</summary>
    public void EvictRoleCache(string logtoSub)
    {
        using var scope = ApiServices.CreateScope();
        scope.ServiceProvider.GetRequiredService<IUserRoleResolver>().Evict(logtoSub);
    }

    /// <summary>Reads the persisted users via an independent DbContext (never the pipeline's own instance).</summary>
    public async Task<IReadOnlyList<User>> GetUsersAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
    }

    /// <summary>Reads the seeded roles via an independent DbContext.</summary>
    public async Task<IReadOnlyList<Role>> GetRolesAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessRoles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
    }

    /// <summary>Reads a single persisted user by its Logto <c>sub</c>, or null when absent.</summary>
    public async Task<User?> GetUserBySubAsync(string logtoSub)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().SingleOrDefaultAsync(u => u.LogtoSub == logtoSub);
    }

    /// <summary>Inserts a pre-provisioned user directly (bypassing the pipeline) and returns its id.</summary>
    public async Task<int> InsertUserAsync(User user)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        db.AccessUsers.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
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
                        // Enables the Owner-bootstrap path for OwnerSub only (its fake profile email).
                        ["Auth:BootstrapOwnerEmail"] = OwnerEmail,
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
