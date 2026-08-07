using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class LegacyRoleConversionStore(QuranDashboardDbContext db) : ILegacyRoleConversionStore
{
    public Task<LegacyRoleConversionSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        return ReadSnapshotCoreAsync(cancellationToken);
    }

    public async Task<ILegacyRoleConversionLease?> TryAcquireLeaseAsync(CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT id FROM roles FOR UPDATE;", cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                "SELECT id FROM users WHERE role_id IS NOT NULL FOR UPDATE;",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                "SELECT user_id FROM user_permissions WHERE user_id IN (SELECT id FROM users WHERE role_id IS NOT NULL) FOR UPDATE;",
                cancellationToken);
            return new LegacyRoleConversionLease(db, transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private async Task<LegacyRoleConversionSnapshot> ReadSnapshotCoreAsync(CancellationToken cancellationToken)
    {
        var roles = await db.AccessRoles
            .AsNoTracking()
            .OrderBy(role => role.Id)
            .Select(role => new LegacyRoleDefinition(role.Id, role.Name))
            .ToArrayAsync(cancellationToken);
        var users = await db.AccessUsers
            .AsNoTracking()
            .Where(user => user.RoleId != null)
            .OrderBy(user => user.Id)
            .Select(user => new LegacyRoleInventoryUser(
                user.Id,
                user.RoleId!.Value,
                user.Role!.Name,
                user.Status,
                user.NormalizedEmail,
                user.LogtoSub,
                user.UserPermissions.Count))
            .ToArrayAsync(cancellationToken);

        return new LegacyRoleConversionSnapshot(roles, users);
    }

    private sealed class LegacyRoleConversionLease(
        QuranDashboardDbContext db,
        IDbContextTransaction transaction) : ILegacyRoleConversionLease
    {
        private readonly QuranDashboardDbContext _db = db;
        private readonly IDbContextTransaction _transaction = transaction;

        public Task<LegacyRoleConversionSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken)
        {
            return new LegacyRoleConversionStore(_db).ReadSnapshotCoreAsync(cancellationToken);
        }

        public async Task ApplyAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken)
        {
            var users = await _db.AccessUsers
                .Include(user => user.Role)
                .Include(user => user.UserPermissions)
                .Where(user => userIds.Contains(user.Id))
                .OrderBy(user => user.Id)
                .ToArrayAsync(cancellationToken);
            if (users.Length != userIds.Count)
            {
                throw new InvalidOperationException("Legacy role conversion lost a locked user.");
            }

            if (users.Any(user => user.UserPermissions.Count > 0))
            {
                throw new InvalidOperationException("Legacy role conversion found direct grants after preflight.");
            }

            if (users.Any(user => user.Role?.Name is not ("Admin" or "Editor")))
            {
                throw new InvalidOperationException("Legacy role conversion found a non-legacy role after preflight.");
            }

            foreach (var user in users)
            {
                var before = LegacyRoleAuditSnapshot.From(user);
                user.RoleId = null;
                user.Role = null;
                user.UpdatedAtUtc = DateTimeOffset.UtcNow;
                var after = LegacyRoleAuditSnapshot.From(user);
                _db.AccessAuditEvents.Add(CreateAuditEvent(user.Id, before, after));
            }

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

        private static AccessAuditEvent CreateAuditEvent(
            int userId,
            LegacyRoleAuditSnapshot before,
            LegacyRoleAuditSnapshot after)
        {
            return new AccessAuditEvent(
                DateTimeOffset.UtcNow,
                AccessAuditActionType.LegacyRoleRemoved,
                AccessAuditActorType.System,
                null,
                userId,
                "{\"type\":\"system\",\"operation\":\"legacy-role-conversion\"}",
                JsonSerializer.Serialize(after),
                null,
                JsonSerializer.Serialize(before),
                JsonSerializer.Serialize(after),
                "Legacy role conversion.",
                new AccessAuditMetadata(
                    1,
                    null,
                    new Dictionary<string, string>
                    {
                        ["operation"] = "legacy-role-conversion",
                    }));
        }
    }

    private sealed record LegacyRoleAuditSnapshot(
        int Id,
        string LogtoSub,
        string NormalizedEmail,
        string? DisplayName,
        UserStatus Status,
        int? RoleId,
        string? RoleName)
    {
        public static LegacyRoleAuditSnapshot From(User user)
        {
            return new LegacyRoleAuditSnapshot(
                user.Id,
                user.LogtoSub,
                user.NormalizedEmail,
                user.DisplayName,
                user.Status,
                user.RoleId,
                user.Role?.Name);
        }
    }
}
