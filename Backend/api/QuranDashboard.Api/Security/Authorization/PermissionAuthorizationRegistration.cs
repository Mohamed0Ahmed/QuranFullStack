using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Security.Authorization;

// Registers the authorization "policy" layer of the 5-catalogue parity check: exactly one policy per
// permission code (derived from the single canonical PermissionCatalogue) plus the SystemOwner policy. The
// per-code policy names ARE the catalogue codes, so the parity test can read them back and prove zero drift.
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
