using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Abstractions.Access;

public interface ILegacyRoleConversionService
{
    Task<LegacyRoleConversionResult> InventoryAsync(CancellationToken cancellationToken);

    Task<LegacyRoleConversionResult> ConvertAsync(CancellationToken cancellationToken);
}

public interface ILegacyRoleConversionStore
{
    Task<LegacyRoleConversionSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken);

    Task<ILegacyRoleConversionLease?> TryAcquireLeaseAsync(CancellationToken cancellationToken);
}

public interface ILegacyRoleConversionLease : IAsyncDisposable
{
    Task<LegacyRoleConversionSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken);

    Task ApplyAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);
}

public sealed record LegacyRoleConversionSnapshot(
    IReadOnlyList<LegacyRoleDefinition> Roles,
    IReadOnlyList<LegacyRoleInventoryUser> Users);

public sealed record LegacyRoleDefinition(int Id, string Name);

public sealed record LegacyRoleInventoryUser(
    int Id,
    int RoleId,
    string RoleName,
    UserStatus Status,
    string NormalizedEmail,
    string LogtoSub,
    int DirectGrantCount);

public sealed record LegacyRoleConversionResult(
    OwnerReconciliationResult OwnerReconciliation,
    IReadOnlyList<LegacyRoleInventoryUser> Users,
    IReadOnlyList<string> PreflightViolations,
    bool CanConvert,
    bool Applied)
{
    public IReadOnlyList<LegacyRoleInventoryUser> OwnerUsers => Users
        .Where(user => user.RoleName == "Owner")
        .ToArray();

    public IReadOnlyList<LegacyRoleInventoryUser> AdminUsers => Users
        .Where(user => user.RoleName == "Admin")
        .ToArray();

    public IReadOnlyList<LegacyRoleInventoryUser> EditorUsers => Users
        .Where(user => user.RoleName == "Editor")
        .ToArray();

    public IReadOnlyList<LegacyRoleInventoryUser> AdminOrEditorUsers => Users
        .Where(user => user.RoleName is "Admin" or "Editor")
        .ToArray();

    public bool HasAdminOrEditorReferences => AdminOrEditorUsers.Count > 0;
}
