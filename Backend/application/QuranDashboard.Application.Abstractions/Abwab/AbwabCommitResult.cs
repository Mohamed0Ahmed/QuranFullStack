namespace QuranDashboard.Application.Abstractions.Abwab;

// The committed coordinates of one audited write. ChangeSetSequence is the head value assigned FROM
// AbwabRevisionState.AuditHeadSequence; TimelineGeneration is the immutable stamp captured at creation.
public sealed record AbwabCommitResult(Guid ChangeSetId, long ChangeSetSequence, long TimelineGeneration);
