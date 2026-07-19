using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class LemmasDependencyInjection
{
    public static IServiceCollection AddLemmas(this IServiceCollection services)
    {
        services.AddScoped<EfLemmasReader>();
        services.AddScoped<ILemmasReader>(sp => new CachedLemmasReader(
            sp.GetRequiredService<EfLemmasReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
