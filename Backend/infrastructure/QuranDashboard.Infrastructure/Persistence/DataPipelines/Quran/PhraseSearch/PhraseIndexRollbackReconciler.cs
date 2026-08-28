using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal static class PhraseIndexRollbackReconciler
{
    internal static async Task<PhraseIndexRollbackExecution> ReconcileAsync(
        NpgsqlConnection connection,
        PhraseSourceStateCoordinator sourceStateCoordinator,
        PhraseIndexRollbackTarget target)
    {
        var observed = await TryReadStateAsync(connection, sourceStateCoordinator, target);
        if (observed is null
            && await TryReopenConnectionAsync(connection))
        {
            observed = await TryReadStateAsync(connection, sourceStateCoordinator, target);
        }

        if (observed is null)
        {
            return OutcomeUnknown(target, null);
        }

        if (MatchesCommittedState(observed, target))
        {
            return new PhraseIndexRollbackExecution(
                PhraseIndexRollbackOutcome.ReconciledAfterCommitFailure,
                PhraseIndexRollbackRetryDirective.NotApplicable,
                "Phrase index rollback activated the compatible previous generation; "
                    + "the commit acknowledgement failed, but the intended state was reconciled.",
                target.IntendedActiveBuildId,
                target.OriginalActiveBuildId,
                target.SourceRevision,
                target.SourceFingerprint);
        }

        if (MatchesOriginalState(observed, target))
        {
            return new PhraseIndexRollbackExecution(
                PhraseIndexRollbackOutcome.RetrySafeFailure,
                PhraseIndexRollbackRetryDirective.SafeToRetry,
                "Phrase index rollback was not committed. The same rollback may be retried safely.",
                observed.State.ActiveBuildId,
                observed.State.PreviousBuildId,
                observed.State.SourceRevision,
                observed.State.SourceFingerprint ?? string.Empty);
        }

        return OutcomeUnknown(target, observed.State);
    }

    internal static async Task DisposeTransactionSafelyAsync(NpgsqlTransaction transaction)
    {
        try
        {
            await transaction.DisposeAsync();
        }
        catch (Exception)
        {
        }
    }

    private static async Task<PhraseIndexRollbackObservedState?> TryReadStateAsync(
        NpgsqlConnection connection,
        PhraseSourceStateCoordinator sourceStateCoordinator,
        PhraseIndexRollbackTarget target)
    {
        NpgsqlTransaction? transaction = null;
        try
        {
            transaction = await connection.BeginTransactionAsync(CancellationToken.None);
            await sourceStateCoordinator.LockSourceMutationAsync(
                connection,
                transaction,
                CancellationToken.None);
            var state = await sourceStateCoordinator.LockStateAsync(
                connection,
                transaction,
                CancellationToken.None);
            var originalActive = await ReadBuildStateAsync(
                connection,
                transaction,
                target.OriginalActiveBuildId);
            var intendedActive = await ReadBuildStateAsync(
                connection,
                transaction,
                target.IntendedActiveBuildId);
            await transaction.RollbackAsync(CancellationToken.None);
            return new PhraseIndexRollbackObservedState(state, originalActive, intendedActive);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (transaction is not null)
            {
                await DisposeTransactionSafelyAsync(transaction);
            }
        }
    }

    private static async Task<PhraseIndexRollbackObservedBuild?> ReadBuildStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT status, activated_at_utc
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
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        if (!await reader.ReadAsync(CancellationToken.None))
        {
            return null;
        }

        return new PhraseIndexRollbackObservedBuild(
            (PhraseIndexBuildStatus)reader.GetInt16(0),
            reader.IsDBNull(1) ? null : reader.GetFieldValue<DateTimeOffset>(1));
    }

    private static async Task<bool> TryReopenConnectionAsync(NpgsqlConnection connection)
    {
        try
        {
            await connection.CloseAsync();
            await connection.OpenAsync(CancellationToken.None);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool MatchesCommittedState(
        PhraseIndexRollbackObservedState observed,
        PhraseIndexRollbackTarget target) =>
        MatchesSourceState(observed.State, target)
        && observed.State.ActiveBuildId == target.IntendedActiveBuildId
        && observed.State.PreviousBuildId == target.OriginalActiveBuildId
        && observed.OriginalActive?.Status == PhraseIndexBuildStatus.Superseded
        && observed.IntendedActive?.Status == PhraseIndexBuildStatus.Active
        && observed.IntendedActive.ActivatedAtUtc == target.AttemptedAtUtc;

    private static bool MatchesOriginalState(
        PhraseIndexRollbackObservedState observed,
        PhraseIndexRollbackTarget target) =>
        MatchesSourceState(observed.State, target)
        && observed.State.ActiveBuildId == target.OriginalActiveBuildId
        && observed.State.PreviousBuildId == target.IntendedActiveBuildId
        && observed.OriginalActive?.Status == PhraseIndexBuildStatus.Active
        && observed.IntendedActive?.Status == PhraseIndexBuildStatus.Superseded
        && observed.OriginalActive.ActivatedAtUtc == target.OriginalActiveActivatedAtUtc
        && observed.IntendedActive.ActivatedAtUtc == target.IntendedActiveActivatedAtUtc;

    private static bool MatchesSourceState(
        PhraseSourceState state,
        PhraseIndexRollbackTarget target) =>
        !state.IsStale
        && state.SourceRevision == target.SourceRevision
        && string.Equals(state.SourceFingerprint, target.SourceFingerprint, StringComparison.Ordinal);

    private static PhraseIndexRollbackExecution OutcomeUnknown(
        PhraseIndexRollbackTarget target,
        PhraseSourceState? observedState) => new(
            PhraseIndexRollbackOutcome.RollbackOutcomeUnknown,
            PhraseIndexRollbackRetryDirective.DoNotRetry,
            "Phrase index rollback outcome could not be reconciled. Do not retry rollback-phrase-index; "
                + "inspect the active and previous build IDs before any operator action.",
            observedState?.ActiveBuildId,
            observedState?.PreviousBuildId,
            observedState?.SourceRevision ?? target.SourceRevision,
            observedState?.SourceFingerprint ?? target.SourceFingerprint);

    private sealed record PhraseIndexRollbackObservedState(
        PhraseSourceState State,
        PhraseIndexRollbackObservedBuild? OriginalActive,
        PhraseIndexRollbackObservedBuild? IntendedActive);

    private sealed record PhraseIndexRollbackObservedBuild(
        PhraseIndexBuildStatus Status,
        DateTimeOffset? ActivatedAtUtc);
}

internal sealed record PhraseIndexRollbackTarget(
    Guid OriginalActiveBuildId,
    Guid IntendedActiveBuildId,
    long SourceRevision,
    string SourceFingerprint,
    DateTimeOffset AttemptedAtUtc,
    DateTimeOffset? OriginalActiveActivatedAtUtc,
    DateTimeOffset? IntendedActiveActivatedAtUtc);
