using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Security;

// The input to a separate permanent security-audit unit of work (FR-039). The executor takes the
// AbwabWriteBarrier then the AbwabRevisionState row locks (same order as the product protocol) and verifies
// ExpectedGeneration BEFORE running `Operation`, but it NEVER advances AuditHeadSequence and NEVER creates a
// product ChangeSet / Restore-head. `Operation` performs the domain read + validation + staging and returns
// whether an audit event should be appended.
public sealed record SecurityAuditWriteRequest(
    ExpectedTimelineGeneration ExpectedGeneration,
    string ActorSubject,
    Func<CancellationToken, Task<SecurityAuditOutcome>> Operation);
