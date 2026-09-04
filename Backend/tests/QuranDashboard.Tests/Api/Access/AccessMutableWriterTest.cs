using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Api.Controllers.Access;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;
using QuranDashboard.TestRuntime;
using QuranDashboard.Tests.TestRuntime;
using QuranDashboard.Tests.TestSupport.Access;

namespace QuranDashboard.Tests.Api.Access;

public abstract class AccessMutableWriterTest(AccessTestFixture fixture) : IAsyncLifetime
{
    protected AccessTestFixture Fixture { get; } = fixture;

    public Task InitializeAsync() => Fixture.BeginScenarioAsync();

    public Task DisposeAsync() => Fixture.EndScenarioAsync();
}

public sealed class AccessTestFixture : IAsyncLifetime
{
    private const string LockCommand = "access-mutable";
    private readonly object apiFactoryLock = new();
    private AdvisoryLockLease? lockLease;
    private DatabaseContract? contract;
    private ContractValidationResult? contractValidation;
    private InspectionTargetValidation? targetValidation;
    private WebApplicationFactory<AccessController>? apiFactory;
    private ServiceProvider? queryProvider;
    private string controlConnectionString = string.Empty;
    private string applicationConnectionString = string.Empty;
    private IReadOnlyCollection<DatabaseBackgroundActivity> scenarioBackgroundActivities = [];
    private ProtectedStateFingerprintReport? verifiedBoundaryFingerprint;
    private bool scenarioActive;

    public FakeExternalUserProfileSource ProfileSource { get; } = new();

    private string targetConnectionString = string.Empty;

    public string ApplicationConnectionString => applicationConnectionString;

    public const string OwnerSub = "logto-owner";

    public static string OwnerEmail => FakeExternalUserProfileSource.EmailFor(OwnerSub);

    public const string SecondOwnerSub = "logto-owner-second";

    public static string SecondOwnerEmail => FakeExternalUserProfileSource.EmailFor(SecondOwnerSub);

    private string RunId { get; } = ResolveRunId();

    public async Task InitializeAsync()
    {
        targetConnectionString = Environment.GetEnvironmentVariable(
                TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"{TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable} is required for MutableWriter tests.");

        contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        contractValidation = DatabaseContractValidator.Validate(contract);
        if (!contractValidation.IsValid)
        {
            throw new InvalidOperationException(
                $"The Test Database contract is invalid: {ViolationSummary(contractValidation.Violations)}");
        }

        var controlBuilder = new NpgsqlConnectionStringBuilder(targetConnectionString)
        {
            Pooling = false,
        };
        controlConnectionString = controlBuilder.ConnectionString;
        targetValidation = InspectionTargetValidator.Validate(controlConnectionString, contract);
        if (!targetValidation.IsValid)
        {
            throw new InvalidOperationException(
                $"The MutableWriter target is invalid: {ViolationSummary(targetValidation.Violations)}");
        }

        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            controlConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            RunId,
            LockCommand);
        lockLease = acquisition.Lease ?? throw new InvalidOperationException(
            $"The MutableWriter exclusive lock was not acquired; status={acquisition.Report.Status}.");
        try
        {
            var inspection = await DatabaseInspector.InspectAsync(
                contract,
                contractValidation,
                targetValidation,
                CancellationToken.None);
            if (!inspection.Succeeded || inspection.Catalogue?.Healthy != true)
            {
                throw new InvalidOperationException(
                    $"The Test Database Capability or read-only System Catalogue health check failed: "
                    + ViolationSummary(inspection.Violations));
            }

            await using var fingerprintConnection = new NpgsqlConnection(controlConnectionString);
            await fingerprintConnection.OpenAsync();
            verifiedBoundaryFingerprint = await ProtectedStateFingerprint.ComputeAsync(
                fingerprintConnection,
                contract);

            var applicationBuilder = new NpgsqlConnectionStringBuilder(targetConnectionString)
            {
                ApplicationName = "quran-dashboard-access-tests-mutable",
            };
            var options = string.IsNullOrWhiteSpace(applicationBuilder.Options)
                ? new List<string>()
                : [applicationBuilder.Options];
            options.Add($"-c role={contract.Roles.Application}");
            options.Add("-c default_transaction_read_only=off");
            applicationBuilder.Options = string.Join(' ', options);
            applicationConnectionString = applicationBuilder.ConnectionString;
        }
        catch
        {
            await lockLease.DisposeAsync();
            lockLease = null;
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            await EndScenarioAsync();
            await VerifyFinalProtectedStateAsync();
        }
        finally
        {
            if (lockLease is not null)
            {
                await lockLease.DisposeAsync();
                lockLease = null;
            }
        }
    }

    public async Task BeginScenarioAsync(
        IReadOnlyCollection<DatabaseBackgroundActivity>? backgroundActivities = null)
    {
        if (scenarioActive)
        {
            throw new InvalidOperationException("A MutableWriter scenario is already active.");
        }

        await StopScenarioApiAsync();
        await ResetAfterApiStoppedAsync("initial");
        try
        {
            scenarioBackgroundActivities = backgroundActivities?.ToArray() ?? [];
            ProfileSource.Reset();
            queryProvider = BuildQueryProvider();
            apiFactory = BuildApiFactory(scenarioBackgroundActivities);
            _ = apiFactory.Services;
            scenarioActive = true;
        }
        catch
        {
            await StopScenarioApiAsync();
            await ResetAfterApiStoppedAsync("initial");
            throw;
        }
    }

    public async Task EndScenarioAsync()
    {
        if (!scenarioActive)
        {
            return;
        }

        await StopScenarioApiAsync();
        await ResetAfterApiStoppedAsync("final");
        scenarioActive = false;
    }

    public async Task RestartScenarioAsync()
    {
        var backgroundActivities = scenarioBackgroundActivities;
        await EndScenarioAsync();
        await BeginScenarioAsync(backgroundActivities);
    }

    public HttpClient CreateApiClient() => CreateApiClient(Factory);

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
        return CreateApiFactory(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(AuthorizationPipelineProbeController).Assembly);
            services.AddSingleton<AuthorizationPipelineProbe>();
            configureServices?.Invoke(services);
        });
    }

    public WebApplicationFactory<AccessController> CreateApiFactory(
        Action<IServiceCollection>? configureServices = null)
    {
        return Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => configureServices?.Invoke(services));
        });
    }

    public IServiceProvider ApiServices => Factory.Services;

    // The centralized MutableWriter fixture serves feature tests outside Access as well.
    public IServiceProvider Services => ApiServices;

    public IServiceProvider QueryServices => QueryProvider;

    public async Task<(int UserId, uint Version)> CreateActiveNonOwnerAsync(string logtoSub)
    {
        await using var scope = QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var email = $"{logtoSub}@example.test";
        var user = new User
        {
            LogtoSub = logtoSub,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.AccessUsers.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, user.Version);
    }

    public async Task<IReadOnlyList<User>> GetUsersAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().OrderBy(user => user.Id).ToListAsync();
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessRoles.AsNoTracking().OrderBy(role => role.Id).ToListAsync();
    }

    public async Task VerifyPermissionCatalogueAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var persistedCodes = await db.AccessPermissions.AsNoTracking()
            .Where(permission => permission.RetiredAtUtc == null)
            .OrderBy(permission => permission.DisplayOrder)
            .Select(permission => permission.Code)
            .ToListAsync();

        persistedCodes.Should().Equal(AbwabPermissionCatalogue.All.Select(permission => permission.Code));
    }

    public async Task<string> ComputeProtectedStateFingerprintAsync()
    {
        var activeContract = contract ?? throw new InvalidOperationException("The database contract is unavailable.");
        var boundaryFingerprint = verifiedBoundaryFingerprint
            ?? throw new InvalidOperationException("The verified Protected State boundary is unavailable.");
        await using var connection = new NpgsqlConnection(controlConnectionString);
        await connection.OpenAsync();
        return (await ProtectedStateFingerprint.ComputeWithVerifiedCanonicalAsync(
            connection,
            activeContract,
            boundaryFingerprint.Components.CanonicalQuranData)).Fingerprint;
    }

    public async Task<User?> GetUserBySubAsync(string logtoSub)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().SingleOrDefaultAsync(user => user.LogtoSub == logtoSub);
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

    public async Task RenameUserAsync(int userId, string displayName)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var user = await db.AccessUsers.SingleAsync(candidate => candidate.Id == userId);
        user.DisplayName = displayName;
        await db.SaveChangesAsync();
    }

    public async Task<int> InsertPersonaAsync(string key)
    {
        var persona = TestAccessPersonas.For(key);
        int? roleId = persona.IsOwner
            ? (await GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id
            : null;

        return await InsertUserAsync(persona.BuildUser(roleId));
    }

    private WebApplicationFactory<AccessController> Factory
    {
        get
        {
            lock (apiFactoryLock)
            {
                return apiFactory ?? throw new InvalidOperationException(
                    "The Access MutableWriter API is stopped outside an active scenario.");
            }
        }
    }

    private ServiceProvider QueryProvider => queryProvider
        ?? throw new InvalidOperationException(
            "The Access MutableWriter query provider is stopped outside an active scenario.");

    private WebApplicationFactory<AccessController> BuildApiFactory(
        IReadOnlyCollection<DatabaseBackgroundActivity> backgroundActivities)
    {
        return new WebApplicationFactory<AccessController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:QuranDashboardDb", targetConnectionString);
                builder.UseSetting("Testing:DatabaseActivity:Profile", "Mutable");
                var activityIndex = 0;
                foreach (var activity in backgroundActivities)
                {
                    builder.UseSetting(
                        $"Testing:DatabaseActivity:EnabledBackgroundActivities:{activityIndex++}",
                        activity.ToString());
                }

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = targetConnectionString,
                        ["Testing:DatabaseActivity:Profile"] = "Mutable",
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Auth:InteractiveClientId"] = TestJwtTokens.TestClientId,
                        ["OwnerBootstrap:Emails:0"] = OwnerEmail,
                        ["OwnerBootstrap:Emails:1"] = SecondOwnerEmail,
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                        ["Access:PermissionCatalogueStartupSync:Enabled"] = "false",
                    };
                    activityIndex = 0;
                    foreach (var activity in backgroundActivities)
                    {
                        settings[$"Testing:DatabaseActivity:EnabledBackgroundActivities:{activityIndex++}"] =
                            activity.ToString();
                    }

                    configuration.AddInMemoryCollection(settings);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IExternalUserProfileSource>();
                    services.AddSingleton<IExternalUserProfileSource>(ProfileSource);
                    TestJwtTokens.ConfigureOfflineValidation(services);
                });
            });
    }

    private ServiceProvider BuildQueryProvider()
    {
        return new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(applicationConnectionString))
            .AddSingleton<IEmailIdentityNormalizer, EmailIdentityNormalizer>()
            .BuildServiceProvider();
    }

    private async Task StopScenarioApiAsync()
    {
        if (apiFactory is not null)
        {
            await apiFactory.DisposeAsync();
            apiFactory = null;
        }

        if (queryProvider is not null)
        {
            await queryProvider.DisposeAsync();
            queryProvider = null;
        }

        NpgsqlConnection.ClearAllPools();
    }

    private async Task ResetAfterApiStoppedAsync(string phase)
    {
        // WebApplicationFactory uses an in-process TestServer, so there is no API PID or TCP port to
        // report. The awaited host disposal above plus TestRuntime's zero-writer check is the stop proof.
        var activeContract = contract ?? throw new InvalidOperationException("The database contract is unavailable.");
        var activeValidation = contractValidation
            ?? throw new InvalidOperationException("The database contract validation is unavailable.");
        var activeTarget = targetValidation
            ?? throw new InvalidOperationException("The Test Database target validation is unavailable.");
        var boundaryFingerprint = verifiedBoundaryFingerprint
            ?? throw new InvalidOperationException("The verified Protected State boundary is unavailable.");
        var inspection = await DatabaseInspector.InspectAsync(
            activeContract,
            activeValidation,
            activeTarget,
            CancellationToken.None);
        var report = await MutableStateResetter.ExecuteAfterInProcessApiStoppedAsync(
            activeContract,
            activeValidation,
            activeTarget,
            inspection,
            RunId,
            LockCommand,
            boundaryFingerprint,
            phase);
        if (!report.Succeeded)
        {
            throw new InvalidOperationException(
                $"Access MutableWriter {phase} reset failed: {ViolationSummary(report.Violations)}");
        }
    }

    private async Task VerifyFinalProtectedStateAsync()
    {
        var activeContract = contract ?? throw new InvalidOperationException("The database contract is unavailable.");
        var boundaryFingerprint = verifiedBoundaryFingerprint
            ?? throw new InvalidOperationException("The verified Protected State boundary is unavailable.");
        await using var connection = new NpgsqlConnection(controlConnectionString);
        await connection.OpenAsync();
        var finalFingerprint = await ProtectedStateFingerprint.ComputeAsync(connection, activeContract);
        if (!string.Equals(
                finalFingerprint.Fingerprint,
                boundaryFingerprint.Fingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Protected State changed between the MutableWriter invocation boundaries.");
        }
    }

    private static string ResolveRunId()
    {
        var configured = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_TEST_RUN_ID");
        if (!AdvisoryLockProtocol.IsValidRunId(configured))
        {
            throw new InvalidOperationException(
                "QURAN_DASHBOARD_TEST_RUN_ID is required for MutableWriter tests. "
                + "Use the repository scripts/test runner instead of direct dotnet test execution.");
        }

        return configured!;
    }

    private static string ViolationSummary(IEnumerable<ContractViolation> violations)
    {
        var codes = violations.Select(violation => violation.Code).ToArray();
        return codes.Length == 0 ? "no violation code was reported" : string.Join(", ", codes);
    }

}

[CollectionDefinition(nameof(MutableDatabaseCollection), DisableParallelization = true)]
public sealed class MutableDatabaseCollection : ICollectionFixture<AccessTestFixture>;
