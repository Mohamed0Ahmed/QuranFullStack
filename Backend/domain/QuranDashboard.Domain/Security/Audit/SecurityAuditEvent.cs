namespace QuranDashboard.Domain.Security.Audit;

public sealed class SecurityAuditEvent
{
    public long Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string ActorSubject { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset ServerTimestampUtc { get; set; }
}
