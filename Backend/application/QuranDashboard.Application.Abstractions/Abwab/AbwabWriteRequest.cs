namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed record AbwabWriteRequest(
    ExpectedTimelineGeneration ExpectedGeneration,
    string ActorSubject,
    IReadOnlyList<AbwabAuditEventDraft> Events);
