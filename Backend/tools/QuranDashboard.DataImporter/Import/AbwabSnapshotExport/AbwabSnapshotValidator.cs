using System.Text.Json;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

internal static class AbwabSnapshotValidator
{
    internal static AbwabSnapshotValidationResult Validate(AbwabSnapshotReadResult readResult)
    {
        var errors = new List<string>();
        var snapshot = readResult.Snapshot;

        ValidateTransaction(readResult, errors);
        ValidateTableSet(readResult.ActualAbwabTables, errors);
        ValidateSchemaAndCounts(snapshot, readResult.DatabaseCounts, errors);
        var ids = ValidateIds(snapshot, errors);
        ValidateForeignReferences(snapshot, ids, errors);
        AbwabSnapshotTopologyValidator.Validate(snapshot, errors);

        var checks = errors.Count == 0
            ? new[]
            {
                "format-v4",
                "transaction-repeatable-read-read-only",
                "linking-excluded",
                "exact-table-set",
                "derived-inclusion-sync-rows-excluded",
                "xmin-excluded",
                "row-schema-exact",
                "counts-match-rows",
                "ids-unique",
                "foreign-references-resolve",
                "hierarchies-acyclic",
                "relation-types-valid",
            }
            : [];

        return new AbwabSnapshotValidationResult(errors.Count == 0, checks, [], errors);
    }

    private static void ValidateTransaction(
        AbwabSnapshotReadResult readResult,
        ICollection<string> errors)
    {
        var snapshot = readResult.Snapshot;
        if (!string.Equals(readResult.TransactionIsolation, "repeatable read", StringComparison.Ordinal)
            || !snapshot.Source.TransactionReadOnly)
        {
            errors.Add("The export transaction was not repeatable-read and read-only.");
        }

        if (snapshot.Scope.LinkingRowsIncluded || snapshot.Scope.LinkingSummaryIncluded)
        {
            errors.Add("The snapshot scope includes Linking data.");
        }
    }

    private static void ValidateTableSet(IReadOnlyList<string> actualTables, ICollection<string> errors)
    {
        if (!actualTables.ToHashSet(StringComparer.Ordinal)
                .SetEquals(AbwabSnapshotContract.Tables))
        {
            errors.Add("The public Abwab table set does not match the eight-table snapshot contract.");
        }
    }

    private static void ValidateSchemaAndCounts(
        AbwabSnapshotDocument snapshot,
        IReadOnlyDictionary<string, long> databaseCounts,
        ICollection<string> errors)
    {
        if (snapshot.SchemaColumns.Any(column => string.Equals(column.Column, "xmin", StringComparison.Ordinal)))
        {
            errors.Add("The schema contract contains the excluded xmin system column.");
        }

        var schemaByTable = snapshot.SchemaColumns
            .GroupBy(column => column.Table, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(column => column.Column).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        foreach (var table in AbwabSnapshotContract.Tables)
        {
            if (!schemaByTable.TryGetValue(table, out var columns) || columns.Count == 0)
            {
                errors.Add($"Schema metadata is missing for {table}.");
                continue;
            }

            if (!columns.Contains("id"))
            {
                errors.Add($"Schema metadata for {table} has no id column.");
            }

            var rows = snapshot.Tables[table];
            if (snapshot.Counts[table].Total != rows.Count)
            {
                errors.Add($"Declared and serialized row counts differ for {table}.");
            }

            if (!databaseCounts.TryGetValue(table, out var databaseCount))
            {
                errors.Add($"Database row count is missing for {table}.");
            }
            else if (string.Equals(
                         table,
                         AbwabSnapshotContract.ExcludedDerivedRowsTable,
                         StringComparison.Ordinal))
            {
                ValidateExcludedDerivedRows(snapshot, table, databaseCount, rows, errors);
            }
            else if (databaseCount != rows.Count || databaseCount != snapshot.Counts[table].Total)
            {
                errors.Add($"Database, declared, and serialized row counts differ for {table}.");
            }

            if (rows.Any(row => !row.EnumerateObject()
                    .Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal)
                    .SetEquals(columns)))
            {
                errors.Add($"Serialized row fields do not match schema metadata for {table}.");
            }

            if (rows.Any(row => row.TryGetProperty("xmin", out _)))
            {
                errors.Add($"Serialized rows contain the excluded xmin column for {table}.");
            }
        }
    }

    private static IReadOnlyDictionary<string, HashSet<long>> ValidateIds(
        AbwabSnapshotDocument snapshot,
        ICollection<string> errors)
    {
        var ids = new Dictionary<string, HashSet<long>>(StringComparer.Ordinal);
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var tableIds = new HashSet<long>();
            var invalid = 0;
            foreach (var row in snapshot.Tables[table])
            {
                if (!TryReadInt64(row, "id", out var id) || id <= 0 || !tableIds.Add(id))
                {
                    invalid++;
                }
            }

            if (invalid > 0)
            {
                errors.Add($"{table} contains {invalid} missing, non-positive, or duplicate ids.");
            }

            ids.Add(table, tableIds);
        }

        return ids;
    }

    private static void ValidateForeignReferences(
        AbwabSnapshotDocument snapshot,
        IReadOnlyDictionary<string, HashSet<long>> ids,
        ICollection<string> errors)
    {
        ValidateReferences(snapshot.Tables["abwab_doors"], "section_id", ids["abwab_sections"], false, "doors.section", errors);
        ValidateReferences(snapshot.Tables["abwab_doors"], "parent_id", ids["abwab_doors"], true, "doors.parent", errors);
        ValidateReferences(snapshot.Tables["abwab_door_aliases"], "door_id", ids["abwab_doors"], false, "aliases.door", errors);
        ValidateReferences(snapshot.Tables["abwab_door_relations"], "door_a_id", ids["abwab_doors"], false, "relations.door_a", errors);
        ValidateReferences(snapshot.Tables["abwab_door_relations"], "door_b_id", ids["abwab_doors"], false, "relations.door_b", errors);
        ValidateReferences(snapshot.Tables["abwab_door_relations"], "broader_door_id", ids["abwab_doors"], true, "relations.broader", errors);
        ValidateReferences(snapshot.Tables["abwab_template_nodes"], "template_id", ids["abwab_templates"], false, "template_nodes.template", errors);
        ValidateReferences(snapshot.Tables["abwab_template_nodes"], "parent_node_id", ids["abwab_template_nodes"], true, "template_nodes.parent", errors);
        ValidateReferences(snapshot.Tables["abwab_door_inclusions"], "target_door_id", ids["abwab_doors"], false, "inclusions.target", errors);
        ValidateReferences(snapshot.Tables["abwab_door_inclusions"], "source_door_id", ids["abwab_doors"], false, "inclusions.source", errors);
    }

    private static void ValidateExcludedDerivedRows(
        AbwabSnapshotDocument snapshot,
        string table,
        long databaseCount,
        IReadOnlyList<JsonElement> rows,
        ICollection<string> errors)
    {
        var excludedCounts = snapshot.Scope.SourceExcludedRowCounts;
        if (excludedCounts.Count != 1
            || !excludedCounts.TryGetValue(table, out var excludedCount)
            || excludedCount != databaseCount)
        {
            errors.Add($"The source-excluded row count does not match the database count for {table}.");
        }

        if (rows.Count != 0 || snapshot.Counts[table].Total != 0)
        {
            errors.Add($"Derived Linking-dependent rows were serialized for {table}.");
        }
    }

    private static void ValidateReferences(
        IReadOnlyList<JsonElement> rows,
        string property,
        IReadOnlySet<long> targetIds,
        bool nullable,
        string relationship,
        ICollection<string> errors)
    {
        var missing = rows.Count(row =>
        {
            if (!row.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return !nullable;
            }

            return !value.TryGetInt64(out var id) || !targetIds.Contains(id);
        });

        if (missing > 0)
        {
            errors.Add($"{relationship} has {missing} unresolved references.");
        }
    }

    private static bool TryReadInt64(JsonElement row, string property, out long value)
    {
        value = 0;
        return row.TryGetProperty(property, out var element) && element.TryGetInt64(out value);
    }
}
