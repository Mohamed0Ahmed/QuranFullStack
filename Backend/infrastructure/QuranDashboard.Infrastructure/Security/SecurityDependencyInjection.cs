using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Infrastructure.Security.Caching;
using QuranDashboard.Infrastructure.Security.Persistence;

namespace QuranDashboard.Infrastructure.Security;

public static class SecurityDependencyInjection
{
    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddScoped<ISecurityAuditWriteExecutor, SecurityAuditedCommitExecutor>();
        services.AddScoped<ISystemOwnerStore, SystemOwnerStore>();
        services.AddScoped<IPermissionAssignmentStore, PermissionAssignmentStore>();
        services.AddSingleton<IEffectivePermissionCache, EffectivePermissionCache>();

        return services;
    }
}
