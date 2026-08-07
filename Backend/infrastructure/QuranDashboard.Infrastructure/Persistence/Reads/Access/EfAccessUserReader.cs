using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Access;

public sealed class EfAccessUserReader(QuranDashboardDbContext db) : IAccessUserReader
{
    public async Task<PagedResult<AccessUserSummary>> ListAsync(
        AccessUserListQuery query,
        CancellationToken cancellationToken)
    {
        var users = db.AccessUsers.AsNoTracking().AsQueryable();
        if (ParseStatus(query.Status) is { } status)
        {
            users = users.Where(user => user.Status == status);
        }

        if (query.IsOwner is { } isOwner)
        {
            users = isOwner
                ? users.Where(user => user.Role != null && user.Role.Name == RoleNames.Owner)
                : users.Where(user => user.Role == null || user.Role.Name != RoleNames.Owner);
        }

        if (query.Search is not null)
        {
            users = users.Where(user => user.NormalizedEmail == query.Search);
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var offset = ((long)query.Page - AccessUserPaging.MinimumPage) * query.PageSize;
        if (offset >= totalCount)
        {
            return new PagedResult<AccessUserSummary>(query.Page, query.PageSize, totalCount, []);
        }

        var projections = await users
            .OrderByDescending(user => user.UpdatedAtUtc)
            .ThenByDescending(user => user.Id)
            .Skip(checked((int)offset))
            .Take(query.PageSize)
            .Select(user => new AccessUserListProjection(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Status,
                user.Role != null && user.Role.Name == RoleNames.Owner,
                user.Status == UserStatus.Active && (user.Role == null || user.Role.Name != RoleNames.Owner)
                    ? user.UserPermissions.Count
                    : 0,
                user.CreatedAtUtc,
                user.UpdatedAtUtc,
                user.Version))
            .ToListAsync(cancellationToken);

        var items = projections
            .Select(user => new AccessUserSummary(
                user.Id,
                user.Email,
                user.DisplayName,
                AccessUserContractMapper.StatusValue(user.Status),
                user.IsOwner,
                user.PermissionCount,
                user.CreatedAtUtc,
                user.UpdatedAtUtc,
                user.Version))
            .ToArray();
        return new PagedResult<AccessUserSummary>(query.Page, query.PageSize, totalCount, items);
    }

    public async Task<AccessUserDetail?> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken);
        return user is null ? null : AccessUserContractMapper.ToDetail(user);
    }

    public async Task<AccessUserPermissions?> GetPermissionsAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken);
        return user is null ? null : AccessUserContractMapper.ToPermissions(user);
    }

    private Task<User?> LoadUserAsync(int userId, CancellationToken cancellationToken) =>
        db.AccessUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .Include(user => user.UserPermissions)
            .ThenInclude(grant => grant.Permission)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    private static UserStatus? ParseStatus(string? status) => status switch
    {
        null => null,
        "pending" => UserStatus.Pending,
        "active" => UserStatus.Active,
        "disabled" => UserStatus.Disabled,
        _ => throw new InvalidOperationException($"Unhandled access user status filter '{status}'.")
    };

    private sealed record AccessUserListProjection(
        int Id,
        string Email,
        string? DisplayName,
        UserStatus Status,
        bool IsOwner,
        int PermissionCount,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        uint Version);
}
