namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseSimilarityEdgeCopier
{
    internal async Task<long> CopyCandidatesAsync(
        NpgsqlConnection connection,
        Guid buildId,
        short mode,
        short wordCount,
        int requiredMatches,
        IReadOnlyList<PhraseVariantVector> variants,
        IReadOnlySet<ulong> candidates,
        CancellationToken ct)
    {
        await using var importer = await BeginCopyAsync(connection, ct);
        long edgeCount = 0;
        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();
            var leftIndex = PhraseSimilarityCandidateGenerator.UnpackLeftIndex(candidate);
            var rightIndex = PhraseSimilarityCandidateGenerator.UnpackRightIndex(candidate);
            if (await WriteIfQualifiedAsync(
                    importer,
                    buildId,
                    mode,
                    wordCount,
                    requiredMatches,
                    variants[leftIndex],
                    variants[rightIndex],
                    ct))
            {
                edgeCount++;
            }
        }

        await importer.CompleteAsync(ct);
        return edgeCount;
    }

    internal async Task<long> CopyBruteForceAsync(
        NpgsqlConnection connection,
        Guid buildId,
        short mode,
        short wordCount,
        int requiredMatches,
        IReadOnlyList<PhraseVariantVector> variants,
        CancellationToken ct)
    {
        await using var importer = await BeginCopyAsync(connection, ct);
        long edgeCount = 0;
        for (var leftIndex = 0; leftIndex < variants.Count - 1; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < variants.Count; rightIndex++)
            {
                ct.ThrowIfCancellationRequested();
                if (await WriteIfQualifiedAsync(
                        importer,
                        buildId,
                        mode,
                        wordCount,
                        requiredMatches,
                        variants[leftIndex],
                        variants[rightIndex],
                        ct))
                {
                    edgeCount++;
                }
            }
        }

        await importer.CompleteAsync(ct);
        return edgeCount;
    }

    private static async Task<bool> WriteIfQualifiedAsync(
        NpgsqlBinaryImporter importer,
        Guid buildId,
        short mode,
        short wordCount,
        int requiredMatches,
        PhraseVariantVector left,
        PhraseVariantVector right,
        CancellationToken ct)
    {
        var differences = new List<short>();
        for (short position = 0; position < wordCount; position++)
        {
            if (left.ExactTokenIds[position] != right.ExactTokenIds[position])
            {
                differences.Add((short)(position + 1));
            }
        }

        var matchedCount = wordCount - differences.Count;
        if (matchedCount < requiredMatches)
        {
            return false;
        }

        await importer.StartRowAsync(ct);
        await importer.WriteAsync(buildId, NpgsqlDbType.Uuid, ct);
        await importer.WriteAsync(mode, NpgsqlDbType.Smallint, ct);
        await importer.WriteAsync(wordCount, NpgsqlDbType.Smallint, ct);
        await importer.WriteAsync(left.Id, NpgsqlDbType.Bigint, ct);
        await importer.WriteAsync(right.Id, NpgsqlDbType.Bigint, ct);
        await importer.WriteAsync((short)matchedCount, NpgsqlDbType.Smallint, ct);
        await importer.WriteAsync((short)differences.Count, NpgsqlDbType.Smallint, ct);
        await importer.WriteAsync(
            differences.ToArray(),
            NpgsqlDbType.Array | NpgsqlDbType.Smallint,
            ct);
        return true;
    }

    private static async Task<NpgsqlBinaryImporter> BeginCopyAsync(
        NpgsqlConnection connection,
        CancellationToken ct)
    {
        var importer = await connection.BeginBinaryImportAsync(
            """
            COPY quran_phrase_similarity_edges (
              build_id,
              mode,
              word_count,
              left_variant_id,
              right_variant_id,
              matched_count,
              difference_count,
              difference_positions
            ) FROM STDIN (FORMAT BINARY)
            """,
            ct);
        importer.Timeout = TimeSpan.FromSeconds(PhraseIndexBuildConstants.CommandTimeoutSeconds);
        return importer;
    }
}
