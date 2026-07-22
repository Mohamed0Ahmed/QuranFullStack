using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Security;

public sealed record SecurityAuditWriteRequest(
    ExpectedTimelineGeneration ExpectedGeneration,
    string ActorSubject,
    Func<CancellationToken, Task<SecurityAuditOutcome>> Operation);
