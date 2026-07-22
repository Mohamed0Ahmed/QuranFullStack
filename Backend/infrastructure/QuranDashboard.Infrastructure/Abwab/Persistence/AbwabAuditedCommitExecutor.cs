using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Domain.Abwab.Concurrency;
using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Infrastructure.Abwab.Persistence;

// Ordering is load-bearing: lock barrier, then revision head; verify ExpectedTimelineGeneration BEFORE mutating; advance head then assign ChangeSetSequence FROM it; publish caches only after commit.
// One manual transaction with provider retries OFF (a retry would re-run non-idempotent work); both write locks held through commit.
public sealed class AbwabAuditedCommitExecutor(
    QuranDashboardDbContext db,
    IServerClock clock,
    IAbwabCachePublisher cachePublisher) : IAbwabWriteExecutor
{
    // FOR UPDATE takes the pessimistic row lock that serializes every audited commit.
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
