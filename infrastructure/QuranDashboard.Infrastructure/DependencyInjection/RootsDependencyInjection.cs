using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class RootsDependencyInjection
{
    public static IServiceCollection AddRoots(this IServiceCollection services)
    {
        services.AddScoped<EfRootsReader>();
        services.AddScoped<IRootsReader>(sp => new CachedRootsReader(
            sp.GetRequiredService<EfRootsReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
