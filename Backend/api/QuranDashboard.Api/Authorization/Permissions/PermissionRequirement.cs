using Microsoft.AspNetCore.Authorization;

namespace QuranDashboard.Api.Authorization.Permissions;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
