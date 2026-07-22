using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Application.Security.Permissions;

// An Abwab writer (barrier + generation contract) that grants a permission to a role or subject. Registered
// against the stabilization registry via AbwabWriterRegistrations; carries ExpectedTimelineGeneration so the
// generation contract-coverage gate stays green.
public sealed record GrantPermissionCommand(
    PermissionTargetKind TargetKind,
    string TargetKey,
    string PermissionCode,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    long ExpectedVersion,
    string ActorSubject) : IAbwabMutationCommand;
