using System.Text.Json;
using FluentAssertions;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestRuntime;

public sealed class TestRuntimeCommandTests
{
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

    private static string ContractPath
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "QuranDashboard.sln")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull("the tests must run beneath the Backend solution");
            return Path.Combine(directory!.FullName, "testing", "test-database-contract.json");
        }
    }

    private static string[] Split(string migrations) =>
        string.IsNullOrEmpty(migrations) ? [] : migrations.Split(',');
}
