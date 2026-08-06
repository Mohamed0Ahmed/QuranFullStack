using System.Diagnostics;
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
