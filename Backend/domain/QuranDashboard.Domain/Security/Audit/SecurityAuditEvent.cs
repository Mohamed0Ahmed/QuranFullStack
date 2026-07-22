namespace QuranDashboard.Domain.Security.Audit;

// A permanent, append-only security-audit record (FR-039). It is DELIBERATELY separate from the product
// ChangeSet/AuditEvent stream: its `Id` is a database identity that advances on its OWN sequence and NEVER
// touches AbwabRevisionState.AuditHeadSequence, and it is never a Product-Restore-head event. Append-only
// is enforced at the database (trigger + restricted role), like the product audit tables.
public sealed class SecurityAuditEvent
{
    public long Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string ActorSubject { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset ServerTimestampUtc { get; set; }
}
