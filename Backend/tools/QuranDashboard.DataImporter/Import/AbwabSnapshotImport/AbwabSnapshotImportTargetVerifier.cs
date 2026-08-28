using System.Globalization;
using Npgsql;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal static class AbwabSnapshotImportTargetVerifier
{
    private const int CommandTimeoutSeconds = 300;

    internal static async Task<string> ValidateBeforeImportAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AbwabSnapshotDocument snapshot,
        string compiledMigrationHead,
        CancellationToken cancellationToken)
    {
        var targetMigrationHead = await ReadMigrationHeadAsync(connection, transaction, cancellationToken);
        if (!string.Equals(targetMigrationHead, compiledMigrationHead, StringComparison.Ordinal))
        {
            throw new AbwabSnapshotImportException(
                $"The target migration head is not the compiled current head '{compiledMigrationHead}'.");
        }

        await ValidateTableAndSchemaContractAsync(connection, transaction, snapshot, cancellationToken);
        var counts = await ReadCountsAsync(connection, transaction, cancellationToken);
        var nonEmpty = counts.Where(item => item.Value.Total != 0).Select(item => item.Key).ToArray();
        if (nonEmpty.Length > 0)
        {
            throw new AbwabSnapshotImportException(
                $"Abwab snapshot import requires all eight target tables to be empty: {string.Join(", ", nonEmpty)}.");
        }

        return targetMigrationHead;
    }

    internal static async Task<AbwabSnapshotImportDatabaseResult> ValidateAfterImportAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AbwabSnapshotDocument snapshot,
        string targetMigrationHead,
        CancellationToken cancellationToken)
    {
        var postImportMigrationHead = await ReadMigrationHeadAsync(
            connection,
            transaction,
            cancellationToken);
        if (!string.Equals(postImportMigrationHead, targetMigrationHead, StringComparison.Ordinal))
        {
            throw new AbwabSnapshotImportException(
                "The target migration head changed while the Abwab import transaction was running.");
        }

        await ValidateTableAndSchemaContractAsync(connection, transaction, snapshot, cancellationToken);

        var counts = await ReadCountsAsync(connection, transaction, cancellationToken);
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            if (!counts.TryGetValue(table, out var observed)
                || !snapshot.Counts.TryGetValue(table, out var expected)
                || observed != expected)
            {
                throw new AbwabSnapshotImportException(
                    $"Post-import total/active/archive counts do not match the snapshot for {table}.");
            }
        }

        var observedIds = await ReadIdsAsync(connection, transaction, cancellationToken);
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var expectedIds = snapshot.Tables[table]
                .Select(row => row.GetProperty("id").GetInt64())
                .ToHashSet();
            if (!observedIds.TryGetValue(table, out var actualIds) || !actualIds.SetEquals(expectedIds))
            {
                throw new AbwabSnapshotImportException(
                    $"Post-import IDs do not exactly match the snapshot for {table}.");
            }
        }

        await ValidateSequenceStatesAsync(
            connection,
            transaction,
            snapshot,
            cancellationToken);

        var missingReferences = await ExecuteScalarLongAsync(
            connection,
            transaction,
            AbwabSnapshotImportSql.ReadMissingReferences,
            cancellationToken);
        if (missingReferences != 0)
        {
            throw new AbwabSnapshotImportException(
                $"Post-import Abwab references contain {missingReferences.ToString(CultureInfo.InvariantCulture)} unresolved rows.");
        }

        await AbwabSnapshotExactRowVerifier.EnsureExactAsync(
            connection,
            transaction,
            snapshot,
            cancellationToken);

        return new AbwabSnapshotImportDatabaseResult(
            postImportMigrationHead,
            counts,
            [
                "target-current-migration-head",
                "target-schema-exact",
                "target-empty-before-import",
                "explicit-ids-restored",
                "parent-links-restored-parent-first",
                "identity-sequences-reset",
                "post-import-counts-exact",
                "post-import-active-archive-counts-exact",
                "post-import-ids-exact",
                "post-import-sequence-states-exact",
                "post-import-references-resolve",
                "post-import-rows-exact",
                "inclusion-sync-target-empty",
            ]);
    }

    private static async Task ValidateTableAndSchemaContractAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AbwabSnapshotDocument snapshot,
        CancellationToken cancellationToken)
    {
        var tableNames = await ReadTableNamesAsync(connection, transaction, cancellationToken);
        if (!tableNames.ToHashSet(StringComparer.Ordinal).SetEquals(AbwabSnapshotContract.Tables))
        {
            throw new AbwabSnapshotImportException(
                "The live public Abwab table set does not match the eight-table v4 contract.");
        }

        var targetSchema = await ReadSchemaAsync(connection, transaction, cancellationToken);
        var tableOrder = AbwabSnapshotContract.Tables
            .Select((table, index) => (table, index))
            .ToDictionary(item => item.table, item => item.index, StringComparer.Ordinal);
        var sourceSchema = snapshot.SchemaColumns
            .OrderBy(column => tableOrder[column.Table])
            .ThenBy(column => column.Position)
            .ToArray();
        if (!targetSchema.SequenceEqual(sourceSchema))
        {
            throw new AbwabSnapshotImportException(
                "The live Abwab schema does not exactly match the snapshot schema metadata.");
        }
    }

    private static async Task<string> ReadMigrationHeadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            AbwabSnapshotImportSql.ReadMigrationHead);
        return await command.ExecuteScalarAsync(cancellationToken) as string
            ?? throw new AbwabSnapshotImportException("The target database has no applied migration head.");
    }

    private static async Task<IReadOnlyList<string>> ReadTableNamesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, transaction, AbwabSnapshotImportSql.ReadTableNames);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var names = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task<IReadOnlyList<AbwabSnapshotSchemaColumn>> ReadSchemaAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, transaction, AbwabSnapshotImportSql.ReadSchema);
        command.Parameters.AddWithValue("tables", AbwabSnapshotContract.Tables);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new List<AbwabSnapshotSchemaColumn>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new AbwabSnapshotSchemaColumn(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                string.Equals(reader.GetString(3), "YES", StringComparison.Ordinal),
                reader.GetInt32(4)));
        }

        return columns;
    }

    private static async Task<IReadOnlyDictionary<string, AbwabSnapshotTableCount>> ReadCountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, transaction, AbwabSnapshotImportSql.ReadCounts);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var counts = new Dictionary<string, AbwabSnapshotTableCount>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
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

    private static async Task<IReadOnlyDictionary<string, HashSet<long>>> ReadIdsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var ids = AbwabSnapshotContract.Tables.ToDictionary(
            table => table,
            _ => new HashSet<long>(),
            StringComparer.Ordinal);
        await using var command = CreateCommand(connection, transaction, AbwabSnapshotImportSql.ReadIds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids[reader.GetString(0)].Add(reader.GetInt64(1));
        }

        return ids;
    }

    private static async Task ValidateSequenceStatesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AbwabSnapshotDocument snapshot,
        CancellationToken cancellationToken)
    {
        var observed = new Dictionary<string, AbwabSnapshotSequenceState>(StringComparer.Ordinal);
        await using var command = CreateCommand(
            connection,
            transaction,
            AbwabSnapshotImportSql.ReadSequenceStates);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            observed.Add(reader.GetString(0), new AbwabSnapshotSequenceState(reader.GetInt64(1), reader.GetBoolean(2)));
        }

        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var expectedNext = snapshot.Tables[table].Count == 0
                ? 1
                : checked(snapshot.Tables[table].Max(row => row.GetProperty("id").GetInt64()) + 1);
            if (!observed.TryGetValue(table, out var state)
                || state.NextValue != expectedNext
                || state.IsCalled)
            {
                throw new AbwabSnapshotImportException(
                    $"Post-import identity sequence state does not match restored IDs for {table}.");
            }
        }
    }

    private static async Task<long> ExecuteScalarLongAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, transaction, sql);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    internal static NpgsqlCommand CreateCommand(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql) =>
        new(sql, connection, transaction) { CommandTimeout = CommandTimeoutSeconds };
}
