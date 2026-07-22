using Microsoft.AspNetCore.Authorization;

namespace QuranDashboard.Api.Security.Authorization;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
