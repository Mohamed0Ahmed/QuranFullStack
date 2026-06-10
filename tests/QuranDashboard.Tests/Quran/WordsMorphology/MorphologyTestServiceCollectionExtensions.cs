using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;
using QuranDashboard.Infrastructure.Files.Quran.Morphology;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Morphology;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

internal static class MorphologyTestServiceCollectionExtensions
{
    public static IServiceCollection AddMorphologyImportServices(this IServiceCollection services)
    {
        services.AddSingleton<MorphologyManifestReader>();
        services.AddSingleton<JsonAlignedCorpusReader>();
        services.AddSingleton<JsonQulRootReader>();
        services.AddSingleton<JsonQulLemmaReader>();
        services.AddSingleton<JsonQulStemReader>();
        services.AddSingleton<MorphologyAssembler>();
        services.AddScoped<IMorphologyImportSource, MorphologyImportSource>();
        services.AddScoped<IMorphologyImportWriter, EfBulkMorphologyWriter>();

        return services;
    }
}
