using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AccessDependencyInjection
{
    public static IServiceCollection AddAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LogtoManagementApiOptions>(
            configuration.GetSection(LogtoManagementApiOptions.SectionName));

        services.AddOptions<OwnerBootstrapOptions>()
            .Bind(configuration.GetSection(OwnerBootstrapOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OwnerBootstrapOptions>, OwnerBootstrapOptionsValidator>();

        services.AddMemoryCache();

        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IUserRoleResolver, CachedUserRoleResolver>();
        services.AddHttpClient<IExternalUserProfileSource, LogtoManagementApiUserProfileSource>();

        return services;
    }
}
