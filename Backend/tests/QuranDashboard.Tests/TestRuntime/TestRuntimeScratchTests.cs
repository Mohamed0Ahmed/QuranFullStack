using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Npgsql;
using QuranDashboard.TestRuntime;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.Tests.TestSupport.Process;

namespace QuranDashboard.Tests.TestRuntime;

[Collection(nameof(TestRuntimeScratchCollection))]
public sealed class TestRuntimeScratchTests(TestRuntimeScratchFixture fixture)
{
    [Fact]
    public async Task Create_RequiresTheExpectedExclusiveKeeperBeforeCreatingAnything()
    {
        var runId = NewRunId("missing_lock");

        var run = await ExecuteAsync(
            ["scratch", "create", "--run-id", runId, "--command", "scratch-rehearsal", "--subtype", "migration"]);

        run.ExitCode.Should().Be(3, run.Report.ToString());
        run.Report.GetProperty("violations").EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .Should().Contain("scratch.lock.not-owned");
        (await DatabaseExistsAsync(ScratchName(runId))).Should().BeFalse();
    }

    [Theory]
    [InlineData("phrase-search-index-build")]
    [InlineData("recovery")]
    public async Task Create_RejectsAFullOnlyRehearsalSubtypeBeforeCreatingAnything(string subtype)
    {
        var runId = NewRunId("full_only");

        var run = await ExecuteAsync(
            ["scratch", "create", "--run-id", runId, "--command", "scratch-rehearsal", "--subtype", subtype]);

        run.ExitCode.Should().Be(3, run.Report.ToString());
        run.Report.GetProperty("violations").EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .Should().Contain("scratch.subtype.not-approved");
        (await DatabaseExistsAsync(ScratchName(runId))).Should().BeFalse();
    }

    [Fact]
    public async Task Create_UsesTemplate0ExpectedOwnerAndARecordedReceipt_ThenCleanupRequiresEveryIdentity()
    {
        var runId = NewRunId("lifecycle");
        var contract = DatabaseContractReader.Read(ContractPath);
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            runId,
            "scratch-rehearsal",
            TimeSpan.FromSeconds(2));
        acquisition.Lease.Should().NotBeNull();
        await using var lease = acquisition.Lease!;

        try
        {
            var created = await ExecuteAsync(
                ["scratch", "create", "--run-id", runId, "--command", "scratch-rehearsal", "--subtype", "migration"]);

            created.ExitCode.Should().Be(0, created.Report.ToString());
            var scratch = created.Report.GetProperty("scratch");
            scratch.GetProperty("database").GetString().Should().Be(ScratchName(runId));
            scratch.GetProperty("owner").GetString().Should().Be(contract.Roles.ScratchAdministrator);
            scratch.GetProperty("template").GetString().Should().Be("template0");
            scratch.GetProperty("receiptRecorded").GetBoolean().Should().BeTrue();
            (await ReadDatabaseOwnerAsync(ScratchName(runId))).Should().Be(contract.Roles.ScratchAdministrator);
            (await RelationExistsAsync(ScratchName(runId), "__EFMigrationsHistory")).Should().BeFalse();

            var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable] = fixture.ConnectionString,
                [ScratchDatabaseExecutionContext.RunIdEnvironmentVariable] = runId,
                [ScratchDatabaseExecutionContext.CommandEnvironmentVariable] = "scratch-rehearsal",
                [ScratchDatabaseExecutionContext.SubtypeEnvironmentVariable] = "migration",
            };
            var executionContext = await ScratchDatabaseExecutionContext.ResolveAsync(
                ContractPath,
                environment.GetValueOrDefault);
            executionContext.Database.Should().Be(ScratchName(runId));
            new NpgsqlConnectionStringBuilder(executionContext.ConnectionString).Database
                .Should().Be(ScratchName(runId));

            var wrongRun = await ExecuteAsync(
                ["scratch", "cleanup", "--run-id", NewRunId("wrong"), "--command", "scratch-rehearsal"]);

            wrongRun.ExitCode.Should().Be(3);
            wrongRun.Report.GetProperty("violations").EnumerateArray()
                .Select(item => item.GetProperty("code").GetString())
                .Should().Contain("scratch.receipt.missing");
            (await DatabaseExistsAsync(ScratchName(runId))).Should().BeTrue();

            await SetDatabaseMarkerAsync(
                ScratchName(runId),
                contract.Markers.ScratchReceipt,
                new string('0', 64));
            var receiptMismatch = await ExecuteAsync(
                ["scratch", "cleanup", "--run-id", runId, "--command", "scratch-rehearsal"]);

            receiptMismatch.ExitCode.Should().Be(3);
            receiptMismatch.Report.GetProperty("violations").EnumerateArray()
                .Select(item => item.GetProperty("code").GetString())
                .Should().Contain("scratch.receipt.mismatch");
            (await DatabaseExistsAsync(ScratchName(runId))).Should().BeTrue();

            await RestoreReceiptMarkerAsync(runId, contract);
            await ChangeDatabaseOwnerAsync(ScratchName(runId), fixture.Login);
            var ownerMismatch = await ExecuteAsync(
                ["scratch", "cleanup", "--run-id", runId, "--command", "scratch-rehearsal"]);

            ownerMismatch.ExitCode.Should().Be(3);
            ownerMismatch.Report.GetProperty("violations").EnumerateArray()
                .Select(item => item.GetProperty("code").GetString())
                .Should().Contain("scratch.owner.mismatch");
            (await DatabaseExistsAsync(ScratchName(runId))).Should().BeTrue();

            await ChangeDatabaseOwnerAsync(ScratchName(runId), contract.Roles.ScratchAdministrator);
            var cleaned = await ExecuteAsync(
                ["scratch", "cleanup", "--run-id", runId, "--command", "scratch-rehearsal"]);

            cleaned.ExitCode.Should().Be(0);
            cleaned.Report.GetProperty("scratch").GetProperty("removed").GetBoolean().Should().BeTrue();
            (await DatabaseExistsAsync(ScratchName(runId))).Should().BeFalse();
        }
        finally
        {
            if (await DatabaseExistsAsync(ScratchName(runId)))
            {
                await DropDatabaseAsync(ScratchName(runId));
            }
        }
    }

    [Fact]
    public async Task CrashCleanup_RemovesOnlyAStaleScratchWhoseReceiptAndDatabaseMarkersStillMatch()
    {
        var staleRunId = NewRunId("stale");
        var cleanupRunId = NewRunId("reaper");
        var contract = DatabaseContractReader.Read(ContractPath);
        var creationLock = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            staleRunId,
            "scratch-rehearsal",
            TimeSpan.FromSeconds(2));
        creationLock.Lease.Should().NotBeNull();
        await using (creationLock.Lease!)
        {
            var created = await ExecuteAsync(
                ["scratch", "create", "--run-id", staleRunId, "--command", "scratch-rehearsal", "--subtype", "schema-drift"]);
            created.ExitCode.Should().Be(0, created.Report.ToString());
        }

        var cleanupLock = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            cleanupRunId,
            "scratch-rehearsal",
            TimeSpan.FromSeconds(2));
        cleanupLock.Lease.Should().NotBeNull();
        await using var lease = cleanupLock.Lease!;

        var reaped = await ExecuteAsync(
            ["scratch", "reap", "--run-id", cleanupRunId, "--command", "scratch-rehearsal"]);

        reaped.ExitCode.Should().Be(0);
        reaped.Report.GetProperty("scratch").GetProperty("removedDatabases").EnumerateArray()
            .Select(item => item.GetString())
            .Should().Contain(ScratchName(staleRunId));
        (await DatabaseExistsAsync(ScratchName(staleRunId))).Should().BeFalse();
    }

    [Fact]
    public async Task ResolveTarget_RejectsThePersistentTestDatabaseAndNeverEmitsCredentialsOrDumpEvidence()
    {
        const string credential = "scratch-secret-sentinel";
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["scratch", "resolve", "--run-id", "not-recorded", "--command", "scratch-rehearsal", "--subtype", "migration"],
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? $"Host=localhost;Database=quran_dashboard_test;Username=test;Password={credential}"
                : null);

        exitCode.Should().Be(3);
        output.ToString().Should().NotContain(credential).And.NotContain("connectionString");
        using var report = JsonDocument.Parse(output.ToString());
        report.RootElement.GetProperty("violations").EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .Should().Contain("scratch.receipt.missing");
        report.RootElement.GetProperty("scratch").GetProperty("dumpFilesRetained").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task RepositoryRunner_ExecutesOnlyTheMigratedRehearsalClassesInsideOwnedScratchDatabases()
    {
        var repositoryRoot = LocateRepositoryRoot();
        var startInfo = new ProcessStartInfo("node");
        startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, "scripts", "test"));
        startInfo.ArgumentList.Add("focused");
        foreach (var className in new[]
                 {
                     "QuranDashboard.Tests.Api.Access.AccessMigrationPathTests",
                     "QuranDashboard.Tests.Api.Access.AccessSchemaDriftTests",
                     "QuranDashboard.Tests.Api.Access.PermissionCatalogueStartupSyncTests",
                     "QuranDashboard.Tests.Api.Access.PermissionCatalogueSynchronizerTests",
                 })
        {
            startInfo.ArgumentList.Add("--backend-class");
            startInfo.ArgumentList.Add(className);
        }
        startInfo.ArgumentList.Add("--no-build");
        startInfo.WorkingDirectory = repositoryRoot;
        startInfo.Environment[TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable] = fixture.ConnectionString;

        var run = await ProcessExecution.RunAsync(startInfo, TimeSpan.FromMinutes(4));

        run.TimedOut.Should().BeFalse(run.Output);
        run.ExitCode.Should().Be(0, run.Output);
        run.Output.Should().Contain("EmptyScratchDestructiveRehearsal");
        run.Output.Should().Contain("\"dumpFilesRetained\": 0");
        run.Output.Should().NotContain(fixture.CredentialSentinel).And.NotContain("connectionString");
        (await CountScratchDatabasesAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BackendDelegate_RoutesAMigratedScratchClassThroughTheRepositoryRunner()
    {
        var repositoryRoot = LocateRepositoryRoot();
        var startInfo = new ProcessStartInfo(Path.Combine(repositoryRoot, "Backend", "scripts", "test-backend"));
        startInfo.ArgumentList.Add("feature");
        startInfo.ArgumentList.Add("--class");
        startInfo.ArgumentList.Add("QuranDashboard.Tests.Api.Access.AccessMigrationPathTests");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.WorkingDirectory = repositoryRoot;
        startInfo.Environment[TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable] = fixture.ConnectionString;

        var run = await ProcessExecution.RunAsync(startInfo, TimeSpan.FromMinutes(2));

        run.TimedOut.Should().BeFalse(run.Output);
        run.ExitCode.Should().Be(0, run.Output);
        run.Output.Should().Contain("delegating EmptyScratch class through scripts/test");
        run.Output.Should().Contain("EmptyScratchDestructiveRehearsal");
        run.Output.Should().NotContain(fixture.CredentialSentinel).And.NotContain("connectionString");
        (await CountScratchDatabasesAsync()).Should().Be(0);
    }

    private async Task<(int ExitCode, JsonElement Report)> ExecuteAsync(IReadOnlyList<string> args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            args,
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? fixture.ConnectionString
                : null);
        error.ToString().Should().BeEmpty();
        using var document = JsonDocument.Parse(output.ToString());
        return (exitCode, document.RootElement.Clone());
    }

    private async Task<bool> DatabaseExistsAsync(string database)
    {
        await using var connection = await OpenMaintenanceConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname = @database)",
            connection);
        command.Parameters.AddWithValue("database", database);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string?> ReadDatabaseOwnerAsync(string database)
    {
        await using var connection = await OpenMaintenanceConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.pg_get_userbyid(datdba) FROM pg_catalog.pg_database WHERE datname = @database",
            connection);
        command.Parameters.AddWithValue("database", database);
        return (string?)await command.ExecuteScalarAsync();
    }

    private async Task<long> CountScratchDatabasesAsync()
    {
        await using var connection = await OpenMaintenanceConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_catalog.pg_database WHERE datname LIKE 'quran\\_test\\_scratch\\_%' ESCAPE '\\'",
            connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> RelationExistsAsync(string database, string relation)
    {
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = database,
            Pooling = false,
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT to_regclass(@relation) IS NOT NULL", connection);
        command.Parameters.AddWithValue("relation", relation);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task RestoreReceiptMarkerAsync(string runId, DatabaseContract contract)
    {
        var receipt = ScratchDatabaseReceiptStore.Read(runId);
        receipt.Should().NotBeNull();
        await SetDatabaseMarkerAsync(ScratchName(runId), contract.Markers.ScratchReceipt, receipt!.Receipt);
    }

    private async Task SetDatabaseMarkerAsync(string database, string marker, string value)
    {
        await using var connection = await OpenMaintenanceConnectionAsync();
        await using var command = new NpgsqlCommand(
            $"ALTER DATABASE {PostgreSqlIdentifier.Quote(database)} SET {PostgreSqlIdentifier.Quote(marker)} TO '{value}'",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync(string database)
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = await OpenMaintenanceConnectionAsync();
        await using var command = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS {PostgreSqlIdentifier.Quote(database)} WITH (FORCE)",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ChangeDatabaseOwnerAsync(string database, string owner)
    {
        await using var connection = await OpenMaintenanceConnectionAsync();
        await using var command = new NpgsqlCommand(
            $"ALTER DATABASE {PostgreSqlIdentifier.Quote(database)} OWNER TO {PostgreSqlIdentifier.Quote(owner)}",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<NpgsqlConnection> OpenMaintenanceConnectionAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        };
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static string NewRunId(string prefix) => $"{prefix}_{Guid.NewGuid():N}"[..32];

    private static string ScratchName(string runId) => $"quran_test_scratch_{runId}";

    private static string ContractPath => TestRuntimeTestPaths.ContractPath;

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "test")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository-root scripts/test was not found above the test output.");
    }
}

public sealed class TestRuntimeScratchFixture : IAsyncLifetime
{
    private ExclusivePostgreSqlLease? server;

    internal string CredentialSentinel { get; } = $"scratch-runtime-{Guid.NewGuid():N}";

    internal string ConnectionString { get; private set; } = string.Empty;

    internal string Login { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        server = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
            nameof(TestRuntimeScratchFixture),
            "postgres:18-alpine",
            builder => builder.WithPassword(CredentialSentinel));
        var containerConnection = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        };
        Login = containerConnection.Username!;
        await using (var connection = new NpgsqlConnection(containerConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await using var createRole = new NpgsqlCommand(
                "CREATE ROLE quran_dashboard_test_scratch_admin NOLOGIN CREATEDB",
                connection);
            await createRole.ExecuteNonQueryAsync();
            await using var grantRole = new NpgsqlCommand(
                $"GRANT quran_dashboard_test_scratch_admin TO {PostgreSqlIdentifier.Quote(Login)}",
                connection);
            await grantRole.ExecuteNonQueryAsync();
            await using var createDatabase = new NpgsqlCommand(
                "CREATE DATABASE quran_dashboard_test TEMPLATE template0",
                connection);
            await createDatabase.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(containerConnection.ConnectionString)
        {
            Database = "quran_dashboard_test",
        }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (server is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await server.DisposeAsync();
        }
    }
}

[CollectionDefinition(nameof(TestRuntimeScratchCollection), DisableParallelization = true)]
public sealed class TestRuntimeScratchCollection : ICollectionFixture<TestRuntimeScratchFixture>;
