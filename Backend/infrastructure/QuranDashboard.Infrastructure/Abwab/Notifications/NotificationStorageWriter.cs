using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Notifications;

namespace QuranDashboard.Infrastructure.Abwab.Notifications;

// Transaction-capable notification storage writer (028 US6, FR-032/033). It JOINS the caller's domain
// transaction: it writes through the SAME scoped DbContext the caller mutates and NEVER begins, commits, or
// rolls back a transaction of its own. So if the caller's unit of work rolls back, the notification row is
// undone with it — a notification can never commit for a rolled-back action. Registered scoped, it shares
// the request DbContext with its caller (033's restore path being the first such caller).
//
// Deduplication is by unique source identity: a notification whose SourceIdentity already exists is not
// written again (DuplicateIgnored, returning the existing id). The unique index on source_identity is the
// hard DB backstop that holds even under concurrency; this pre-check keeps the common sequential case a
// clean no-op instead of a constraint violation that would poison the caller's transaction.
//
// Storage only — no public port/endpoint/mock/HTTP/UI.
public sealed class NotificationStorageWriter(QuranDashboardDbContext db, IServerClock clock)
{
    public async Task<NotificationWriteResult> WriteAsync(
        NotificationWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existingId = await db.Set<NotificationRecord>()
            .Where(n => n.SourceIdentity == request.SourceIdentity)
            .Select(n => (Guid?)n.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingId is Guid duplicateOf)
        {
            return new NotificationWriteResult(NotificationWriteOutcome.DuplicateIgnored, duplicateOf);
        }

        var record = new NotificationRecord
        {
            Id = Guid.NewGuid(),
            RecipientSubject = request.RecipientSubject,
            SourceIdentity = request.SourceIdentity,
            Payload = request.Payload,
            CreatedAtUtc = clock.UtcNow,
        };

        db.Set<NotificationRecord>().Add(record);

        // Flushes into the caller's ambient transaction (if any); the caller still owns commit/rollback.
        await db.SaveChangesAsync(cancellationToken);

        return new NotificationWriteResult(NotificationWriteOutcome.Stored, record.Id);
    }
}
