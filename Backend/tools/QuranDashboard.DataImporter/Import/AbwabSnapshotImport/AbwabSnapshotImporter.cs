using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal sealed class AbwabSnapshotImporter
{
    internal async Task<AbwabSnapshotImportExecution> ImportAsync(
        string connectionString,
        AbwabSnapshotSourcePackage package,
        string compiledMigrationHead,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(compiledMigrationHead);
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);

        NpgsqlConnection? connection = null;
        NpgsqlTransaction? transaction = null;
        try
        {
            connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            transaction = await connection.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            await ExecuteStaticAsync(
                connection,
                transaction,
                AbwabSnapshotImportSql.LockTables,
                cancellationToken);
            var targetMigrationHead = await AbwabSnapshotImportTargetVerifier.ValidateBeforeImportAsync(
                connection,
                transaction,
                package.Snapshot,
                compiledMigrationHead,
                cancellationToken);

            await RestoreRowsAsync(connection, transaction, package.Snapshot, cancellationToken);
            await ExecuteStaticAsync(
                connection,
                transaction,
                AbwabSnapshotImportSql.ResetIdentitySequences,
                cancellationToken);

            var result = await AbwabSnapshotImportTargetVerifier.ValidateAfterImportAsync(
                connection,
                transaction,
                package.Snapshot,
                targetMigrationHead,
                cancellationToken);
            if (!await sourceUnchangedCheck(cancellationToken))
            {
                throw new AbwabSnapshotImportException(
                    "The snapshot or checksum sidecar changed during import; no commit was attempted.");
            }

            try
            {
                await transaction.CommitAsync(CancellationToken.None);
                var committedResult = result with
                {
                    Checks = result.Checks
                        .Concat(["source-unchanged-after-import", "transaction-committed"])
                        .ToArray(),
                };
                return new AbwabSnapshotImportExecution(
                    AbwabSnapshotImportContract.PassVerdict,
                    AbwabSnapshotImportContract.PersistedTrue,
                    committedResult,
                    committedResult.Checks,
                    [],
                    []);
            }
            catch (Exception commitException)
            {
                await TerminateOriginalAsync(transaction, connection);
                transaction = null;
                connection = null;
                return await new AbwabSnapshotCommitReconciler().ReconcileAsync(
                    connectionString,
                    package.Snapshot,
                    compiledMigrationHead,
                    commitException);
            }
        }
        finally
        {
            await TerminateOriginalAsync(transaction, connection);
        }
    }

    private static async Task TerminateOriginalAsync(
        NpgsqlTransaction? transaction,
        NpgsqlConnection? connection)
    {
        if (transaction is not null)
        {
            try
            {
                await transaction.DisposeAsync();
            }
            catch
            {
            }

            try
            {
                transaction.Dispose();
            }
            catch
            {
            }
        }

        if (connection is null)
        {
            return;
        }

        try
        {
            await connection.CloseAsync();
        }
        catch
        {
        }

        try
        {
            await connection.DisposeAsync();
        }
        catch
        {
        }

        try
        {
            connection.Dispose();
        }
        catch
        {
        }

        try
        {
            NpgsqlConnection.ClearPool(connection);
        }
        catch
        {
        }
    }

    private static async Task RestoreRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AbwabSnapshotDocument snapshot,
        CancellationToken cancellationToken)
    {
        await ExecuteRowsAndVerifyAsync(
            connection,
            transaction,
            AbwabSnapshotImportSql.InsertSections,
            snapshot.Tables["abwab_sections"],
            snapshot.Counts["abwab_sections"].Total,
            "abwab_sections",
            cancellationToken);
        await ExecuteRowsAndVerifyAsync(
            connection,
            transaction,
            AbwabSnapshotImportSql.InsertTemplates,
            snapshot.Tables["abwab_templates"],
            snapshot.Counts["abwab_templates"].Total,
            "abwab_templates",
            cancellationToken);
        foreach (var batch in BuildParentFirstBatches(snapshot.Tables["abwab_doors"], "parent_id"))
        {
            await ExecuteRowsAndVerifyAsync(
                connection,
                transaction,
                AbwabSnapshotImportSql.InsertDoors,
                batch,
                batch.Count,
                "abwab_doors",
                cancellationToken);
        }

        foreach (var batch in BuildParentFirstBatches(
                     snapshot.Tables["abwab_template_nodes"],
                     "parent_node_id"))
        {
            await ExecuteRowsAndVerifyAsync(
                connection,
                transaction,
                AbwabSnapshotImportSql.InsertTemplateNodes,
                batch,
                batch.Count,
                "abwab_template_nodes",
                cancellationToken);
        }
        await ExecuteRowsAndVerifyAsync(
            connection,
            transaction,
            AbwabSnapshotImportSql.InsertAliases,
            snapshot.Tables["abwab_door_aliases"],
            snapshot.Counts["abwab_door_aliases"].Total,
            "abwab_door_aliases",
            cancellationToken);
        await ExecuteRowsAndVerifyAsync(
            connection,
            transaction,
            AbwabSnapshotImportSql.InsertRelations,
            snapshot.Tables["abwab_door_relations"],
            snapshot.Counts["abwab_door_relations"].Total,
            "abwab_door_relations",
            cancellationToken);
        await ExecuteRowsAndVerifyAsync(
            connection,
            transaction,
            AbwabSnapshotImportSql.InsertInclusions,
            snapshot.Tables["abwab_door_inclusions"],
            snapshot.Counts["abwab_door_inclusions"].Total,
            "abwab_door_inclusions",
            cancellationToken);
    }

    private static async Task ExecuteRowsAndVerifyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        IReadOnlyList<JsonElement> rows,
        int expectedAffectedRows,
        string subject,
        CancellationToken cancellationToken)
    {
        await using var command = AbwabSnapshotImportTargetVerifier.CreateCommand(connection, transaction, sql);
        command.Parameters.Add(new NpgsqlParameter("rows", NpgsqlDbType.Jsonb)
        {
            Value = JsonSerializer.Serialize(rows),
        });
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != expectedAffectedRows)
        {
            throw new AbwabSnapshotImportException(
                $"Restore affected {affected} rows for {subject}; expected {expectedAffectedRows}.");
        }
    }

    private static async Task ExecuteStaticAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = AbwabSnapshotImportTargetVerifier.CreateCommand(connection, transaction, sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<IReadOnlyList<JsonElement>> BuildParentFirstBatches(
        IReadOnlyList<JsonElement> rows,
        string parentProperty)
    {
        var remaining = rows
            .OrderBy(row => row.GetProperty("id").GetInt64())
            .ToDictionary(row => row.GetProperty("id").GetInt64());
        var inserted = new HashSet<long>();
        var batches = new List<IReadOnlyList<JsonElement>>();

        while (remaining.Count > 0)
        {
            var batch = remaining.Values
                .Where(row =>
                {
                    var parent = row.GetProperty(parentProperty);
                    return parent.ValueKind == JsonValueKind.Null
                        || inserted.Contains(parent.GetInt64());
                })
                .OrderBy(row => row.GetProperty("id").GetInt64())
                .ToArray();
            if (batch.Length == 0)
            {
                throw new AbwabSnapshotImportException(
                    $"No parent-first insertion progress was possible for {parentProperty}.");
            }

            batches.Add(batch);
            foreach (var row in batch)
            {
                var id = row.GetProperty("id").GetInt64();
                remaining.Remove(id);
                inserted.Add(id);
            }
        }

        return batches;
    }
}
