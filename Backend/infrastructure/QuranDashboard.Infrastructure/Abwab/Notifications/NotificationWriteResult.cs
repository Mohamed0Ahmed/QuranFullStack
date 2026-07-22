namespace QuranDashboard.Infrastructure.Abwab.Notifications;

public enum NotificationWriteOutcome
{
    // A new notification row was staged into the caller's unit of work.
    Stored,

    // A notification with the same source identity already existed: deduplicated, nothing written.
    DuplicateIgnored,
}

// The writer's outcome plus the id of the notification (the newly stored one, or the pre-existing one on
// a deduplicated write).
public sealed record NotificationWriteResult(NotificationWriteOutcome Outcome, Guid NotificationId);
