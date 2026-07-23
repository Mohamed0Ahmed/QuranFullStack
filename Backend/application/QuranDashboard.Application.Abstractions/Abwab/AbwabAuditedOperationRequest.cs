using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Application.Abstractions.Abwab;

// The operation runs INSIDE the barrier/revision FOR-UPDATE lock, after the ExpectedTimelineGeneration
// check and before any ChangeSet is written; it is handed the locked AbwabRevisionState so a structural
// writer can validate/bump TreeRevision atomically with its own entity mutations.
public sealed record AbwabAuditedOperationRequest(
    ExpectedTimelineGeneration ExpectedGeneration,
    string ActorSubject,
    Func<AbwabRevisionState, CancellationToken, Task<AbwabAuditedOperationOutcome>> Operation);
