using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Security.Permissions;

public sealed record PermissionCatalogueEntryDto(string Code, bool SystemOwnerOnly, bool DashboardAdminBaseline, bool Assignable);

public sealed record PermissionAssignmentDto(
    string TargetKind,
    string TargetKey,
    string PermissionCode,
    long Version,
    bool IsGranted);

public sealed record PermissionAdminViewDto(
    IReadOnlyList<PermissionCatalogueEntryDto> Catalogue,
    IReadOnlyList<PermissionAssignmentDto> Assignments);

public sealed record GrantPermissionRequest(
    string TargetKind,
    string TargetKey,
    string PermissionCode,
    long ExpectedTimelineGeneration,
    long ExpectedVersion);

public sealed record RevokePermissionRequest(
    string TargetKind,
    string TargetKey,
    string PermissionCode,
    long ExpectedTimelineGeneration,
    long ExpectedVersion);

internal static class PermissionTargetKindParsing
{
    public static bool TryParse(string value, out PermissionTargetKind kind) =>
        Enum.TryParse(value, ignoreCase: true, out kind) && Enum.IsDefined(kind);
}
