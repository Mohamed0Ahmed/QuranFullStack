using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Application.Abstractions.Security;

// Persistence port for permission assignments. `FindTrackedAsync` returns a tracked row so a grant/revoke
// mutation persists on the shared unit of work; `ListGrantedAsync` is a no-track read for `/me` and the
// admin list projection.
public interface IPermissionAssignmentStore
{
    Task<PermissionAssignment?> FindTrackedAsync(
        PermissionTargetKind targetKind,
        string targetKey,
        string permissionCode,
        CancellationToken cancellationToken);

    // Granted-only — feeds the /me effective-permission projection.
    Task<IReadOnlyList<PermissionAssignment>> ListGrantedAsync(CancellationToken cancellationToken);

    // ALL assignments including revoked tombstones (IsGranted=false) with their current Version — feeds the
    // admin List projection so the client can carry the correct expected version on a re-grant after revoke.
    Task<IReadOnlyList<PermissionAssignment>> ListAllAsync(CancellationToken cancellationToken);

    void Add(PermissionAssignment assignment);
}
