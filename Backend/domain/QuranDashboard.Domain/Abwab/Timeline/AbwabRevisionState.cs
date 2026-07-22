namespace QuranDashboard.Domain.Abwab.Timeline;

// Singleton technical state driving the commit protocol. Seeded exactly once at migration
// (AuditHeadSequence=0, TimelineGeneration=0, TreeRevision=0). Row-locked by every audited commit;
// AuditHeadSequence advances by one per successful commit and feeds each ChangeSet.ChangeSetSequence
// (never the inverse). These counters are current concurrency/reconciliation state, not product
// snapshot values, and are never inverse-restored: a rollback leaves all three unchanged (§7.1).
public sealed class AbwabRevisionState
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public long AuditHeadSequence { get; set; }

    public long TimelineGeneration { get; set; }

    public long TreeRevision { get; set; }
}
