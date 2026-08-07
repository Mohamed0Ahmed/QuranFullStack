using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authorization.Permissions;

namespace QuranDashboard.Api.Authorization.Metadata;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute(string permissionCode) : AuthorizeAttribute, IAuthorizationRequirementData
{
    public string PermissionCode { get; } = permissionCode;

    public IEnumerable<IAuthorizationRequirement> GetRequirements() =>
        [new PermissionRequirement(PermissionCode)];
}
