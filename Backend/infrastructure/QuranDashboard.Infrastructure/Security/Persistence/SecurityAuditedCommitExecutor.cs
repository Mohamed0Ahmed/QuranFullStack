using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Abwab.Concurrency;
using QuranDashboard.Domain.Abwab.Timeline;
using QuranDashboard.Domain.Security.Audit;

namespace QuranDashboard.Infrastructure.Security.Persistence;

// Both singleton row locks (barrier, revision) are held through commit, serializing concurrent owner/permission writes.
// Generation is verified before any mutation (stale → 409, zero rows touched).
public sealed class SecurityAuditedCommitExecutor(
    QuranDashboardDbContext db,
    IServerClock clock) : ISecurityAuditWriteExecutor
{
    private const string LockBarrierSql = "SELECT * FROM abwab_write_barrier WHERE id = 1 FOR UPDATE";
    private const string LockRevisionSql = "SELECT * FROM abwab_revision_state WHERE id = 1 FOR UPDATE";

    public async Task<SecurityAuditCommitResult> ExecuteAsync(SecurityAuditWriteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var barrier = await LockSingletonAsync<AbwabWriteBarrier>(LockBarrierSql, cancellationToken);
        if (barrier.State != AbwabWriteBarrierState.Writable)
        {
            throw new AbwabStabilizationActiveException(barrier.State);
        }

        var revision = await LockSingletonAsync<AbwabRevisionState>(LockRevisionSql, cancellationToken);

        if (request.ExpectedGeneration.Generation != revision.TimelineGeneration)
        {
            throw new AbwabTimelineGenerationStaleException(
                request.ExpectedGeneration.Generation,
                revision.TimelineGeneration);
        }

        var outcome = await request.Operation(cancellationToken);

        if (outcome.IsNoOp)
        {
            await transaction.CommitAsync(cancellationToken);
            return new SecurityAuditCommitResult(Audited: false, EventCount: 0);
        }

        var stampedAt = clock.UtcNow;
        foreach (var draft in outcome.Events)
        {
            db.Set<SecurityAuditEvent>().Add(new SecurityAuditEvent
            {
                EventType = draft.EventType,
                ActorSubject = request.ActorSubject,
                Payload = draft.Payload,
                ServerTimestampUtc = stampedAt,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SecurityAuditCommitResult(Audited: true, EventCount: outcome.Events.Count);
    }

    private async Task<T> LockSingletonAsync<T>(string sql, CancellationToken cancellationToken)
        where T : class
    {
        var rows = await db.Set<T>().FromSqlRaw(sql).ToListAsync(cancellationToken);
        return rows.Single();
    }
}
