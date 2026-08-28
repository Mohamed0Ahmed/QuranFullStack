using System.Text.Json;
using Npgsql;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal static class AbwabSnapshotExactRowVerifier
{
    internal static async Task EnsureExactAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AbwabSnapshotDocument snapshot,
        CancellationToken cancellationToken)
    {
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var actualRows = await ReadRowsAsync(connection, transaction, table, cancellationToken);
            var expectedRows = snapshot.Tables[table];
            if (actualRows.Count != expectedRows.Count
                || actualRows.Where((row, index) => !JsonElement.DeepEquals(row, expectedRows[index])).Any())
            {
                throw new AbwabSnapshotImportException(
                    $"Restored rows do not exactly match every snapshot column for {table}.");
            }
        }
    }

    internal static async Task<bool> MatchesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AbwabSnapshotDocument snapshot,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureExactAsync(connection, transaction, snapshot, cancellationToken);
            return true;
        }
        catch (AbwabSnapshotImportException)
        {
            return false;
        }
    }

    private static async Task<IReadOnlyList<JsonElement>> ReadRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table,
        CancellationToken cancellationToken)
    {
        var sql = table switch
        {
            "abwab_sections" => "SELECT to_jsonb(row_data)::text FROM public.abwab_sections row_data ORDER BY id",
            "abwab_doors" => "SELECT to_jsonb(row_data)::text FROM public.abwab_doors row_data ORDER BY id",
            "abwab_door_aliases" => "SELECT to_jsonb(row_data)::text FROM public.abwab_door_aliases row_data ORDER BY id",
            "abwab_door_relations" => "SELECT to_jsonb(row_data)::text FROM public.abwab_door_relations row_data ORDER BY id",
            "abwab_templates" => "SELECT to_jsonb(row_data)::text FROM public.abwab_templates row_data ORDER BY id",
            "abwab_template_nodes" => "SELECT to_jsonb(row_data)::text FROM public.abwab_template_nodes row_data ORDER BY id",
            "abwab_door_inclusions" => "SELECT to_jsonb(row_data)::text FROM public.abwab_door_inclusions row_data ORDER BY id",
            "abwab_door_inclusion_unit_syncs" => "SELECT to_jsonb(row_data)::text FROM public.abwab_door_inclusion_unit_syncs row_data ORDER BY id",
            _ => throw new InvalidOperationException("The Abwab exact-row table is outside the static allowlist."),
        };
        await using var command = AbwabSnapshotImportTargetVerifier.CreateCommand(connection, transaction, sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            using var document = JsonDocument.Parse(reader.GetString(0));
            rows.Add(document.RootElement.Clone());
        }

        return rows;
    }
}
