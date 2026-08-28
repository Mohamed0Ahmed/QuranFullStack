using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;
using Microsoft.Extensions.Options;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexBuildDatabase
{
    private readonly PhraseSourceSnapshotReader sourceReader;
    private readonly PhraseDatabaseStoragePreflight storagePreflight;
    private readonly PhraseIndexOptions options;

    public PhraseIndexBuildDatabase(
        PhraseSourceSnapshotReader sourceReader,
        PhraseDatabaseStoragePreflight storagePreflight,
        IOptions<PhraseIndexOptions> options)
    {
        this.sourceReader = sourceReader;
        this.storagePreflight = storagePreflight;
        this.options = options.Value;
    }

    internal Task AcquireBuilderLockAsync(NpgsqlConnection connection, CancellationToken ct) =>
        ExecuteWithoutTransactionAsync(
            connection,
            "SELECT pg_advisory_lock(@namespace, @key)",
            ct,
            new NpgsqlParameter("namespace", PhraseSourceStateCoordinatorContract.AdvisoryLockNamespace),
            new NpgsqlParameter("key", PhraseSourceStateCoordinatorContract.IndexBuildLockKey));

    internal Task ReleaseBuilderLockAsync(NpgsqlConnection connection) =>
        ExecuteWithoutTransactionAsync(
            connection,
            "SELECT pg_advisory_unlock(@namespace, @key)",
            CancellationToken.None,
            new NpgsqlParameter("namespace", PhraseSourceStateCoordinatorContract.AdvisoryLockNamespace),
            new NpgsqlParameter("key", PhraseSourceStateCoordinatorContract.IndexBuildLockKey));

    internal async Task<int> RecoverAbandonedBuildsAsync(
        NpgsqlConnection connection,
        CancellationToken ct)
    {
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var buildIds = new List<Guid>();
        await using (var command = new NpgsqlCommand(
                         """
                         SELECT build.id
                         FROM quran_phrase_index_builds AS build
                         CROSS JOIN quran_phrase_index_state AS state
                         WHERE state.id = 1
                           AND build.status IN (1, 2)
                           AND state.active_build_id IS DISTINCT FROM build.id
                           AND state.previous_build_id IS DISTINCT FROM build.id
                         ORDER BY build.started_at_utc, build.id
                         FOR UPDATE OF build, state
                         """,
                         connection,
                         transaction)
                     {
                         CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
                     })
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                buildIds.Add(reader.GetGuid(0));
            }
        }

        if (buildIds.Count == 0)
        {
            await transaction.CommitAsync(ct);
            return 0;
        }

        var buildIdValues = buildIds.ToArray();
        await using (var command = new NpgsqlCommand(
                         """
                         DELETE FROM quran_phrase_similarity_anchor_stats
                         WHERE build_id = ANY(@build_ids);
                         DELETE FROM quran_phrase_similarity_edges
                         WHERE build_id = ANY(@build_ids);
                         DELETE FROM quran_phrase_occurrences
                         WHERE build_id = ANY(@build_ids);
                         DELETE FROM quran_phrase_variants
                         WHERE build_id = ANY(@build_ids);
                         DELETE FROM quran_phrase_search_tokens
                         WHERE build_id = ANY(@build_ids);
                         """,
                         connection,
                         transaction)
                     {
                         CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
                     })
        {
            command.Parameters.Add(new NpgsqlParameter(
                "build_ids",
                NpgsqlDbType.Array | NpgsqlDbType.Uuid)
            {
                Value = buildIdValues,
            });
            await command.ExecuteNonQueryAsync(ct);
        }

        var recoveredAtUtc = DateTimeOffset.UtcNow;
        int recoveredCount;
        await using (var command = new NpgsqlCommand(
                         """
                         UPDATE quran_phrase_index_builds AS build
                         SET status = 5,
                             exact_ready = false,
                             similarity_ready = false,
                             failed_at_utc = @recovered_at_utc,
                             completed_at_utc = @recovered_at_utc,
                             validation_verdict = 'fail',
                             failure_summary = 'abandoned-before-activation'
                         WHERE build.id = ANY(@build_ids)
                           AND build.status IN (1, 2)
                           AND NOT EXISTS (
                             SELECT 1
                             FROM quran_phrase_index_state AS state
                             WHERE state.id = 1
                               AND (state.active_build_id = build.id
                                    OR state.previous_build_id = build.id)
                           )
                         """,
                         connection,
                         transaction)
                     {
                         CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
                     })
        {
            command.Parameters.Add(new NpgsqlParameter(
                "build_ids",
                NpgsqlDbType.Array | NpgsqlDbType.Uuid)
            {
                Value = buildIdValues,
            });
            command.Parameters.AddWithValue("recovered_at_utc", recoveredAtUtc);
            recoveredCount = await command.ExecuteNonQueryAsync(ct);
        }

        if (recoveredCount != buildIds.Count)
        {
            throw new InvalidOperationException(
                "Abandoned phrase index build recovery did not update every locked build.");
        }

        await transaction.CommitAsync(ct);
        return recoveredCount;
    }

    internal async Task<PhraseSourceSnapshot> ReadSourceSnapshotAsync(
        NpgsqlConnection connection,
        CancellationToken ct)
    {
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            ct);
        var state = await ReadStateAsync(connection, transaction, ct);
        var source = await sourceReader.ReadAsync(connection, transaction, ct);
        var fingerprint = source.Passed
            ? PhraseSourceFingerprint.Compute(source.Tokens)
            : string.Empty;
        await transaction.CommitAsync(ct);
        return new PhraseSourceSnapshot(
            state.SourceRevision,
            state.SourceFingerprint ?? string.Empty,
            fingerprint,
            state.ActiveBuildId,
            state.PreviousBuildId,
            source.Tokens,
            source.AyahCount,
            source.MaximumAyahLength,
            source.Checks);
    }

    internal async Task<bool> HasActiveBuildAsync(
        NpgsqlConnection connection,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            "SELECT active_build_id IS NOT NULL FROM quran_phrase_index_state WHERE id = 1",
            connection)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    internal Task CreateBuildAsync(
        NpgsqlConnection connection,
        Guid buildId,
        PhraseSourceSnapshot snapshot,
        DateTimeOffset startedAtUtc,
        CancellationToken ct) => ExecuteWithoutTransactionAsync(
            connection,
            """
            INSERT INTO quran_phrase_index_builds (
              id,
              status,
              format_version,
              exact_ready,
              similarity_ready,
              builder_version,
              source_revision,
              source_fingerprint,
              started_at_utc,
              search_token_count,
              variant_count,
              occurrence_count,
              similarity_edge_count,
              similarity_anchor_stat_count
            ) VALUES (
              @build_id,
              1,
              @format_version,
              false,
              false,
              @builder_version,
              @source_revision,
              @source_fingerprint,
              @started_at_utc,
              0,
              0,
              0,
              0,
              0
            )
            """,
            ct,
            new NpgsqlParameter("build_id", buildId),
            new NpgsqlParameter("format_version", PhraseIndexBuildConstants.FormatVersion),
            new NpgsqlParameter("builder_version", PhraseIndexBuildConstants.BuilderVersion),
            new NpgsqlParameter("source_revision", snapshot.SourceRevision),
            new NpgsqlParameter("source_fingerprint", snapshot.SourceFingerprint),
            new NpgsqlParameter("started_at_utc", startedAtUtc));

    internal Task MarkValidatedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        PhraseIndexBuildTotals totals,
        CancellationToken ct) => ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_builds
            SET status = 2,
                exact_ready = true,
                similarity_ready = true,
                validated_at_utc = @now,
                search_token_count = @search_tokens,
                variant_count = @variants,
                occurrence_count = @occurrences,
                similarity_edge_count = @edges,
                similarity_anchor_stat_count = @anchor_stats,
                validation_verdict = 'pass'
            WHERE id = @build_id
            """,
            ct,
            new NpgsqlParameter("now", DateTimeOffset.UtcNow),
            new NpgsqlParameter("search_tokens", totals.SearchTokens),
            new NpgsqlParameter("variants", totals.Variants),
            new NpgsqlParameter("occurrences", totals.Occurrences),
            new NpgsqlParameter("edges", totals.SimilarityEdges),
            new NpgsqlParameter("anchor_stats", totals.SimilarityAnchorStats),
            new NpgsqlParameter("build_id", buildId));

    internal Task MarkFailedAsync(
        NpgsqlConnection connection,
        Guid buildId,
        string verdict,
        string failureSummary,
        CancellationToken ct) => ExecuteWithoutTransactionAsync(
            connection,
            """
            UPDATE quran_phrase_index_builds
            SET status = 5,
                exact_ready = false,
                similarity_ready = false,
                failed_at_utc = @now,
                completed_at_utc = @now,
                validation_verdict = @verdict,
                failure_summary = @failure_summary
            WHERE id = @build_id
              AND status <> 3
            """,
            ct,
            new NpgsqlParameter("now", DateTimeOffset.UtcNow),
            new NpgsqlParameter("verdict", verdict),
            new NpgsqlParameter("failure_summary", failureSummary),
            new NpgsqlParameter("build_id", buildId));

    internal Task RecordReportPathAsync(
        NpgsqlConnection connection,
        Guid buildId,
        string reportDirectory,
        CancellationToken ct) => ExecuteWithoutTransactionAsync(
            connection,
            "UPDATE quran_phrase_index_builds SET report_path = @path WHERE id = @build_id",
            ct,
            new NpgsqlParameter("path", reportDirectory),
            new NpgsqlParameter("build_id", buildId));

    internal async Task<PhraseIndexGenerationState> CleanupFailedGenerationAsync(
        NpgsqlConnection connection,
        Guid buildId,
        CancellationToken ct)
    {
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await ExecuteAsync(
            connection,
            transaction,
            """
            DELETE FROM quran_phrase_similarity_anchor_stats AS child
            WHERE child.build_id = @build_id
              AND EXISTS (
                SELECT 1
                FROM quran_phrase_index_builds AS build
                CROSS JOIN quran_phrase_index_state AS state
                WHERE build.id = child.build_id
                  AND build.status = 5
                  AND state.id = 1
                  AND state.active_build_id IS DISTINCT FROM build.id
                  AND state.previous_build_id IS DISTINCT FROM build.id
              );
            DELETE FROM quran_phrase_similarity_edges AS child
            WHERE child.build_id = @build_id
              AND EXISTS (
                SELECT 1
                FROM quran_phrase_index_builds AS build
                CROSS JOIN quran_phrase_index_state AS state
                WHERE build.id = child.build_id
                  AND build.status = 5
                  AND state.id = 1
                  AND state.active_build_id IS DISTINCT FROM build.id
                  AND state.previous_build_id IS DISTINCT FROM build.id
              );
            DELETE FROM quran_phrase_occurrences AS child
            WHERE child.build_id = @build_id
              AND EXISTS (
                SELECT 1
                FROM quran_phrase_index_builds AS build
                CROSS JOIN quran_phrase_index_state AS state
                WHERE build.id = child.build_id
                  AND build.status = 5
                  AND state.id = 1
                  AND state.active_build_id IS DISTINCT FROM build.id
                  AND state.previous_build_id IS DISTINCT FROM build.id
              );
            DELETE FROM quran_phrase_variants AS child
            WHERE child.build_id = @build_id
              AND EXISTS (
                SELECT 1
                FROM quran_phrase_index_builds AS build
                CROSS JOIN quran_phrase_index_state AS state
                WHERE build.id = child.build_id
                  AND build.status = 5
                  AND state.id = 1
                  AND state.active_build_id IS DISTINCT FROM build.id
                  AND state.previous_build_id IS DISTINCT FROM build.id
              );
            DELETE FROM quran_phrase_search_tokens AS child
            WHERE child.build_id = @build_id
              AND EXISTS (
                SELECT 1
                FROM quran_phrase_index_builds AS build
                CROSS JOIN quran_phrase_index_state AS state
                WHERE build.id = child.build_id
                  AND build.status = 5
                  AND state.id = 1
                  AND state.active_build_id IS DISTINCT FROM build.id
                  AND state.previous_build_id IS DISTINCT FROM build.id
              );
            UPDATE quran_phrase_index_builds
            SET exact_ready = false,
                similarity_ready = false
            WHERE id = @build_id
              AND status = 5
              AND NOT EXISTS (
                SELECT 1
                FROM quran_phrase_index_state AS state
                WHERE state.id = 1
                  AND (state.active_build_id = @build_id OR state.previous_build_id = @build_id)
              );
            """,
            ct,
            new NpgsqlParameter("build_id", buildId));

        var state = await ReadGenerationStateAsync(connection, transaction, buildId, ct);
        await transaction.CommitAsync(ct);
        return state;
    }

    internal Task CleanupEligibleSupersededBuildsAsync(
        NpgsqlConnection connection,
        CancellationToken ct) => ExecuteWithoutTransactionAsync(
            connection,
            """
            DELETE FROM quran_phrase_index_builds AS build
            WHERE build.status = 4
              AND build.completed_at_utc < @cutoff
              AND NOT EXISTS (
                SELECT 1
                FROM quran_phrase_index_state AS state
                WHERE state.id = 1
                  AND (state.active_build_id = build.id OR state.previous_build_id = build.id)
              )
            """,
            ct,
            new NpgsqlParameter(
                "cutoff",
                DateTimeOffset.UtcNow.AddMinutes(-options.CleanupGraceMinutes)));

    internal Task CleanupExpiredFailedBuildAuditsAsync(
        NpgsqlConnection connection,
        CancellationToken ct) => ExecuteWithoutTransactionAsync(
            connection,
            """
            DELETE FROM quran_phrase_index_builds AS build
            WHERE build.status = 5
              AND build.report_path IS NOT NULL
              AND build.completed_at_utc < @cutoff
              AND NOT EXISTS (
                SELECT 1
                FROM quran_phrase_index_state AS state
                WHERE state.id = 1
                  AND (state.active_build_id = build.id OR state.previous_build_id = build.id)
              )
            """,
            ct,
            new NpgsqlParameter(
                "cutoff",
                DateTimeOffset.UtcNow.AddDays(-options.FailedBuildRetentionDays)));

    internal Task<PhraseDiskPreflight> ReadDiskPreflightAsync(
        NpgsqlConnection connection,
        CancellationToken ct) => storagePreflight.ReadAsync(connection, ct);

    private static async Task<PhraseIndexGenerationState> ReadGenerationStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                     SELECT 1 FROM quran_phrase_search_tokens WHERE build_id = @build_id
                     UNION ALL
                     SELECT 1 FROM quran_phrase_variants WHERE build_id = @build_id
                     UNION ALL
                     SELECT 1 FROM quran_phrase_occurrences WHERE build_id = @build_id
                     UNION ALL
                     SELECT 1 FROM quran_phrase_similarity_edges WHERE build_id = @build_id
                     UNION ALL
                     SELECT 1 FROM quran_phrase_similarity_anchor_stats WHERE build_id = @build_id
                   ),
                   COALESCE(build.exact_ready, false),
                   COALESCE(build.similarity_ready, false)
            FROM (VALUES (1)) AS singleton(value)
            LEFT JOIN quran_phrase_index_builds AS build ON build.id = @build_id
            """,
            connection,
            transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException("Phrase index generation state could not be observed.");
        }

        return new PhraseIndexGenerationState(
            reader.GetBoolean(0),
            reader.GetBoolean(1),
            reader.GetBoolean(2));
    }

    private static async Task<PhraseSourceState> ReadStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT source_revision,
                   source_fingerprint,
                   active_build_id,
                   previous_build_id,
                   is_stale,
                   stale_reason
            FROM quran_phrase_index_state
            WHERE id = 1
            """,
            connection,
            transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException("PhraseSearch source state singleton is missing.");
        }

        return new PhraseSourceState(
            reader.GetInt64(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetBoolean(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    private static Task ExecuteWithoutTransactionAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken ct,
        params NpgsqlParameter[] parameters) => ExecuteAsync(
            connection,
            transaction: null,
            sql,
            ct,
            parameters);

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
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
