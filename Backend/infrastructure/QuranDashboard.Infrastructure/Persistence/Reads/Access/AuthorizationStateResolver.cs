using System.Collections.Frozen;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Access;

public sealed class AuthorizationStateResolver(QuranDashboardDbContext db) : IAuthorizationStateResolver
{
    private string? _resolvedSub;
    private Task<AuthorizationState?>? _resolution;

    public Task<AuthorizationState?> ResolveAsync(string logtoSub, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logtoSub);

        if (_resolvedSub is not null && !string.Equals(_resolvedSub, logtoSub, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Authorization state can resolve only one subject per scope.");
        }

        _resolvedSub = logtoSub;
        return _resolution ??= ResolveCoreAsync(logtoSub, cancellationToken);
    }

    private async Task<AuthorizationState?> ResolveCoreAsync(string logtoSub, CancellationToken cancellationToken)
    {
        var projection = await db.AccessUsers
            .AsNoTracking()
            .Where(user => user.LogtoSub == logtoSub)
            .Select(user => new AuthorizationStateProjection(
                user.Id,
                user.Status,
                user.Role != null && user.Role.Name == RoleNames.Owner,
                user.UserPermissions
                    .Where(grant => user.Status == UserStatus.Active
                                    && (user.Role == null || user.Role.Name != RoleNames.Owner))
                    .Select(grant => grant.Permission.Code)
                    .ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

        return projection is null
            ? null
            : new AuthorizationState(
                projection.UserId,
                projection.Status,
                projection.IsOwner,
                projection.PermissionCodes.ToFrozenSet(StringComparer.Ordinal));
    }

    private sealed record AuthorizationStateProjection(
        int UserId,
        UserStatus Status,
        bool IsOwner,
        string[] PermissionCodes);
}
