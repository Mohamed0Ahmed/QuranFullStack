using System.Text.Json;
using FluentAssertions;
using Npgsql;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestRuntime;

[Collection(nameof(TestRuntimeRefreshCollection))]
public sealed class TestRuntimeRefreshTests(TestRuntimeRefreshFixture fixture)
{
    [Theory]
    [InlineData("inspect")]
    [InlineData("dry-run")]
    public async Task PlanningModes_ReportTheCompleteMaintenancePathWithoutMutation(string mode)
    {
        var result = await RunAsync(["refresh", mode, "--login", fixture.Login]);

        result.ExitCode.Should().Be(0);
        var refresh = result.Report.RootElement.GetProperty("refresh");
        refresh.GetProperty("applied").GetBoolean().Should().BeFalse();
        refresh.GetProperty("plannedStages").EnumerateArray().Select(item => item.GetString()).Should().ContainInOrder(
            "create-staged-database-from-template0",
            "apply-committed-migrations",
            "import-foundation",
            "build-phrase-index",
            "validate-staged-capability",
            "verify-target-idle",
            "swap-capability-name",
            "validate-installed-capability",
            "remove-replaced-database");
        (await DatabaseExistsAsync("quran_dashboard_test")).Should().BeTrue();
        (await DevelopmentProbeAsync()).Should().Be(152);
    }

    [Fact]
    public async Task Apply_RefusesAnActiveTargetSessionBeforeCreatingAStagedDatabase()
    {
        await using var active = new NpgsqlConnection(fixture.ConnectionString);
        await active.OpenAsync();
        var pipeline = new FakePipeline();

        var result = await RunAsync(
            [
                "refresh", "apply", "--login", fixture.Login, "--run-id", "active-session",
                "--reason", "test-maintenance", "--yes",
            ],
            Dependencies(pipeline, new FakeValidator()));

        result.ExitCode.Should().Be(3, result.Output);
        result.Report.RootElement.GetProperty("violations").EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .Should().Contain("refresh.target.sessions-active");
        result.Report.RootElement.GetProperty("refresh").GetProperty("targetSessions").GetArrayLength().Should().Be(1);
        pipeline.RunCount.Should().Be(0);
        (await DatabaseExistsAsync("quran_dashboard_test_refresh_active-session")).Should().BeFalse();
        (await DevelopmentProbeAsync()).Should().Be(152);
    }

    [Fact]
    public async Task Verify_RejectsAMigratedButUnbuiltCapability()
    {
        var result = await RunAsync(["refresh", "verify", "--login", fixture.Login]);

        result.ExitCode.Should().Be(3, result.Output);
        var violations = result.Report.RootElement.GetProperty("violations").EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .ToArray();
        violations.Should().Contain("refresh.validation.canonical-count");
        violations.Should().Contain("refresh.validation.canonical-empty");
        violations.Should().Contain("refresh.validation.quran-oracle-mismatch");
        violations.Should().Contain("refresh.validation.phrase-search-invariant");
    }

    [Fact]
    public async Task ValidationFingerprint_DetectsSchemaStateChangesIndependentlyOfCanonicalRows()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var contractValidation = DatabaseContractValidator.Validate(contract);
        var validator = new CapabilityRefreshValidator(TestRuntimeTestPaths.RefreshOraclePath);
        var before = await validator.ValidateAsync(
            contract,
            contractValidation,
            fixture.ConnectionString,
            fixture.Login,
            requiredMarkers: null,
            CancellationToken.None);

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await TestRuntimeRefreshFixture.ExecuteAsync(
                connection,
                "CREATE INDEX issue_152_schema_fingerprint_probe ON quran_surahs (surah_number)");
        }

        try
        {
            var after = await validator.ValidateAsync(
                contract,
                contractValidation,
                fixture.ConnectionString,
                fixture.Login,
                requiredMarkers: null,
                CancellationToken.None);

            after.CanonicalQuranFingerprint.Should().Be(before.CanonicalQuranFingerprint);
            after.SystemCatalogueFingerprint.Should().Be(before.SystemCatalogueFingerprint);
            after.SchemaStateFingerprint.Should().NotBe(before.SchemaStateFingerprint);
            after.ProtectedStateFingerprint.Should().NotBe(before.ProtectedStateFingerprint);
        }
        finally
        {
            await using var connection = new NpgsqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();
            await TestRuntimeRefreshFixture.ExecuteAsync(
                connection,
                "DROP INDEX IF EXISTS issue_152_schema_fingerprint_probe");
        }
    }

    [Fact]
    public async Task Apply_BuildsAndInstallsTheStagedCapabilityWithoutReadingTheDevelopmentDatabase()
    {
        var pipeline = new FakePipeline();
        var result = await RunAsync(
            [
                "refresh", "apply", "--login", fixture.Login, "--run-id", "successful-refresh",
                "--reason", "test-maintenance", "--yes",
            ],
            Dependencies(pipeline, new FakeValidator()));

        result.ExitCode.Should().Be(0, result.Output);
        var refresh = result.Report.RootElement.GetProperty("refresh");
        refresh.GetProperty("applied").GetBoolean().Should().BeTrue();
        refresh.GetProperty("replacedDatabaseRemoved").GetBoolean().Should().BeTrue();
        pipeline.RunCount.Should().Be(1);
        (await TargetTableExistsAsync("old_capability_probe")).Should().BeFalse();
        (await DatabaseExistsAsync("quran_dashboard_test_refresh_successful-refresh")).Should().BeFalse();
        (await DatabaseExistsAsync("quran_dashboard_test_replaced_successful-refresh")).Should().BeFalse();
        (await DevelopmentProbeAsync()).Should().Be(152);
    }

    [Fact]
    public async Task Apply_RollsTheNamesBackWhenPostSwapValidationFails()
    {
        await fixture.ResetTargetAsync();
        if (!await TargetTableExistsAsync("old_capability_probe"))
        {
            await using var target = new NpgsqlConnection(fixture.ConnectionString);
            await target.OpenAsync();
            await TestRuntimeRefreshFixture.ExecuteAsync(target, "CREATE TABLE old_capability_probe (id integer PRIMARY KEY)");
        }

        var validator = new RealPostSwapValidator();
        var result = await RunAsync(
            [
                "refresh", "apply", "--login", fixture.Login, "--run-id", "rollback-refresh",
                "--reason", "test-maintenance", "--yes",
            ],
            Dependencies(new FakePipeline(), validator));

        result.ExitCode.Should().Be(3);
        var refresh = result.Report.RootElement.GetProperty("refresh");
        refresh.GetProperty("swapRolledBack").GetBoolean().Should().BeTrue();
        (await TargetTableExistsAsync("old_capability_probe")).Should().BeTrue();
        (await DatabaseExistsAsync("quran_dashboard_test_replaced_rollback-refresh")).Should().BeFalse();
        (await DatabaseExistsAsync("quran_dashboard_test_refresh_rollback-refresh")).Should().BeTrue();
        (await DevelopmentProbeAsync()).Should().Be(152);
    }

    private async Task<(int ExitCode, JsonDocument Report, string Output)> RunAsync(
        IReadOnlyList<string> args,
        CapabilityRefreshDependencies? dependencies = null)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            args,
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? fixture.ConnectionString
                : null,
            refreshDependencies: dependencies);
        error.ToString().Should().BeEmpty();
        var text = output.ToString();
        return (exitCode, JsonDocument.Parse(text), text);
    }

    private static CapabilityRefreshDependencies Dependencies(
        ICanonicalRefreshPipeline pipeline,
        ICapabilityRefreshValidator validator) =>
        new(pipeline, validator, () => new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero));

    private async Task<bool> TargetTableExistsAsync(string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT to_regclass(@table) IS NOT NULL", connection);
        command.Parameters.AddWithValue("table", "public." + table);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<int> DevelopmentProbeAsync()
    {
        var development = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = "quran_dashboard" };
        await using var connection = new NpgsqlConnection(development.ConnectionString);
        await connection.OpenAsync();
        return Convert.ToInt32(await TestRuntimeRefreshFixture.ScalarAsync(
            connection,
            "SELECT id FROM development_probe"));
    }

    private async Task<bool> DatabaseExistsAsync(string database)
    {
        var maintenance = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @database)",
            connection);
        command.Parameters.AddWithValue("database", database);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private sealed class FakePipeline : ICanonicalRefreshPipeline
    {
        public int RunCount { get; private set; }

        public Task<CanonicalPipelinePreparation> PrepareAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new CanonicalPipelinePreparation(true, new string('a', 64), []));

        public Task<IReadOnlyList<CapabilityRefreshStageReport>> RunAsync(
            string connectionString,
            string runId,
            long advisoryLockKey,
            CancellationToken cancellationToken)
        {
            RunCount++;
            IReadOnlyList<CapabilityRefreshStageReport> reports =
            [
                new("import-foundation", "passed"),
                new("rebuild-words", "passed"),
                new("build-phrase-index", "passed"),
                new("import-morphology-enriched", "passed"),
                new("generate-i3rab", "passed"),
                new("import-mutashabihat", "passed"),
                new("import-navigation-metadata", "passed"),
                new("import-full-i3rab", "passed"),
                new("import-tafsirs-curated-10", "passed"),
                new("import-translations-curated-10", "passed"),
            ];
            return Task.FromResult(reports);
        }
    }

    private sealed class FakeValidator(int? failOnCall = null) : ICapabilityRefreshValidator
    {
        private int calls;

        public Task<CapabilityRefreshValidation> ValidateAsync(
            DatabaseContract contract,
            ContractValidationResult contractValidation,
            string connectionString,
            string selectedLogin,
            IReadOnlyDictionary<string, string>? requiredMarkers,
            CancellationToken cancellationToken)
        {
            calls++;
            return Task.FromResult(calls == failOnCall
                ? new CapabilityRefreshValidation(
                    false,
                    new string('b', 64),
                    new string('c', 64),
                    new string('d', 64),
                    new string('e', 64),
                    [new ContractViolation("refresh.validation.synthetic-post-swap")])
                : new CapabilityRefreshValidation(
                    true,
                    new string('b', 64),
                    new string('c', 64),
                    new string('d', 64),
                    new string('e', 64),
                    []));
        }
    }

    private sealed class RealPostSwapValidator : ICapabilityRefreshValidator
    {
        private readonly CapabilityRefreshValidator real = new(TestRuntimeTestPaths.RefreshOraclePath);
        private int calls;

        public Task<CapabilityRefreshValidation> ValidateAsync(
            DatabaseContract contract,
            ContractValidationResult contractValidation,
            string connectionString,
            string selectedLogin,
            IReadOnlyDictionary<string, string>? requiredMarkers,
            CancellationToken cancellationToken)
        {
            calls++;
            return calls < 3
                ? Task.FromResult(new CapabilityRefreshValidation(
                    true,
                    new string('b', 64),
                    new string('c', 64),
                    new string('d', 64),
                    new string('e', 64),
                    []))
                : real.ValidateAsync(
                    contract,
                    contractValidation,
                    connectionString,
                    selectedLogin,
                    requiredMarkers,
                    cancellationToken);
        }
    }
}
