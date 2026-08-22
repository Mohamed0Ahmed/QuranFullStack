using Microsoft.Extensions.Configuration;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Infrastructure.Caching.Quran.MushafReader;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class MushafReaderDependencyInjection
{
    public static IServiceCollection AddMushafReader(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MushafReaderOptions>(configuration.GetSection("MushafReader"));
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MushafReaderOptions>>().Value);
        services.AddMemoryCache();

        services.AddScoped<EfMushafPageReader>();
        services.AddScoped<IMushafPageReader>(sp => new CachedMushafPageReader(
            sp.GetRequiredService<EfMushafPageReader>(),
            sp.GetRequiredService<IMemoryCache>()));
        services.AddScoped<IMushafDoorHighlightsReader, EfMushafDoorHighlightsReader>();
        services.AddScoped<IMushafAyahDoorsReader, EfMushafAyahDoorsReader>();

        services.AddScoped<EfAyahStudyReader>();
        services.AddScoped<IAyahStudyReader>(sp => new CachedAyahStudyReader(
            sp.GetRequiredService<EfAyahStudyReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<EfMushafSurahCatalogReader>();
        services.AddScoped<IMushafSurahCatalogReader>(sp => new CachedMushafSurahCatalogReader(
            sp.GetRequiredService<EfMushafSurahCatalogReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<EfMushafStudySourceCatalogReader>();
        services.AddScoped<IMushafStudySourceCatalogReader>(sp => new CachedMushafStudySourceCatalogReader(
            sp.GetRequiredService<EfMushafStudySourceCatalogReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<EfWordAnalysisReader>();
        services.AddScoped<IWordAnalysisReader>(sp => new CachedWordAnalysisReader(
            sp.GetRequiredService<EfWordAnalysisReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<EfAyahSimilaritiesReader>();
        services.AddScoped<IAyahSimilaritiesReader>(sp => new CachedAyahSimilaritiesReader(
            sp.GetRequiredService<EfAyahSimilaritiesReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<EfAyahMutashabihatReader>();
        services.AddScoped<IAyahMutashabihatReader>(sp => new CachedAyahMutashabihatReader(
            sp.GetRequiredService<EfAyahMutashabihatReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
