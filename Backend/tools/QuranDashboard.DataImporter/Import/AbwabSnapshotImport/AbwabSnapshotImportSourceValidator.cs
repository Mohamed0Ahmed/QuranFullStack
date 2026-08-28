using System.Globalization;
using System.Text.Json;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal static class AbwabSnapshotImportSourceValidator
{
    internal static AbwabSnapshotValidationResult Validate(AbwabSnapshotDocument snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var errors = new List<string>();
        ValidateHeaderAndScope(snapshot, errors);
        ValidateCollections(snapshot, errors);
        if (errors.Count > 0)
        {
            return Failure(errors);
        }

        ValidateSchema(snapshot, errors);
        ValidateCountsAndRows(snapshot, errors);
        ValidateIntegerRanges(snapshot, errors);
        ValidateIds(snapshot, errors);
        if (errors.Count > 0)
        {
            return Failure(errors);
        }

        var databaseCounts = AbwabSnapshotContract.Tables.ToDictionary(
            table => table,
            table => string.Equals(
                    table,
                    AbwabSnapshotContract.ExcludedDerivedRowsTable,
                    StringComparison.Ordinal)
                ? snapshot.Scope.SourceExcludedRowCounts[table]
                : (long)snapshot.Tables[table].Count,
            StringComparer.Ordinal);
        var relationalValidation = AbwabSnapshotValidator.Validate(new AbwabSnapshotReadResult(
            snapshot,
            snapshot.Scope.AbwabTables,
            databaseCounts,
            "repeatable read"));
        if (!relationalValidation.Succeeded)
        {
            return relationalValidation;
        }

        return new AbwabSnapshotValidationResult(
            true,
            relationalValidation.Checks
                .Concat(["import-source-contract-v4", "restore-policy-valid", "integer-ranges-valid"])
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            [],
            []);
    }

    private static void ValidateHeaderAndScope(
        AbwabSnapshotDocument snapshot,
        ICollection<string> errors)
    {
        if (!string.Equals(snapshot.Format, AbwabSnapshotContract.Format, StringComparison.Ordinal)
            || snapshot.FormatVersion != AbwabSnapshotContract.FormatVersion)
        {
            errors.Add(
                $"The snapshot must use {AbwabSnapshotContract.Format} format version {AbwabSnapshotContract.FormatVersion}.");
        }

        if (snapshot.Source is null
            || string.IsNullOrWhiteSpace(snapshot.Source.Database)
            || string.IsNullOrWhiteSpace(snapshot.Source.ServerVersion)
            || string.IsNullOrWhiteSpace(snapshot.Source.MigrationHead)
            || !snapshot.Source.TransactionReadOnly)
        {
            errors.Add("The snapshot source provenance contract is invalid.");
        }

        if (snapshot.Scope is null
            || snapshot.Scope.LinkingRowsIncluded
            || snapshot.Scope.LinkingSummaryIncluded
            || snapshot.Scope.AbwabTables is null
            || !snapshot.Scope.AbwabTables.ToHashSet(StringComparer.Ordinal)
                .SetEquals(AbwabSnapshotContract.Tables))
        {
            errors.Add("The snapshot scope must contain exactly the eight Abwab tables and no Linking data.");
        }

        if (snapshot.RestorePolicy is null
            || !string.Equals(
                snapshot.RestorePolicy.Target,
                "fresh-database-at-current-migration-head",
                StringComparison.Ordinal)
            || !snapshot.RestorePolicy.PreserveExplicitIds
            || !snapshot.RestorePolicy.ResetIdentitySequences
            || !snapshot.RestorePolicy.RequireEmptyTargets
            || !snapshot.RestorePolicy.RequireEmptyInclusionSyncs)
        {
            errors.Add("The snapshot restore policy is not compatible with safe v4 import.");
        }
    }

    private static void ValidateCollections(
        AbwabSnapshotDocument snapshot,
        ICollection<string> errors)
    {
        if (snapshot.SchemaColumns is null
            || snapshot.Counts is null
            || snapshot.Tables is null
            || snapshot.Scope?.SourceExcludedRowCounts is null)
        {
            errors.Add("The snapshot schema, count, table, or exclusion collections are missing.");
            return;
        }

        if (!snapshot.Counts.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(AbwabSnapshotContract.Tables)
            || !snapshot.Tables.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(AbwabSnapshotContract.Tables))
        {
            errors.Add("The snapshot table and count keys must exactly match the eight-table contract.");
        }

        var excluded = snapshot.Scope.SourceExcludedRowCounts;
        if (excluded.Count != 1
            || !excluded.TryGetValue(AbwabSnapshotContract.ExcludedDerivedRowsTable, out var excludedRows)
            || excludedRows < 0)
        {
            errors.Add("The snapshot must declare one non-negative source-excluded inclusion-sync row count.");
        }
    }

    private static void ValidateSchema(
        AbwabSnapshotDocument snapshot,
        ICollection<string> errors)
    {
        var schemaTables = snapshot.SchemaColumns
            .Select(column => column.Table)
            .ToHashSet(StringComparer.Ordinal);
        if (!schemaTables.SetEquals(AbwabSnapshotContract.Tables))
        {
            errors.Add("The snapshot schema metadata must cover exactly the eight Abwab tables.");
            return;
        }

        var duplicateColumns = snapshot.SchemaColumns
            .GroupBy(column => (column.Table, column.Column))
            .Count(group => group.Count() != 1);
        if (duplicateColumns > 0
            || snapshot.SchemaColumns.Any(column =>
                string.IsNullOrWhiteSpace(column.Table)
                || string.IsNullOrWhiteSpace(column.Column)
                || string.IsNullOrWhiteSpace(column.DataType)
                || column.Position <= 0
                || string.Equals(column.Column, "xmin", StringComparison.Ordinal)))
        {
            errors.Add("The snapshot schema metadata contains invalid or duplicate columns.");
        }

        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var positions = snapshot.SchemaColumns
                .Where(column => string.Equals(column.Table, table, StringComparison.Ordinal))
                .Select(column => column.Position)
                .Order()
                .ToArray();
            if (!positions.SequenceEqual(Enumerable.Range(1, positions.Length)))
            {
                errors.Add($"Schema column positions are not contiguous for {table}.");
            }
        }
    }

    private static void ValidateCountsAndRows(
        AbwabSnapshotDocument snapshot,
        ICollection<string> errors)
    {
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            if (!snapshot.Tables.TryGetValue(table, out var rows)
                || rows is null
                || !snapshot.Counts.TryGetValue(table, out var declared)
                || declared is null)
            {
                errors.Add($"Snapshot rows or counts are missing for {table}.");
                continue;
            }

            var expectedFields = snapshot.SchemaColumns
                .Where(column => string.Equals(column.Table, table, StringComparison.Ordinal))
                .Select(column => column.Column)
                .ToHashSet(StringComparer.Ordinal);
            if (rows.Any(row => row.ValueKind != JsonValueKind.Object
                || !row.EnumerateObject()
                    .Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal)
                    .SetEquals(expectedFields)))
            {
                errors.Add($"Snapshot row fields do not exactly match schema metadata for {table}.");
            }

            if (declared.Total != rows.Count)
            {
                errors.Add($"Snapshot declared and serialized totals differ for {table}.");
            }

            var hasDeletedAt = expectedFields.Contains("deleted_at");
            if (!hasDeletedAt)
            {
                if (declared.Active.HasValue || declared.Archived.HasValue)
                {
                    errors.Add($"Snapshot active/archive counts are not applicable to {table}.");
                }

                continue;
            }

            var archived = rows.Count(row =>
                row.TryGetProperty("deleted_at", out var deletedAt)
                && deletedAt.ValueKind != JsonValueKind.Null);
            var active = rows.Count - archived;
            if (declared.Active != active || declared.Archived != archived)
            {
                errors.Add($"Snapshot active/archive counts differ from serialized rows for {table}.");
            }
        }

        var syncTable = AbwabSnapshotContract.ExcludedDerivedRowsTable;
        if (snapshot.Tables.TryGetValue(syncTable, out var syncRows)
            && snapshot.Counts.TryGetValue(syncTable, out var syncCount)
            && (syncRows.Count != 0 || syncCount.Total != 0))
        {
            errors.Add("The v4 snapshot must leave inclusion-sync rows serialized as an empty table.");
        }
    }

    private static void ValidateIntegerRanges(
        AbwabSnapshotDocument snapshot,
        ICollection<string> errors)
    {
        foreach (var column in snapshot.SchemaColumns.Where(column =>
                     string.Equals(column.DataType, "integer", StringComparison.Ordinal)))
        {
            var invalid = snapshot.Tables[column.Table].Count(row =>
            {
                if (!row.TryGetProperty(column.Column, out var value))
                {
                    return true;
                }

                return value.ValueKind == JsonValueKind.Null
                    ? !column.Nullable
                    : !value.TryGetInt32(out _);
            });
            if (invalid > 0)
            {
                errors.Add(
                    $"{column.Table}.{column.Column} contains {invalid.ToString(CultureInfo.InvariantCulture)} out-of-range or invalid integer values.");
            }
        }
    }

    private static void ValidateIds(
        AbwabSnapshotDocument snapshot,
        ICollection<string> errors)
    {
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var ids = new HashSet<long>();
            var invalid = snapshot.Tables[table].Count(row =>
                !row.TryGetProperty("id", out var value)
                || !value.TryGetInt64(out var id)
                || id <= 0
                || !ids.Add(id));
            if (invalid > 0)
            {
                errors.Add($"{table} contains missing, non-positive, out-of-range, or duplicate IDs.");
            }
        }
    }

    private static AbwabSnapshotValidationResult Failure(IReadOnlyList<string> errors) =>
        new(false, [], [], errors);
}
