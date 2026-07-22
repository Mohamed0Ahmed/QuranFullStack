using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Notifications;

namespace QuranDashboard.Infrastructure.Abwab.Notifications;

// Joins the caller's transaction (never opens its own), so a notification can never commit for a rolled-back action.
// The unique index on source_identity is the concurrency backstop; the pre-check avoids a constraint violation that would poison the caller's transaction.
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
