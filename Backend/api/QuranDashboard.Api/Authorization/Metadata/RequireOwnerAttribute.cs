using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authorization.Owner;

namespace QuranDashboard.Api.Authorization.Metadata;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireOwnerAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [OwnerOnlyRequirement.Instance];
}
