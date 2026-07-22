namespace QuranDashboard.Infrastructure.Abwab.Notifications;

// Input to the notification storage writer: the recipient subject, the idempotency key (source identity),
// and the payload. The writer assigns the Id and the server timestamp itself, so a caller can forge neither.
public sealed record NotificationWriteRequest(string RecipientSubject, string SourceIdentity, string Payload);
