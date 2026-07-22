namespace QuranDashboard.Domain.Abwab.Audit;

// The audited envelope for exactly one Abwab mutation. A write with no ChangeSet is rejected at
// SavingChanges. TimelineGeneration is the immutable generation stamp captured at creation (§7.9);
// ChangeSetSequence is the commit-order coordinate assigned FROM the row-locked
// AbwabRevisionState.AuditHeadSequence (never the inverse) — distinct from AuditEvent.EventOrdinal.
public sealed class ChangeSet
{
    public Guid Id { get; set; }

    public long TimelineGeneration { get; set; }

    public long ChangeSetSequence { get; set; }

    public string ActorSubject { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<AuditEvent> Events { get; } = [];
}
