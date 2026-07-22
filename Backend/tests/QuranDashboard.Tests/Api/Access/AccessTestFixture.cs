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

    public FakeExternalUserProfileSource ProfileSource { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

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

    public IServiceProvider ApiServices => Factory.Services;

    private WebApplicationFactory<AccessController> Factory
    {
        get
        {
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
        await db.Database.ExecuteSqlRawAsync("TRUNCATE users RESTART IDENTITY CASCADE;");
        ProfileSource.Reset();
        EvictRoleCache(OwnerSub);
    }

    public void EvictRoleCache(string logtoSub)
    {
        using var scope = ApiServices.CreateScope();
        scope.ServiceProvider.GetRequiredService<IUserRoleResolver>().Evict(logtoSub);
    }

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
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Auth:BootstrapOwnerEmail"] = OwnerEmail,
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                    }));

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<QuranDashboardDbContext>();
                    services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                    services.AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString));

                    services.RemoveAll<IExternalUserProfileSource>();
                    services.AddSingleton<IExternalUserProfileSource>(ProfileSource);

                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.Configuration = new OpenIdConnectConfiguration { Issuer = TestJwtTokens.TestIssuer };
                        options.Configuration.SigningKeys.Add(TestJwtTokens.SigningKey);
                        options.TokenValidationParameters.ValidIssuer = TestJwtTokens.TestIssuer;
                        options.TokenValidationParameters.IssuerSigningKey = TestJwtTokens.SigningKey;
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
