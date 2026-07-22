namespace QuranDashboard.Infrastructure.Abwab.Notifications;

// The writer assigns the Id and the server timestamp itself, so a caller can forge neither.
public sealed record NotificationWriteRequest(string RecipientSubject, string SourceIdentity, string Payload);
