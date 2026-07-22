namespace QuranDashboard.Application.Abstractions.Abwab;

// The EF-free input to an audited Abwab write. The executor takes the barrier, row-locks the revision
// state, checks ExpectedGeneration before any mutation, stamps a ChangeSet and its events, and commits.
public sealed record AbwabWriteRequest(
    ExpectedTimelineGeneration ExpectedGeneration,
    string ActorSubject,
    IReadOnlyList<AbwabAuditEventDraft> Events);
