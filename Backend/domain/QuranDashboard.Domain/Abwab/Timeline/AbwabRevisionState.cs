namespace QuranDashboard.Domain.Abwab.Timeline;

public sealed class AbwabRevisionState
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public long AuditHeadSequence { get; set; }

    public long TimelineGeneration { get; set; }

    public long TreeRevision { get; set; }
}
