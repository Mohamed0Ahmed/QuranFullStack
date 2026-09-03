using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Api.Controllers.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Access;

// Creates a migrated schema per case inside the runner-owned scratch database: the load-bearing claim is
// what a never-synchronized database does at boot, which no shared, already-populated schema can show.
[Collection(nameof(PermissionCatalogueStartupScratchCollection))]
public sealed class PermissionCatalogueStartupSyncTests(AccessMigrationTestFixture fixture)
{
    private const string OwnerSub = "catalogue-startup-owner";
    private const string CataloguePath = "/api/access/permissions";
    private const string HealthPath = "/api/health";
    private const string HealthCheckName = "permission_catalogue";

    // Nothing listens on port 1, so the sync's first query fails on connect rather than after a timeout.
    private const string UnreachableConnection =
        "Host=127.0.0.1;Port=1;Database=catalogue_startup_unreachable;Username=none;Password=none";

    // An anonymous route whose model binding fails before any handler runs: it answers without a
    // database, which is what makes it evidence that the host itself came up.
    private const string DatabaseFreeBadRequestPath = "/api/words/unique/tashkeel?page=abc";

    private static string OwnerEmail => FakeExternalUserProfileSource.EmailFor(OwnerSub);

    [Fact]
    public async Task MigratedDatabase_CarriesNoPermissionRowsBeforeAnyHostBoots()
    {
        await using var database = await LeaseMigratedDatabaseAsync();

        (await ReadPersistedCodesAsync(database.ConnectionString)).Should().BeEmpty();
    }

    [Fact]
    public async Task StartupSync_OnAMigrationsOnlyDatabase_ServesTheCanonicalCatalogueAsAssignable()
    {
        await using var database = await LeaseMigratedDatabaseAsync();
        (await ReadPersistedCodesAsync(database.ConnectionString)).Should().BeEmpty();
        await SeedActiveOwnerAsync(database.ConnectionString);
        await using var factory = BuildFactory(database.ConnectionString, startupSyncEnabled: true);
        using var client = CreateOwnerClient(factory);

        using var response = await client.GetAsync(CataloguePath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        ReadItemCodes(data).Should().Equal(CanonicalCodes);
        data.GetProperty("assignmentReady").GetBoolean().Should().BeTrue();
        (await ReadPersistedCodesAsync(database.ConnectionString)).Should().BeEquivalentTo(CanonicalCodes);
    }

    [Fact]
    public async Task StartupSync_RepeatedOnASecondHostBoot_LeavesTheCatalogueUnchanged()
    {
        await using var database = await LeaseMigratedDatabaseAsync();
        await SeedActiveOwnerAsync(database.ConnectionString);
        await using (var firstFactory = BuildFactory(database.ConnectionString, startupSyncEnabled: true))
        {
            using var firstClient = CreateOwnerClient(firstFactory);
            (await firstClient.GetAsync(CataloguePath)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        await using var secondFactory = BuildFactory(database.ConnectionString, startupSyncEnabled: true);
        using var secondClient = CreateOwnerClient(secondFactory);

        using var response = await secondClient.GetAsync(CataloguePath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        ReadItemCodes(data).Should().Equal(CanonicalCodes);
        data.GetProperty("assignmentReady").GetBoolean().Should().BeTrue();
        (await ReadPersistedCodesAsync(database.ConnectionString)).Should().BeEquivalentTo(CanonicalCodes);
    }

    [Fact]
    public async Task StartupSync_WithAnUnknownDatabaseCode_KeepsItAndStaysAssignable()
    {
        await using var database = await LeaseMigratedDatabaseAsync();
        await SeedActiveOwnerAsync(database.ConnectionString);
        await AddPermissionAsync(database.ConnectionString, "future.example");
        await using var factory = BuildFactory(database.ConnectionString, startupSyncEnabled: true);
        using var client = CreateOwnerClient(factory);

        using var response = await client.GetAsync(CataloguePath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        ReadItemCodes(data).Should().Equal(CanonicalCodes);
        data.GetProperty("assignmentReady").GetBoolean().Should().BeTrue();
        (await ReadPersistedCodesAsync(database.ConnectionString)).Should().Contain("future.example");
    }

    [Fact]
    public async Task DisabledStartupSync_OnAnEmptyTable_ServesTheCanonicalCatalogueAsNotAssignable()
    {
        await using var database = await LeaseMigratedDatabaseAsync();
        await SeedActiveOwnerAsync(database.ConnectionString);
        await using var factory = BuildFactory(database.ConnectionString, startupSyncEnabled: false);
        using var client = CreateOwnerClient(factory);

        using var response = await client.GetAsync(CataloguePath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        ReadItemCodes(data).Should().Equal(CanonicalCodes);
        data.GetProperty("assignmentReady").GetBoolean().Should().BeFalse();
        (await ReadPersistedCodesAsync(database.ConnectionString)).Should().BeEmpty();
    }

    [Fact]
    public async Task Health_WithoutPersistedPermissions_IsDegradedAndStillAnswers200()
    {
        await using var database = await LeaseMigratedDatabaseAsync();
        await using var factory = BuildFactory(database.ConnectionString, startupSyncEnabled: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync(HealthPath);

        // 200, not 503: railway.json gates the deploy on this route, so a missing catalogue must never
        // make the application undeployable.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("degraded");
        ReadCheckStatus(data, HealthCheckName).Should().Be("degraded");
    }

    [Fact]
    public async Task Health_AfterStartupSync_IsHealthy()
    {
        await using var database = await LeaseMigratedDatabaseAsync();
        await using var factory = BuildFactory(database.ConnectionString, startupSyncEnabled: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync(HealthPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("healthy");
        ReadCheckStatus(data, HealthCheckName).Should().Be("healthy");
    }

    [Fact]
    public async Task StartupSync_AgainstAnUnreachableDatabase_StartsDegradedAndStillServes()
    {
        var startupLog = new RecordingLoggerProvider();
        await using var factory = BuildFactory(UnreachableConnection, startupSyncEnabled: true, startupLog);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync(DatabaseFreeBadRequestPath);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        startupLog.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains("starts degraded", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> CanonicalCodes =>
        AbwabPermissionCatalogue.All.Select(permission => permission.Code).ToArray();

    private Task<AccessMigrationDatabase> LeaseMigratedDatabaseAsync()
    {
        return fixture.CreateMigratedDatabaseAsync();
    }

    private static IReadOnlyList<string> ReadItemCodes(JsonElement data)
    {
        return data.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("code").GetString()!)
            .ToArray();
    }

    private static string? ReadCheckStatus(JsonElement data, string checkName)
    {
        return data.GetProperty("checks").EnumerateArray()
            .Single(check => check.GetProperty("name").GetString() == checkName)
            .GetProperty("status").GetString();
    }

    private static async Task<IReadOnlyList<string>> ReadPersistedCodesAsync(string connectionString)
    {
        await using var db = CreateDbContext(connectionString);
        return await db.AccessPermissions.AsNoTracking()
            .OrderBy(permission => permission.DisplayOrder)
            .Select(permission => permission.Code)
            .ToListAsync();
    }

    private static async Task AddPermissionAsync(string connectionString, string code)
    {
        await using var db = CreateDbContext(connectionString);
        db.AccessPermissions.Add(new Permission(code, "مستقبلي", "Future", 100));
        await db.SaveChangesAsync();
    }

    private static async Task SeedActiveOwnerAsync(string connectionString)
    {
        await using var db = CreateDbContext(connectionString);
        var ownerRoleId = await db.AccessRoles
            .Where(role => role.Name == RoleNames.Owner)
            .Select(role => role.Id)
            .SingleAsync();
        db.AccessUsers.Add(new User
        {
            LogtoSub = OwnerSub,
            Email = OwnerEmail,
            NormalizedEmail = OwnerEmail.ToUpperInvariant(),
            RoleId = ownerRoleId,
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private static QuranDashboardDbContext CreateDbContext(string connectionString)
    {
        return new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }

    private static HttpClient CreateOwnerClient(WebApplicationFactory<AccessController> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint(OwnerSub));
        return client;
    }

    private static WebApplicationFactory<AccessController> BuildFactory(
        string connectionString,
        bool startupSyncEnabled,
        ILoggerProvider? startupLog = null)
    {
        return new WebApplicationFactory<AccessController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = connectionString,
                        ["Auth:Authority"] = TestJwtTokens.TestIssuer,
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Auth:InteractiveClientId"] = TestJwtTokens.TestClientId,
                        ["OwnerBootstrap:Emails:0"] = OwnerEmail,
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                        ["Access:PermissionCatalogueStartupSync:Enabled"] =
                            startupSyncEnabled ? "true" : "false",
                    }));

                builder.ConfigureTestServices(services =>
                {
                    if (startupLog is not null)
                    {
                        services.AddSingleton(startupLog);
                    }

                    services.RemoveAll<QuranDashboardDbContext>();
                    services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                    services.AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(connectionString));

                    services.RemoveAll<IExternalUserProfileSource>();
                    services.AddSingleton<IExternalUserProfileSource>(new FakeExternalUserProfileSource());

                    TestJwtTokens.ConfigureOfflineValidation(services);
                });
            });
    }
}
