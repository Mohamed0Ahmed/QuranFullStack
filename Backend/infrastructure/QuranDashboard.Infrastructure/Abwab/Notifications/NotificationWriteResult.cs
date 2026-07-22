namespace QuranDashboard.Infrastructure.Abwab.Notifications;

public enum NotificationWriteOutcome
{
    Stored,

    DuplicateIgnored,
}

public sealed record NotificationWriteResult(NotificationWriteOutcome Outcome, Guid NotificationId);
