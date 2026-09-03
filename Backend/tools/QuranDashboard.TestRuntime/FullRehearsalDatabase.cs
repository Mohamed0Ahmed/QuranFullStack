using Npgsql;

namespace QuranDashboard.TestRuntime;

internal sealed class FullRehearsalDatabase
{
    internal async Task<FullRehearsalSnapshot> InspectAsync(
        DatabaseContract contract,
        InspectionTargetValidation target,
        string? runId,
        string? command,
        CancellationToken cancellationToken)
    {
        var targetConnection = new NpgsqlConnectionStringBuilder(target.Connection!.ConnectionString)
        {
            Pooling = false,
        };
        await using var connection = new NpgsqlConnection(targetConnection.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string targetSql = """
            SELECT current_database(),
                   (SELECT oid::bigint FROM pg_catalog.pg_database WHERE datname = current_database()),
                   current_setting('server_version_num')::integer / 10000,
                   pg_is_in_recovery()
            """;
        await using var targetCommand = new NpgsqlCommand(targetSql, connection);
        await using var targetReader = await targetCommand.ExecuteReaderAsync(cancellationToken);
        await targetReader.ReadAsync(cancellationToken);
        var database = targetReader.GetString(0);
        var databaseOid = targetReader.GetInt64(1);
        var postgresMajor = targetReader.GetInt32(2);
        var inRecovery = targetReader.GetBoolean(3);
        await targetReader.DisposeAsync();

        var markers = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var marker in contract.Markers.AsDictionary())
        {
            await using var markerCommand = new NpgsqlCommand(
                """
                SELECT substring(configured.setting FROM position('=' IN configured.setting) + 1)
                FROM pg_catalog.pg_db_role_setting AS settings
                INNER JOIN pg_catalog.pg_database AS database ON database.oid = settings.setdatabase
                CROSS JOIN LATERAL unnest(settings.setconfig) AS configured(setting)
                WHERE database.datname = current_database()
                  AND settings.setrole = 0
                  AND split_part(configured.setting, '=', 1) = @marker
                """,
                connection);
            markerCommand.Parameters.AddWithValue("marker", marker.Value);
            markers[marker.Key] = (string?)await markerCommand.ExecuteScalarAsync(cancellationToken);
        }

        string? migrationHead = null;
        await using (var historyCommand = new NpgsqlCommand(
                         "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL",
                         connection))
        {
            if ((bool)(await historyCommand.ExecuteScalarAsync(cancellationToken))!)
            {
                await using var migrationCommand = new NpgsqlCommand(
                    "SELECT max(\"MigrationId\") FROM public.\"__EFMigrationsHistory\"",
                    connection);
                migrationHead = (string?)await migrationCommand.ExecuteScalarAsync(cancellationToken);
            }
        }

        string? computedFingerprint;
        try
        {
            computedFingerprint = (await ProtectedStateFingerprint.ComputeAsync(
                connection,
                contract,
                cancellationToken)).Fingerprint;
        }
        catch (PostgresException)
        {
            computedFingerprint = null;
        }
        var lockOwned = runId is not null
                        && command is not null
                        && await AdvisoryLockProtocol.VerifyOwnershipAsync(
                            connection,
                            contract.AdvisoryLock.Key,
                            runId,
                            command,
                            AdvisoryLockMode.Exclusive,
                            cancellationToken);
        return new FullRehearsalSnapshot(
            database,
            databaseOid,
            target.EndpointKind!,
            postgresMajor,
            inRecovery,
            IsTrue(markers["capabilityEnabled"]),
            IsTrue(markers["resetEnabled"]),
            IsTrue(markers["rehearsalEnabled"]),
            markers["rehearsalSubtype"],
            markers["canonicalPipeline"],
            markers["canonicalInputProvenance"],
            markers["protectedStateFingerprint"],
            computedFingerprint,
            markers["migrationHead"],
            migrationHead,
            DateTimeOffset.TryParse(markers["refreshedAtUtc"], out var provisionedAtUtc)
                ? provisionedAtUtc
                : null,
            lockOwned);
    }

    internal async Task<IReadOnlyList<ContractViolation>> RemoveAsync(
        InspectionTargetValidation target,
        long expectedDatabaseOid,
        CancellationToken cancellationToken)
    {
        var database = target.Database!;
        var builder = new NpgsqlConnectionStringBuilder(target.Connection!.ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        };
        await using var maintenance = new NpgsqlConnection(builder.ConnectionString);
        await maintenance.OpenAsync(cancellationToken);
        await using (var identity = new NpgsqlCommand(
                         "SELECT oid::bigint FROM pg_catalog.pg_database WHERE datname = @database",
                         maintenance))
        {
            identity.Parameters.AddWithValue("database", database);
            var actualOid = await identity.ExecuteScalarAsync(cancellationToken);
            if (actualOid is not long oid || oid != expectedDatabaseOid)
            {
                return [new ContractViolation("rehearsal.cleanup.target-identity-changed")];
            }
        }
        await using (var sessions = new NpgsqlCommand(
                         "SELECT count(*) FROM pg_catalog.pg_stat_activity WHERE datname = @database",
                         maintenance))
        {
            sessions.Parameters.AddWithValue("database", database);
            if ((long)(await sessions.ExecuteScalarAsync(cancellationToken))! != 0)
            {
                return [new ContractViolation("rehearsal.cleanup.target-in-use")];
            }
        }

        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE {PostgreSqlIdentifier.Quote(database)}",
            maintenance);
        await drop.ExecuteNonQueryAsync(cancellationToken);
        return [];
    }

    private static bool IsTrue(string? value) => string.Equals(value, "true", StringComparison.Ordinal);
}
