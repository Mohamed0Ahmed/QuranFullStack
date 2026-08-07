using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

internal static class AccessUserContractMapper
{
    public static bool IsOwner(User user) => string.Equals(user.Role?.Name, RoleNames.Owner, StringComparison.Ordinal);

    public static string StatusValue(UserStatus status) => status switch
    {
        UserStatus.Pending => "pending",
        UserStatus.Active => "active",
        UserStatus.Disabled => "disabled",
        _ => throw new InvalidOperationException($"Unhandled {nameof(UserStatus)} variant '{status}'.")
    };

    public static AccessUserDetail ToDetail(User user)
    {
        var isOwner = IsOwner(user);
        var permissionCodes = isOwner || user.Status != UserStatus.Active
            ? []
            : user.UserPermissions
                .OrderBy(grant => grant.Permission.DisplayOrder)
                .Select(grant => grant.Permission.Code)
                .ToArray();

        return new AccessUserDetail(
            user.Id,
            user.LogtoSub,
            user.Email,
            user.NormalizedEmail,
            user.UserName,
            user.DisplayName,
            user.Title,
            StatusValue(user.Status),
            isOwner,
            permissionCodes,
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            user.Version);
    }

    public static AccessUserPermissions ToPermissions(User user)
    {
        var isOwner = IsOwner(user);
        var permissionCodes = isOwner || user.Status != UserStatus.Active
            ? []
            : user.UserPermissions
                .OrderBy(grant => grant.Permission.DisplayOrder)
                .Select(grant => grant.Permission.Code)
                .ToArray();

        return new AccessUserPermissions(
            user.Id,
            StatusValue(user.Status),
            isOwner,
            user.Version,
            permissionCodes);
    }

    public static AccessAuditUserSnapshot ToAuditSnapshot(User user) => new(
        user.Id,
        user.LogtoSub,
        user.Email,
        user.NormalizedEmail,
        user.DisplayName,
        user.Status,
        IsOwner(user));

    public static AccessAuditUserState ToAuditState(User user, IReadOnlyList<string> permissionCodes) => new(
        user.LogtoSub,
        user.Status,
        IsOwner(user),
        permissionCodes);
}
