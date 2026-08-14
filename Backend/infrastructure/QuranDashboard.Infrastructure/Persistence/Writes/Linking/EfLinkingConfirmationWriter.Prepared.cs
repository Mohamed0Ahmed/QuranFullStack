using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    public async Task<LinkingConfirmationWriteResult> ConfirmPreparedAsync(
        LinkingConfirmationJobLease lease,
        LinkingOperationRequest request,
        LinkingOperationIntent intent,
        Func<LinkingOperationIntent, LinkingConfirmedDoorState, LinkingOperationClassification> classify,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(classify);

        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeIdempotencyLockAsync(lease.IdempotencyKey, cancellationToken);
        var revision = await LockRevisionAsync(transaction, cancellationToken);
        if (revision != request.ExpectedLinkingDataRevision)
        {
            throw new LinkingDataStaleException(request.ExpectedLinkingDataRevision, revision);
        }

        var jobs = await db.LinkingConfirmationJobs.FromSqlInterpolated(
                $"""
                SELECT job.*, job.xmin
                FROM linking_confirmation_jobs job
                WHERE id = {lease.JobId}
                  AND status = 'finalizing'
                  AND lease_owner = {lease.LeaseOwner}
                  AND attempt_count = {lease.AttemptCount}
                  AND lease_expires_at_utc > CURRENT_TIMESTAMP
                  AND cancellation_requested_at_utc IS NULL
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
        var job = jobs.SingleOrDefault();
        if (job is null)
        {
            throw new LinkingStaleVersionException();
        }

        var preflights = await db.LinkingPreparedPreflights.FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE id = {lease.PreflightId}
                  AND actor_user_id = {lease.ActorUserId}
                  AND door_id = {lease.DoorId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
        var preflight = preflights.SingleOrDefault();
        if (preflight is null
            || preflight.Status != LinkingPreparedPreflightStatus.Ready
            || preflight.ConfirmationAcceptedAtUtc is null
            || preflight.IsBlocked != false
            || preflight.ExpectedDoorVersion is null
            || string.IsNullOrWhiteSpace(preflight.IntentHash)
            || string.IsNullOrWhiteSpace(preflight.PreflightToken))
        {
            throw new LinkingStaleVersionException();
        }

        var expectedRequestHash = LinkingConfirmationRequestHasher.ComputePrepared(
            preflight.Id,
            preflight.PreflightToken,
            preflight.LinkingDataRevision);
        if (!string.Equals(job.RequestHash, lease.RequestHash, StringComparison.Ordinal)
            || !string.Equals(job.RequestHash, expectedRequestHash, StringComparison.Ordinal))
        {
            throw new LinkingIdempotencyConflictException();
        }

        if (await db.LinkingOperations.AsNoTracking().AnyAsync(
                operation => operation.IdempotencyKey == lease.IdempotencyKey,
                cancellationToken))
        {
            throw new LinkingIdempotencyConflictException();
        }

        var loaded = await LoadLockedStateAsync(lease.DoorId, cancellationToken);
        if (loaded is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.DoorNotFound(lease.DoorId);
        }

        var affected = await db.LinkingPreparedAffectedContributions.AsNoTracking()
            .Where(contribution => contribution.PreflightId == preflight.Id)
            .OrderBy(contribution => contribution.ContributionId)
            .Select(contribution => new LinkingPreflightContributionComponent(
                contribution.ContributionId,
                contribution.ExpectedContributionVersion))
            .ToListAsync(cancellationToken);
        var currentContributions = loaded.State.Contributions.ToDictionary(contribution => contribution.Id);
        if (preflight.ExpectedDoorVersion != loaded.State.DoorVersion
            || affected.Any(expected => !currentContributions.TryGetValue(expected.Id, out var current)
                || current.Version != expected.Version))
        {
            throw new LinkingStaleVersionException();
        }

        var freshToken = LinkingPreparedPreflightToken.Compute(
            preflight.Id,
            preflight.ActorUserId,
            preflight.RequestHash,
            preflight.IntentHash,
            preflight.LinkingDataRevision,
            new LinkingPreflightDoorComponent(loaded.State.DoorId, loaded.State.DoorVersion),
            affected);
        if (!string.Equals(preflight.PreflightToken, freshToken, StringComparison.Ordinal))
        {
            throw new LinkingStaleVersionException();
        }

        intent = intent with { IsDoorArchived = loaded.State.IsArchived };
        var classification = classify(intent, loaded.State);
        if (classification.IsBlocked)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.InvalidClassification(classification);
        }

        var requestContract = new LinkingConfirmationRequestContract(
            LinkingConfirmationRequestContracts.PreparedJob,
            LinkingConfirmationRequestContracts.SchemaVersion,
            job.RequestHash,
            preflight.LinkingDataRevision,
            preflight.Id,
            job.Id,
            preflight.Id);
        var result = await PersistOperationAsync(
            lease.ActorUserId,
            request,
            classification,
            loaded,
            requestContract,
            cancellationToken);
        var operation = await db.LinkingOperations.SingleAsync(
            candidate => candidate.IdempotencyKey == lease.IdempotencyKey,
            cancellationToken);
        var now = operation.ConfirmedAtUtc;
        job.Status = LinkingConfirmationJobStatus.Succeeded;
        job.Stage = LinkingConfirmationJobStage.Committing;
        job.ProcessedItems = job.TotalItems;
        job.OperationId = operation.Id;
        job.OutcomeDocumentJson = operation.OutcomeJson;
        job.FailureCode = null;
        job.CompletedAtUtc = now;
        job.LeaseOwner = null;
        job.LeaseExpiresAtUtc = null;
        job.UpdatedAtUtc = now;
        preflight.Status = LinkingPreparedPreflightStatus.Confirmed;
        preflight.FailureCode = null;
        preflight.ConfirmedAtUtc = now;
        preflight.CompletedAtUtc = now;
        preflight.UpdatedAtUtc = now;
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LinkingConfirmationWriteResult.Success(result, false);
    }
}
