namespace QuranDashboard.Domain.Abwab.Audit;

public sealed class AuditEvent
{
    public Guid Id { get; set; }

    public Guid ChangeSetId { get; set; }

    public int EventOrdinal { get; set; }

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset ServerTimestampUtc { get; set; }
}
