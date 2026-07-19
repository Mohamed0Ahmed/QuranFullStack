using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class StemsDependencyInjection
{
    public static IServiceCollection AddStems(this IServiceCollection services)
    {
        services.AddScoped<EfStemsReader>();
        services.AddScoped<IStemsReader>(sp => new CachedStemsReader(
            sp.GetRequiredService<EfStemsReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
