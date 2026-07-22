using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Notifications;

namespace QuranDashboard.Infrastructure.Abwab.Notifications;

// Low-level recipient/read-state repository (028 US6, FR-032/034). Read state is kept OUTSIDE product
// audit/restore: these are plain mutable rows (read <-> unread), never routed through the ChangeSet/audit
// kernel, so the mutating helpers save directly rather than joining a product transaction. Storage only —
// no public port/endpoint/mock/HTTP/UI; 032 builds the notification surfaces on top of these primitives.
public sealed class NotificationReadStateRepository(QuranDashboardDbContext db, IServerClock clock)
{
    public async Task<IReadOnlyList<NotificationRecord>> ListForRecipientAsync(
        string recipientSubject,
        CancellationToken cancellationToken) =>
        await db.Set<NotificationRecord>()
            .AsNoTracking()
            .Where(n => n.RecipientSubject == recipientSubject)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<NotificationReadState?> FindReadStateAsync(
        Guid notificationId,
        string recipientSubject,
        CancellationToken cancellationToken) =>
        await db.Set<NotificationReadState>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.NotificationId == notificationId && s.RecipientSubject == recipientSubject,
                cancellationToken);

    public Task<NotificationReadState> MarkReadAsync(
        Guid notificationId,
        string recipientSubject,
        CancellationToken cancellationToken) =>
        SetReadStateAsync(notificationId, recipientSubject, isRead: true, cancellationToken);

    public Task<NotificationReadState> MarkUnreadAsync(
        Guid notificationId,
        string recipientSubject,
        CancellationToken cancellationToken) =>
        SetReadStateAsync(notificationId, recipientSubject, isRead: false, cancellationToken);

    private async Task<NotificationReadState> SetReadStateAsync(
        Guid notificationId,
        string recipientSubject,
        bool isRead,
        CancellationToken cancellationToken)
    {
        var state = await db.Set<NotificationReadState>()
            .SingleOrDefaultAsync(
                s => s.NotificationId == notificationId && s.RecipientSubject == recipientSubject,
                cancellationToken);

        if (state is null)
        {
            state = new NotificationReadState
            {
                Id = Guid.NewGuid(),
                NotificationId = notificationId,
                RecipientSubject = recipientSubject,
            };
            db.Set<NotificationReadState>().Add(state);
        }

        state.IsRead = isRead;
        state.ReadAtUtc = isRead ? clock.UtcNow : null;

        await db.SaveChangesAsync(cancellationToken);
        return state;
    }
}
