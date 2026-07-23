using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AbwabCoreDependencyInjection
{
    public static IServiceCollection AddAbwabCoreReads(this IServiceCollection services)
    {
        services.AddScoped<IAbwabCoreReadPort, EfAbwabCoreReadPort>();
        services.AddScoped<IManualProtectionReadPort, EfManualProtectionReadPort>();

        return services;
    }
}
