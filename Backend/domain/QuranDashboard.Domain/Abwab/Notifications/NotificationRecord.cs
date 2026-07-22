namespace QuranDashboard.Domain.Abwab.Notifications;

// Durable notification storage record (028 US6, FR-032/033). Written INSIDE a caller's domain transaction
// so it can never commit for a rolled-back action. SourceIdentity is the idempotency key: a unique index on
// it prevents duplicate notifications for the same source event, even under concurrency. This is storage
// only — 028 exposes no public port/endpoint/mock/HTTP/UI (032 owns surfaces, 033 emits restore events
// through the writer). Deliberately NOT IAbwabAuditable: notifications are their own stream, outside the
// product ChangeSet/audit envelope. RecipientSubject is the principal's subject (the same subject identity
// the security/audit substrate uses) — no Quran foreign key and no users foreign key is introduced here.
public sealed class NotificationRecord
{
    public Guid Id { get; set; }

    public string RecipientSubject { get; set; } = string.Empty;

    public string SourceIdentity { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
