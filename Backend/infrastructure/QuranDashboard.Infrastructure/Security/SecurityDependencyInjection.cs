using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Infrastructure.Security.Caching;
using QuranDashboard.Infrastructure.Security.Persistence;

namespace QuranDashboard.Infrastructure.Security;

// Infrastructure composition of the US5 security slice: the separate security-audit commit executor, the
// tracked stores it shares a request scope with, and the effective-permission cache. The application
// handlers/resolver are registered in AddApplication (Infrastructure does not reference the Application
// project). Kept out of the Abwab kernel DI so the security surface stays a distinct, auditable unit.
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
