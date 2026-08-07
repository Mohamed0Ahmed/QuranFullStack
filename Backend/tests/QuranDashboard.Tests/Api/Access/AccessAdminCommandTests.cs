using System.Diagnostics;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.Tests.TestSupport.Process;
using AccessAdminProgram = QuranDashboard.AccessAdmin.Program;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessProcessGlobalCollection))]
public sealed class AccessAdminCommandTests
{
    [Fact]
    public async Task IncompleteIdentityBackfillCommand_ReturnsUsageBeforeConstructingDatabaseServices()
    {
        using var processState = ProcessGlobalStateScope.Enter(captureConsole: true);

        var exitCode = await AccessAdminProgram.Main(["identity", "backfill"]);

        exitCode.Should().Be(2);
        processState.ConsoleOutput.Should().Contain("Usage:");
    }

    [Fact]
    public async Task OwnerValidationCommand_WithNormalizedUniqueEmails_ReportsAwaitingVerifiedSignIn()
    {
        await using var database = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(AccessAdminCommandTests));
        using var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                [AccessAdminConnectionString.EnvironmentVariable] = database.ConnectionString,
                ["OwnerBootstrap__Emails__0"] = " Owner@Example.Test ",
                ["OwnerBootstrap__Emails__1"] = "owner-two@example.test",
            },
            captureConsole: true);

        var exitCode = await AccessAdminProgram.Main(["owners", "validate"]);

        exitCode.Should().Be(3);
        processState.ConsoleOutput.Should().Contain("owner_config_fingerprint=");
        processState.ConsoleOutput.Should().Contain("owner_awaiting_verified_sign_in=2");
        processState.ConsoleOutput.Should().NotContain("Owner@Example.Test");
    }

    [Fact]
    public async Task OwnerValidationCommand_WithAnInvalidOwnerConfiguration_ReturnsControlledOperationalFailure()
    {
        using var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                ["OwnerBootstrap__Emails__0"] = string.Empty,
            },
            captureConsole: true);

        var exitCode = await AccessAdminProgram.Main(["owners", "validate"]);

        exitCode.Should().Be(4);
        processState.ConsoleOutput.Should().Contain("access_admin_failure=OptionsValidationException");
    }

    [Fact]
    public async Task OwnerDryRun_WithAnUnprovisionedConfiguredIdentity_ReportsAwaitingVerifiedSignIn()
    {
        await using var database = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(AccessAdminCommandTests));
        using var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                ["OwnerBootstrap__Emails__0"] = "owner-unresolved@example.test",
            });

        var run = await RunWrapperAsync(database.ConnectionString, "owners", "reconcile", "--dry-run");

        run.ExitCode.Should().Be(3);
        run.Output.Should().Contain("owner_awaiting_verified_sign_in=1");
        run.Output.Should().Contain("owner_ready=false");
        run.Output.Should().Contain("owner_apply_reacquires_lock_and_recomputes=true");

        await using var db = new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(database.ConnectionString)
                .Options);
        (await db.AccessUsers.CountAsync()).Should().Be(0);
        (await db.AccessAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task OwnerApply_InProductionWithoutExplicitConfirmation_ReturnsUsageBeforeConstructingDatabaseServices()
    {
        using var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Production",
                ["OwnerBootstrap__Emails__0"] = "owner@example.test",
                [AccessAdminConnectionString.EnvironmentVariable] = AccessAdminConnectionString.UnreachableDatabase,
            },
            captureConsole: true);

        var exitCode = await AccessAdminProgram.Main(
            ["owners", "reconcile", "--apply", "--reason", "operator confirmation"]);

        exitCode.Should().Be(2);
        processState.ConsoleOutput.Should().Contain("Usage:");
    }

    [Fact]
    public async Task LegacyRoleConvert_InProductionWithoutExplicitConfirmation_ReturnsUsageBeforeConstructingDatabaseServices()
    {
        using var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Production",
                ["OwnerBootstrap__Emails__0"] = "owner@example.test",
                [AccessAdminConnectionString.EnvironmentVariable] = AccessAdminConnectionString.UnreachableDatabase,
            },
            captureConsole: true);

        var exitCode = await AccessAdminProgram.Main(["legacy-roles", "convert", "--apply"]);

        exitCode.Should().Be(2);
        processState.ConsoleOutput.Should().Contain("Usage:");
    }

    [Fact]
    public void CreateHost_LoadsTheToolConfigurationFromItsExecutableDirectory()
    {
        var testDirectory = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), $"access-admin-{Guid.NewGuid():N}"));
        var processState = ProcessGlobalStateScope.Enter(currentDirectory: testDirectory.FullName);

        try
        {
            using var host = AccessAdminProgram.CreateHost([]);

            host.Services.GetRequiredService<IConfiguration>()
                .GetConnectionString("QuranDashboardDb")
                .Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            processState.Dispose();
            testDirectory.Delete();
        }

        processState.RestoreFailures.Should().BeEmpty();
    }

    [Fact]
    public async Task Wrapper_RunsADocumentedCommandWithoutAnExplicitEnvironment()
    {
        await using var database = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(AccessAdminCommandTests));

        var run = await RunWrapperAsync(database.ConnectionString, "identity", "scan");

        run.ExitCode.Should().Be(0);
        run.Output.Should().Contain("users=0");
    }

    [Fact]
    public async Task Wrapper_UnreachableDatabase_PropagatesAControlledOperationalFailure()
    {
        var run = await RunWrapperAsync(
            AccessAdminConnectionString.UnreachableDatabase,
            "authorization",
            "preflight");

        run.ExitCode.Should().Be(4);
        run.Output.Should().Contain("access_admin_failure=");
        run.Output.Should().NotContain("   at ");
    }

    [Fact]
    public async Task OwnerStatus_WhenTheManagementProviderIsUnavailable_ReturnsASafeOperationalFailure()
    {
        await using var database = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(AccessAdminCommandTests));
        await using (var db = new QuranDashboardDbContext(
                         new DbContextOptionsBuilder<QuranDashboardDbContext>()
                             .UseNpgsql(database.ConnectionString)
                             .Options))
        {
            db.AccessUsers.Add(new User
            {
                LogtoSub = "logto-cli-provider-unavailable",
                Email = "owner-cli-provider@example.test",
                NormalizedEmail = "OWNER-CLI-PROVIDER@EXAMPLE.TEST",
                Status = UserStatus.Pending,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        using var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                [AccessAdminConnectionString.EnvironmentVariable] = database.ConnectionString,
                ["OwnerBootstrap__Emails__0"] = "owner-cli-provider@example.test",
                ["Auth__ManagementApi__Endpoint"] = "https://127.0.0.1:1",
                ["Auth__ManagementApi__Resource"] = "https://tenant.example/api",
                ["Auth__ManagementApi__AppId"] = "test-app-id",
                ["Auth__ManagementApi__AppSecret"] = "test-app-secret",
            },
            captureConsole: true);

        var exitCode = await AccessAdminProgram.Main(["owners", "status"]);

        exitCode.Should().Be(4);
        processState.ConsoleOutput.Should().Contain("access_admin_failure=LogtoProviderUnavailable");
        processState.ConsoleOutput.Should().Contain("Logto Management API configuration and availability");
        processState.ConsoleOutput.Should().NotContain("127.0.0.1");
        processState.ConsoleOutput.Should().NotContain("logto-cli-provider-unavailable");
        processState.ConsoleOutput.Should().NotContain("test-app-secret");
        processState.ConsoleOutput.Should().NotContain("   at ");
    }

    private static async Task<ProcessRunResult> RunWrapperAsync(
        string connectionString,
        params string[] args)
    {
        var startInfo = new ProcessStartInfo("bash");
        startInfo.ArgumentList.Add(LocateWrapper());
        foreach (var argument in args)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment[AccessAdminConnectionString.EnvironmentVariable] = connectionString;
        startInfo.Environment.Remove("DOTNET_ENVIRONMENT");

        var run = await ProcessExecution.RunAsync(startInfo);

        run.TimedOut.Should().BeFalse();

        return run;
    }

    private static string LocateWrapper()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var wrapper = Path.Combine(directory.FullName, "scripts", "access-admin");
            if (File.Exists(wrapper))
            {
                return wrapper;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Backend/scripts/access-admin was not found above the test output.");
    }
}
