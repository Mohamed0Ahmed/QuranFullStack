using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Security.Permissions;

// Request/response contracts for the Owner-only permission-administration surface. English identifiers;
// user-facing messages are Arabic and carried by the ApiResponse envelope.

public sealed record PermissionCatalogueEntryDto(string Code, bool SystemOwnerOnly, bool DashboardAdminBaseline, bool Assignable);

// `IsGranted` distinguishes a live grant from a revoked tombstone. Tombstones are returned so the client can
// carry the correct expected version on a re-grant after revoke; the UI shows only granted rows.
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
