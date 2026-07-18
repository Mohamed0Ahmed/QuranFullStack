using Microsoft.Extensions.Configuration;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AccessDependencyInjection
{
    public static IServiceCollection AddAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LogtoManagementApiOptions>(
            configuration.GetSection(LogtoManagementApiOptions.SectionName));
        services.AddMemoryCache();

        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddHttpClient<IExternalUserProfileSource, LogtoManagementApiUserProfileSource>();

        return services;
    }
}
