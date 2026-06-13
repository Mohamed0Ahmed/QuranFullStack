using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Infrastructure.Files.Quran.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

internal static class MutashabihatTestServiceCollectionExtensions
{
    public static IServiceCollection AddMutashabihatReaderServices(this IServiceCollection services)
    {
        services.AddSingleton<MutashabihatManifestReader>();
        services.AddSingleton<JsonPhrasesReader>();
        services.AddSingleton<JsonSimilarAyahReader>();

        return services;
    }
}
