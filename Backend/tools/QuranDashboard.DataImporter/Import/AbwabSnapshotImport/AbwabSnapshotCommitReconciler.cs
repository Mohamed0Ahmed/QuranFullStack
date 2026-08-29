using System.Data;
using Npgsql;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal sealed class AbwabSnapshotCommitReconciler
{
    internal async Task<AbwabSnapshotImportExecution> ReconcileAsync(
        string connectionString,
        AbwabSnapshotDocument snapshot,
        string compiledMigrationHead,
        Exception commitException)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(CancellationToken.None);
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.RepeatableRead,
                CancellationToken.None);
            await ExecuteReadOnlyAsync(connection, transaction);

            try
            {
                var exactResult = await AbwabSnapshotImportTargetVerifier.ValidateAfterImportAsync(
                    connection,
                    transaction,
                    snapshot,
                    compiledMigrationHead,
                    CancellationToken.None);
                if (await AbwabSnapshotExactRowVerifier.MatchesAsync(
                        connection,
                        transaction,
                        snapshot,
                        CancellationToken.None))
                {
                    await transaction.CommitAsync(CancellationToken.None);
                    return new AbwabSnapshotImportExecution(
                        AbwabSnapshotImportContract.PassVerdict,
                        AbwabSnapshotImportContract.PersistedTrue,
                        exactResult,
                        ["commit-ack-failure-reconciled-exact"],
                        [$"COMMIT-ACK-FAILED: {commitException.GetType().Name}; fresh read-only reconciliation proved the exact imported state."],
                        []);
                }
            }
            catch (AbwabSnapshotImportException)
            {
            }

            var counts = await ReadCountsAsync(connection, transaction);
            var migrationHead = await ReadMigrationHeadAsync(connection, transaction);
            await transaction.CommitAsync(CancellationToken.None);
            if (counts.Values.All(count => count.Total == 0))
            {
                return new AbwabSnapshotImportExecution(
                    AbwabSnapshotImportContract.FailVerdict,
                    AbwabSnapshotImportContract.PersistedFalse,
                    new AbwabSnapshotImportDatabaseResult(migrationHead, counts, ["commit-reconciled-target-empty"]),
                    ["commit-reconciled-target-empty"],
                    [],
                    ["Commit acknowledgement failed; fresh read-only reconciliation proved all eight target tables empty."]);
            }

            return Unknown(commitException, "fresh target state was partial or mixed");
        }
        catch (Exception reconciliationException)
        {
            return Unknown(
                commitException,
                $"fresh target state was unreadable ({reconciliationException.GetType().Name})");
        }
    }

    private static async Task<IReadOnlyDictionary<string, AbwabSnapshotTableCount>> ReadCountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = AbwabSnapshotImportTargetVerifier.CreateCommand(
            connection,
            transaction,
            AbwabSnapshotImportSql.ReadCounts);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        var counts = new Dictionary<string, AbwabSnapshotTableCount>(StringComparer.Ordinal);
        while (await reader.ReadAsync(CancellationToken.None))
        {
            counts.Add(
                reader.GetString(0),
                new AbwabSnapshotTableCount(
                    reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3)));
        }

        return counts;
    }

    private static async Task<string> ReadMigrationHeadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = AbwabSnapshotImportTargetVerifier.CreateCommand(
            connection,
            transaction,
            AbwabSnapshotImportSql.ReadMigrationHead);
        return await command.ExecuteScalarAsync(CancellationToken.None) as string ?? "unavailable";
    }

    private static async Task ExecuteReadOnlyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = AbwabSnapshotImportTargetVerifier.CreateCommand(
            connection,
            transaction,
            "SET TRANSACTION READ ONLY");
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private static AbwabSnapshotImportExecution Unknown(Exception commitException, string reason) =>
        new(
            AbwabSnapshotImportContract.FailVerdict,
            AbwabSnapshotImportContract.PersistedUnknown,
            null,
            ["commit-outcome-unknown"],
            [$"COMMIT-ACK-FAILED: {commitException.GetType().Name}."],
            [$"Commit acknowledgement failed and {reason}; persistence is unknown and requires operator reconciliation."]);
}
