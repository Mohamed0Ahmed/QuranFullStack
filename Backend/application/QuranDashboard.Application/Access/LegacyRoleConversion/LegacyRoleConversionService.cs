using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access.LegacyRoleConversion;

public sealed class LegacyRoleConversionService(
    ILegacyRoleConversionStore store,
    IOwnerReconciliationService ownerReconciliationService) : ILegacyRoleConversionService
{
    public async Task<LegacyRoleConversionResult> InventoryAsync(CancellationToken cancellationToken)
    {
        var ownerReconciliation = await ownerReconciliationService.GetStatusAsync(cancellationToken);
        var snapshot = await store.ReadSnapshotAsync(cancellationToken);
        return BuildResult(ownerReconciliation, snapshot, applied: false);
    }

    public async Task<LegacyRoleConversionResult> ConvertAsync(CancellationToken cancellationToken)
    {
        await using var lease = await store.TryAcquireLeaseAsync(cancellationToken)
            ?? throw new LegacyRoleConversionLockUnavailableException();
        var snapshot = await lease.ReadSnapshotAsync(cancellationToken);
        var ownerReconciliation = await ownerReconciliationService.GetStatusAsync(cancellationToken);
        var result = BuildResult(ownerReconciliation, snapshot, applied: false);
        if (!result.CanConvert || !result.HasAdminOrEditorReferences)
        {
            return result;
        }

        await lease.ApplyAsync(
            result.AdminOrEditorUsers.Select(user => user.Id).ToArray(),
            cancellationToken);
        await lease.CommitAsync(cancellationToken);

        return BuildResult(
            ownerReconciliation,
            new LegacyRoleConversionSnapshot(
                snapshot.Roles,
                snapshot.Users
                    .Where(user => user.RoleName is not ("Admin" or "Editor"))
                    .ToArray()),
            applied: true);
    }

    private static LegacyRoleConversionResult BuildResult(
        OwnerReconciliationResult ownerReconciliation,
        LegacyRoleConversionSnapshot snapshot,
        bool applied)
    {
        var violations = new List<string>();
        var ownerRoleCount = snapshot.Roles.Count(role => role.Name == "Owner");
        if (ownerRoleCount != 1)
        {
            violations.Add($"owner_role_count={ownerRoleCount}");
        }

        var unsupportedRoleNames = snapshot.Roles
            .Select(role => role.Name)
            .Where(roleName => roleName is not ("Owner" or "Admin" or "Editor"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unsupportedRoleNames.Length > 0)
        {
            violations.Add($"unsupported_role_names={string.Join(',', unsupportedRoleNames)}");
        }

        var unsupportedRoleUserIds = snapshot.Users
            .Where(user => user.RoleName is not ("Owner" or "Admin" or "Editor"))
            .Select(user => user.Id)
            .Order()
            .ToArray();
        if (unsupportedRoleUserIds.Length > 0)
        {
            violations.Add($"unsupported_role_user_ids={string.Join(',', unsupportedRoleUserIds)}");
        }

        var grantedLegacyRoleUserIds = snapshot.Users
            .Where(user => user.RoleName is "Admin" or "Editor")
            .Where(user => user.DirectGrantCount > 0)
            .Select(user => user.Id)
            .Order()
            .ToArray();
        if (grantedLegacyRoleUserIds.Length > 0)
        {
            violations.Add($"legacy_role_grant_user_ids={string.Join(',', grantedLegacyRoleUserIds)}");
        }

        if (!ownerReconciliation.IsReady)
        {
            violations.Add("owner_reconciliation_not_ready");
        }

        return new LegacyRoleConversionResult(
            ownerReconciliation,
            snapshot.Users.OrderBy(user => user.Id).ToArray(),
            violations,
            violations.Count == 0,
            applied);
    }
}

public sealed class LegacyRoleConversionLockUnavailableException() : InvalidOperationException(
    "Legacy role conversion could not acquire its serialization lock.");
