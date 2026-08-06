using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class OwnerReconciliationStore(QuranDashboardDbContext db) : IOwnerReconciliationStore
{
    private const long AdvisoryLockKey = 844889163665073473L;

    public Task<OwnerReconciliationSnapshot> ReadSnapshotAsync(
        OwnerBootstrapConfiguration configuration,
        CancellationToken cancellationToken)
    {
        return ReadSnapshotAsync(configuration, lockRows: false, cancellationToken);
    }

    public async Task<IOwnerReconciliationLease?> TryAcquireLeaseAsync(CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await TryAcquireLockAsync(cancellationToken))
        {
            await transaction.DisposeAsync();
            return null;
        }

        return new OwnerReconciliationLease(db, transaction);
    }

    private async Task<OwnerReconciliationSnapshot> ReadSnapshotAsync(
        OwnerBootstrapConfiguration configuration,
        bool lockRows,
        CancellationToken cancellationToken)
    {
        var ownerRole = await db.AccessRoles.SingleOrDefaultAsync(
            role => role.Name == RoleNames.Owner,
            cancellationToken)
            ?? throw new InvalidOperationException("The Owner role is required for reconciliation.");
        var configuredEmails = configuration.NormalizedEmails.Order(StringComparer.Ordinal).ToArray();

        if (lockRows)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM users WHERE role_id = {ownerRole.Id} OR normalized_email = ANY({configuredEmails}) FOR UPDATE;",
                cancellationToken);
        }

        IQueryable<User> usersQuery = db.AccessUsers
            .Include(user => user.Role)
            .Where(user => user.RoleId == ownerRole.Id || configuredEmails.Contains(user.NormalizedEmail));
        if (!lockRows)
        {
            usersQuery = usersQuery.AsNoTracking();
        }

        var users = await usersQuery
            .OrderBy(user => user.Id)
            .ToListAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToArray();
        if (lockRows && userIds.Length > 0)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM user_permissions WHERE user_id = ANY({userIds}) FOR UPDATE;",
                cancellationToken);
        }

        IQueryable<UserPermission> grantsQuery = db.AccessUserPermissions
            .Include(grant => grant.Permission)
            .Where(grant => userIds.Contains(grant.UserId));
        if (!lockRows)
        {
            grantsQuery = grantsQuery.AsNoTracking();
        }

        var grants = await grantsQuery
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
                    user.RoleId == ownerRole.Id,
                    user.Role?.Name,
                    grantsByUserId.GetValueOrDefault(user.Id, [])))
                .ToArray());
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

    private sealed class OwnerReconciliationLease(
        QuranDashboardDbContext db,
        IDbContextTransaction transaction) : IOwnerReconciliationLease
    {
        private readonly QuranDashboardDbContext _db = db;
        private readonly IDbContextTransaction _transaction = transaction;

        public Task<OwnerReconciliationSnapshot> ReadSnapshotAsync(
            OwnerBootstrapConfiguration configuration,
            CancellationToken cancellationToken)
        {
            return new OwnerReconciliationStore(_db)
                .ReadSnapshotAsync(configuration, lockRows: true, cancellationToken);
        }

        public async Task ApplyAsync(OwnerReconciliationMutation mutation, CancellationToken cancellationToken)
        {
            var users = _db.AccessUsers.Local.ToDictionary(user => user.Id);
            foreach (var roleChange in mutation.RoleChanges)
            {
                var user = users[roleChange.UserId];
                user.RoleId = roleChange.IsOwner
                    ? (await OwnerRoleIdAsync(cancellationToken))
                    : null;
                user.Status = roleChange.Status;
                user.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }

            var grants = _db.AccessUserPermissions.Local.ToDictionary(grant => (grant.UserId, grant.PermissionId));
            _db.AccessUserPermissions.RemoveRange(mutation.GrantRevocations.Select(revocation =>
                grants[(revocation.UserId, revocation.PermissionId)]));
            _db.AccessAuditEvents.AddRange(mutation.AuditEntries.Select(CreateAuditEvent));
            await _db.SaveChangesAsync(cancellationToken);
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            return _transaction.CommitAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }

        private async Task<int> OwnerRoleIdAsync(CancellationToken cancellationToken)
        {
            return await _db.AccessRoles
                .Where(role => role.Name == RoleNames.Owner)
                .Select(role => role.Id)
                .SingleAsync(cancellationToken);
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
}
