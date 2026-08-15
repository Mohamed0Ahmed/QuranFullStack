using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationJobStore(
    QuranDashboardDbContext db,
    ILinkingDataRevisionWriterStore revisionStore,
    ILinkingScalabilityPolicy policy) : ILinkingConfirmationJobStore
{
    private const int ActorLockNamespace = 193648317;
    private const int IdempotencyLockNamespace = 193648319;
    private const int ClaimLockNamespace = 193648320;
    private const int JobLockNamespace = 193648321;

    private static readonly JsonSerializerOptions OutcomeJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<LinkingConfirmationJobReceipt> EnqueueAsync(
        int actorUserId,
        CreateLinkingConfirmationJobRequest request,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeAdvisoryLockAsync(ActorLockNamespace, actorUserId, cancellationToken);
        await TakeAdvisoryLockAsync(
            IdempotencyLockNamespace,
            LockKey(request.IdempotencyKey),
            cancellationToken);

        var operation = await db.LinkingOperations.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.IdempotencyKey == request.IdempotencyKey,
            cancellationToken);
        if (operation is not null)
        {
            var outcome = ToDurableOutcome(operation, actorUserId, request);
            await transaction.CommitAsync(cancellationToken);
            return new LinkingConfirmationJobReceipt(
                new LinkingConfirmationSubmissionDto.DurableOutcome(outcome),
                false);
        }

        var existingByKey = await db.LinkingConfirmationJobs.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.IdempotencyKey == request.IdempotencyKey,
            cancellationToken);
        if (existingByKey is not null)
        {
            var existingPreflight = await db.LinkingPreparedPreflights.AsNoTracking().SingleAsync(
                candidate => candidate.Id == existingByKey.PreflightId,
                cancellationToken);
            RequireExactJobRequest(existingByKey, existingPreflight, actorUserId, request);
            var existingStatus = ToStatus(existingByKey);
            await transaction.CommitAsync(cancellationToken);
            return new LinkingConfirmationJobReceipt(
                new LinkingConfirmationSubmissionDto.Job(existingStatus),
                false);
        }

        var revision = await LockRevisionAsync(transaction, cancellationToken);
        var preflight = await LockOwnedPreflightAsync(
            actorUserId,
            request.PreflightId,
            cancellationToken)
            ?? throw Conflict(
                LinkingConfirmationJobConflictKind.PreflightStale,
                LinkingPreparedPreflightFailureCode.PreflightStale);
        var requestHash = LinkingConfirmationRequestHasher.ComputePrepared(
            request.PreflightId,
            request.PreflightToken,
            preflight.LinkingDataRevision);

        var existingForPreflight = await db.LinkingConfirmationJobs.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.PreflightId == request.PreflightId,
            cancellationToken);
        if (existingForPreflight is not null)
        {
            RequireSamePreflightRequest(existingForPreflight, preflight, actorUserId, request);
            var existingStatus = ToStatus(existingForPreflight);
            await transaction.CommitAsync(cancellationToken);
            return new LinkingConfirmationJobReceipt(
                new LinkingConfirmationSubmissionDto.Job(existingStatus),
                false);
        }

        var now = await DatabaseNowAsync(cancellationToken);
        RequireReadyPreflight(preflight, request.PreflightToken, revision, now);

        var activePreflights = await db.LinkingPreparedPreflights.CountAsync(
            candidate => candidate.ActorUserId == actorUserId
                && candidate.ConfirmationAcceptedAtUtc == null
                && (candidate.Status == LinkingPreparedPreflightStatus.Queued
                    || candidate.Status == LinkingPreparedPreflightStatus.Preparing
                    || candidate.Status == LinkingPreparedPreflightStatus.Ready),
            cancellationToken);
        var activeJobs = await db.LinkingConfirmationJobs.CountAsync(
            candidate => candidate.ActorUserId == actorUserId
                && (candidate.Status == LinkingConfirmationJobStatus.Queued
                    || candidate.Status == LinkingConfirmationJobStatus.Running
                    || candidate.Status == LinkingConfirmationJobStatus.Finalizing),
            cancellationToken);
        if (activePreflights + activeJobs > policy.ActiveWorkflowsPerActor)
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.ActiveWorkflowLimit,
                LinkingPreparedPreflightFailureCode.ActiveLinkingWorkflowLimit);
        }

        var totalItems = await db.LinkingPreparedAyahs.CountAsync(
            ayah => ayah.PreflightId == preflight.Id && ayah.IsRequested,
            cancellationToken);
        var job = new LinkingConfirmationJob
        {
            Id = Guid.NewGuid(),
            PreflightId = preflight.Id,
            ActorUserId = actorUserId,
            DoorId = preflight.DoorId,
            IdempotencyKey = request.IdempotencyKey,
            RequestHash = requestHash,
            Status = LinkingConfirmationJobStatus.Queued,
            Stage = LinkingConfirmationJobStage.LoadingPrepared,
            ProcessedItems = 0,
            TotalItems = totalItems,
            AttemptCount = 0,
            CleanupAttemptCount = 0,
            QueuedAtUtc = now,
            UpdatedAtUtc = now,
        };
        preflight.ConfirmationAcceptedAtUtc = now;
        preflight.UpdatedAtUtc = now;
        db.LinkingConfirmationJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LinkingConfirmationJobReceipt(
            new LinkingConfirmationSubmissionDto.Job(ToStatus(job)),
            true);
    }

    public async Task<LinkingConfirmationJobStatusDto?> GetStatusAsync(
        int actorUserId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await db.LinkingConfirmationJobs.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == jobId
                && candidate.ActorUserId == actorUserId
                && candidate.CleanupStartedAtUtc == null,
            cancellationToken);
        return job is null ? null : ToStatus(job);
    }

    public async Task<LinkingConfirmationJobStatusDto?> CancelAsync(
        int actorUserId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeAdvisoryLockAsync(JobLockNamespace, LockKey(jobId), cancellationToken);
        var job = await LockOwnedJobAsync(actorUserId, jobId, cancellationToken);
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = await DatabaseNowAsync(cancellationToken);
        switch (job.Status)
        {
            case LinkingConfirmationJobStatus.Queued:
            {
                var preflight = await LockPreflightAsync(job.PreflightId, cancellationToken);
                if (preflight is null)
                {
                    throw new InvalidOperationException("A retained confirmation job lost its pinned preflight.");
                }

                ApplyTerminal(
                    job,
                    preflight,
                    LinkingConfirmationJobStatus.Cancelled,
                    LinkingConfirmationJobFailureCode.ConfirmationCancelled,
                    LinkingPreparedPreflightStatus.Cancelled,
                    LinkingPreparedPreflightFailureCode.ConfirmationCancelled,
                    now);
                job.CancellationRequestedAtUtc = now;
                break;
            }
            case LinkingConfirmationJobStatus.Running:
            {
                var preflight = await LockPreflightAsync(job.PreflightId, cancellationToken);
                if (preflight is null)
                {
                    throw new InvalidOperationException("A retained confirmation job lost its pinned preflight.");
                }

                ApplyTerminal(
                    job,
                    preflight,
                    LinkingConfirmationJobStatus.Cancelled,
                    LinkingConfirmationJobFailureCode.ConfirmationCancelled,
                    LinkingPreparedPreflightStatus.Cancelled,
                    LinkingPreparedPreflightFailureCode.ConfirmationCancelled,
                    now);
                job.CancellationRequestedAtUtc = now;
                break;
            }
            case LinkingConfirmationJobStatus.Cancelled:
                break;
            case LinkingConfirmationJobStatus.Finalizing:
            case LinkingConfirmationJobStatus.Succeeded:
                throw Conflict(
                    LinkingConfirmationJobConflictKind.CancellationTooLate,
                    LinkingPreparedPreflightFailureCode.CancellationTooLate);
            default:
                throw Conflict(
                    LinkingConfirmationJobConflictKind.TerminalState,
                    job.FailureCode ?? LinkingConfirmationJobFailureCode.ConfirmationFailed);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToStatus(job);
    }

    public async Task<LinkingDurableConfirmationOutcomeDto?> GetDurableOutcomeAsync(
        int actorUserId,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var operation = await db.LinkingOperations.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.IdempotencyKey == idempotencyKey,
            cancellationToken);
        if (operation is null || operation.ActorUserId != actorUserId)
        {
            return null;
        }

        if (!string.Equals(
                operation.RequestContractKind,
                LinkingConfirmationRequestContracts.PreparedJob,
                StringComparison.Ordinal)
            || operation.PreparedPreflightReferenceId is null
            || operation.ConfirmationJobReferenceId is null)
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.IdempotencyConflict,
                LinkingPreparedPreflightFailureCode.IdempotencyConflict);
        }

        return ToDurableOutcome(operation);
    }

    private LinkingDurableConfirmationOutcomeDto ToDurableOutcome(
        LinkingOperation operation,
        int actorUserId,
        CreateLinkingConfirmationJobRequest request)
    {
        if (operation.ActorUserId != actorUserId
            || operation.PreparedPreflightReferenceId != request.PreflightId
            || !string.Equals(
                operation.RequestContractKind,
                LinkingConfirmationRequestContracts.PreparedJob,
                StringComparison.Ordinal)
            || operation.RequestSchemaVersion != LinkingConfirmationRequestContracts.SchemaVersion
            || operation.LinkingDataRevision is not { } revision
            || !string.Equals(
                operation.RequestHash,
                LinkingConfirmationRequestHasher.ComputePrepared(
                    request.PreflightId,
                    request.PreflightToken,
                    revision),
                StringComparison.Ordinal))
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.IdempotencyConflict,
                LinkingPreparedPreflightFailureCode.IdempotencyConflict);
        }

        return ToDurableOutcome(operation);
    }

    private static LinkingDurableConfirmationOutcomeDto ToDurableOutcome(LinkingOperation operation) =>
        new(
            "durable_outcome",
            operation.ConfirmationJobReferenceId!.Value,
            operation.PreparedPreflightReferenceId!.Value,
            operation.IdempotencyKey,
            "succeeded",
            operation.ConfirmedAtUtc,
            DeserializeOutcome(operation.OutcomeJson));

    private static void RequireExactJobRequest(
        LinkingConfirmationJob job,
        LinkingPreparedPreflight preflight,
        int actorUserId,
        CreateLinkingConfirmationJobRequest request)
    {
        if (job.IdempotencyKey != request.IdempotencyKey
            || !MatchesPreflightRequest(job, preflight, actorUserId, request))
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.IdempotencyConflict,
                LinkingPreparedPreflightFailureCode.IdempotencyConflict);
        }
    }

    private static void RequireSamePreflightRequest(
        LinkingConfirmationJob job,
        LinkingPreparedPreflight preflight,
        int actorUserId,
        CreateLinkingConfirmationJobRequest request)
    {
        if (!MatchesPreflightRequest(job, preflight, actorUserId, request))
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.IdempotencyConflict,
                LinkingPreparedPreflightFailureCode.IdempotencyConflict);
        }
    }

    private static bool MatchesPreflightRequest(
        LinkingConfirmationJob job,
        LinkingPreparedPreflight preflight,
        int actorUserId,
        CreateLinkingConfirmationJobRequest request)
    {
        var hash = LinkingConfirmationRequestHasher.ComputePrepared(
            request.PreflightId,
            request.PreflightToken,
            preflight.LinkingDataRevision);
        return job.ActorUserId == actorUserId
            && job.PreflightId == request.PreflightId
            && string.Equals(job.RequestHash, hash, StringComparison.Ordinal);
    }

    private static void RequireReadyPreflight(
        LinkingPreparedPreflight preflight,
        string suppliedToken,
        long currentRevision,
        DateTimeOffset now)
    {
        if (preflight.Status != LinkingPreparedPreflightStatus.Ready
            || preflight.ConfirmationAcceptedAtUtc is not null)
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.PreflightNotReady,
                LinkingPreparedPreflightFailureCode.PreflightNotReady);
        }

        if (preflight.IsBlocked != false)
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.PreflightBlocked,
                LinkingPreparedPreflightFailureCode.PreflightBlocked);
        }

        if (preflight.ExpiresAtUtc <= now)
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.PreflightExpired,
                LinkingPreparedPreflightFailureCode.PreflightExpired);
        }

        if (preflight.LinkingDataRevision != currentRevision
            || !string.Equals(preflight.PreflightToken, suppliedToken, StringComparison.Ordinal))
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.PreflightStale,
                LinkingPreparedPreflightFailureCode.PreflightStale);
        }
    }

    private LinkingConfirmationJobStatusDto ToStatus(LinkingConfirmationJob job) =>
        new(
            job.Id,
            job.PreflightId,
            LinkingConfirmationJobLifecycleTokens.ToToken(job.Status),
            LinkingConfirmationJobLifecycleTokens.ToToken(job.Stage),
            job.ProcessedItems,
            job.TotalItems,
            policy.PollAfterMilliseconds,
            job.CancellationRequestedAtUtc is not null,
            job.QueuedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.OutcomeDocumentJson is null ? null : DeserializeOutcome(job.OutcomeDocumentJson),
            LinkingConfirmationJobLifecycleTokens.ToToken(job.FailureCode));

    private static LinkingConfirmationResultDto DeserializeOutcome(string json) =>
        JsonSerializer.Deserialize<LinkingConfirmationResultDto>(json, OutcomeJsonOptions)
        ?? throw new InvalidOperationException("Stored linking confirmation outcome is empty.");

    private async Task TakeAdvisoryLockAsync(
        int lockNamespace,
        int key,
        CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockNamespace}, {key})",
            cancellationToken);

    private async Task<long> LockRevisionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Expected an Npgsql connection.");
        var npgsqlTransaction = transaction.GetDbTransaction() as NpgsqlTransaction
            ?? throw new InvalidOperationException("Expected an Npgsql transaction.");
        return await revisionStore.LockForReadAsync(connection, npgsqlTransaction, cancellationToken);
    }

    private async Task<LinkingPreparedPreflight?> LockOwnedPreflightAsync(
        int actorUserId,
        Guid preflightId,
        CancellationToken cancellationToken) =>
        (await db.LinkingPreparedPreflights.FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE id = {preflightId}
                  AND actor_user_id = {actorUserId}
                  AND cleanup_started_at_utc IS NULL
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private async Task<LinkingConfirmationJob?> LockOwnedJobAsync(
        int actorUserId,
        Guid jobId,
        CancellationToken cancellationToken) =>
        (await db.LinkingConfirmationJobs.FromSqlInterpolated(
                $"""
                SELECT job.*, job.xmin
                FROM linking_confirmation_jobs job
                WHERE id = {jobId}
                  AND actor_user_id = {actorUserId}
                  AND cleanup_started_at_utc IS NULL
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private async Task<LinkingPreparedPreflight?> LockPreflightAsync(
        Guid preflightId,
        CancellationToken cancellationToken) =>
        (await db.LinkingPreparedPreflights.FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE id = {preflightId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private async Task<DateTimeOffset> DatabaseNowAsync(CancellationToken cancellationToken) =>
        await db.Database.SqlQuery<DateTimeOffset>($"SELECT CURRENT_TIMESTAMP AS \"Value\"")
            .SingleAsync(cancellationToken);

    private static int LockKey(Guid key) =>
        BitConverter.ToInt32(SHA256.HashData(key.ToByteArray()), 0);

    private static LinkingConfirmationJobConflictException Conflict(
        LinkingConfirmationJobConflictKind kind,
        LinkingPreparedPreflightFailureCode failureCode) =>
        new(kind, LinkingPreparedPreflightLifecycleTokens.ToToken(failureCode)!);

    private static LinkingConfirmationJobConflictException Conflict(
        LinkingConfirmationJobConflictKind kind,
        LinkingConfirmationJobFailureCode failureCode) =>
        new(kind, LinkingConfirmationJobLifecycleTokens.ToToken(failureCode)!);

    private static void ApplyTerminal(
        LinkingConfirmationJob job,
        LinkingPreparedPreflight preflight,
        LinkingConfirmationJobStatus jobStatus,
        LinkingConfirmationJobFailureCode jobFailure,
        LinkingPreparedPreflightStatus preflightStatus,
        LinkingPreparedPreflightFailureCode preflightFailure,
        DateTimeOffset now)
    {
        job.Status = jobStatus;
        job.FailureCode = jobFailure;
        job.CompletedAtUtc = now;
        job.LeaseOwner = null;
        job.LeaseExpiresAtUtc = null;
        job.UpdatedAtUtc = now;
        preflight.Status = preflightStatus;
        preflight.FailureCode = preflightFailure;
        preflight.CompletedAtUtc = now;
        preflight.UpdatedAtUtc = now;
    }
}
