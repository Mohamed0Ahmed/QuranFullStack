using System.Data.Common;

namespace QuranDashboard.Infrastructure.Access;

public sealed partial class AuthorizationSchemaPreflight(QuranDashboardDbContext db)
{
    public async Task<AuthorizationSchemaPreflightResult> InspectAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            var tables = await ReadTablesAsync(connection, cancellationToken);
            var columns = await ReadColumnsAsync(connection, cancellationToken);
            var constraints = await ReadConstraintsAsync(connection, cancellationToken);
            var indexes = await ReadIndexesAsync(connection, cancellationToken);
            var violations = new List<string>();

            ValidateTables(tables, violations);
            ValidateColumns(tables, columns, violations);
            ValidateConstraints(constraints, violations);
            ValidateIndexes(indexes, violations);

            return new AuthorizationSchemaPreflightResult(violations);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task<HashSet<string>> ReadTablesAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = current_schema()
              AND table_type = 'BASE TABLE';
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tables = new HashSet<string>(StringComparer.Ordinal);

        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<Dictionary<string, DatabaseColumn>> ReadColumnsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_relation.relname,
                   attribute.attname,
                   NOT attribute.attnotnull,
                   format_type(attribute.atttypid, attribute.atttypmod),
                   coalesce(nullif(attribute.attidentity::text, ''), '-')
            FROM pg_attribute attribute
            JOIN pg_class table_relation ON table_relation.oid = attribute.attrelid
            JOIN pg_namespace schema ON schema.oid = table_relation.relnamespace
            WHERE schema.nspname = current_schema()
              AND table_relation.relkind = 'r'
              AND attribute.attnum > 0
              AND NOT attribute.attisdropped;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new Dictionary<string, DatabaseColumn>(StringComparer.Ordinal);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(
                QualifiedName(reader.GetString(0), reader.GetString(1)),
                new DatabaseColumn(
                    reader.GetBoolean(2),
                    reader.GetString(3),
                    reader.GetString(4)));
        }

        return columns;
    }

    private static async Task<Dictionary<string, string>> ReadConstraintsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT source_table.relname,
                   constraint_definition.conname,
                   pg_get_constraintdef(constraint_definition.oid)
            FROM pg_constraint constraint_definition
            JOIN pg_class source_table ON source_table.oid = constraint_definition.conrelid
            JOIN pg_namespace schema ON schema.oid = source_table.relnamespace
            WHERE constraint_definition.contype IN ('c', 'f', 'p', 'u')
              AND schema.nspname = current_schema();
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var constraints = new Dictionary<string, string>(StringComparer.Ordinal);

        while (await reader.ReadAsync(cancellationToken))
        {
            constraints.Add(
                QualifiedName(reader.GetString(0), reader.GetString(1)),
                Normalize(reader.GetString(2)));
        }

        return constraints;
    }

    private static async Task<Dictionary<string, DatabaseIndex>> ReadIndexesAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_relation.relname,
                   index_relation.relname,
                   index_definition.indisvalid AND index_definition.indisready,
                   replace(
                       pg_get_indexdef(index_definition.indexrelid),
                       format('%I.', current_schema()),
                       '')
            FROM pg_index index_definition
            JOIN pg_class table_relation ON table_relation.oid = index_definition.indrelid
            JOIN pg_class index_relation ON index_relation.oid = index_definition.indexrelid
            JOIN pg_namespace schema ON schema.oid = table_relation.relnamespace
            WHERE schema.nspname = current_schema();
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var indexes = new Dictionary<string, DatabaseIndex>(StringComparer.Ordinal);

        while (await reader.ReadAsync(cancellationToken))
        {
            indexes.Add(
                QualifiedName(reader.GetString(0), reader.GetString(1)),
                new DatabaseIndex(reader.GetBoolean(2), Normalize(reader.GetString(3))));
        }

        return indexes;
    }

    private static void ValidateTables(IReadOnlySet<string> tables, ICollection<string> violations)
    {
        foreach (var table in AuthorizationSchemaRequirements.Tables.Where(table => !tables.Contains(table)))
        {
            violations.Add($"missing_table={table}");
        }
    }

    private static void ValidateColumns(
        IReadOnlySet<string> tables,
        IReadOnlyDictionary<string, DatabaseColumn> columns,
        ICollection<string> violations)
    {
        foreach (var requirement in AuthorizationSchemaRequirements.Columns.Where(requirement => tables.Contains(requirement.TableName)))
        {
            var key = QualifiedName(requirement.TableName, requirement.ColumnName);
            if (!columns.TryGetValue(key, out var column))
            {
                violations.Add($"missing_column={key}");
            }
            else if (column.IsNullable != requirement.IsNullable)
            {
                violations.Add($"invalid_nullability={key}");
            }
            else if (!string.Equals(column.DataType, requirement.DataType, StringComparison.Ordinal))
            {
                violations.Add($"invalid_column_type={key}");
            }
            else if (!string.Equals(column.Identity, requirement.Identity, StringComparison.Ordinal))
            {
                violations.Add($"invalid_column_identity={key}");
            }
        }
    }

    private static void ValidateConstraints(
        IReadOnlyDictionary<string, string> constraints,
        ICollection<string> violations)
    {
        foreach (var requirement in AuthorizationSchemaRequirements.Constraints)
        {
            var key = QualifiedName(requirement.TableName, requirement.ConstraintName);
            if (!constraints.TryGetValue(key, out var definition))
            {
                violations.Add($"missing_constraint={key}");
            }
            else if (!string.Equals(definition, Normalize(requirement.Definition), StringComparison.Ordinal))
            {
                violations.Add($"invalid_constraint={key}");
            }
        }
    }

    private static void ValidateIndexes(
        IReadOnlyDictionary<string, DatabaseIndex> indexes,
        ICollection<string> violations)
    {
        foreach (var requirement in AuthorizationSchemaRequirements.Indexes)
        {
            var key = QualifiedName(requirement.TableName, requirement.IndexName);
            if (!indexes.TryGetValue(key, out var index))
            {
                violations.Add($"missing_index={key}");
            }
            else if (!index.IsUsable)
            {
                violations.Add($"unusable_index={key}");
            }
            else if (!string.Equals(index.Definition, Normalize(requirement.Definition), StringComparison.Ordinal))
            {
                violations.Add($"invalid_index={key}");
            }
        }
    }

    private static string QualifiedName(string tableName, string objectName)
    {
        return $"{tableName}.{objectName}";
    }

    private static string Normalize(string definition)
    {
        return WhitespaceRuns().Replace(definition, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRuns();

    private sealed record DatabaseColumn(bool IsNullable, string DataType, string Identity);

    private sealed record DatabaseIndex(bool IsUsable, string Definition);
}

public sealed record AuthorizationSchemaPreflightResult(IReadOnlyList<string> Violations)
{
    public bool IsClean => Violations.Count == 0;
}
