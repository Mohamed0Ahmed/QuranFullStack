namespace QuranDashboard.Domain.Abwab.Audit;

public sealed class ChangeSet
{
    public Guid Id { get; set; }

    public long TimelineGeneration { get; set; }

    public long ChangeSetSequence { get; set; }

    public string ActorSubject { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<AuditEvent> Events { get; } = [];
}
