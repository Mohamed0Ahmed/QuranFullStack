namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed record AbwabAuditedOperationResult(bool Audited, Guid? ChangeSetId, long? ChangeSetSequence, long TimelineGeneration);
