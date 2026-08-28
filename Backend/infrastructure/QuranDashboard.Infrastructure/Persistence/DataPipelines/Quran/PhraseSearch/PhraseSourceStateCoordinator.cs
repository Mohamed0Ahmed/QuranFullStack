namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

public sealed class PhraseSourceStateCoordinator
{
    internal const string SourceApprovalRequiredReason = "source-approval-required";
    internal const string SourceFingerprintChangedReason = "source-fingerprint-changed";
    internal const string SourceIntegrityCheckFailedReason = "source-integrity-check-failed";

    private const string LockSourceMutationSql = """
        SELECT pg_advisory_xact_lock(@namespace, @index_build_key);
        SELECT pg_advisory_xact_lock(@namespace, @source_mutation_key)
        """;

    private const string LockStateSql = """
        SELECT source_revision,
               source_fingerprint,
               active_build_id,
               is_stale,
               stale_reason
        FROM quran_phrase_index_state
        WHERE id = 1
        FOR UPDATE
        """;

    private readonly PhraseSourceSnapshotReader sourceReader;

    public PhraseSourceStateCoordinator(PhraseSourceSnapshotReader sourceReader)
    {
        this.sourceReader = sourceReader;
    }

    internal async Task<PhraseSourceBootstrapResult> BootstrapAsync(
        NpgsqlConnection connection,
        string approvedFingerprint,
        int approvedFingerprintVersion,
        CancellationToken ct)
    {
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await LockSourceMutationAsync(connection, transaction, ct);
        var state = await LockStateAsync(connection, transaction, ct);
        var source = await sourceReader.ReadAsync(connection, transaction, ct);

        if (!source.Passed)
        {
            state = await InvalidateRejectedSourceAsync(
                connection,
                transaction,
                state,
                SourceIntegrityCheckFailedReason);
            var cleanup = await CleanupUnreferencedGenerationsAsync(
                connection,
                CancellationToken.None);
            return new PhraseSourceBootstrapResult(
                state,
                source,
                string.Empty,
                cleanup.Warning);
        }

        var fingerprint = PhraseSourceFingerprint.Compute(source.Tokens);
        if (approvedFingerprintVersion != PhraseIndexBuildConstants.SourceFingerprintVersion
            || !string.Equals(approvedFingerprint, fingerprint, StringComparison.Ordinal))
        {
            state = await InvalidateRejectedSourceAsync(
                connection,
                transaction,
                state,
                SourceApprovalRequiredReason);
            var cleanup = await CleanupUnreferencedGenerationsAsync(
                connection,
                CancellationToken.None);
            return new PhraseSourceBootstrapResult(
                state,
                source,
                fingerprint,
                cleanup.Warning);
        }

        if (state.SourceFingerprint is null)
        {
            await UpdateInitializedStateAsync(connection, transaction, state.SourceRevision + 1, fingerprint, ct);
            state = state with
            {
                SourceRevision = state.SourceRevision + 1,
                SourceFingerprint = fingerprint,
                IsStale = false,
                StaleReason = null,
            };
        }
        else if (!string.Equals(state.SourceFingerprint, fingerprint, StringComparison.Ordinal))
        {
            await InvalidateBuildPointersAsync(
                connection,
                transaction,
                state.SourceRevision + 1,
                approvedFingerprint: fingerprint,
                reason: SourceFingerprintChangedReason,
                ct: ct);
            state = state with
            {
                SourceRevision = state.SourceRevision + 1,
                SourceFingerprint = fingerprint,
                ActiveBuildId = null,
                IsStale = true,
                StaleReason = SourceFingerprintChangedReason,
            };
        }

        await transaction.CommitAsync(ct);
        var completedCleanup = await CleanupUnreferencedGenerationsAsync(
            connection,
            CancellationToken.None);
        return new PhraseSourceBootstrapResult(
            state,
            source,
            fingerprint,
            completedCleanup.Warning);
    }

    internal async Task LockSourceMutationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(LockSourceMutationSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("namespace", PhraseSourceStateCoordinatorContract.AdvisoryLockNamespace);
        command.Parameters.AddWithValue("index_build_key", PhraseSourceStateCoordinatorContract.IndexBuildLockKey);
        command.Parameters.AddWithValue("source_mutation_key", PhraseSourceStateCoordinatorContract.SourceMutationLockKey);
        await command.ExecuteNonQueryAsync(ct);
    }

    internal async Task<PhraseSourceState> LockStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(LockStateSql, connection, transaction)
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
            reader.GetBoolean(3),
            reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    internal async Task BeginFoundationResetAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await LockStateAsync(connection, transaction, ct);

        await ExecuteExactlyOneAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_state
            SET active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = true,
                stale_reason = 'foundation-import',
                updated_at_utc = @now
            WHERE id = 1
            """,
            ct,
            "PhraseSearch foundation reset invalidation",
            new NpgsqlParameter("now", DateTimeOffset.UtcNow));
    }

    internal Task CompleteFoundationResetAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct) => ExecuteExactlyOneAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_state
            SET source_revision = source_revision + 1,
                source_fingerprint = NULL,
                active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = true,
                stale_reason = 'foundation-import',
                updated_at_utc = @now
            WHERE id = 1
            """,
            ct,
            "PhraseSearch foundation reset completion",
            new NpgsqlParameter("now", DateTimeOffset.UtcNow));

    internal Task<PhraseIndexCleanupResult> CleanupUnreferencedGenerationsAsync(
        NpgsqlConnection connection,
        CancellationToken ct) => PhraseIndexGenerationCleanup.CleanupAfterSourceInvalidationAsync(
            connection,
            ct);

    internal async Task<PhraseSourceReadResult> RefreshAfterDisplayRebuildAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int expectedReadableWords,
        CancellationToken ct)
    {
        var state = await LockStateAsync(connection, transaction, ct);
        var source = await sourceReader.ReadAsync(
            connection,
            transaction,
            ct,
            expectedReadableWords,
            expectedAyahs: null);
        if (!source.Passed)
        {
            return source;
        }

        var fingerprint = PhraseSourceFingerprint.Compute(source.Tokens);
        if (string.Equals(state.SourceFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return source;
        }

        await InvalidateBuildPointersAsync(
            connection,
            transaction,
            state.SourceRevision + 1,
            approvedFingerprint: fingerprint,
            reason: "display-word-source-changed",
            ct: ct);
        return source;
    }

    internal async Task<PhraseSourceState> InvalidateRejectedSourceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PhraseSourceState state,
        string reason)
    {
        var invalidatedRevision = state.SourceRevision + 1;
        await InvalidateBuildPointersAsync(
            connection,
            transaction,
            invalidatedRevision,
            approvedFingerprint: null,
            reason: reason,
            ct: CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return state with
        {
            SourceRevision = invalidatedRevision,
            ActiveBuildId = null,
            IsStale = true,
            StaleReason = reason,
        };
    }

    private static Task UpdateInitializedStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long revision,
        string fingerprint,
        CancellationToken ct) => ExecuteExactlyOneAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_state
            SET source_revision = @revision,
                source_fingerprint = @fingerprint,
                is_stale = false,
                stale_reason = NULL,
                updated_at_utc = @now
            WHERE id = 1
            """,
            ct,
            "PhraseSearch source state initialization",
            new NpgsqlParameter("revision", revision),
            new NpgsqlParameter("fingerprint", fingerprint),
            new NpgsqlParameter("now", DateTimeOffset.UtcNow));

    private static Task InvalidateBuildPointersAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long revision,
        string? approvedFingerprint,
        string reason,
        CancellationToken ct) => ExecuteExactlyOneAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_state
            SET source_revision = @revision,
                source_fingerprint = COALESCE(@approved_fingerprint, source_fingerprint),
                active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = true,
                stale_reason = @reason,
                updated_at_utc = @now
            WHERE id = 1
            """,
            ct,
            "PhraseSearch source invalidation",
            new NpgsqlParameter("revision", revision),
            new NpgsqlParameter("approved_fingerprint", NpgsqlDbType.Text)
            {
                Value = approvedFingerprint is null ? DBNull.Value : approvedFingerprint,
            },
            new NpgsqlParameter("reason", reason),
            new NpgsqlParameter("now", DateTimeOffset.UtcNow));

    private static async Task ExecuteExactlyOneAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct,
        string operation,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddRange(parameters);
        var affectedRows = await command.ExecuteNonQueryAsync(ct);
        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                $"{operation} affected {affectedRows.ToString(CultureInfo.InvariantCulture)} rows; expected exactly one.");
        }
    }

}
