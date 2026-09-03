using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class OwnerReconciliationStore(
    QuranDashboardDbContext db,
    IOwnerBootstrapConfigurationSource configurationSource) : IOwnerReconciliationStore
{
    private const long AdvisoryLockKey = 844889163665073473L;

    public Task<OwnerReconciliationSnapshot> ReadSnapshotAsync(
        OwnerBootstrapConfiguration configuration,
        CancellationToken cancellationToken)
    {
        return ReadSnapshotAsync(configuration, lockRows: false, cancellationToken);
    }

    public async Task<OwnerReconciliationCommitResult> TryCommitAsync(
        OwnerReconciliationCommitIntent intent,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await TryAcquireLockAsync(cancellationToken))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return OwnerReconciliationCommitResult.LockUnavailable;
        }

        var currentConfiguration = configurationSource.GetCurrent();
        if (!ConfigurationMatches(intent.ExpectedConfiguration, currentConfiguration))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return OwnerReconciliationCommitResult.StateChanged;
        }

        var currentSnapshot = await ReadSnapshotAsync(currentConfiguration, lockRows: true, cancellationToken);
        if (!SnapshotMatches(intent.PlanningSnapshot, currentSnapshot))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return OwnerReconciliationCommitResult.StateChanged;
        }

        await ApplyAsync(intent.PreparedMutation, currentSnapshot.OwnerRoleId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OwnerReconciliationCommitResult.Applied;
    }

    private async Task<OwnerReconciliationSnapshot> ReadSnapshotAsync(
        OwnerBootstrapConfiguration configuration,
        bool lockRows,
        CancellationToken cancellationToken)
    {
        var ownerRole = await db.AccessRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Name == RoleNames.Owner, cancellationToken)
            ?? throw new InvalidOperationException("The Owner role is required for reconciliation.");
        var configuredEmails = configuration.NormalizedEmails.Order(StringComparer.Ordinal).ToArray();

        if (lockRows)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM users WHERE role_id = {ownerRole.Id} OR normalized_email = ANY({configuredEmails}) ORDER BY id FOR UPDATE;",
                cancellationToken);
        }

        var users = await db.AccessUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .Where(user => user.RoleId == ownerRole.Id || configuredEmails.Contains(user.NormalizedEmail))
            .OrderBy(user => user.Id)
            .ToListAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToArray();
        if (lockRows && userIds.Length > 0)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM user_permissions WHERE user_id = ANY({userIds}) ORDER BY user_id, permission_id FOR UPDATE;",
                cancellationToken);
        }

        var grants = await db.AccessUserPermissions
            .AsNoTracking()
            .Include(grant => grant.Permission)
            .Where(grant => userIds.Contains(grant.UserId))
            .OrderBy(grant => grant.UserId)
            .ThenBy(grant => grant.PermissionId)
            .ToListAsync(cancellationToken);
        var grantsByUserId = grants
            .GroupBy(grant => grant.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<OwnerReconciliationGrant>)group
                    .OrderBy(grant => grant.Permission.Code, StringComparer.Ordinal)
                    .Select(grant => new OwnerReconciliationGrant(
                        grant.UserId,
                        grant.PermissionId,
                        grant.Permission.Code))
                    .ToArray());

        return new OwnerReconciliationSnapshot(
            ownerRole.Id,
            users.Select(user => new OwnerReconciliationUser(
                    user.Id,
                    user.LogtoSub,
                    user.NormalizedEmail,
                    user.DisplayName,
                    user.Status,
                    user.RoleId,
                    user.RoleId == ownerRole.Id,
                    user.Role?.Name,
                    grantsByUserId.GetValueOrDefault(user.Id, [])))
                .ToArray());
    }

    private async Task ApplyAsync(
        OwnerReconciliationMutation mutation,
        int ownerRoleId,
        CancellationToken cancellationToken)
    {
        var roleChangeIds = mutation.RoleChanges.Select(change => change.UserId).Distinct().ToArray();
        var users = await db.AccessUsers
            .Where(user => roleChangeIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);
        foreach (var roleChange in mutation.RoleChanges)
        {
            var user = users[roleChange.UserId];
            user.RoleId = roleChange.IsOwner ? ownerRoleId : null;
            user.Status = roleChange.Status;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        var revokedUserIds = mutation.GrantRevocations.Select(revocation => revocation.UserId).Distinct().ToArray();
        var revokedPermissionIds = mutation.GrantRevocations.Select(revocation => revocation.PermissionId).Distinct().ToArray();
        var grants = await db.AccessUserPermissions
            .Where(grant => revokedUserIds.Contains(grant.UserId)
                && revokedPermissionIds.Contains(grant.PermissionId))
            .ToDictionaryAsync(grant => (grant.UserId, grant.PermissionId), cancellationToken);
        db.AccessUserPermissions.RemoveRange(mutation.GrantRevocations.Select(revocation =>
            grants[(revocation.UserId, revocation.PermissionId)]));
        db.AccessAuditEvents.AddRange(mutation.AuditEntries.Select(CreateAuditEvent));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryAcquireLockAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT pg_try_advisory_xact_lock(@lock_key);";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lock_key";
        parameter.DbType = DbType.Int64;
        parameter.Value = AdvisoryLockKey;
        command.Parameters.Add(parameter);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static bool ConfigurationMatches(
        OwnerBootstrapConfiguration expected,
        OwnerBootstrapConfiguration current)
    {
        return string.Equals(
                expected.ConfigurationFingerprint,
                current.ConfigurationFingerprint,
                StringComparison.Ordinal)
            && expected.NormalizedEmails.SetEquals(current.NormalizedEmails);
    }

    private static bool SnapshotMatches(
        OwnerReconciliationSnapshot expected,
        OwnerReconciliationSnapshot current)
    {
        if (expected.OwnerRoleId != current.OwnerRoleId || expected.Users.Count != current.Users.Count)
        {
            return false;
        }

        for (var userIndex = 0; userIndex < expected.Users.Count; userIndex++)
        {
            var expectedUser = expected.Users[userIndex];
            var currentUser = current.Users[userIndex];
            if (expectedUser.Id != currentUser.Id
                || !string.Equals(expectedUser.LogtoSub, currentUser.LogtoSub, StringComparison.Ordinal)
                || !string.Equals(expectedUser.NormalizedEmail, currentUser.NormalizedEmail, StringComparison.Ordinal)
                || !string.Equals(expectedUser.DisplayName, currentUser.DisplayName, StringComparison.Ordinal)
                || expectedUser.Status != currentUser.Status
                || expectedUser.RoleId != currentUser.RoleId
                || expectedUser.IsOwner != currentUser.IsOwner
                || !string.Equals(expectedUser.RoleName, currentUser.RoleName, StringComparison.Ordinal)
                || expectedUser.DirectGrants.Count != currentUser.DirectGrants.Count)
            {
                return false;
            }

            for (var grantIndex = 0; grantIndex < expectedUser.DirectGrants.Count; grantIndex++)
            {
                var expectedGrant = expectedUser.DirectGrants[grantIndex];
                var currentGrant = currentUser.DirectGrants[grantIndex];
                if (expectedGrant.UserId != currentGrant.UserId
                    || expectedGrant.PermissionId != currentGrant.PermissionId
                    || !string.Equals(expectedGrant.PermissionCode, currentGrant.PermissionCode, StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static AccessAuditEvent CreateAuditEvent(OwnerReconciliationAuditEntry entry)
    {
        return new AccessAuditEvent(
            DateTimeOffset.UtcNow,
            entry.ActionType,
            AccessAuditActorType.System,
            null,
            entry.Target.Id,
            "{\"type\":\"system\",\"operation\":\"owner-reconciliation\"}",
            JsonSerializer.Serialize(entry.Target),
            entry.PermissionCode,
            JsonSerializer.Serialize(entry.Before),
            JsonSerializer.Serialize(entry.After),
            entry.Reason,
            new AccessAuditMetadata(
                1,
                null,
                new Dictionary<string, string>
                {
                    ["operation"] = "owner-reconciliation",
                    ["configurationFingerprint"] = entry.ConfigurationFingerprint,
                    ["configuredNormalizedEmail"] = entry.ConfiguredNormalizedEmail,
                    ["evidenceSource"] = entry.EvidenceSource,
                }));
    }
}
