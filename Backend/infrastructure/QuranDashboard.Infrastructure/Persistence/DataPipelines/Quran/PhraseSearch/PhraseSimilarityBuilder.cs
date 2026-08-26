using System.Diagnostics;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseSimilarityBuilder
{
    private readonly PhraseSimilarityCandidateGenerator candidateGenerator;
    private readonly PhraseSimilarityEdgeCopier edgeCopier;

    public PhraseSimilarityBuilder(
        PhraseSimilarityCandidateGenerator candidateGenerator,
        PhraseSimilarityEdgeCopier edgeCopier)
    {
        this.candidateGenerator = candidateGenerator;
        this.edgeCopier = edgeCopier;
    }

    private const string ReadPartitionSql = """
        SELECT id, exact_token_ids
        FROM quran_phrase_variants
        WHERE build_id = @build_id
          AND mode = @mode
          AND word_count = @word_count
        ORDER BY id
        """;

    private const string BuildAnchorStatsSql = """
        WITH thresholds(threshold) AS (
          VALUES (50::smallint), (60::smallint), (70::smallint), (80::smallint), (90::smallint)
        ),
        neighbors AS (
          SELECT build_id,
                 left_variant_id AS variant_id,
                 mode,
                 word_count,
                 matched_count
          FROM quran_phrase_similarity_edges
          WHERE build_id = @build_id
          UNION ALL
          SELECT build_id,
                 right_variant_id AS variant_id,
                 mode,
                 word_count,
                 matched_count
          FROM quran_phrase_similarity_edges
          WHERE build_id = @build_id
        )
        INSERT INTO quran_phrase_similarity_anchor_stats (
          build_id,
          variant_id,
          threshold,
          mode,
          word_count,
          neighbor_count,
          best_matched_count
        )
        SELECT neighbor.build_id,
               neighbor.variant_id,
               threshold.threshold,
               neighbor.mode,
               neighbor.word_count,
               COUNT(*)::integer,
               MAX(neighbor.matched_count)::smallint
        FROM neighbors AS neighbor
        CROSS JOIN thresholds AS threshold
        WHERE neighbor.matched_count * 100 >= threshold.threshold * neighbor.word_count
        GROUP BY neighbor.build_id,
                 neighbor.variant_id,
                 threshold.threshold,
                 neighbor.mode,
                 neighbor.word_count
        """;

    internal async Task<PhraseSimilarityStageResult> BuildAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        IReadOnlyList<PhraseLengthBuildMetric> exactMetrics,
        CancellationToken ct)
    {
        var metrics = new List<PhraseLengthBuildMetric>();
        long totalEdges = 0;

        foreach (var exactMetric in exactMetrics.Where(metric =>
                     metric.WordCount >= PhraseIndexBuildConstants.MinimumSimilarityLength))
        {
            ct.ThrowIfCancellationRequested();
            var stopwatch = Stopwatch.StartNew();
            var variants = await ReadPartitionAsync(
                connection,
                transaction,
                buildId,
                exactMetric.Mode,
                exactMetric.WordCount,
                ct);

            var partition = await BuildPartitionAsync(
                connection,
                buildId,
                exactMetric.Mode,
                exactMetric.WordCount,
                variants,
                ct);
            stopwatch.Stop();
            totalEdges += partition.EdgeCount;
            metrics.Add(exactMetric with
            {
                Algorithm = partition.Algorithm,
                CandidateEmissions = partition.CandidateEmissions,
                UniqueCandidates = partition.UniqueCandidates,
                VerifiedPairs = partition.VerifiedPairs,
                Edges = partition.EdgeCount,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                PeakManagedMemoryBytes = partition.PeakManagedMemoryBytes,
            });
        }

        await ExecuteAsync(
            connection,
            transaction,
            BuildAnchorStatsSql,
            ct,
            new NpgsqlParameter("build_id", buildId));
        var anchorStatCount = await ReadScalarLongAsync(
            connection,
            transaction,
            "SELECT COUNT(*) FROM quran_phrase_similarity_anchor_stats WHERE build_id = @build_id",
            ct,
            new NpgsqlParameter("build_id", buildId));

        return new PhraseSimilarityStageResult(totalEdges, anchorStatCount, metrics);
    }

    private async Task<PartitionBuildResult> BuildPartitionAsync(
        NpgsqlConnection connection,
        Guid buildId,
        short mode,
        short wordCount,
        IReadOnlyList<PhraseVariantVector> variants,
        CancellationToken ct)
    {
        var requiredMatches = (wordCount + 1) / 2;
        var candidateSet = candidateGenerator.Create(variants, wordCount);
        if (candidateSet.UsesBruteForce)
        {
            var edges = await edgeCopier.CopyBruteForceAsync(
                connection,
                buildId,
                mode,
                wordCount,
                requiredMatches,
                variants,
                ct);
            return new PartitionBuildResult(
                "bounded-brute-force",
                candidateSet.CandidateEmissions,
                candidateSet.UniqueCandidates,
                candidateSet.VerifiedPairs,
                edges,
                candidateSet.PeakManagedMemoryBytes);
        }
        var edgeCount = await edgeCopier.CopyCandidatesAsync(
            connection,
            buildId,
            mode,
            wordCount,
            requiredMatches,
            variants,
            candidateSet.Candidates,
            ct);
        return new PartitionBuildResult(
            candidateSet.Algorithm,
            candidateSet.CandidateEmissions,
            candidateSet.UniqueCandidates,
            candidateSet.VerifiedPairs,
            edgeCount,
            candidateSet.PeakManagedMemoryBytes);
    }

    private static async Task<List<PhraseVariantVector>> ReadPartitionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        short mode,
        short wordCount,
        CancellationToken ct)
    {
        var variants = new List<PhraseVariantVector>();
        await using var command = new NpgsqlCommand(ReadPartitionSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("mode", NpgsqlDbType.Smallint, mode);
        command.Parameters.AddWithValue("word_count", NpgsqlDbType.Smallint, wordCount);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        while (await reader.ReadAsync(ct))
        {
            variants.Add(new PhraseVariantVector(reader.GetInt64(0), reader.GetFieldValue<int[]>(1)));
        }

        return variants;
    }

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

    private static async Task ExecuteAsync(
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
        await command.ExecuteNonQueryAsync(ct);
    }

    private sealed record PartitionBuildResult(
        string Algorithm,
        long CandidateEmissions,
        long UniqueCandidates,
        long VerifiedPairs,
        long EdgeCount,
        long PeakManagedMemoryBytes);
}
