using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Application.Security.Permissions;

public sealed record RevokePermissionCommand(
    PermissionTargetKind TargetKind,
    string TargetKey,
    string PermissionCode,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    long ExpectedVersion,
    string ActorSubject) : IAbwabMutationCommand;
