using System.Text.Json;
using FluentAssertions;
using Npgsql;

namespace QuranDashboard.Tests.TestRuntime;

[Collection(nameof(TestRuntimeAdministrationCollection))]
public sealed class TestRuntimeAdministrationTests(TestRuntimeAdministrationFixture fixture)
{
    [Fact]
    public async Task Apply_WithoutExplicitAdministrativeAuthority_IsRejectedWithoutGrantingRoles()
    {
        const string login = "ticket_151_unprivileged_login";
        var password = $"unprivileged-{Guid.NewGuid():N}";
        await using (var administrator = new NpgsqlConnection(fixture.ServerAdministratorConnectionString))
        {
            await administrator.OpenAsync();
            await ExecuteAsync(
                administrator,
                $"CREATE ROLE {login} LOGIN PASSWORD '{password}' NOSUPERUSER NOCREATEROLE NOCREATEDB");
        }

        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Username = login,
            Password = password,
        }.ConnectionString;
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await QuranDashboard.TestRuntime.TestRuntimeCommand.ExecuteAsync(
            ["admin", "apply", "--login", login, "--run-id", "unprivileged-apply"],
            output,
            error,
            name => name == QuranDashboard.TestRuntime.TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? connectionString
                : null);

        exitCode.Should().Be(3);
        output.ToString().Should().NotContain(password);
        using var report = JsonDocument.Parse(output.ToString());
        report.RootElement.GetProperty("violations")
            .EnumerateArray()
            .Select(violation => violation.GetProperty("code").GetString())
            .Should().Contain("administration.authority.insufficient");
    }

    [Fact]
    public async Task Apply_WithoutExactMarkerParameterPrivileges_ReportsEveryMissingGrantBeforeMutation()
    {
        await fixture.SetMarkerParameterPrivilegesAsync(granted: false);
        var roleCountBefore = await CountCapabilityRolesAsync();

        var apply = await RunAsync("apply");

        apply.ExitCode.Should().Be(3, apply.Output);
        var missingParameters = apply.Report.RootElement.GetProperty("violations")
            .EnumerateArray()
            .Where(violation => violation.GetProperty("code").GetString()
                == "administration.authority.parameter-set-missing")
            .Select(violation => violation.GetProperty("subject").GetString())
            .ToArray();
        missingParameters.Should().BeEquivalentTo(
            QuranDashboard.TestRuntime.DatabaseContractReader.Read(ContractPath).Markers.AsDictionary().Values);
        (await CountCapabilityRolesAsync()).Should().Be(roleCountBefore);
    }

    [Fact]
    public async Task AdministrationModes_AsNonSuperuserWithExplicitAuthority_ReconcileIdempotentlyAndEnforcePrivilegeBoundaries()
    {
        await fixture.SetMarkerParameterPrivilegesAsync(granted: true);

        var inspect = await RunAsync("inspect");
        inspect.ExitCode.Should().Be(3);
        inspect.Report.RootElement.GetProperty("administration").GetProperty("compliant").GetBoolean()
            .Should().BeFalse();
        (await CountCapabilityRolesAsync()).Should().Be(0);

        var dryRun = await RunAsync("dry-run");
        dryRun.ExitCode.Should().Be(0);
        dryRun.Report.RootElement.GetProperty("administration").GetProperty("applied").GetBoolean()
            .Should().BeFalse();
        dryRun.Report.RootElement.GetProperty("administration").GetProperty("plannedOperations").GetArrayLength()
            .Should().Be(5);
        (await CountCapabilityRolesAsync()).Should().Be(0);

        var apply = await RunAsync("apply");
        apply.ExitCode.Should().Be(0, apply.Output);
        apply.Report.RootElement.GetProperty("administration").GetProperty("applied").GetBoolean()
            .Should().BeTrue();
        apply.Report.RootElement.GetProperty("administration").GetProperty("compliant").GetBoolean()
            .Should().BeTrue();
        apply.Report.RootElement.GetProperty("advisoryLock").GetProperty("mode").GetString()
            .Should().Be("exclusive");
        apply.Report.RootElement.GetProperty("advisoryLock").GetProperty("status").GetString()
            .Should().Be("acquired");

        var repeatedApply = await RunAsync("apply");
        repeatedApply.ExitCode.Should().Be(0, repeatedApply.Output);
        repeatedApply.Report.RootElement.GetProperty("administration").GetProperty("applied").GetBoolean()
            .Should().BeFalse();

        var verify = await RunAsync("verify");
        verify.ExitCode.Should().Be(0, verify.Output);
        verify.Report.RootElement.GetProperty("violations").GetArrayLength().Should().Be(0);

        await AssertReaderBoundaryAsync();
        await AssertApplicationBoundaryAsync();
        await AssertResetterBoundaryAsync();
        await AssertScratchAdministratorBoundaryAsync();
        await AssertMarkerBoundaryAsync();
        await AssertDevelopmentDatabaseGrantDriftIsRejectedAsync();

        foreach (var run in new[] { inspect, dryRun, apply, repeatedApply, verify })
        {
            run.Output.Should().NotContain(fixture.CredentialSentinel);
        }
    }

    [Fact]
    public async Task DirectCapabilityMutation_WithoutExpectedExclusiveOwner_IsRefusedWithRunnerGuidance()
    {
        await fixture.SetMarkerParameterPrivilegesAsync(granted: true);

        var contract = QuranDashboard.TestRuntime.DatabaseContractReader.Read(ContractPath);
        var validation = QuranDashboard.TestRuntime.DatabaseContractValidator.Validate(contract);
        var target = QuranDashboard.TestRuntime.InspectionTargetValidator.Validate(
            fixture.ConnectionString,
            contract);

        var report = await QuranDashboard.TestRuntime.CapabilityAdministrator.ExecuteAsync(
            contract,
            validation,
            target,
            QuranDashboard.TestRuntime.CapabilityAdministrationMode.Apply,
            fixture.Login,
            null,
            null,
            CancellationToken.None);

        report.Succeeded.Should().BeFalse();
        report.Violations.Should().ContainSingle(violation =>
            violation.Code == "lock.exclusive-ownership.required"
            && violation.Subject == "Use QuranDashboard.TestRuntime admin apply --run-id <run-id>.");
    }

    private async Task AssertReaderBoundaryAsync()
    {
        await using var connection = await OpenAsRoleAsync("quran_dashboard_test_reader");
        await ScalarAsync(connection, "SELECT count(*) FROM public.quran_surahs");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "DELETE FROM public.quran_surahs WHERE false");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "UPDATE public.users SET id = id WHERE false");
    }

    private async Task AssertApplicationBoundaryAsync()
    {
        await using var connection = await OpenAsRoleAsync("quran_dashboard_test_application");
        await ExecuteAsync(connection, "UPDATE public.users SET id = id WHERE false");
        await ExecuteAsync(connection, "DELETE FROM public.users WHERE false");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "DELETE FROM public.quran_surahs WHERE false");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "TRUNCATE public.users");
    }

    private async Task AssertResetterBoundaryAsync()
    {
        await using var connection = await OpenAsRoleAsync("quran_dashboard_test_resetter");
        await ExecuteAsync(connection, "BEGIN");
        try
        {
            await ExecuteAsync(connection, "TRUNCATE public.access_audit_events CONTINUE IDENTITY RESTRICT");
            await ExecuteAsync(connection, "UPDATE public.linking_data_state SET generation = generation WHERE false");
        }
        finally
        {
            await ExecuteAsync(connection, "ROLLBACK");
        }

        await AssertInsufficientPrivilegeAsync(
            connection,
            "TRUNCATE public.roles");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "SELECT count(*) FROM public.quran_surahs");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "DELETE FROM public.users WHERE false");
    }

    private async Task AssertScratchAdministratorBoundaryAsync()
    {
        await using var connection = await OpenAsRoleAsync("quran_dashboard_test_scratch_admin");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "ALTER DATABASE quran_dashboard CONNECTION LIMIT -1");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "ALTER DATABASE quran_dashboard_test CONNECTION LIMIT -1");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "UPDATE public.users SET id = id WHERE false");

        const string scratchDatabase = "quran_test_scratch_administration_verification";
        await ExecuteAsync(connection, $"CREATE DATABASE {scratchDatabase} TEMPLATE template0");
        await ExecuteAsync(connection, "RESET ROLE");
        var owner = (string)(await ScalarAsync(
            connection,
            $"SELECT pg_catalog.pg_get_userbyid(datdba) FROM pg_catalog.pg_database WHERE datname = '{scratchDatabase}'"))!;
        owner.Should().Be("quran_dashboard_test_scratch_admin");
        await ExecuteAsync(connection, $"DROP DATABASE {scratchDatabase}");
    }

    private async Task AssertMarkerBoundaryAsync()
    {
        await using var connection = await OpenAsRoleAsync("quran_dashboard_test_reader");
        var enabled = await ScalarAsync(
            connection,
            "SELECT current_setting('quran_dashboard.test_runtime.enabled')");
        enabled.Should().Be("true");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.enabled TO 'false'");
    }

    private async Task AssertDevelopmentDatabaseGrantDriftIsRejectedAsync()
    {
        var developmentConnectionString = new NpgsqlConnectionStringBuilder(fixture.ServerAdministratorConnectionString)
        {
            Database = "quran_dashboard",
        }.ConnectionString;
        await using (var connection = new NpgsqlConnection(developmentConnectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(
                connection,
                "GRANT UPDATE ON authored.developer_owned_probe TO quran_dashboard_test_scratch_admin");
        }

        var verify = await RunAsync("verify");
        verify.ExitCode.Should().Be(3);
        verify.Report.RootElement.GetProperty("violations")
            .EnumerateArray()
            .Select(violation => violation.GetProperty("code").GetString())
            .Should().Contain("administration.role.development-mutation-privilege");
    }

    private async Task<(int ExitCode, JsonDocument Report, string Output)> RunAsync(string mode)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await QuranDashboard.TestRuntime.TestRuntimeCommand.ExecuteAsync(
            mode == "apply"
                ? ["admin", mode, "--login", fixture.Login, "--run-id", Guid.NewGuid().ToString("N")]
                : ["admin", mode, "--login", fixture.Login, "--contract", ContractPath],
            output,
            error,
            name => name == QuranDashboard.TestRuntime.TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? fixture.ConnectionString
                : null);
        error.ToString().Should().BeEmpty();
        var text = output.ToString();
        return (exitCode, JsonDocument.Parse(text), text);
    }

    private async Task<long> CountCapabilityRolesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        return (long)(await ScalarAsync(
            connection,
            """
            SELECT count(*)
            FROM pg_catalog.pg_roles
            WHERE rolname LIKE 'quran_dashboard_test_%'
            """))!;
    }

    private async Task<NpgsqlConnection> OpenAsRoleAsync(string role)
    {
        var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"SET ROLE {role}");
        return connection;
    }

    private static async Task AssertInsufficientPrivilegeAsync(NpgsqlConnection connection, string sql)
    {
        var operation = async () => await ExecuteAsync(connection, sql);
        await operation.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == PostgresErrorCodes.InsufficientPrivilege);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    private static string ContractPath => TestRuntimeTestPaths.ContractPath;
}
