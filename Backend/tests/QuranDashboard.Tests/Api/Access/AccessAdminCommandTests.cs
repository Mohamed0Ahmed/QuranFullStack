using System.Diagnostics;
using AccessAdminProgram = QuranDashboard.AccessAdmin.Program;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessMigrationCollection))]
public sealed class AccessAdminCommandTests(AccessMigrationTestFixture fixture)
{
    [Fact]
    public async Task IncompleteIdentityBackfillCommand_ReturnsUsageBeforeConstructingDatabaseServices()
    {
        var exitCode = await AccessAdminProgram.Main(["identity", "backfill"]);

        exitCode.Should().Be(2);
    }

    [Fact]
    public void CreateHost_LoadsTheToolConfigurationFromItsExecutableDirectory()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var testDirectory = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), $"access-admin-{Guid.NewGuid():N}"));

        try
        {
            Directory.SetCurrentDirectory(testDirectory.FullName);
            using var host = AccessAdminProgram.CreateHost([]);
            var configuration = host.Services.GetRequiredService<IConfiguration>();

            configuration.GetConnectionString("QuranDashboardDb").Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            testDirectory.Delete();
        }
    }

    [Fact]
    public async Task Wrapper_RunsADocumentedCommandWithoutAnExplicitEnvironment()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToHeadAsync(db);

        var run = await RunWrapperAsync(database.ConnectionString, "identity", "scan");

        run.ExitCode.Should().Be(0);
        run.Output.Should().Contain("users=0");
    }

    private static async Task<AccessAdminRun> RunWrapperAsync(
        string connectionString,
        params string[] args)
    {
        var startInfo = new ProcessStartInfo("bash")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(LocateWrapper());
        foreach (var argument in args)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["ConnectionStrings__QuranDashboardDb"] = connectionString;
        startInfo.Environment.Remove("DOTNET_ENVIRONMENT");

        using var process = Process.Start(startInfo)!;
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new AccessAdminRun(process.ExitCode, standardOutput + standardError);
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
