using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AccessDependencyInjection
{
    public static IServiceCollection AddAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEmailIdentityNormalizer, EmailIdentityNormalizer>();
        services.AddScoped<IEmailIdentityPreflight, EmailIdentityPreflight>();
        services.Configure<LogtoManagementApiOptions>(
            configuration.GetSection(LogtoManagementApiOptions.SectionName));

        services.AddOptions<OwnerBootstrapOptions>()
            .Bind(configuration.GetSection(OwnerBootstrapOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OwnerBootstrapOptions>, OwnerBootstrapOptionsValidator>();

        services.AddMemoryCache();

        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IUserRoleResolver, CachedUserRoleResolver>();
        services.AddScoped<IPermissionCatalogueSynchronizer, PermissionCatalogueSynchronizer>();
        services.AddHttpClient<IExternalUserProfileSource, LogtoManagementApiUserProfileSource>();

        return services;
    }
}
