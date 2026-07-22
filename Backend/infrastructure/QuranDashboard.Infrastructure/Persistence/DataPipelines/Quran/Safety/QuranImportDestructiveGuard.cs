namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Safety;

// US2 (FR-005/006/007, SC-001): the single fail-closed guard every destructive Quran import step
// routes through. It generalizes the navigation-only EnsureWriteIsolation into a schema-driven
// defense so a TRUNCATE ... CASCADE (or DELETE) can never silently destroy a future Abwab dependent:
//
//   1. Advisory lock  — a transaction-scoped pg_advisory_xact_lock serializes destructive imports
//      against any writer that cooperatively takes the same lock, so a dependent created concurrently
//      cannot be lost to the CASCADE (it either serializes before and is seen by the preflight, or
//      after and never overlaps the destructive window).
//   2. Closure preflight — computes the transitive FK-dependent closure of the destructive targets
//      from pg_catalog and FAILS CLOSED if any reached table is out of the Quran domain (not named
//      `quran_*`). Today the closure is entirely Quran (guard passes); the moment an Abwab table
//      gains an FK into a Quran table, that table appears in the closure and the import is refused.
//
// No Abwab table/FK exists yet, so this is a structural guarantee: it holds now and stays fail-closed
// the instant the first Abwab->Quran FK is introduced.
public static class QuranImportDestructiveGuard
{
    // Feature 028 (US2). Arbitrary but stable process-wide key; cooperating writers use the same key.
    public const long DestructiveImportLockKey = 20280002L;

    private const int CommandTimeoutSeconds = 600;

    // Reused from the navigation isolation guard: capture the comma-separated TRUNCATE table list and
    // the single DELETE FROM target. Both stop naturally before trailing keywords (RESTART/CASCADE/…).
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
            // Fail closed rather than open: the target parser only recognizes unqualified lowercase
            // identifiers, so a schema-qualified / quoted / CTE-based destructive statement yields zero
            // targets. Executing it unpreflighted would defeat the whole US2 guarantee, so refuse it and
            // require an explicitly parseable statement (or extend the parser) before it can run.
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
        // Walk the FK graph from the destructive targets (principals) to their transitive dependents
        // (referencing tables), then surface any dependent that is a persistent table outside the
        // Quran domain — i.e. a table a TRUNCATE ... CASCADE would silently destroy.
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
