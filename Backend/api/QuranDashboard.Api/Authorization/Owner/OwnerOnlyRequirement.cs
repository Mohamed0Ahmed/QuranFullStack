using Microsoft.AspNetCore.Authorization;

namespace QuranDashboard.Api.Authorization.Owner;

public sealed class OwnerOnlyRequirement : IAuthorizationRequirement
{
    public static OwnerOnlyRequirement Instance { get; } = new();
}
