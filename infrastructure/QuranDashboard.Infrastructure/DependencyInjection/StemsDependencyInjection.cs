using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class StemsDependencyInjection
{
    /// <summary>
    /// Registers the read-only Stems Explorer (Feature 016): the EF reader as
    /// scoped, and <see cref="IStemsReader"/> as a factory wrapping it in the
    /// bounded <see cref="CachedStemsReader"/> using the existing shared
    /// <see cref="IMemoryCache"/>. No global cache configuration is applied.
    /// </summary>
    public static IServiceCollection AddStems(this IServiceCollection services)
    {
        services.AddScoped<EfStemsReader>();
        services.AddScoped<IStemsReader>(sp => new CachedStemsReader(
            sp.GetRequiredService<EfStemsReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
