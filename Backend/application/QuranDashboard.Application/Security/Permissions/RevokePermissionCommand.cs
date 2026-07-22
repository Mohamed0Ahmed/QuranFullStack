using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Application.Security.Permissions;

// An Abwab writer (barrier + generation contract) that revokes a permission from a role or subject.
public sealed record RevokePermissionCommand(
    PermissionTargetKind TargetKind,
    string TargetKey,
    string PermissionCode,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    long ExpectedVersion,
    string ActorSubject) : IAbwabMutationCommand;
