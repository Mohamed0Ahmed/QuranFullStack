using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Npgsql;
using NpgsqlTypes;

namespace QuranDashboard.TestRuntime;

internal sealed record MutableResetReport(
    string Phase,
    string Status,
    int ResetTableCount,
    int EmptyTableCount,
    bool SingletonValid,
    bool SequencesPreserved,
    bool ProtectedStateMatches,
    string? BeforeFingerprint,
    string? AfterFingerprint,
    string ExpectedFingerprint,
    int? ApiPort,
    int? ApiProcessId,
    bool ApiProcessAlive,
    bool ApiPortOpen,
    int ActiveDatabaseConnections,
    bool RecoveryAttempted);

internal static class MutableStateResetter
{
    internal static Task<TestRuntimeReport> ExecuteAsync(
        DatabaseContract contract,
        ContractValidationResult validation,
        InspectionTargetValidation targetValidation,
        TestRuntimeReport inspection,
        string runId,
        string lockCommand,
        string expectedFingerprint,
        int apiPort,
        int? apiProcessId,
        string phase,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreAsync(
            contract,
            validation,
            targetValidation,
            inspection,
            runId,
            lockCommand,
            expectedFingerprint,
            apiPort,
            apiProcessId,
            phase,
            inProcessApiHostWasDisposed: false,
            verifiedCanonicalQuranDataFingerprint: null,
            cancellationToken);

    internal static Task<TestRuntimeReport> ExecuteAfterInProcessApiStoppedAsync(
        DatabaseContract contract,
        ContractValidationResult validation,
        InspectionTargetValidation targetValidation,
        TestRuntimeReport inspection,
        string runId,
        string lockCommand,
        ProtectedStateFingerprintReport verifiedBoundaryFingerprint,
        string phase,
        CancellationToken cancellationToken = default) =>
        // In-process TestServer hosts have no dedicated PID or listening port. Their lifecycle owner
        // must await host disposal first; the reset still rejects every remaining database writer.
        ExecuteCoreAsync(
            contract,
            validation,
            targetValidation,
            inspection,
            runId,
            lockCommand,
            verifiedBoundaryFingerprint.Fingerprint,
            apiPort: null,
            apiProcessId: null,
            phase,
            inProcessApiHostWasDisposed: true,
            verifiedBoundaryFingerprint.Components.CanonicalQuranData,
            cancellationToken);

    private static async Task<TestRuntimeReport> ExecuteCoreAsync(
        DatabaseContract contract,
        ContractValidationResult validation,
        InspectionTargetValidation targetValidation,
        TestRuntimeReport inspection,
        string runId,
        string lockCommand,
        string expectedFingerprint,
        int? apiPort,
        int? apiProcessId,
        string phase,
        bool inProcessApiHostWasDisposed,
        string? verifiedCanonicalQuranDataFingerprint,
        CancellationToken cancellationToken)
    {
        var resetTables = contract.DataClasses.MutableApplicationState
            .Where(table => table != contract.LinkingDataBaseline.Table)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var capabilityWasDirty = IsDirtyCapability(inspection);
        var recoverableDirtyInspection = phase == "initial"
                                         && capabilityWasDirty
                                         && inspection.Violations.All(violation =>
                                             violation.Code == "inspection.markers.invalid");
        if (!inspection.Succeeded && !recoverableDirtyInspection)
        {
            var violations = capabilityWasDirty && phase != "initial"
                ? inspection.Violations
                    .Append(new ContractViolation("mutable-reset.initial-recovery.required"))
                    .ToArray()
                : inspection.Violations;
            return CreateReport(
                contract,
                validation,
                inspection,
                expectedFingerprint,
                apiPort,
                apiProcessId,
                phase,
                "refused",
                resetTables.Length,
                capabilityWasDirty: capabilityWasDirty,
                violations: violations);
        }

        await using var connection = new NpgsqlConnection(targetValidation.Connection!.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var ownsLock = await AdvisoryLockProtocol.VerifyOwnershipAsync(
            connection,
            contract.AdvisoryLock.Key,
            runId,
            lockCommand,
            AdvisoryLockMode.Exclusive,
            cancellationToken);
        if (!ownsLock)
        {
            return CreateReport(
                contract,
                validation,
                inspection,
                expectedFingerprint,
                apiPort,
                apiProcessId,
                phase,
                "refused",
                resetTables.Length,
                violations:
                [
                    new ContractViolation(
                        "lock.exclusive-ownership.required",
                        "Use the supported runner with an exclusive TestRuntime keeper for this run."),
                ]);
        }

        var roleReady = await ResetterRoleIsReadyAsync(
            connection,
            contract,
            resetTables,
            cancellationToken);
        if (!roleReady)
        {
            return CreateReport(
                contract,
                validation,
                inspection,
                expectedFingerprint,
                apiPort,
                apiProcessId,
                phase,
                "refused",
                resetTables.Length,
                capabilityWasDirty: capabilityWasDirty,
                violations: [new ContractViolation("mutable-reset.resetter-role.invalid")]);
        }

        var missingFinalApiStopProof = phase == "final"
                                       && apiProcessId is null
                                       && !inProcessApiHostWasDisposed;
        var processAlive = apiProcessId is not null && IsProcessAlive(apiProcessId.Value);
        var portOpen = apiPort is not null
                       && await IsPortOpenAsync(apiPort.Value, cancellationToken);
        var activeConnections = await CountUnexpectedConnectionsAsync(
            connection,
            contract.AdvisoryLock.Key,
            runId,
            lockCommand,
            cancellationToken);
        if (missingFinalApiStopProof || processAlive || portOpen || activeConnections != 0)
        {
            var violations = new List<ContractViolation>();
            if (missingFinalApiStopProof)
            {
                violations.Add(new ContractViolation("mutable-reset.api-stop-proof.required"));
            }

            if (processAlive)
            {
                violations.Add(new ContractViolation("mutable-reset.api-process-live"));
            }

            if (portOpen)
            {
                violations.Add(new ContractViolation("mutable-reset.api-port-live", apiPort!.Value.ToString()));
            }

            if (activeConnections != 0)
            {
                violations.Add(new ContractViolation(
                    "mutable-reset.database-writer-live",
                    activeConnections.ToString()));
            }

            return CreateReport(
                contract,
                validation,
                inspection,
                expectedFingerprint,
                apiPort,
                apiProcessId,
                phase,
                "refused",
                resetTables.Length,
                apiProcessAlive: processAlive,
                apiPortOpen: portOpen,
                activeDatabaseConnections: activeConnections,
                capabilityWasDirty: capabilityWasDirty,
                violations: violations);
        }

        var before = await ComputeProtectedStateFingerprintAsync(
            connection,
            contract,
            verifiedCanonicalQuranDataFingerprint,
            cancellationToken);
        if (!string.Equals(before.Fingerprint, expectedFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            return CreateReport(
                contract,
                validation,
                inspection,
                expectedFingerprint,
                apiPort,
                apiProcessId,
                phase,
                "protected-corrupt",
                resetTables.Length,
                beforeFingerprint: before.Fingerprint,
                protectedStateMatches: false,
                capabilityWasDirty: capabilityWasDirty,
                violations: [new ContractViolation("protected-state.fingerprint.mismatch")]);
        }

        var finalActiveConnections = await CountUnexpectedConnectionsAsync(
            connection,
            contract.AdvisoryLock.Key,
            runId,
            lockCommand,
            cancellationToken);
        if (finalActiveConnections != 0)
        {
            return CreateReport(
                contract,
                validation,
                inspection,
                expectedFingerprint,
                apiPort,
                apiProcessId,
                phase,
                "refused",
                resetTables.Length,
                activeDatabaseConnections: finalActiveConnections,
                capabilityWasDirty: capabilityWasDirty,
                violations:
                [
                    new ContractViolation(
                        "mutable-reset.database-writer-live",
                        finalActiveConnections.ToString()),
                ]);
        }

        var sequencesBefore = await ReadMutableSequenceValuesAsync(connection, contract, cancellationToken);
        var emptyTableCount = 0;
        var singletonValid = false;
        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await ExecuteAsync(
                connection,
                transaction,
                $"SET LOCAL ROLE {PostgreSqlIdentifier.Quote(contract.Roles.Resetter)}",
                cancellationToken);
            var selectedRole = await ScalarStringAsync(
                connection,
                transaction,
                "SELECT current_user",
                cancellationToken);
            if (selectedRole != contract.Roles.Resetter)
            {
                throw new InvalidOperationException("The resetter role could not be selected.");
            }

            var targets = string.Join(", ", resetTables.Select(table => $"public.{PostgreSqlIdentifier.Quote(table)}"));
            await ExecuteAsync(
                connection,
                transaction,
                $"TRUNCATE TABLE {targets} CONTINUE IDENTITY RESTRICT",
                cancellationToken);

            await using (var baseline = new NpgsqlCommand(
                             $"""
                             UPDATE public.{PostgreSqlIdentifier.Quote(contract.LinkingDataBaseline.Table)}
                             SET generation = @generation,
                                 updated_at_utc = @updated_at_utc
                             WHERE id = @id
                             """,
                             connection,
                             transaction))
            {
                baseline.Parameters.AddWithValue("generation", contract.LinkingDataBaseline.Generation);
                baseline.Parameters.AddWithValue("updated_at_utc", contract.LinkingDataBaseline.UpdatedAtUtc);
                baseline.Parameters.AddWithValue("id", contract.LinkingDataBaseline.Id);
                if (await baseline.ExecuteNonQueryAsync(cancellationToken) != 1)
                {
                    throw new InvalidOperationException("The linking data singleton is missing.");
                }
            }

            foreach (var table in resetTables)
            {
                var count = await ScalarLongAsync(
                    connection,
                    transaction,
                    $"SELECT count(*) FROM public.{PostgreSqlIdentifier.Quote(table)}",
                    cancellationToken);
                if (count != 0)
                {
                    throw new InvalidOperationException("A mutable reset table is not empty.");
                }

                emptyTableCount++;
            }

            singletonValid = await ScalarBoolAsync(
                connection,
                transaction,
                $"""
                SELECT count(*) = 1
                   AND bool_and(id = @id AND generation = @generation AND updated_at_utc = @updated_at_utc)
                FROM public.{PostgreSqlIdentifier.Quote(contract.LinkingDataBaseline.Table)}
                """,
                contract.LinkingDataBaseline,
                cancellationToken);
            if (!singletonValid)
            {
                throw new InvalidOperationException("The linking data singleton is invalid.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is PostgresException or InvalidOperationException)
        {
            var dirtyMarkerUpdated = await SetDirtyMarkerAsync(
                connection,
                contract,
                isDirty: true,
                cancellationToken);
            var violations = new List<ContractViolation>
            {
                new(
                    "mutable-reset.cleanup-failed",
                    exception is PostgresException postgres ? postgres.SqlState : exception.GetType().Name),
            };
            if (!dirtyMarkerUpdated)
            {
                violations.Add(new ContractViolation("mutable-reset.dirty-marker.failed"));
            }

            return CreateReport(
                contract,
                validation,
                inspection,
                expectedFingerprint,
                apiPort,
                apiProcessId,
                phase,
                "dirty",
                resetTables.Length,
                emptyTableCount,
                singletonValid,
                beforeFingerprint: before.Fingerprint,
                protectedStateMatches: true,
                capabilityWasDirty: capabilityWasDirty,
                violations: violations);
        }

        var sequencesAfter = await ReadMutableSequenceValuesAsync(connection, contract, cancellationToken);
        var sequencesPreserved = sequencesBefore.Count == sequencesAfter.Count
                                 && sequencesBefore.All(sequence =>
                                     sequencesAfter.TryGetValue(sequence.Key, out var value)
                                     && value == sequence.Value);
        var after = await ComputeProtectedStateFingerprintAsync(
            connection,
            contract,
            verifiedCanonicalQuranDataFingerprint,
            cancellationToken);
        var protectedStateMatches = string.Equals(
            before.Fingerprint,
            after.Fingerprint,
            StringComparison.Ordinal);
        var violationsAfter = new List<ContractViolation>();
        if (!sequencesPreserved)
        {
            violationsAfter.Add(new ContractViolation("mutable-reset.sequence.changed"));
        }

        if (!protectedStateMatches)
        {
            violationsAfter.Add(new ContractViolation("protected-state.fingerprint.mismatch"));
        }

        var status = !protectedStateMatches
            ? "protected-corrupt"
            : sequencesPreserved
                ? "clean"
                : "dirty";
        var reportInspection = inspection;
        if (status == "dirty")
        {
            var dirtyMarkerUpdated = await SetDirtyMarkerAsync(
                connection,
                contract,
                isDirty: true,
                cancellationToken);
            if (!dirtyMarkerUpdated)
            {
                violationsAfter.Add(new ContractViolation("mutable-reset.dirty-marker.failed"));
            }
        }
        else if (status == "clean" && capabilityWasDirty)
        {
            var dirtyMarkerUpdated = await SetDirtyMarkerAsync(
                connection,
                contract,
                isDirty: false,
                cancellationToken);
            if (!dirtyMarkerUpdated)
            {
                status = "dirty";
                violationsAfter.Add(new ContractViolation("mutable-reset.dirty-marker.failed"));
            }
            else
            {
                reportInspection = await DatabaseInspector.InspectAsync(
                    contract,
                    validation,
                    targetValidation,
                    cancellationToken);
            }
        }

        return CreateReport(
            contract,
            validation,
            reportInspection,
            expectedFingerprint,
            apiPort,
            apiProcessId,
            phase,
            status,
            resetTables.Length,
            emptyTableCount,
            singletonValid,
            sequencesPreserved,
            protectedStateMatches,
            before.Fingerprint,
            after.Fingerprint,
            capabilityWasDirty: capabilityWasDirty,
            violations: violationsAfter);
    }

    private static Task<ProtectedStateFingerprintReport> ComputeProtectedStateFingerprintAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        string? verifiedCanonicalQuranDataFingerprint,
        CancellationToken cancellationToken)
    {
        return verifiedCanonicalQuranDataFingerprint is null
            ? ProtectedStateFingerprint.ComputeAsync(connection, contract, cancellationToken)
            : ProtectedStateFingerprint.ComputeWithVerifiedCanonicalAsync(
                connection,
                contract,
                verifiedCanonicalQuranDataFingerprint,
                cancellationToken);
    }

    private static TestRuntimeReport CreateReport(
        DatabaseContract contract,
        ContractValidationResult validation,
        TestRuntimeReport inspection,
        string expectedFingerprint,
        int? apiPort,
        int? apiProcessId,
        string phase,
        string status,
        int resetTableCount,
        int emptyTableCount = 0,
        bool singletonValid = false,
        bool sequencesPreserved = false,
        bool protectedStateMatches = false,
        string? beforeFingerprint = null,
        string? afterFingerprint = null,
        bool apiProcessAlive = false,
        bool apiPortOpen = false,
        int activeDatabaseConnections = 0,
        bool capabilityWasDirty = false,
        IReadOnlyList<ContractViolation>? violations = null)
    {
        var reportedViolations = violations ?? [];
        return new TestRuntimeReport(
            "reset",
            status == "clean",
            DatabaseInspector.ToContractReport(contract, validation),
            inspection.Target,
            inspection.Migration,
            inspection.Catalogue,
            inspection.Markers,
            inspection.Privileges,
            reportedViolations,
            MutableReset: new MutableResetReport(
                phase,
                status,
                resetTableCount,
                emptyTableCount,
                singletonValid,
                sequencesPreserved,
                protectedStateMatches,
                beforeFingerprint,
                afterFingerprint,
                expectedFingerprint.ToLowerInvariant(),
                apiPort,
                apiProcessId,
                apiProcessAlive,
                apiPortOpen,
                activeDatabaseConnections,
                capabilityWasDirty && phase == "initial"));
    }

    private static async Task<Dictionary<string, SequenceState>> ReadMutableSequenceValuesAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT sequences.sequencename
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
            ORDER BY sequences.sequencename COLLATE "C"
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(
            "tables",
            NpgsqlDbType.Array | NpgsqlDbType.Text,
            contract.DataClasses.MutableApplicationState);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var sequenceNames = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            sequenceNames.Add(reader.GetString(0));
        }

        await reader.DisposeAsync();
        var values = new Dictionary<string, SequenceState>(StringComparer.Ordinal);
        foreach (var sequenceName in sequenceNames)
        {
            await using var stateCommand = new NpgsqlCommand(
                $"SELECT last_value, is_called FROM public.{PostgreSqlIdentifier.Quote(sequenceName)}",
                connection);
            await using var stateReader = await stateCommand.ExecuteReaderAsync(cancellationToken);
            await stateReader.ReadAsync(cancellationToken);
            values[sequenceName] = new SequenceState(
                stateReader.GetInt64(0),
                stateReader.GetBoolean(1));
        }

        return values;
    }

    private static bool IsDirtyCapability(TestRuntimeReport inspection) =>
        inspection.Markers is not null
        && inspection.Markers.States.TryGetValue("mutableStateDirty", out var marker)
        && marker.Present
        && marker.IsDirty is true;

    private static async Task<bool> SetDirtyMarkerAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        bool isDirty,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new NpgsqlCommand(
                $"ALTER DATABASE {PostgreSqlIdentifier.Quote(contract.Targets.TestDatabase)} SET {contract.Markers.MutableStateDirty} TO '{isDirty.ToString().ToLowerInvariant()}'",
                connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (PostgresException)
        {
            return false;
        }
    }

    private static async Task<bool> ResetterRoleIsReadyAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        IReadOnlyCollection<string> resetTables,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = @role AND NOT rolcanlogin)
               AND pg_catalog.pg_has_role(session_user, @role, 'MEMBER')
               AND (
                   SELECT bool_and(pg_catalog.has_table_privilege(
                       @role,
                       pg_catalog.format('%I.%I', 'public', table_name),
                       'SELECT'))
                      AND bool_and(pg_catalog.has_table_privilege(
                       @role,
                       pg_catalog.format('%I.%I', 'public', table_name),
                       'TRUNCATE'))
                   FROM unnest(@reset_tables) AS reset_table(table_name))
               AND pg_catalog.has_table_privilege(
                   @role,
                   pg_catalog.format('%I.%I', 'public', @singleton),
                   'SELECT')
               AND pg_catalog.has_table_privilege(
                   @role,
                   pg_catalog.format('%I.%I', 'public', @singleton),
                   'UPDATE')
               AND NOT pg_catalog.has_table_privilege(
                   @role,
                   pg_catalog.format('%I.%I', 'public', @singleton),
                   'TRUNCATE')
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", contract.Roles.Resetter);
        command.Parameters.AddWithValue(
            "reset_tables",
            NpgsqlDbType.Array | NpgsqlDbType.Text,
            resetTables);
        command.Parameters.AddWithValue("singleton", contract.LinkingDataBaseline.Table);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<int> CountUnexpectedConnectionsAsync(
        NpgsqlConnection connection,
        long lockKey,
        string runId,
        string lockCommand,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT count(*)::integer
            FROM pg_catalog.pg_stat_activity AS activity
            WHERE activity.datname = current_database()
              AND activity.pid <> pg_catalog.pg_backend_pid()
              AND activity.backend_type = 'client backend'
              AND NOT (
                  activity.application_name = @application_name
                  AND EXISTS (
                      SELECT 1
                      FROM pg_catalog.pg_locks AS advisory_lock
                      WHERE advisory_lock.pid = activity.pid
                        AND advisory_lock.locktype = 'advisory'
                        AND advisory_lock.granted
                        AND advisory_lock.objsubid = 1
                        AND advisory_lock.mode = 'ExclusiveLock'
                        AND ((advisory_lock.classid::bigint << 32) | advisory_lock.objid::bigint) = @key))
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("application_name", $"qdtr:{runId}:{lockCommand}");
        command.Parameters.AddWithValue("key", lockKey);
        return (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static async Task<bool> IsPortOpenAsync(int port, CancellationToken cancellationToken)
    {
        return await IsPortOpenAsync(IPAddress.Loopback, port, cancellationToken)
               || await IsPortOpenAsync(IPAddress.IPv6Loopback, port, cancellationToken);
    }

    private static async Task<bool> IsPortOpenAsync(
        IPAddress address,
        int port,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(250));
        using var client = new TcpClient(address.AddressFamily);
        try
        {
            await client.ConnectAsync(address, port, timeout.Token);
            return true;
        }
        catch (Exception exception) when (exception is SocketException
                                          or OperationCanceledException
                                          or NotSupportedException)
        {
            return false;
        }
    }

    private static async Task<bool> ScalarBoolAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        LinkingDataBaseline baseline,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", baseline.Id);
        command.Parameters.AddWithValue("generation", baseline.Generation);
        command.Parameters.AddWithValue("updated_at_utc", baseline.UpdatedAtUtc);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<long> ScalarLongAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<string> ScalarStringAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record SequenceState(long LastValue, bool IsCalled);
}
