using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    public async Task<LinkingConfirmationWriteResult> ConfirmPreparedAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeJobLockAsync(lease.JobId, cancellationToken);
        var job = await LockFinalizingJobAsync(lease, cancellationToken)
            ?? throw new LinkingStaleVersionException();
        await TakeIdempotencyLockAsync(lease.IdempotencyKey, cancellationToken);
        var revision = await LockRevisionAsync(transaction, cancellationToken);
        var preflight = await LockAcceptedPreflightAsync(lease, cancellationToken)
            ?? throw new LinkingStaleVersionException();
        ValidateRequestContract(lease, job, preflight, revision);

        if (await db.LinkingOperations.AsNoTracking().AnyAsync(
                operation => operation.IdempotencyKey == lease.IdempotencyKey,
                cancellationToken))
        {
            throw new LinkingIdempotencyConflictException();
        }

        await syncLock.TakeAfterGlobalLocksBeforeDoorAndUnitLocksAsync(cancellationToken);
        var door = await LockDoorAsync(lease.DoorId, cancellationToken);
        if (door is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.DoorNotFound(lease.DoorId);
        }

        var affected = await LoadAffectedContributionVersionsAsync(preflight.Id, cancellationToken);
        ValidatePreparedVersions(preflight, door, affected);
        ValidatePreparedToken(preflight, door, affected);

        var now = await DatabaseNowAsync(cancellationToken);
        var operation = CreatePreparedOperation(lease, preflight, now);
        db.LinkingOperations.Add(operation);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        if (preflight.IsNoOp != true)
        {
            await ApplyPreparedRelationalStateAsync(
                preflight.Id,
                operation.Id,
                lease.DoorId,
                lease.ActorUserId,
                cancellationToken);
        }

        var result = await CreatePreparedResultAsync(preflight, cancellationToken);
        operation.OutcomeJson = SerializeOutcome(result);
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

    private async Task<LinkingConfirmationJob?> LockFinalizingJobAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken) =>
        (await db.LinkingConfirmationJobs.FromSqlInterpolated(
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
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private async Task<LinkingPreparedPreflight?> LockAcceptedPreflightAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken) =>
        (await db.LinkingPreparedPreflights.FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE id = {lease.PreflightId}
                  AND actor_user_id = {lease.ActorUserId}
                  AND door_id = {lease.DoorId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault(candidate =>
            candidate.Status == LinkingPreparedPreflightStatus.Ready
            && candidate.ConfirmationAcceptedAtUtc is not null
            && candidate.IsBlocked == false
            && candidate.IsNoOp is not null
            && candidate.ExpectedDoorVersion is not null
            && candidate.IntentHash is not null
            && candidate.PreflightToken is not null);

    private async Task<AbwabDoor?> LockDoorAsync(
        int doorId,
        CancellationToken cancellationToken) =>
        (await db.AbwabDoors.FromSqlInterpolated(
                $"""
                SELECT id, section_id, parent_id, name, description, representative_ayah_text,
                       order_value, global_order_value, created_at, created_by, updated_at, updated_by,
                       approved_at, approved_by, deleted_at, deleted_by, xmin
                FROM abwab_doors
                WHERE id = {doorId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private async Task<IReadOnlyList<LinkingPreflightContributionComponent>>
        LoadAffectedContributionVersionsAsync(
            Guid preflightId,
            CancellationToken cancellationToken)
    {
        var expected = await db.LinkingPreparedAffectedContributions.AsNoTracking()
            .Where(contribution => contribution.PreflightId == preflightId)
            .OrderBy(contribution => contribution.ContributionId)
            .Select(contribution => new LinkingPreflightContributionComponent(
                contribution.ContributionId,
                contribution.ExpectedContributionVersion))
            .ToListAsync(cancellationToken);
        if (expected.Count == 0)
        {
            return expected;
        }

        var ids = expected.Select(contribution => contribution.Id).ToList();
        var current = await db.LinkingSourceContributions.AsNoTracking()
            .Where(contribution => ids.Contains(contribution.Id) && contribution.DeletedAtUtc == null)
            .Select(contribution => new LinkingPreflightContributionComponent(
                contribution.Id,
                contribution.Version))
            .ToDictionaryAsync(contribution => contribution.Id, cancellationToken);
        if (expected.Any(contribution =>
                !current.TryGetValue(contribution.Id, out var found)
                || found.Version != contribution.Version))
        {
            throw new LinkingStaleVersionException();
        }

        return expected;
    }

    private static void ValidateRequestContract(
        LinkingConfirmationJobLease lease,
        LinkingConfirmationJob job,
        LinkingPreparedPreflight preflight,
        long revision)
    {
        if (revision != preflight.LinkingDataRevision)
        {
            throw new LinkingDataStaleException(preflight.LinkingDataRevision, revision);
        }

        var expectedRequestHash = LinkingConfirmationRequestHasher.ComputePrepared(
            preflight.Id,
            preflight.PreflightToken!,
            preflight.LinkingDataRevision);
        if (!string.Equals(job.RequestHash, lease.RequestHash, StringComparison.Ordinal)
            || !string.Equals(job.RequestHash, expectedRequestHash, StringComparison.Ordinal))
        {
            throw new LinkingIdempotencyConflictException();
        }
    }

    private static void ValidatePreparedVersions(
        LinkingPreparedPreflight preflight,
        AbwabDoor door,
        IReadOnlyList<LinkingPreflightContributionComponent> affected)
    {
        if (preflight.ExpectedDoorVersion != door.Version)
        {
            throw new LinkingStaleVersionException();
        }

        if (affected.Count != affected.Select(contribution => contribution.Id).Distinct().Count())
        {
            throw new LinkingStaleVersionException();
        }
    }

    private static void ValidatePreparedToken(
        LinkingPreparedPreflight preflight,
        AbwabDoor door,
        IReadOnlyList<LinkingPreflightContributionComponent> affected)
    {
        var freshToken = LinkingPreparedPreflightToken.Compute(
            preflight.Id,
            preflight.ActorUserId,
            preflight.RequestHash,
            preflight.IntentHash!,
            preflight.LinkingDataRevision,
            new LinkingPreflightDoorComponent(door.Id, door.Version),
            affected);
        if (!string.Equals(preflight.PreflightToken, freshToken, StringComparison.Ordinal))
        {
            throw new LinkingStaleVersionException();
        }
    }
}
