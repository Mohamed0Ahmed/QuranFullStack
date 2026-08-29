namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal static class PhraseIndexGenerationCleanup
{
    internal const string PendingWarning =
        "PhraseSearch unreferenced-generation cleanup remains pending; "
        + "a later build or source operation will retry it.";

    internal static async Task<PhraseIndexCleanupResult> CleanupAfterSourceInvalidationAsync(
        NpgsqlConnection connection,
        CancellationToken ct)
    {
        try
        {
            await using var transaction = await connection.BeginTransactionAsync(ct);
            await AcquireBuilderFenceAsync(connection, transaction, ct);
            await ExecuteAsync(
                connection,
                transaction,
                """
                DELETE FROM quran_phrase_similarity_anchor_stats AS child
                WHERE EXISTS (
                  SELECT 1
                  FROM quran_phrase_index_builds AS build
                  CROSS JOIN quran_phrase_index_state AS state
                  WHERE build.id = child.build_id
                    AND state.id = 1
                    AND state.active_build_id IS DISTINCT FROM build.id
                    AND state.previous_build_id IS DISTINCT FROM build.id
                );
                DELETE FROM quran_phrase_similarity_edges AS child
                WHERE EXISTS (
                  SELECT 1
                  FROM quran_phrase_index_builds AS build
                  CROSS JOIN quran_phrase_index_state AS state
                  WHERE build.id = child.build_id
                    AND state.id = 1
                    AND state.active_build_id IS DISTINCT FROM build.id
                    AND state.previous_build_id IS DISTINCT FROM build.id
                );
                DELETE FROM quran_phrase_occurrences AS child
                WHERE EXISTS (
                  SELECT 1
                  FROM quran_phrase_index_builds AS build
                  CROSS JOIN quran_phrase_index_state AS state
                  WHERE build.id = child.build_id
                    AND state.id = 1
                    AND state.active_build_id IS DISTINCT FROM build.id
                    AND state.previous_build_id IS DISTINCT FROM build.id
                );
                DELETE FROM quran_phrase_variants AS child
                WHERE EXISTS (
                  SELECT 1
                  FROM quran_phrase_index_builds AS build
                  CROSS JOIN quran_phrase_index_state AS state
                  WHERE build.id = child.build_id
                    AND state.id = 1
                    AND state.active_build_id IS DISTINCT FROM build.id
                    AND state.previous_build_id IS DISTINCT FROM build.id
                );
                DELETE FROM quran_phrase_search_tokens AS child
                WHERE EXISTS (
                  SELECT 1
                  FROM quran_phrase_index_builds AS build
                  CROSS JOIN quran_phrase_index_state AS state
                  WHERE build.id = child.build_id
                    AND state.id = 1
                    AND state.active_build_id IS DISTINCT FROM build.id
                    AND state.previous_build_id IS DISTINCT FROM build.id
                );
                UPDATE quran_phrase_index_builds AS build
                SET status = 5,
                    exact_ready = false,
                    similarity_ready = false,
                    failed_at_utc = @now,
                    completed_at_utc = @now,
                    validation_verdict = 'fail',
                    failure_summary = 'source-invalidated-before-activation'
                FROM quran_phrase_index_state AS state
                WHERE build.status IN (1, 2)
                  AND state.id = 1
                  AND state.active_build_id IS DISTINCT FROM build.id
                  AND state.previous_build_id IS DISTINCT FROM build.id;
                UPDATE quran_phrase_index_builds AS build
                SET exact_ready = false,
                    similarity_ready = false
                FROM quran_phrase_index_state AS state
                WHERE build.status = 5
                  AND state.id = 1
                  AND state.active_build_id IS DISTINCT FROM build.id
                  AND state.previous_build_id IS DISTINCT FROM build.id;
                DELETE FROM quran_phrase_index_builds AS build
                USING quran_phrase_index_state AS state
                WHERE build.status IN (3, 4)
                  AND state.id = 1
                  AND state.active_build_id IS DISTINCT FROM build.id
                  AND state.previous_build_id IS DISTINCT FROM build.id
                """,
                ct,
                new NpgsqlParameter("now", DateTimeOffset.UtcNow));
            await transaction.CommitAsync(ct);
            return PhraseIndexCleanupResult.Completed;
        }
        catch (Exception)
        {
            return PhraseIndexCleanupResult.Pending(PendingWarning);
        }
    }

    private static Task AcquireBuilderFenceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct) => ExecuteAsync(
            connection,
            transaction,
            "SELECT pg_advisory_xact_lock(@namespace, @index_build_key)",
            ct,
            new NpgsqlParameter("namespace", PhraseSourceStateCoordinatorContract.AdvisoryLockNamespace),
            new NpgsqlParameter("index_build_key", PhraseSourceStateCoordinatorContract.IndexBuildLockKey));

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

}

internal sealed record PhraseIndexCleanupResult(bool Succeeded, string? Warning)
{
    internal static PhraseIndexCleanupResult Completed { get; } = new(true, null);

    internal static PhraseIndexCleanupResult Pending(string warning) => new(false, warning);
}
