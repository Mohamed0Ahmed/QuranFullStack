using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AbwabDependencyInjection
{
    public static IServiceCollection AddAbwab(this IServiceCollection services)
    {
        services.AddScoped<IAbwabSectionsWriter, EfAbwabSectionsWriter>();

        return services;
    }
}
