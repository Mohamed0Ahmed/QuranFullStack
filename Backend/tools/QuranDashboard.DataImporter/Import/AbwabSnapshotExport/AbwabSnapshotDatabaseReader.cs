using System.Data;
using System.Globalization;
using System.Text.Json;
using Npgsql;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

internal sealed class AbwabSnapshotDatabaseReader
{
    internal async Task<AbwabSnapshotReadResult> ReadAsync(
        string connectionString,
        DateTimeOffset exportedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY", cancellationToken);

        var (isolation, readOnly) = await ReadTransactionStateAsync(connection, transaction, cancellationToken);
        var migrationHead = await ReadMigrationHeadAsync(connection, transaction, cancellationToken);
        var actualTables = await ReadAbwabTableNamesAsync(connection, transaction, cancellationToken);
        var schema = await ReadSchemaAsync(connection, transaction, cancellationToken);
        var databaseCounts = new Dictionary<string, long>(StringComparer.Ordinal);
        var tables = new Dictionary<string, IReadOnlyList<JsonElement>>(StringComparer.Ordinal);

        foreach (var table in AbwabSnapshotContract.Tables)
        {
            databaseCounts.Add(
                table,
                await ReadDatabaseCountAsync(connection, transaction, table, cancellationToken));
        }

        foreach (var table in AbwabSnapshotContract.Tables)
        {
            tables.Add(
                table,
                string.Equals(table, AbwabSnapshotContract.ExcludedDerivedRowsTable, StringComparison.Ordinal)
                    ? []
                    : await ReadRowsAsync(connection, transaction, table, cancellationToken));
        }

        var sourceExcludedRowCounts = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            [AbwabSnapshotContract.ExcludedDerivedRowsTable] =
                databaseCounts[AbwabSnapshotContract.ExcludedDerivedRowsTable],
        };

        var snapshot = new AbwabSnapshotDocument(
            AbwabSnapshotContract.Format,
            AbwabSnapshotContract.FormatVersion,
            exportedAtUtc,
            new AbwabSnapshotSource(connection.Database, connection.ServerVersion, migrationHead, readOnly),
            new AbwabSnapshotScope(
                AbwabSnapshotContract.Tables,
                false,
                false,
                sourceExcludedRowCounts),
            new AbwabSnapshotRestorePolicy(
                "fresh-database-at-current-migration-head",
                true,
                true,
                true,
                true),
            schema,
            BuildCounts(tables, schema),
            tables);

        await transaction.CommitAsync(cancellationToken);
        return new AbwabSnapshotReadResult(
            snapshot,
            actualTables,
            databaseCounts,
            isolation);
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

    private static async Task<(string Isolation, bool ReadOnly)> ReadTransactionStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql =
            "SELECT current_setting('transaction_isolation'), current_setting('transaction_read_only')";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return (reader.GetString(0), string.Equals(reader.GetString(1), "on", StringComparison.Ordinal));
    }

    private static async Task<string> ReadMigrationHeadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql =
            "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteScalarAsync(cancellationToken) as string
            ?? throw new InvalidOperationException("The target database has no applied EF migration head.");
    }

    private static async Task<IReadOnlyList<string>> ReadAbwabTableNamesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
              AND table_name LIKE 'abwab\_%' ESCAPE '\'
            ORDER BY table_name
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
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
        const string sql = """
            SELECT table_name, column_name, data_type, is_nullable, ordinal_position
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = ANY (@tables)
              AND column_name <> 'xmin'
            ORDER BY array_position(@tables, table_name), ordinal_position
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
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

    private static async Task<long> ReadDatabaseCountAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT count(*) FROM public.\"{table}\"";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyList<JsonElement>> ReadRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT to_jsonb(row_data)::text FROM public.\"{table}\" AS row_data ORDER BY id";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            using var document = JsonDocument.Parse(reader.GetString(0));
            rows.Add(document.RootElement.Clone());
        }

        return rows;
    }

    private static IReadOnlyDictionary<string, AbwabSnapshotTableCount> BuildCounts(
        IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> tables,
        IReadOnlyList<AbwabSnapshotSchemaColumn> schema)
    {
        var counts = new Dictionary<string, AbwabSnapshotTableCount>(StringComparer.Ordinal);
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var rows = tables[table];
            var hasDeletedAt = schema.Any(column =>
                string.Equals(column.Table, table, StringComparison.Ordinal)
                && string.Equals(column.Column, "deleted_at", StringComparison.Ordinal));
            if (!hasDeletedAt)
            {
                counts.Add(table, new AbwabSnapshotTableCount(rows.Count));
                continue;
            }

            var archived = rows.Count(row => row.GetProperty("deleted_at").ValueKind != JsonValueKind.Null);
            counts.Add(table, new AbwabSnapshotTableCount(rows.Count, rows.Count - archived, archived));
        }

        return counts;
    }
}
