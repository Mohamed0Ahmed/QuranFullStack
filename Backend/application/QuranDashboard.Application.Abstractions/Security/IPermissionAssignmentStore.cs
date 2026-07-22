using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Application.Abstractions.Security;

public interface IPermissionAssignmentStore
{
    Task<PermissionAssignment?> FindTrackedAsync(
        PermissionTargetKind targetKind,
        string targetKey,
        string permissionCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionAssignment>> ListGrantedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionAssignment>> ListAllAsync(CancellationToken cancellationToken);

    void Add(PermissionAssignment assignment);
}
