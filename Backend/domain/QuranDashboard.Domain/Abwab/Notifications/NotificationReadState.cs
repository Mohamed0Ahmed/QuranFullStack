namespace QuranDashboard.Domain.Abwab.Notifications;

// NotificationId references NotificationRecord within Abwab — not a Quran foreign key.
public sealed class NotificationReadState
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    public string RecipientSubject { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }
}
