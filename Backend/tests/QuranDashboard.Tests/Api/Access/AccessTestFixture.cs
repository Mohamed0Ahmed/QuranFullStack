using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Api.Controllers.Access;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;
using QuranDashboard.Tests.TestSupport.Access;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Api.Access;

public sealed class AccessTestFixture : IAsyncLifetime
{
    private readonly object _apiFactoryLock = new();
    private PostgreSqlDatabaseLease? _databaseLease;
    private WebApplicationFactory<AccessController>? _apiFactory;
    private ServiceProvider? _queryProvider;

    // Shared as a singleton so its call counters survive across requests.
    public FakeExternalUserProfileSource ProfileSource { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    // This sub's fake profile email matches the configured Owner-bootstrap email, so it drives the owner path.
    public const string OwnerSub = "logto-owner";

    public static string OwnerEmail => FakeExternalUserProfileSource.EmailFor(OwnerSub);

    public const string SecondOwnerSub = "logto-owner-second";

    public static string SecondOwnerEmail => FakeExternalUserProfileSource.EmailFor(SecondOwnerSub);

    public async Task InitializeAsync()
    {
        _databaseLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(nameof(AccessTestFixture));
        ConnectionString = _databaseLease.ConnectionString;

        _queryProvider = BuildQueryProvider();
    }

    public async Task DisposeAsync()
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

        if (_databaseLease is not null)
        {
            await _databaseLease.DisposeAsync();
            _databaseLease = null;
        }
    }

    public HttpClient CreateApiClient()
    {
        return CreateApiClient(Factory);
    }

    public HttpClient CreateApiClient(WebApplicationFactory<AccessController> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public WebApplicationFactory<AccessController> CreateAuthorizationPipelineFactory(
        Action<IServiceCollection>? configureServices = null)
    {
        return Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddApplicationPart(typeof(AuthorizationPipelineProbeController).Assembly);
                services.AddSingleton<AuthorizationPipelineProbe>();
                configureServices?.Invoke(services);
            });
        });
    }

    public IServiceProvider ApiServices => Factory.Services;

    public IServiceProvider QueryServices => QueryProvider;

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

    public async Task ResetAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE users, permissions RESTART IDENTITY CASCADE;");
        ProfileSource.Reset();
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
        var normalizer = scope.ServiceProvider.GetRequiredService<IEmailIdentityNormalizer>();
        if (string.IsNullOrWhiteSpace(user.NormalizedEmail))
        {
            user.NormalizedEmail = normalizer.Normalize(user.Email);
        }

        db.AccessUsers.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    public async Task<int> InsertPersonaAsync(string key)
    {
        var persona = TestAccessPersonas.For(key);
        int? roleId = persona.IsOwner
            ? (await GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id
            : null;

        return await InsertUserAsync(persona.BuildUser(roleId));
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
                        // fetched from it: TestJwtTokens.ConfigureOfflineValidation seeds
                        // JwtBearerOptions.Configuration instead.
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        // Required non-blank by the validator; inert otherwise, since the effective
                        // audience is pinned by TestJwtTokens.ConfigureOfflineValidation.
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Auth:InteractiveClientId"] = TestJwtTokens.TestClientId,
                        // Supplies the two configured Owner identities used by the access fixtures.
                        ["OwnerBootstrap:Emails:0"] = OwnerEmail,
                        ["OwnerBootstrap:Emails:1"] = SecondOwnerEmail,
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                        // ResetAsync truncates `permissions` and each Access test opts into
                        // synchronizing it, so the startup sync must not race that contract.
                        ["Access:PermissionCatalogueStartupSync:Enabled"] = "false",
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

                    TestJwtTokens.ConfigureOfflineValidation(services);
                });
            });
    }

    private ServiceProvider BuildQueryProvider()
    {
        return new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString))
            .AddSingleton<IEmailIdentityNormalizer, EmailIdentityNormalizer>()
            .BuildServiceProvider();
    }
}

[CollectionDefinition(nameof(AccessCollection))]
public sealed class AccessCollection : ICollectionFixture<AccessTestFixture>;
