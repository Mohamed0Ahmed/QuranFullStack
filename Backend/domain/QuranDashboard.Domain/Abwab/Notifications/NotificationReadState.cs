namespace QuranDashboard.Domain.Abwab.Notifications;

// Per-recipient read/unread state for a notification. Kept OUTSIDE product audit/restore (FR-034): a plain
// MUTABLE table (read <-> unread toggles), never IAbwabAuditable, never append-only, never routed through
// the ChangeSet/audit kernel. NotificationId is a within-Abwab reference to NotificationRecord (not a Quran
// foreign key).
public sealed class NotificationReadState
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    public string RecipientSubject { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }
}
