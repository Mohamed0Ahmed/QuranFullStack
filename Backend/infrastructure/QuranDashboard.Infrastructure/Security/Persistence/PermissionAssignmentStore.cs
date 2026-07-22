using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Infrastructure.Security.Persistence;

public sealed class PermissionAssignmentStore(QuranDashboardDbContext db) : IPermissionAssignmentStore
{
    public async Task<PermissionAssignment?> FindTrackedAsync(
        PermissionTargetKind targetKind,
        string targetKey,
        string permissionCode,
        CancellationToken cancellationToken) =>
        await db.Set<PermissionAssignment>()
            .SingleOrDefaultAsync(
                assignment => assignment.TargetKind == targetKind
                    && assignment.TargetKey == targetKey
                    && assignment.PermissionCode == permissionCode,
                cancellationToken);

    public async Task<IReadOnlyList<PermissionAssignment>> ListGrantedAsync(CancellationToken cancellationToken) =>
        await db.Set<PermissionAssignment>()
            .AsNoTracking()
            .Where(assignment => assignment.IsGranted)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PermissionAssignment>> ListAllAsync(CancellationToken cancellationToken) =>
        await db.Set<PermissionAssignment>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public void Add(PermissionAssignment assignment) => db.Set<PermissionAssignment>().Add(assignment);
}
