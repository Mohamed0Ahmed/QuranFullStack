using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using FluentAssertions;
using Npgsql;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestRuntime;

[Collection(nameof(TestRuntimeResetCollection))]
public sealed class TestRuntimeMutableResetTests(TestRuntimeResetFixture fixture)
{
    [Fact]
    public async Task Reset_ClearsOnlyTheMutableAllowlistAndPreservesSequencesAndProtectedState()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedFingerprint = await FingerprintAsync();
        var runId = $"reset-{Guid.NewGuid():N}"[..32];
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            runId,
            "mutable-reset",
            TimeSpan.FromSeconds(2));
        acquisition.Lease.Should().NotBeNull();
        await using var lease = acquisition.Lease!;

        await SeedMutableFamiliesAsync(contract);
        var sequencesBefore = await ReadMutableSequenceValuesAsync(contract);
        var apiPort = ReserveUnusedPort();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            [
                "reset",
                "--run-id", runId,
                "--command", "mutable-reset",
                "--expected-fingerprint", expectedFingerprint,
                "--api-port", apiPort.ToString(),
                "--api-process-id", "none",
                "--phase", "final",
            ],
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? fixture.ConnectionString
                : null);

        exitCode.Should().Be(0, $"stderr: {error}{Environment.NewLine}stdout: {output}");
        error.ToString().Should().BeEmpty();
        using var report = JsonDocument.Parse(output.ToString());
        var reset = report.RootElement.GetProperty("mutableReset");
        reset.GetProperty("status").GetString().Should().Be("clean");
        reset.GetProperty("phase").GetString().Should().Be("final");
        reset.GetProperty("resetTableCount").GetInt32().Should().Be(35);
        reset.GetProperty("emptyTableCount").GetInt32().Should().Be(35);
        reset.GetProperty("singletonValid").GetBoolean().Should().BeTrue();
        reset.GetProperty("sequencesPreserved").GetBoolean().Should().BeTrue();
        reset.GetProperty("protectedStateMatches").GetBoolean().Should().BeTrue();
        reset.GetProperty("beforeFingerprint").GetString().Should().Be(expectedFingerprint);
        reset.GetProperty("afterFingerprint").GetString().Should().Be(expectedFingerprint);

        await AssertMutableBaselineAsync(contract);
        (await ReadMutableSequenceValuesAsync(contract)).Should().BeEquivalentTo(sequencesBefore);
        (await FingerprintAsync()).Should().Be(expectedFingerprint);
    }

    [Fact]
    public async Task Reset_WithProtectedStateMismatchRefusesMutationUntilAVerifiedInitialReset()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedFingerprint = await FingerprintAsync();
        var runId = $"mismatch-{Guid.NewGuid():N}"[..32];
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            runId,
            "mutable-reset",
            TimeSpan.FromSeconds(2));
        acquisition.Lease.Should().NotBeNull();
        await using var lease = acquisition.Lease!;
        await SeedMutableFamiliesAsync(contract);

        var refused = await RunResetAsync(runId, new string('0', 64), "final");

        refused.ExitCode.Should().Be(3, refused.Output);
        refused.Reset.GetProperty("status").GetString().Should().Be("protected-corrupt");
        refused.Reset.GetProperty("beforeFingerprint").GetString().Should().Be(expectedFingerprint);
        refused.Reset.GetProperty("afterFingerprint").ValueKind.Should().Be(JsonValueKind.Null);
        (await CountAsync("users")).Should().NotBe(0);

        var recovered = await RunResetAsync(runId, expectedFingerprint, "initial");
        recovered.ExitCode.Should().Be(0, recovered.Output);
        recovered.Reset.GetProperty("status").GetString().Should().Be("clean");
        recovered.Reset.GetProperty("recoveryAttempted").GetBoolean().Should().BeFalse();
        await AssertMutableBaselineAsync(contract);
    }

    [Fact]
    public async Task Reset_RefusesALiveApiProcessPortOrDatabaseWriter()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedFingerprint = await FingerprintAsync();
        var runId = $"live-api-{Guid.NewGuid():N}"[..32];
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            runId,
            "mutable-reset",
            TimeSpan.FromSeconds(2));
        acquisition.Lease.Should().NotBeNull();
        await using var lease = acquisition.Lease!;
        await SeedMutableFamiliesAsync(contract);

        var listener = new TcpListener(IPAddress.IPv6Loopback, 0);
        listener.Start();
        var apiPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        var writerConnectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            ApplicationName = "QuranDashboard.Api:mutable",
            Pooling = false,
        }.ConnectionString;
        await using var writer = new NpgsqlConnection(writerConnectionString);
        await writer.OpenAsync();
        try
        {
            var refused = await RunResetAsync(
                runId,
                expectedFingerprint,
                "final",
                apiPort,
                Environment.ProcessId);

            refused.ExitCode.Should().Be(3, refused.Output);
            refused.Reset.GetProperty("status").GetString().Should().Be("refused");
            refused.Reset.GetProperty("apiProcessAlive").GetBoolean().Should().BeTrue();
            refused.Reset.GetProperty("apiPortOpen").GetBoolean().Should().BeTrue();
            refused.Reset.GetProperty("activeDatabaseConnections").GetInt32().Should().BeGreaterThan(0);
            (await CountAsync("users")).Should().NotBe(0);
        }
        finally
        {
            await writer.CloseAsync();
            listener.Stop();
        }

        var recovered = await RunResetAsync(runId, expectedFingerprint, "initial");
        recovered.ExitCode.Should().Be(0, recovered.Output);
        await AssertMutableBaselineAsync(contract);
    }

    [Fact]
    public async Task Reset_WithoutTheExpectedExclusiveKeeperIsRefused()
    {
        var expectedFingerprint = await FingerprintAsync();

        var refused = await RunResetAsync("missing-keeper", expectedFingerprint, "initial");

        refused.ExitCode.Should().Be(3, refused.Output);
        refused.Reset.GetProperty("status").GetString().Should().Be("refused");
        refused.ViolationCodes.Should().Contain("lock.exclusive-ownership.required");
    }

    [Fact]
    public async Task Reset_WithCapabilityMarkerDriftRefusesBeforeMutation()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedFingerprint = await FingerprintAsync();
        var runId = $"marker-{Guid.NewGuid():N}"[..32];
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            runId,
            "mutable-reset",
            TimeSpan.FromSeconds(2));
        acquisition.Lease.Should().NotBeNull();
        await using var lease = acquisition.Lease!;
        await SeedMutableFamiliesAsync(contract);

        await ExecuteAsync(
            "ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.reset_enabled TO 'false'");
        try
        {
            var refused = await RunResetAsync(runId, expectedFingerprint, "final");

            refused.ExitCode.Should().Be(3, refused.Output);
            refused.Reset.GetProperty("status").GetString().Should().Be("refused");
            refused.ViolationCodes.Should().Contain("inspection.markers.invalid");
            (await CountAsync("users")).Should().NotBe(0);
        }
        finally
        {
            await ExecuteAsync(
                "ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.reset_enabled TO 'true'");
        }

        var recovered = await RunResetAsync(runId, expectedFingerprint, "initial");
        recovered.ExitCode.Should().Be(0, recovered.Output);
        await AssertMutableBaselineAsync(contract);
    }

    [Fact]
    public async Task Reset_WithInvalidResetterPrivilegesRefusesBeforeMutation()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedFingerprint = await FingerprintAsync();
        var runId = $"role-{Guid.NewGuid():N}"[..32];
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            runId,
            "mutable-reset",
            TimeSpan.FromSeconds(2));
        acquisition.Lease.Should().NotBeNull();
        await using var lease = acquisition.Lease!;
        await SeedMutableFamiliesAsync(contract);

        await ExecuteAsync($"REVOKE TRUNCATE ON TABLE public.users FROM {contract.Roles.Resetter}");
        try
        {
            var refused = await RunResetAsync(runId, expectedFingerprint, "final");

            refused.ExitCode.Should().Be(3, refused.Output);
            refused.Reset.GetProperty("status").GetString().Should().Be("refused");
            refused.ViolationCodes.Should().Contain("mutable-reset.resetter-role.invalid");
            (await CountAsync("users")).Should().NotBe(0);
        }
        finally
        {
            await ExecuteAsync($"GRANT TRUNCATE ON TABLE public.users TO {contract.Roles.Resetter}");
        }

        var recovered = await RunResetAsync(runId, expectedFingerprint, "initial");
        recovered.ExitCode.Should().Be(0, recovered.Output);
        await AssertMutableBaselineAsync(contract);
    }

    [Fact]
    public async Task Reset_CleanupFailureReportsDirtyAndOnlyVerifiedInitialResetRecovers()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var runId = $"dirty-{Guid.NewGuid():N}"[..32];
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            runId,
            "mutable-reset",
            TimeSpan.FromSeconds(2));
        acquisition.Lease.Should().NotBeNull();
        await using var lease = acquisition.Lease!;
        await SeedMutableFamiliesAsync(contract);

        await ExecuteAsync(
            """
            CREATE FUNCTION public.ticket_154_reject_truncate() RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                RAISE EXCEPTION 'ticket 154 reset failure probe';
            END
            $function$;

            CREATE TRIGGER ticket_154_reject_truncate
            BEFORE TRUNCATE ON public.users
            FOR EACH STATEMENT EXECUTE FUNCTION public.ticket_154_reject_truncate();
            """);
        var failureStateFingerprint = await FingerprintAsync();
        try
        {
            var dirty = await RunResetAsync(runId, failureStateFingerprint, "final");

            dirty.ExitCode.Should().Be(3, dirty.Output);
            dirty.Reset.GetProperty("status").GetString().Should().Be("dirty");
            dirty.ViolationCodes.Should().Contain("mutable-reset.cleanup-failed");
            (await CountAsync("users")).Should().NotBe(0);
        }
        finally
        {
            await ExecuteAsync(
                """
                DROP TRIGGER IF EXISTS ticket_154_reject_truncate ON public.users;
                DROP FUNCTION IF EXISTS public.ticket_154_reject_truncate();
                """);
        }

        var expectedFingerprint = await FingerprintAsync();
        var finalRetry = await RunResetAsync(runId, expectedFingerprint, "final");
        finalRetry.ExitCode.Should().Be(3, finalRetry.Output);
        finalRetry.Reset.GetProperty("status").GetString().Should().Be("refused");
        finalRetry.ViolationCodes.Should().Contain("mutable-reset.initial-recovery.required");

        var recovered = await RunResetAsync(runId, expectedFingerprint, "initial");
        recovered.ExitCode.Should().Be(0, recovered.Output);
        recovered.Reset.GetProperty("status").GetString().Should().Be("clean");
        recovered.Reset.GetProperty("recoveryAttempted").GetBoolean().Should().BeTrue();
        await AssertMutableBaselineAsync(contract);
    }

    private async Task SeedMutableFamiliesAsync(DatabaseContract contract)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"SET ROLE {contract.Roles.Application}");
        await ExecuteAsync(
            connection,
            """
            WITH inserted_user AS (
                INSERT INTO public.users
                    (logto_sub, email, normalized_email, user_name, display_name, role_id, status, created_at, updated_at)
                VALUES ('ticket-154', 'ticket-154@example.test', 'TICKET-154@EXAMPLE.TEST', 'ticket-154', 'Ticket 154', 1, 0, now(), now())
                RETURNING id
            ), inserted_section AS (
                INSERT INTO public.abwab_sections (name, order_value, created_at, updated_at)
                VALUES ('Ticket 154', 154, now(), now())
                RETURNING id
            )
            INSERT INTO public.linking_workspaces (user_id, created_by, updated_by, created_at, updated_at)
            SELECT id, id, id, now(), now() FROM inserted_user;

            UPDATE public.linking_data_state SET generation = 154, updated_at_utc = now() WHERE id = 1;
            """);
        await ExecuteAsync(connection, "RESET ROLE");
    }

    private async Task AssertMutableBaselineAsync(DatabaseContract contract)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        foreach (var table in contract.DataClasses.MutableApplicationState
                     .Where(table => table != contract.LinkingDataBaseline.Table))
        {
            var count = (long)(await ScalarAsync(connection, $"SELECT count(*) FROM public.\"{table}\""))!;
            count.Should().Be(0, $"{table} is in the mutable reset allowlist");
        }

        await using var singleton = new NpgsqlCommand(
            "SELECT id, generation, updated_at_utc FROM public.linking_data_state",
            connection);
        await using var reader = await singleton.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetInt16(0).Should().Be(1);
        reader.GetInt64(1).Should().Be(1);
        reader.GetFieldValue<DateTimeOffset>(2).Should().Be(DateTimeOffset.UnixEpoch);
        (await reader.ReadAsync()).Should().BeFalse();
    }

    private async Task<Dictionary<string, long?>> ReadMutableSequenceValuesAsync(DatabaseContract contract)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT sequences.sequencename, sequences.last_value
            FROM pg_catalog.pg_sequences AS sequences
            INNER JOIN pg_catalog.pg_class AS sequence_relation ON sequence_relation.relname = sequences.sequencename
            INNER JOIN pg_catalog.pg_namespace AS sequence_namespace
              ON sequence_namespace.oid = sequence_relation.relnamespace
             AND sequence_namespace.nspname = sequences.schemaname
            INNER JOIN pg_catalog.pg_depend AS dependency
              ON dependency.classid = 'pg_catalog.pg_class'::pg_catalog.regclass
             AND dependency.objid = sequence_relation.oid
             AND dependency.refclassid = 'pg_catalog.pg_class'::pg_catalog.regclass
             AND dependency.deptype IN ('a', 'i')
            INNER JOIN pg_catalog.pg_class AS owned_relation ON owned_relation.oid = dependency.refobjid
            WHERE sequences.schemaname = 'public'
              AND owned_relation.relname = ANY(@tables)
            ORDER BY sequences.sequencename
            """,
            connection);
        command.Parameters.AddWithValue("tables", contract.DataClasses.MutableApplicationState);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new Dictionary<string, long?>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            values[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetInt64(1);
        }

        return values;
    }

    private async Task<string> FingerprintAsync()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["fingerprint", "--contract", TestRuntimeTestPaths.ContractPath],
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? fixture.ConnectionString
                : null);
        exitCode.Should().Be(0, $"stderr: {error}{Environment.NewLine}stdout: {output}");
        using var report = JsonDocument.Parse(output.ToString());
        return report.RootElement.GetProperty("protectedStateFingerprint").GetProperty("fingerprint").GetString()!;
    }

    private async Task<ResetCommandResult> RunResetAsync(
        string runId,
        string expectedFingerprint,
        string phase,
        int? apiPort = null,
        int? apiProcessId = null)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var arguments = new List<string>
        {
            "reset",
            "--run-id", runId,
            "--command", "mutable-reset",
            "--expected-fingerprint", expectedFingerprint,
            "--api-port", (apiPort ?? ReserveUnusedPort()).ToString(),
            "--api-process-id", apiProcessId?.ToString() ?? "none",
            "--phase", phase,
        };

        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            arguments,
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? fixture.ConnectionString
                : null);
        error.ToString().Should().BeEmpty();
        var text = output.ToString();
        using var document = JsonDocument.Parse(text);
        var reset = document.RootElement.GetProperty("mutableReset").Clone();
        var violations = document.RootElement.GetProperty("violations")
            .EnumerateArray()
            .Select(violation => violation.GetProperty("code").GetString()!)
            .ToArray();
        return new ResetCommandResult(exitCode, reset, violations, text);
    }

    private async Task<long> CountAsync(string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        return (long)(await ScalarAsync(connection, $"SELECT count(*) FROM public.\"{table}\""))!;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, sql);
    }

    private static int ReserveUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
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

    private sealed record ResetCommandResult(
        int ExitCode,
        JsonElement Reset,
        IReadOnlyList<string> ViolationCodes,
        string Output);
}
