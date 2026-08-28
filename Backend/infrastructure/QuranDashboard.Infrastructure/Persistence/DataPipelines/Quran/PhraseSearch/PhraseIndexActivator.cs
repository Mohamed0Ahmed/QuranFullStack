using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexActivator
{
    private readonly PhraseSourceStateCoordinator sourceStateCoordinator;

    public PhraseIndexActivator(PhraseSourceStateCoordinator sourceStateCoordinator)
    {
        this.sourceStateCoordinator = sourceStateCoordinator;
    }

    internal async Task<PhraseIndexActivationResult> ActivateAsync(
        NpgsqlConnection connection,
        Guid buildId,
        long expectedSourceRevision,
        string expectedSourceFingerprint,
        CancellationToken ct)
    {
        try
        {
            return await ActivateCoreAsync(
                connection,
                buildId,
                expectedSourceRevision,
                expectedSourceFingerprint,
                ct);
        }
        catch (Exception)
        {
            var reconciliation = await TryReconcileAsync(connection, buildId);
            if (reconciliation is null)
            {
                return new PhraseIndexActivationResult(
                    false,
                    false,
                    false,
                    "activation-outcome-unresolved",
                    expectedSourceRevision,
                    expectedSourceFingerprint,
                    null,
                    null);
            }

            if (!reconciliation.Activated)
            {
                throw;
            }

            return reconciliation with
            {
                ReconciledAfterFailure = true,
                FailureReason = "activation-acknowledgement-failed",
            };
        }
    }

    private async Task<PhraseIndexActivationResult> ActivateCoreAsync(
        NpgsqlConnection connection,
        Guid buildId,
        long expectedSourceRevision,
        string expectedSourceFingerprint,
        CancellationToken ct)
    {
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await sourceStateCoordinator.LockSourceMutationAsync(connection, transaction, ct);
        var state = await sourceStateCoordinator.LockStateAsync(connection, transaction, ct);

        if (state.SourceRevision != expectedSourceRevision
            || !string.Equals(
                state.SourceFingerprint,
                expectedSourceFingerprint,
                StringComparison.Ordinal))
        {
            await MarkActivationFailureAsync(
                connection,
                transaction,
                buildId,
                "source-changed-before-activation",
                ct);
            await transaction.CommitAsync(ct);
            return new PhraseIndexActivationResult(
                true,
                false,
                false,
                "source-changed-before-activation",
                state.SourceRevision,
                state.SourceFingerprint ?? string.Empty,
                state.PreviousBuildId,
                state.ActiveBuildId);
        }

        var readiness = await ReadBuildReadinessAsync(connection, transaction, buildId, ct);
        if (!readiness.Exists
            || readiness.FormatVersion != PhraseIndexBuildConstants.FormatVersion
            || !readiness.ExactReady
            || !readiness.SimilarityReady
            || !string.Equals(
                readiness.SourceFingerprint,
                expectedSourceFingerprint,
                StringComparison.Ordinal))
        {
            await MarkActivationFailureAsync(
                connection,
                transaction,
                buildId,
                "build-not-compatible-or-ready",
                ct);
            await transaction.CommitAsync(ct);
            return new PhraseIndexActivationResult(
                true,
                false,
                false,
                "build-not-compatible-or-ready",
                state.SourceRevision,
                state.SourceFingerprint ?? string.Empty,
                state.PreviousBuildId,
                state.ActiveBuildId);
        }

        var now = DateTimeOffset.UtcNow;
        if (state.ActiveBuildId is not null)
        {
            await ExecuteAsync(
                connection,
                transaction,
                """
                UPDATE quran_phrase_index_builds
                SET status = 4,
                    completed_at_utc = COALESCE(completed_at_utc, @now)
                WHERE id = @active_build_id
                  AND status = 3
                """,
                ct,
                new NpgsqlParameter("now", now),
                new NpgsqlParameter("active_build_id", state.ActiveBuildId.Value));
        }

        var previousBuildParameter = new NpgsqlParameter("previous_build_id", NpgsqlDbType.Uuid)
        {
            Value = state.ActiveBuildId is null
                ? DBNull.Value
                : state.ActiveBuildId.Value,
        };
        await ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_builds
            SET status = 3,
                activated_at_utc = @now,
                completed_at_utc = @now
            WHERE id = @build_id;
            UPDATE quran_phrase_index_state
            SET active_build_id = @build_id,
                previous_build_id = @previous_build_id,
                is_stale = false,
                stale_reason = NULL,
                updated_at_utc = @now
            WHERE id = 1
            """,
            ct,
            new NpgsqlParameter("now", now),
            new NpgsqlParameter("build_id", buildId),
            previousBuildParameter);
        await transaction.CommitAsync(ct);

        return new PhraseIndexActivationResult(
            true,
            true,
            false,
            string.Empty,
            state.SourceRevision,
            state.SourceFingerprint ?? string.Empty,
            state.ActiveBuildId,
            buildId);
    }

    private static async Task<PhraseIndexActivationResult?> TryReconcileAsync(
        NpgsqlConnection connection,
        Guid buildId)
    {
        try
        {
            return await ReadActivationStateAsync(connection, buildId);
        }
        catch (Exception)
        {
            try
            {
                await connection.CloseAsync();
                await connection.OpenAsync(CancellationToken.None);
                return await ReadActivationStateAsync(connection, buildId);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    private static async Task<PhraseIndexActivationResult> ReadActivationStateAsync(
        NpgsqlConnection connection,
        Guid buildId)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT state.source_revision,
                   state.source_fingerprint,
                   state.previous_build_id,
                   state.active_build_id,
                   build.status
            FROM quran_phrase_index_state AS state
            LEFT JOIN quran_phrase_index_builds AS build
              ON build.id = @build_id
            WHERE state.id = 1
            """,
            connection)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        if (!await reader.ReadAsync(CancellationToken.None))
        {
            throw new InvalidOperationException("PhraseSearch source state singleton is missing.");
        }

        Guid? previousBuildId = reader.IsDBNull(2) ? null : reader.GetGuid(2);
        Guid? activeBuildId = reader.IsDBNull(3) ? null : reader.GetGuid(3);
        PhraseIndexBuildStatus? buildStatus = reader.IsDBNull(4)
            ? null
            : (PhraseIndexBuildStatus)reader.GetInt16(4);
        var activated = activeBuildId == buildId
            && buildStatus == PhraseIndexBuildStatus.Active;
        return new PhraseIndexActivationResult(
            true,
            activated,
            true,
            activated ? "activation-acknowledgement-failed" : string.Empty,
            reader.GetInt64(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            previousBuildId,
            activeBuildId);
    }

    private static async Task<BuildReadiness> ReadBuildReadinessAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT format_version, exact_ready, similarity_ready, source_fingerprint
            FROM quran_phrase_index_builds
            WHERE id = @build_id
            FOR UPDATE
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
            return new BuildReadiness(false, 0, false, false, string.Empty);
        }

        return new BuildReadiness(
            true,
            reader.GetInt32(0),
            reader.GetBoolean(1),
            reader.GetBoolean(2),
            reader.GetString(3));
    }

    private static Task MarkActivationFailureAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        string reason,
        CancellationToken ct) => ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_builds
            SET status = 5,
                failed_at_utc = @now,
                completed_at_utc = @now,
                validation_verdict = 'fail',
                failure_summary = @reason
            WHERE id = @build_id
            """,
            ct,
            new NpgsqlParameter("now", DateTimeOffset.UtcNow),
            new NpgsqlParameter("reason", reason),
            new NpgsqlParameter("build_id", buildId));

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

    private sealed record BuildReadiness(
        bool Exists,
        int FormatVersion,
        bool ExactReady,
        bool SimilarityReady,
        string SourceFingerprint);
}
