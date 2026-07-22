namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed record AbwabCommitResult(Guid ChangeSetId, long ChangeSetSequence, long TimelineGeneration);
