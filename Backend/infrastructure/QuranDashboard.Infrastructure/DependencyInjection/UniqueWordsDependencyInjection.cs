using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Infrastructure.Caching.Quran.Words;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class UniqueWordsDependencyInjection
{
    public static IServiceCollection AddUniqueWords(this IServiceCollection services)
    {
        services.AddScoped<EfUniqueWordsReader>();
        services.AddScoped<IUniqueWordsReader>(sp => new CachedUniqueWordsReader(
            sp.GetRequiredService<EfUniqueWordsReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        // POS catalogue validation for the Unique Words primaryType association filter (Feature 026, US7).
        services.AddScoped<IPosTagCatalogueReader, EfPosTagCatalogueReader>();

        return services;
    }
}
