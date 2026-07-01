using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class LemmasDependencyInjection
{
    /// <summary>
    /// Registers the read-only Lemmas Explorer (Feature 016): the EF reader as
    /// scoped, and <see cref="ILemmasReader"/> as a factory wrapping it in the
    /// bounded <see cref="CachedLemmasReader"/> using the existing shared
    /// <see cref="IMemoryCache"/>. No global cache configuration is applied.
    /// </summary>
    public static IServiceCollection AddLemmas(this IServiceCollection services)
    {
        services.AddScoped<EfLemmasReader>();
        services.AddScoped<ILemmasReader>(sp => new CachedLemmasReader(
            sp.GetRequiredService<EfLemmasReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
