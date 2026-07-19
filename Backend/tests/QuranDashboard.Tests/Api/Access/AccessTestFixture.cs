using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QuranDashboard.Api.Controllers.Access;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

public sealed class AccessTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly object _apiFactoryLock = new();
    private WebApplicationFactory<AccessController>? _apiFactory;
    private ServiceProvider? _queryProvider;

    // Shared as a singleton so its call counters survive across requests.
    public FakeExternalUserProfileSource ProfileSource { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    // This sub's fake profile email matches the configured Owner-bootstrap email, so it drives the owner path.
    public const string OwnerSub = "logto-owner";

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

    // Resolving here shares the host's singletons (notably the one IMemoryCache the request pipeline uses),
    // so the cache primed/evicted through the pipeline is the same instance a test observes.
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

    // The owner sub is fixed across tests (only its email triggers bootstrap), so its entry in the shared
    // role cache is evicted too — otherwise a prior test's cached role would leak past the truncation.
    public async Task ResetAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE users RESTART IDENTITY CASCADE;");
        ProfileSource.Reset();
        EvictRoleCache(OwnerSub);
    }

    public void EvictRoleCache(string logtoSub)
    {
        using var scope = ApiServices.CreateScope();
        scope.ServiceProvider.GetRequiredService<IUserRoleResolver>().Evict(logtoSub);
    }

    // Reads via an independent DbContext, never the pipeline's own instance (test isolation).
    public async Task<IReadOnlyList<User>> GetUsersAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessRoles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
    }

    public async Task<User?> GetUserBySubAsync(string logtoSub)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().SingleOrDefaultAsync(u => u.LogtoSub == logtoSub);
    }

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
