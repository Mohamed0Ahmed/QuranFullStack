using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Domain.Abwab.Concurrency;
using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Infrastructure.Abwab.Persistence;

// The barrier-gated audited-commit protocol (§6.2). Every Abwab writer runs through here inside one
// manual transaction with provider retries locked off (retrying a manual transaction would re-run
// non-idempotent work). Order is load-bearing:
//   1. row-lock + evaluate the AbwabWriteBarrier (fail closed if not Writable);
//   2. row-lock the AbwabRevisionState singleton;
//   3. verify ExpectedTimelineGeneration BEFORE any mutation → exact 409 with zero rows touched;
//   4. advance the head by one and assign ChangeSetSequence FROM it (never the inverse);
//   5. append the ChangeSet + its events, save, commit;
//   6. publish caches ONLY after commit.
// Both write locks are held through commit, so concurrent commits receive one strictly increasing head
// and a rollback leaves head/generation/tree unchanged. Row locks use FromSqlRaw (a read API) — never a
// forbidden write/bypass API — so the bypass-gate stays green.
public sealed class AbwabAuditedCommitExecutor(
    QuranDashboardDbContext db,
    IServerClock clock,
    IAbwabCachePublisher cachePublisher) : IAbwabWriteExecutor
{
    // id = 1 is the seeded singleton row (AbwabWriteBarrier/AbwabRevisionState .SingletonId). FOR UPDATE
    // takes the pessimistic row lock that serializes every audited commit.
    private const string LockBarrierSql = "SELECT * FROM abwab_write_barrier WHERE id = 1 FOR UPDATE";
    private const string LockRevisionSql = "SELECT * FROM abwab_revision_state WHERE id = 1 FOR UPDATE";

    public async Task<AbwabCommitResult> ExecuteAsync(AbwabWriteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var barrier = await LockSingletonAsync<AbwabWriteBarrier>(LockBarrierSql, cancellationToken);
        if (barrier.State != AbwabWriteBarrierState.Writable)
        {
            throw new AbwabWriteBarrierClosedException(barrier.State);
        }

        var revision = await LockSingletonAsync<AbwabRevisionState>(LockRevisionSql, cancellationToken);

        if (request.ExpectedGeneration.Generation != revision.TimelineGeneration)
        {
            throw new AbwabTimelineGenerationStaleException(
                request.ExpectedGeneration.Generation,
                revision.TimelineGeneration);
        }

        var stampedAt = clock.UtcNow;
        revision.AuditHeadSequence += 1;

        var changeSet = new ChangeSet
        {
            Id = Guid.NewGuid(),
            TimelineGeneration = revision.TimelineGeneration,
            ChangeSetSequence = revision.AuditHeadSequence,
            ActorSubject = request.ActorSubject,
            CreatedAtUtc = stampedAt,
        };
        db.Set<ChangeSet>().Add(changeSet);

        foreach (var draft in request.Events)
        {
            db.Set<AuditEvent>().Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ChangeSetId = changeSet.Id,
                EventOrdinal = draft.EventOrdinal,
                Payload = draft.Payload,
                ServerTimestampUtc = stampedAt,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var result = new AbwabCommitResult(changeSet.Id, changeSet.ChangeSetSequence, changeSet.TimelineGeneration);
        await cachePublisher.PublishAsync(result, cancellationToken);
        return result;
    }

    private async Task<T> LockSingletonAsync<T>(string sql, CancellationToken cancellationToken)
        where T : class
    {
        var rows = await db.Set<T>().FromSqlRaw(sql).ToListAsync(cancellationToken);
        return rows.Single();
    }
}
