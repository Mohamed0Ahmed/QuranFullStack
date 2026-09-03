using System.Text.Json;
using FluentAssertions;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestRuntime;

public sealed class TestRuntimeCommandTests
{
    [Theory]
    [InlineData("inspect")]
    [InlineData("dry-run")]
    [InlineData("apply")]
    [InlineData("verify")]
    public async Task Administration_WithoutExplicitLogin_IsRejectedAsUsage(string mode)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var args = mode == "apply"
            ? new[] { "admin", mode, "--run-id", "missing-login" }
            : ["admin", mode, "--contract", ContractPath];
        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            args,
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().BeEmpty();
        error.ToString().Should().Contain("--login <local-login>");
    }

    [Fact]
    public async Task AdministrationApply_WithoutRunId_IsRejectedWithSupportedRunnerGuidance()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["admin", "apply", "--login", "local-login"],
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().BeEmpty();
        error.ToString().Should().Contain("admin apply --login <local-login> --run-id <run-id>");
    }

    [Theory]
    [InlineData("shared")]
    [InlineData("exclusive")]
    public async Task LockHold_WithoutRunIdentity_IsRejectedAsUsage(string mode)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["lock", "hold", "--mode", mode, "--command", "missing-run"],
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().BeEmpty();
        error.ToString().Should().Contain("lock hold --mode shared|exclusive --run-id <run-id> --command <command>");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("lock")]
    public async Task LockingCommands_CannotOverrideTheCommittedContract(string commandKind)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        string[] command = commandKind == "admin"
            ? ["admin", "apply", "--login", "local-login", "--run-id", "run-id"]
            : ["lock", "hold", "--mode", "shared", "--run-id", "run-id", "--command", "reader"];

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            [.. command, "--contract", ContractPath],
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().BeEmpty();
        error.ToString().Should().Contain("Usage:");
    }

    [Fact]
    public async Task Reset_CannotOverrideTheCommittedContract()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            [
                "reset",
                "--run-id", "run-id",
                "--command", "mutable-reset",
                "--expected-fingerprint", new string('0', 64),
                "--api-port", "5014",
                "--api-process-id", "none",
                "--phase", "initial",
                "--contract", ContractPath,
            ],
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().BeEmpty();
        error.ToString().Should().Contain("reset --run-id <run-id>");
    }

    [Fact]
    public async Task Reset_WithDevelopmentDatabaseTargetRefusesBeforeConnectingAndSanitizesTheReport()
    {
        const string credential = "do-not-report";
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            [
                "reset",
                "--run-id", "run-id",
                "--command", "mutable-reset",
                "--expected-fingerprint", new string('0', 64),
                "--api-port", "5014",
                "--api-process-id", "none",
                "--phase", "initial",
            ],
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? $"Host=localhost;Database=quran_dashboard;Username=test;Password={credential}"
                : null);

        exitCode.Should().Be(3);
        output.ToString().Should().NotContain(credential);
        using var report = JsonDocument.Parse(output.ToString());
        report.RootElement.GetProperty("violations")
            .EnumerateArray()
            .Select(violation => violation.GetProperty("code").GetString())
            .Should().Contain("target.development-database");
    }

    [Fact]
    public async Task Reset_FinalPhaseRequiresThePriorApiProcessIdentity()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            [
                "reset",
                "--run-id", "run-id",
                "--command", "mutable-reset",
                "--expected-fingerprint", new string('0', 64),
                "--api-port", "5014",
                "--api-process-id", "none",
                "--phase", "final",
            ],
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().BeEmpty();
        error.ToString().Should().Contain("--api-process-id <pid|none>");
    }

    [Fact]
    public async Task LockHold_WhenLocalDatabaseIsUnavailable_ReturnsLockSpecificSanitizedFailure()
    {
        const string credential = "do-not-report";
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["lock", "hold", "--mode", "shared", "--run-id", "unavailable", "--command", "reader"],
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? $"Host=127.0.0.1;Port=1;Database=quran_dashboard_test;Username=test;Password={credential};Timeout=1"
                : null);

        exitCode.Should().Be(4);
        output.ToString().Should().Contain("lock.database-unavailable").And.NotContain(credential);
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task ContractValidate_WithCommittedContract_ClassifiesTheCurrentEfModelExactlyOnce()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["contract", "validate", "--contract", ContractPath],
            output,
            error);

        exitCode.Should().Be(0, error.ToString());
        using var report = JsonDocument.Parse(output.ToString());
        report.RootElement.GetProperty("succeeded").GetBoolean().Should().BeTrue();
        report.RootElement.GetProperty("contract").GetProperty("mappedTableCount").GetInt32().Should().Be(77);
        report.RootElement.GetProperty("contract").GetProperty("schemaTableCount").GetInt32().Should().Be(1);
        report.RootElement.GetProperty("violations").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ContractValidate_WithDuplicateAndUnclassifiedTables_ReturnsStructuredDrift()
    {
        var contract = await File.ReadAllTextAsync(ContractPath);
        contract = contract
            .Replace("\"roles\",\n      \"permissions\"", "\"roles\",\n      \"quran_surahs\",\n      \"permissions\"")
            .Replace("      \"users\",\n", string.Empty);
        var contractPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(contractPath, contract);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await TestRuntimeCommand.ExecuteAsync(
                ["contract", "validate", "--contract", contractPath],
                output,
                error);

            exitCode.Should().Be(3);
            using var report = JsonDocument.Parse(output.ToString());
            var codes = report.RootElement.GetProperty("violations")
                .EnumerateArray()
                .Select(item => item.GetProperty("code").GetString())
                .ToArray();
            codes.Should().Contain("contract.table.duplicate");
            codes.Should().Contain("contract.model.unclassified-table");
        }
        finally
        {
            File.Delete(contractPath);
        }
    }

    [Fact]
    public async Task ContractValidate_WithMalformedContract_ReturnsASanitizedStructuredReport()
    {
        var contractPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(contractPath, "{ \"contractVersion\": 1, \"dataClasses\": null }");

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await TestRuntimeCommand.ExecuteAsync(
                ["contract", "validate", "--contract", contractPath],
                output,
                error);

            exitCode.Should().Be(3);
            using var report = JsonDocument.Parse(output.ToString());
            report.RootElement.GetProperty("violations")[0].GetProperty("code").GetString()
                .Should().Be("contract.malformed");
            output.ToString().Should().NotContain(contractPath);
            error.ToString().Should().BeEmpty();
        }
        finally
        {
            File.Delete(contractPath);
        }
    }

    [Theory]
    [InlineData("Host=localhost;Database=quran_dashboard;Username=test;Password=do-not-report", "target.development-database")]
    [InlineData("Host=database.example.com;Database=quran_dashboard_test;Username=test;Password=do-not-report", "target.remote")]
    [InlineData("Host=localhost;Database=unknown_database;Username=test;Password=do-not-report", "target.unknown-database")]
    public async Task Inspect_WithUnsafeTarget_RefusesBeforeConnectingAndSanitizesTheReport(
        string connectionString,
        string expectedCode)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["inspect", "--contract", ContractPath],
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? connectionString
                : null);

        exitCode.Should().Be(3);
        output.ToString().Should().NotContain("do-not-report");
        error.ToString().Should().NotContain("do-not-report");
        using var report = JsonDocument.Parse(output.ToString());
        report.RootElement.GetProperty("violations")
            .EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .Should().Contain(expectedCode);
    }

    [Theory]
    [InlineData("001,002", "001,002", "current")]
    [InlineData("001,002", "", "empty")]
    [InlineData("001,002", "002", "pending")]
    [InlineData("001,002", "000,002", "unknown")]
    public void MigrationState_RequiresTheCompleteKnownMigrationSet(
        string expected,
        string applied,
        string state)
    {
        DatabaseInspector.ClassifyMigrationState(Split(expected), Split(applied)).Should().Be(state);
    }

    private static string ContractPath => TestRuntimeTestPaths.ContractPath;

    private static string[] Split(string migrations) =>
        string.IsNullOrEmpty(migrations) ? [] : migrations.Split(',');
}
