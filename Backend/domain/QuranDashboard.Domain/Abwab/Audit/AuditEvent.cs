namespace QuranDashboard.Domain.Abwab.Audit;

// Append-only record of one step within a ChangeSet. EventOrdinal is the deterministic ordering
// WITHIN the operation — not the cross-operation commit coordinate (that is ChangeSet.ChangeSetSequence).
// Enforced append-only at the DB (append-only/TRUNCATE trigger + restricted role); never updated,
// never physically deleted, never truncated.
public sealed class AuditEvent
{
    public Guid Id { get; set; }

    public Guid ChangeSetId { get; set; }

    public int EventOrdinal { get; set; }

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset ServerTimestampUtc { get; set; }
}
