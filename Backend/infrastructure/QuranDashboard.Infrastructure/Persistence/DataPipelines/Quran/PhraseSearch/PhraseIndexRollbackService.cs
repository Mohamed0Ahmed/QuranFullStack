using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexRollbackService : IPhraseIndexRollback
{
    private readonly QuranDashboardDbContext dbContext;
    private readonly PhraseSourceStateCoordinator sourceStateCoordinator;
    private readonly PhraseSourceSnapshotReader sourceReader;

    public PhraseIndexRollbackService(
        QuranDashboardDbContext dbContext,
        PhraseSourceStateCoordinator sourceStateCoordinator,
        PhraseSourceSnapshotReader sourceReader)
    {
        this.dbContext = dbContext;
        this.sourceStateCoordinator = sourceStateCoordinator;
        this.sourceReader = sourceReader;
    }

    public async Task<PhraseIndexRollbackExecution> RollbackAsync(CancellationToken ct)
    {
        var connection = await OpenConnectionAsync(ct);
        var transaction = await connection.BeginTransactionAsync(ct);
        var transactionDisposed = false;

        try
        {
            await sourceStateCoordinator.LockSourceMutationAsync(connection, transaction, ct);
            var state = await sourceStateCoordinator.LockStateAsync(connection, transaction, ct);

            if (state.IsStale
                || state.ActiveBuildId is null
                || state.PreviousBuildId is null)
            {
                await transaction.RollbackAsync(ct);
                return Failure(state, "No compatible previous phrase generation is available.");
            }

            var source = await sourceReader.ReadAsync(connection, transaction, ct);
            if (!source.Passed)
            {
                state = await PhraseSourceStateCoordinator.InvalidateRejectedSourceAsync(
                    connection,
                    transaction,
                    state,
                    PhraseSourceStateCoordinator.SourceIntegrityCheckFailedReason);
                return Failure(state, "Phrase source integrity checks failed; rollback was not applied.");
            }

            var fingerprint = PhraseSourceFingerprint.Compute(source.Tokens);
            if (PhraseIndexBuildConstants.ApprovedSourceFingerprintVersion
                    != PhraseIndexBuildConstants.SourceFingerprintVersion
                || !string.Equals(
                    PhraseIndexBuildConstants.ApprovedSourceFingerprint,
                    fingerprint,
                    StringComparison.Ordinal))
            {
                state = await PhraseSourceStateCoordinator.InvalidateRejectedSourceAsync(
                    connection,
                    transaction,
                    state,
                    PhraseSourceStateCoordinator.SourceApprovalRequiredReason);
                return Failure(state, "Phrase source approval changed; rollback was not applied.");
            }

            if (!string.Equals(state.SourceFingerprint, fingerprint, StringComparison.Ordinal))
            {
                state = await PhraseSourceStateCoordinator.InvalidateRejectedSourceAsync(
                    connection,
                    transaction,
                    state,
                    PhraseSourceStateCoordinator.SourceFingerprintChangedReason);
                return Failure(state, "Phrase source fingerprint changed; rollback was not applied.");
            }

            var originalActiveBuildId = state.ActiveBuildId.Value;
            var intendedActiveBuildId = state.PreviousBuildId.Value;
            var active = await ReadBuildAsync(connection, transaction, originalActiveBuildId, ct);
            var previous = await ReadBuildAsync(connection, transaction, intendedActiveBuildId, ct);
            if (active is null
                || active.Status != PhraseIndexBuildStatus.Active
                || previous is null
                || previous.Status != PhraseIndexBuildStatus.Superseded
                || previous.FormatVersion != PhraseIndexBuildConstants.FormatVersion
                || !previous.ExactReady
                || !previous.SimilarityReady
                || previous.SourceRevision != state.SourceRevision
                || !string.Equals(previous.SourceFingerprint, fingerprint, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(ct);
                return Failure(state, "The previous phrase generation is not source-compatible and ready.");
            }

            var target = new PhraseIndexRollbackTarget(
                originalActiveBuildId,
                intendedActiveBuildId,
                state.SourceRevision,
                fingerprint,
                TruncateToMicroseconds(DateTimeOffset.UtcNow),
                active.ActivatedAtUtc,
                previous.ActivatedAtUtc);
            await ApplyRollbackAsync(connection, transaction, target, ct);

            try
            {
                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await PhraseIndexRollbackReconciler.DisposeTransactionSafelyAsync(transaction);
                transactionDisposed = true;
                return await PhraseIndexRollbackReconciler.ReconcileAsync(
                    connection,
                    sourceStateCoordinator,
                    target);
            }

            return Success(
                PhraseIndexRollbackOutcome.Succeeded,
                "Phrase index rollback activated the compatible previous generation.",
                target);
        }
        finally
        {
            if (!transactionDisposed)
            {
                await PhraseIndexRollbackReconciler.DisposeTransactionSafelyAsync(transaction);
            }
        }
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), TimeSpan.Zero);

    private static PhraseIndexRollbackExecution Success(
        PhraseIndexRollbackOutcome outcome,
        string message,
        PhraseIndexRollbackTarget target) => new(
            outcome,
            PhraseIndexRollbackRetryDirective.NotApplicable,
            message,
            target.IntendedActiveBuildId,
            target.OriginalActiveBuildId,
            target.SourceRevision,
            target.SourceFingerprint);

    private static async Task ApplyRollbackAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PhraseIndexRollbackTarget target,
        CancellationToken ct)
    {
        await ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_builds
            SET status = 4,
                completed_at_utc = COALESCE(completed_at_utc, @now)
            WHERE id = @original_active_build_id
              AND status = 3;
            UPDATE quran_phrase_index_builds
            SET status = 3,
                activated_at_utc = @now,
                completed_at_utc = @now
            WHERE id = @intended_active_build_id
              AND status = 4;
            UPDATE quran_phrase_index_state
            SET active_build_id = @intended_active_build_id,
                previous_build_id = @original_active_build_id,
                is_stale = false,
                stale_reason = NULL,
                updated_at_utc = @now
            WHERE id = 1
              AND active_build_id = @original_active_build_id
              AND previous_build_id = @intended_active_build_id
              AND source_revision = @source_revision
              AND source_fingerprint = @source_fingerprint
            """,
            ct,
            new NpgsqlParameter("now", target.AttemptedAtUtc),
            new NpgsqlParameter("original_active_build_id", target.OriginalActiveBuildId),
            new NpgsqlParameter("intended_active_build_id", target.IntendedActiveBuildId),
            new NpgsqlParameter("source_revision", target.SourceRevision),
            new NpgsqlParameter("source_fingerprint", target.SourceFingerprint));
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for phrase index rollback.");
        }

        if (npgsqlConnection.State != ConnectionState.Open)
        {
            await npgsqlConnection.OpenAsync(ct);
        }

        return npgsqlConnection;
    }

    private static async Task<CompatibleBuild?> ReadBuildAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT status,
                   format_version,
                   exact_ready,
                   similarity_ready,
                   source_revision,
                   source_fingerprint,
                   activated_at_utc
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
            return null;
        }

        return new CompatibleBuild(
            (PhraseIndexBuildStatus)reader.GetInt16(0),
            reader.GetInt32(1),
            reader.GetBoolean(2),
            reader.GetBoolean(3),
            reader.GetInt64(4),
            reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6));
    }

    private static PhraseIndexRollbackExecution Failure(
        PhraseSourceState state,
        string message) => new(
            PhraseIndexRollbackOutcome.Refused,
            PhraseIndexRollbackRetryDirective.NotApplicable,
            message,
            state.ActiveBuildId,
            state.PreviousBuildId,
            state.SourceRevision,
            state.SourceFingerprint ?? string.Empty);

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

    private sealed record CompatibleBuild(
        PhraseIndexBuildStatus Status,
        int FormatVersion,
        bool ExactReady,
        bool SimilarityReady,
        long SourceRevision,
        string SourceFingerprint,
        DateTimeOffset? ActivatedAtUtc);
}
