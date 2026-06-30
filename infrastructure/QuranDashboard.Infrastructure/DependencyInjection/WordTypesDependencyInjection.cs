using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class WordTypesDependencyInjection
{
    public static IServiceCollection AddWordTypes(this IServiceCollection services)
    {
        services.AddScoped<EfWordTypesReader>();
        services.AddScoped<IWordTypesReader>(sp => new CachedWordTypesReader(
            sp.GetRequiredService<EfWordTypesReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
