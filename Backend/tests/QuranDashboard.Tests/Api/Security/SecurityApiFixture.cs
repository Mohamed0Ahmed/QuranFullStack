using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QuranDashboard.Api.Security.Permissions;
using QuranDashboard.Domain.Security.Owners;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Tests.Api.Access;

namespace QuranDashboard.Tests.Api.Security;

public sealed class SecurityApiFixture : IAsyncLifetime
{
    public const string OwnerIssuer = "https://logto.test";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly object _apiFactoryLock = new();
    private WebApplicationFactory<PermissionsController>? _apiFactory;
    private ServiceProvider? _queryProvider;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        _queryProvider = new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString))
            .BuildServiceProvider();

        await using var scope = QueryProvider.CreateAsyncScope();
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
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Add("X-Real-IP", "203.0.113.7");
        return client;
    }

    public static AuthenticationHeaderValue Bearer(string sub) =>
        new("Bearer", TestJwtTokens.Mint(sub));

    public async Task ResetAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        // One batch keeps SET replica + TRUNCATE on the same connection.
        await db.Database.ExecuteSqlRawAsync(
            "SET session_replication_role = replica; TRUNCATE security_audit_events RESTART IDENTITY; SET session_replication_role = origin;");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM permission_assignments;");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM system_owner_memberships;");
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE abwab_revision_state SET audit_head_sequence = 0, timeline_generation = 0, tree_revision = 0 WHERE id = 1;");
        await db.Database.ExecuteSqlRawAsync("UPDATE abwab_write_barrier SET state = 0 WHERE id = 1;");
    }

    public async Task SeedOwnerAsync(string subject)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        db.SystemOwnerMemberships.Add(new SystemOwnerMembership(
            Guid.NewGuid(), OwnerIssuer, subject, accountEnabled: true, DateTimeOffset.UnixEpoch));
        await db.SaveChangesAsync();
    }

    public async Task<long> SeedGrantedAssignmentAsync(PermissionTargetKind kind, string key, string code)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var assignment = new PermissionAssignment(Guid.NewGuid(), kind, key, code, DateTimeOffset.UnixEpoch);
        db.PermissionAssignments.Add(assignment);
        await db.SaveChangesAsync();
        return assignment.Version;
    }

    public async Task<PermissionAssignment?> GetAssignmentAsync(PermissionTargetKind kind, string key, string code)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.PermissionAssignments.AsNoTracking().SingleOrDefaultAsync(
            a => a.TargetKind == kind && a.TargetKey == key && a.PermissionCode == code);
    }

    private ServiceProvider QueryProvider => _queryProvider
        ?? throw new InvalidOperationException(
            $"{nameof(SecurityApiFixture)} has not been initialized. Ensure it is used as an ICollectionFixture.");

    private WebApplicationFactory<PermissionsController> Factory
    {
        get
        {
            lock (_apiFactoryLock)
            {
                return _apiFactory ??= BuildApiFactory();
            }
        }
    }

    private WebApplicationFactory<PermissionsController> BuildApiFactory()
    {
        return new WebApplicationFactory<PermissionsController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = ConnectionString,
                        ["Auth:Authority"] = TestJwtTokens.TestIssuer,
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                        ["RateLimiting:PermissionAdminPermitLimit"] = "100000",
                    }));

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<QuranDashboardDbContext>();
                    services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                    services.AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString));

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
}

[CollectionDefinition(nameof(SecurityApiCollection))]
public sealed class SecurityApiCollection : ICollectionFixture<SecurityApiFixture>;
