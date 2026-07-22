namespace QuranDashboard.Domain.Abwab.Notifications;

// No Quran foreign key and no users foreign key here.
public sealed class NotificationRecord
{
    public Guid Id { get; set; }

    public string RecipientSubject { get; set; } = string.Empty;

    public string SourceIdentity { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
