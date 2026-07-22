using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Security.Authorization;

public static class PermissionAuthorizationRegistration
{
    public static IReadOnlyList<string> PermissionPolicyNames { get; } = PermissionCatalogue.Codes;

    public static void AddPermissionPolicies(AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(AuthorizationPolicyNames.SystemOwner, policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new SystemOwnerRequirement()));

        foreach (var code in PermissionCatalogue.Codes)
        {
            options.AddPolicy(code, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(code)));
        }
    }

    public static IServiceCollection AddPermissionAuthorizationHandlers(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, SystemOwnerAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
