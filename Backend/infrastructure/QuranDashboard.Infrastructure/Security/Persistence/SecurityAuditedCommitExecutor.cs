using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Abwab.Concurrency;
using QuranDashboard.Domain.Abwab.Timeline;
using QuranDashboard.Domain.Security.Audit;

namespace QuranDashboard.Infrastructure.Security.Persistence;

// The separate permanent security-audit unit of work (FR-039). It mirrors the product commit protocol's
// gates but NOT its head advance:
//   1. row-lock + evaluate the AbwabWriteBarrier (fail closed → abwab.stabilization_active);
//   2. row-lock the AbwabRevisionState singleton;
//   3. verify ExpectedTimelineGeneration BEFORE any mutation → exact 409, zero rows touched;
//   4. run the caller's domain operation (read + validate + stage) on the SAME scoped DbContext;
//   5. append the permanent SecurityAuditEvent rows and commit.
// It DELIBERATELY never touches AbwabRevisionState.AuditHeadSequence and never creates a product ChangeSet /
// Restore-head event — the product timeline head is never advanced. Both singleton row locks are held
// through commit, so concurrent owner/permission writes are serialized. Row locks use FromSqlRaw (a read
// API), never a forbidden write/bypass API.
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
            // Idempotent no-op: release the locks, change nothing, append no audit event.
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
