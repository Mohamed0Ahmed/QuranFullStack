using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexValidator
{
    private readonly PhraseIndexBuildExpectations expectations;

    private const string TotalsSql = """
        SELECT (SELECT COUNT(*) FROM quran_phrase_search_tokens WHERE build_id = @build_id),
               (SELECT COUNT(*) FROM quran_phrase_variants WHERE build_id = @build_id),
               (SELECT COUNT(*) FROM quran_phrase_occurrences WHERE build_id = @build_id),
               (SELECT COUNT(*) FROM quran_phrase_similarity_edges WHERE build_id = @build_id),
               (SELECT COUNT(*) FROM quran_phrase_similarity_anchor_stats WHERE build_id = @build_id)
        """;

    private const string BaselineSql = """
        SELECT
          (SELECT COUNT(*) FROM phrase_windows WHERE mode = 1),
          (SELECT COUNT(*) FROM phrase_windows WHERE mode = 2),
          (SELECT COUNT(*) FROM phrase_windows WHERE mode = 1 AND word_count >= 2),
          (SELECT COUNT(*) FROM phrase_windows WHERE mode = 2 AND word_count >= 2),
          (SELECT COUNT(*) FROM quran_phrase_variants
             WHERE build_id = @build_id AND mode = 1 AND word_count >= 2),
          (SELECT COUNT(*) FROM quran_phrase_variants
             WHERE build_id = @build_id AND mode = 2 AND word_count >= 2),
          (SELECT COUNT(*) FROM quran_phrase_variants
             WHERE build_id = @build_id AND word_count >= 2 AND occurrence_count >= 2),
          (SELECT COALESCE(SUM(occurrence_count), 0) FROM quran_phrase_variants
             WHERE build_id = @build_id AND word_count >= 2 AND occurrence_count >= 2),
          (SELECT COALESCE(MAX(word_count), 0) FROM quran_phrase_variants
             WHERE build_id = @build_id AND mode = 1 AND occurrence_count >= 2),
          (SELECT COALESCE(MAX(word_count), 0) FROM quran_phrase_variants
             WHERE build_id = @build_id AND mode = 2 AND occurrence_count >= 2)
        """;

    private const string StagingIntegritySql = """
        SELECT
          (SELECT COUNT(*)
             FROM phrase_windows
            WHERE cardinality(exact_token_ids) <> word_count
                   OR cardinality(search_token_ids) <> word_count),
          (SELECT COUNT(*)
             FROM phrase_windows AS phrase_window
             LEFT JOIN phrase_source_tokens AS first_word
               ON first_word.id = phrase_window.first_quran_word_id
             LEFT JOIN phrase_source_tokens AS last_word
               ON last_word.id = phrase_window.last_quran_word_id
            WHERE first_word.id IS NULL
               OR last_word.id IS NULL
               OR first_word.ayah_id <> phrase_window.ayah_id
               OR last_word.ayah_id <> phrase_window.ayah_id
               OR first_word.word_number <> phrase_window.start_word_number
               OR last_word.word_number <> phrase_window.end_word_number
               OR phrase_window.end_word_number - phrase_window.start_word_number + 1
                    <> phrase_window.word_count)
        """;

    private const string SchemaProtectionsSql = """
        SELECT
          COUNT(*) FILTER (
            WHERE constraint_row.conrelid = 'quran_phrase_variants'::regclass
              AND constraint_row.contype = 'c'),
          COUNT(*) FILTER (
            WHERE constraint_row.conrelid = 'quran_phrase_occurrences'::regclass
              AND constraint_row.contype = 'c'),
          COUNT(*) FILTER (
            WHERE constraint_row.conrelid = 'quran_phrase_occurrences'::regclass
              AND constraint_row.contype = 'f'),
          COUNT(*) FILTER (
            WHERE constraint_row.conrelid = 'quran_phrase_similarity_edges'::regclass
              AND constraint_row.contype = 'c'),
          COUNT(*) FILTER (
            WHERE constraint_row.conrelid = 'quran_phrase_similarity_edges'::regclass
              AND constraint_row.contype = 'f'),
          COUNT(*) FILTER (
            WHERE constraint_row.conrelid = 'quran_phrase_search_tokens'::regclass
              AND constraint_row.contype = 'c')
        FROM pg_constraint AS constraint_row
        WHERE constraint_row.convalidated
          AND constraint_row.conrelid IN (
            'quran_phrase_variants'::regclass,
            'quran_phrase_occurrences'::regclass,
            'quran_phrase_similarity_edges'::regclass,
            'quran_phrase_search_tokens'::regclass
          )
        """;

    private const string SearchTokenIntegritySql = """
        SELECT COUNT(*)
        FROM quran_phrase_search_tokens
        WHERE build_id = @build_id
          AND (btrim(search_text) = '' OR cardinality(exact_token_ids) = 0)
        """;

    private const string ThresholdCountsSql = """
        SELECT threshold.threshold,
               COUNT(edge.*)::bigint
        FROM (VALUES (50::smallint), (60::smallint), (70::smallint), (80::smallint), (90::smallint))
             AS threshold(threshold)
        LEFT JOIN quran_phrase_similarity_edges AS edge
          ON edge.build_id = @build_id
         AND edge.matched_count * 100 >= threshold.threshold * edge.word_count
        GROUP BY threshold.threshold
        ORDER BY threshold.threshold
        """;

    private const string AnchorMismatchSql = """
        WITH thresholds(threshold) AS (
          VALUES (50::smallint), (60::smallint), (70::smallint), (80::smallint), (90::smallint)
        ),
        edge_totals AS (
          SELECT threshold.threshold,
                 COUNT(edge.*)::bigint * 2 AS neighbor_total
          FROM thresholds AS threshold
          LEFT JOIN quran_phrase_similarity_edges AS edge
            ON edge.build_id = @build_id
           AND edge.matched_count * 100 >= threshold.threshold * edge.word_count
          GROUP BY threshold.threshold
        ),
        stat_totals AS (
          SELECT threshold,
                 SUM(neighbor_count)::bigint AS neighbor_total
          FROM quran_phrase_similarity_anchor_stats
          WHERE build_id = @build_id
          GROUP BY threshold
        ),
        total_mismatches AS (
          SELECT edge.threshold
          FROM edge_totals AS edge
          LEFT JOIN stat_totals AS stat ON stat.threshold = edge.threshold
          WHERE edge.neighbor_total <> COALESCE(stat.neighbor_total, 0)
        ),
        invalid_stats AS (
          SELECT 1
          FROM quran_phrase_similarity_anchor_stats
          WHERE build_id = @build_id
            AND (neighbor_count <= 0
                 OR best_matched_count IS NULL
                 OR best_matched_count * 100 < threshold * word_count)
        )
        SELECT (SELECT COUNT(*) FROM total_mismatches)
             + (SELECT COUNT(*) FROM invalid_stats)
        """;

    public PhraseIndexValidator(PhraseIndexBuildExpectations expectations)
    {
        this.expectations = expectations;
    }

    internal async Task<PhraseIndexValidationResult> ValidateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        IReadOnlyList<PhraseBuildCheck> sourceChecks,
        CancellationToken ct)
    {
        var totals = await ReadTotalsAsync(connection, transaction, buildId, ct);
        var checks = new List<PhraseBuildCheck>(sourceChecks);
        checks.Add(HardCheck("TOTAL-VARIANTS", expectations.ExpectedTotalVariants, totals.Variants));
        checks.Add(HardCheck("TOTAL-OCCURRENCES", expectations.ExpectedTotalOccurrences, totals.Occurrences));
        checks.Add(HardCheck("TOTAL-SIMILARITY-EDGES", expectations.ExpectedSimilarityEdges, totals.SimilarityEdges));
        checks.Add(new PhraseBuildCheck(
            "TOTAL-SEARCH-TOKENS",
            "hard",
            "> 0",
            totals.SearchTokens.ToString(CultureInfo.InvariantCulture),
            totals.SearchTokens > 0));
        checks.Add(new PhraseBuildCheck(
            "TOTAL-ANCHOR-STATS",
            "hard",
            "> 0",
            totals.SimilarityAnchorStats.ToString(CultureInfo.InvariantCulture),
            totals.SimilarityAnchorStats > 0));

        await AddBaselineChecksAsync(connection, transaction, buildId, checks, ct);
        await AddIntegrityChecksAsync(connection, transaction, buildId, checks, ct);
        await AddThresholdChecksAsync(connection, transaction, buildId, checks, ct);
        var anchorMismatches = await ReadScalarLongAsync(
            connection,
            transaction,
            AnchorMismatchSql,
            ct,
            new NpgsqlParameter("build_id", buildId));
        checks.Add(HardCheck("ANCHOR-STATS-MATCH-EDGES", 0, anchorMismatches));

        return new PhraseIndexValidationResult(totals, checks);
    }

    private async Task AddBaselineChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        ICollection<PhraseBuildCheck> checks,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(BaselineSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        checks.Add(HardCheck("WINDOWS-SIMPLE-ALL", expectations.ExpectedWindowsPerMode, reader.GetInt64(0)));
        checks.Add(HardCheck("WINDOWS-TASHKIL-ALL", expectations.ExpectedWindowsPerMode, reader.GetInt64(1)));
        checks.Add(HardCheck("WINDOWS-SIMPLE-LENGTH-2-PLUS", expectations.ExpectedWindowsLengthTwoPlusPerMode, reader.GetInt64(2)));
        checks.Add(HardCheck("WINDOWS-TASHKIL-LENGTH-2-PLUS", expectations.ExpectedWindowsLengthTwoPlusPerMode, reader.GetInt64(3)));
        checks.Add(HardCheck("VARIANTS-SIMPLE-LENGTH-2-PLUS", expectations.ExpectedSimpleVariantsLengthTwoPlus, reader.GetInt64(4)));
        checks.Add(HardCheck("VARIANTS-TASHKIL-LENGTH-2-PLUS", expectations.ExpectedTashkilVariantsLengthTwoPlus, reader.GetInt64(5)));
        checks.Add(HardCheck("REPEATED-VARIANTS", expectations.ExpectedRepeatedVariants, reader.GetInt64(6)));
        checks.Add(HardCheck("REPEATED-OCCURRENCES", expectations.ExpectedRepeatedOccurrences, reader.GetInt64(7)));
        checks.Add(HardCheck("MAX-REPEATED-SIMPLE-LENGTH", 24, reader.GetInt16(8)));
        checks.Add(HardCheck("MAX-REPEATED-TASHKIL-LENGTH", 23, reader.GetInt16(9)));
    }

    private static async Task AddIntegrityChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        ICollection<PhraseBuildCheck> checks,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(StagingIntegritySql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        checks.Add(HardCheck("VARIANT-ARRAY-LENGTHS", 0, reader.GetInt64(0)));
        checks.Add(HardCheck("OCCURRENCE-NO-MARKER-OR-CROSS-AYAH", 0, reader.GetInt64(1)));
        await reader.DisposeAsync();

        await using var schemaCommand = new NpgsqlCommand(SchemaProtectionsSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        await using var schemaReader = await schemaCommand.ExecuteReaderAsync(ct);
        await schemaReader.ReadAsync(ct);
        checks.Add(HardCheck("VARIANT-SCHEMA-PROTECTIONS", 6, schemaReader.GetInt64(0)));
        checks.Add(HardCheck("OCCURRENCE-RANGE-PROTECTIONS", 3, schemaReader.GetInt64(1)));
        checks.Add(HardCheck("OCCURRENCE-SCOPE-FKS", 4, schemaReader.GetInt64(2)));
        checks.Add(HardCheck("EDGE-MATH-AND-LENGTH", 6, schemaReader.GetInt64(3)));
        checks.Add(HardCheck("EDGE-ENDPOINT-SCOPE", 2, schemaReader.GetInt64(4)));
        checks.Add(HardCheck("SEARCH-TOKEN-PROTECTIONS", 3, schemaReader.GetInt64(5)));
        await schemaReader.DisposeAsync();

        var dictionaryViolations = await ReadScalarLongAsync(
            connection,
            transaction,
            SearchTokenIntegritySql,
            ct,
            new NpgsqlParameter("build_id", buildId));
        checks.Add(HardCheck("SEARCH-TOKEN-DICTIONARY", 0, dictionaryViolations));
    }

    private async Task AddThresholdChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        ICollection<PhraseBuildCheck> checks,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(ThresholdCountsSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var threshold = reader.GetInt16(0);
            checks.Add(HardCheck(
                $"SIMILARITY-THRESHOLD-{threshold}",
                expectations.ExpectedThresholdCounts[threshold],
                reader.GetInt64(1)));
        }
    }

    private static async Task<PhraseIndexBuildTotals> ReadTotalsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(TotalsSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new PhraseIndexBuildTotals(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4));
    }

    private static PhraseBuildCheck HardCheck(string id, long expected, long observed) =>
        new(
            id,
            "hard",
            expected.ToString(CultureInfo.InvariantCulture),
            observed.ToString(CultureInfo.InvariantCulture),
            expected == observed);

    private static async Task<long> ReadScalarLongAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddRange(parameters);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }
}
