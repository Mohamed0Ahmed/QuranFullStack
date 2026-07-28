using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AbwabDependencyInjection
{
    public static IServiceCollection AddAbwab(this IServiceCollection services)
    {
        services.AddScoped<IAbwabSectionsWriter, EfAbwabSectionsWriter>();
        services.AddScoped<IAbwabDoorsWriter, EfAbwabDoorsWriter>();
        services.AddScoped<IAbwabTreeReader, EfAbwabTreeReader>();

        return services;
    }
}
