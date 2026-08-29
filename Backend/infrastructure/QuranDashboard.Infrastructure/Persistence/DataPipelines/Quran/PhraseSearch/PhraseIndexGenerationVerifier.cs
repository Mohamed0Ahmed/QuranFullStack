namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexGenerationVerifier
{
    internal async Task VerifySingleActiveAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid buildId,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            WITH other_builds AS MATERIALIZED (
              SELECT id, status, exact_ready, similarity_ready
              FROM quran_phrase_index_builds
              WHERE id <> @build_id
            )
            SELECT state.active_build_id = @build_id,
                   state.previous_build_id IS NULL,
                   (SELECT COUNT(*) FROM quran_phrase_index_builds WHERE status <> 5) = 1,
                   EXISTS (
                     SELECT 1
                     FROM quran_phrase_index_builds AS build
                     WHERE build.id = @build_id
                       AND build.status = 3
                       AND build.exact_ready
                       AND build.similarity_ready
                   ),
                   NOT EXISTS (
                     SELECT 1
                     FROM other_builds AS other
                     WHERE EXISTS (
                       SELECT 1 FROM quran_phrase_search_tokens AS child WHERE child.build_id = other.id
                     ) OR EXISTS (
                       SELECT 1 FROM quran_phrase_variants AS child WHERE child.build_id = other.id
                     ) OR EXISTS (
                       SELECT 1 FROM quran_phrase_occurrences AS child WHERE child.build_id = other.id
                     ) OR EXISTS (
                       SELECT 1 FROM quran_phrase_similarity_edges AS child WHERE child.build_id = other.id
                     ) OR EXISTS (
                       SELECT 1 FROM quran_phrase_similarity_anchor_stats AS child WHERE child.build_id = other.id
                     )
                   )
                   AND NOT EXISTS (
                     SELECT 1
                     FROM other_builds
                     WHERE status = 5
                       AND (exact_ready OR similarity_ready)
                   )
            FROM quran_phrase_index_state AS state
            WHERE state.id = 1
            """,
            connection,
            transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)
            || !reader.GetBoolean(0)
            || !reader.GetBoolean(1)
            || !reader.GetBoolean(2)
            || !reader.GetBoolean(3)
            || !reader.GetBoolean(4))
        {
            throw new InvalidOperationException(
                "Phrase index activation did not leave exactly one ready active data generation.");
        }
    }
}
