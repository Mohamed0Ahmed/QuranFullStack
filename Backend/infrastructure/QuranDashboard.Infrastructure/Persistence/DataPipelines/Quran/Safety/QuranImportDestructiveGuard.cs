namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Safety;

public static class QuranImportDestructiveGuard
{
    // Stable process-wide advisory-lock key; cooperating writers MUST use the same key.
    public const long DestructiveImportLockKey = 20280002L;

    private const int CommandTimeoutSeconds = 600;

    private static readonly Regex TruncateTablesPattern = new(
        @"\bTRUNCATE\s+(?:TABLE\s+)?(?<tables>(?:ONLY\s+)?[a-z_][a-z0-9_]*(?:\s*,\s*(?:ONLY\s+)?[a-z_][a-z0-9_]*)*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DeleteFromTablePattern = new(
        @"\bDELETE\s+FROM\s+(?:ONLY\s+)?(?<table>[a-z_][a-z0-9_]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static async Task ExecuteDestructiveAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string destructiveSql,
        CancellationToken ct)
    {
        await AcquireDestructiveLockAsync(connection, transaction, ct);
        await EnsureNoOutOfScopeDependentsAsync(connection, transaction, destructiveSql, ct);

        await using var command = new NpgsqlCommand(destructiveSql, connection, transaction)
        {
            CommandTimeout = CommandTimeoutSeconds
        };
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task AcquireDestructiveLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", connection, transaction)
        {
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.AddWithValue("key", DestructiveImportLockKey);
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task EnsureNoOutOfScopeDependentsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string destructiveSql,
        CancellationToken ct)
    {
        var targets = ExtractDestructiveTargets(destructiveSql);
        if (targets.Count == 0)
        {
            // Fail closed: zero parsed targets (schema-qualified/quoted/CTE statement) means the FK
            // closure can't be verified, so refuse rather than run it unpreflighted.
            throw new QuranImportSafetyException(
                "Destructive Quran import refused fail-closed: no destructive target table could be "
                + "parsed from the statement, so its FK-dependent closure cannot be verified. "
                + "Rewrite the statement with unqualified lowercase table names it can preflight.");
        }

        var offenders = await QueryOutOfScopeDependentsAsync(connection, transaction, targets, ct);
        if (offenders.Count > 0)
        {
            throw new QuranImportSafetyException(
                "Destructive Quran import refused fail-closed: the truncation set "
                + $"[{string.Join(", ", targets)}] has out-of-scope foreign-key dependent(s) that a "
                + "CASCADE would destroy: " + string.Join(", ", offenders)
                + ". Import is blocked until the dependent is detached or explicitly brought in scope.");
        }
    }

    public static IReadOnlyList<string> ExtractDestructiveTargets(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var targets = new List<string>();

        foreach (Match match in TruncateTablesPattern.Matches(sql))
        {
            foreach (var table in SplitTableList(match.Groups["tables"].Value))
            {
                AddTarget(targets, table);
            }
        }

        foreach (Match match in DeleteFromTablePattern.Matches(sql))
        {
            AddTarget(targets, match.Groups["table"].Value);
        }

        return targets;
    }

    private static async Task<IReadOnlyList<string>> QueryOutOfScopeDependentsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<string> targets,
        CancellationToken ct)
    {
        const string sql = """
            WITH RECURSIVE target(oid) AS (
                SELECT c.oid
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relkind = 'r'
                  AND c.relname = ANY(@targets)
                  AND n.nspname NOT LIKE 'pg\_%' ESCAPE '\'
                  AND n.nspname <> 'information_schema'
            ),
            dependents(oid) AS (
                SELECT oid FROM target
                UNION
                SELECT con.conrelid
                FROM pg_constraint con
                JOIN dependents d ON con.confrelid = d.oid
                WHERE con.contype = 'f'
                  AND con.conrelid <> con.confrelid
            )
            SELECT DISTINCT c.relname
            FROM dependents d
            JOIN pg_class c ON c.oid = d.oid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relpersistence IN ('p', 'u')
              AND c.relname <> ALL(@targets)
              AND c.relname NOT LIKE 'quran\_%' ESCAPE '\'
              AND n.nspname NOT LIKE 'pg\_%' ESCAPE '\'
            ORDER BY c.relname
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.AddWithValue("targets", targets.ToArray());

        var offenders = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            offenders.Add(reader.GetString(0));
        }

        return offenders;
    }

    private static IEnumerable<string> SplitTableList(string tables) =>
        tables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(table => table.StartsWith("ONLY ", StringComparison.OrdinalIgnoreCase)
                ? table[5..].Trim()
                : table);

    private static void AddTarget(List<string> targets, string table)
    {
        var normalized = table.Trim().ToLowerInvariant();
        if (normalized.Length > 0 && !targets.Contains(normalized))
        {
            targets.Add(normalized);
        }
    }
}
