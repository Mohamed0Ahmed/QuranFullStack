using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.MorphologyImporting;

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
        services.AddScoped<MorphologyImportSource>();
        services.AddScoped<IMorphologyImportSource>(sp => sp.GetRequiredService<MorphologyImportSource>());
        services.AddScoped<IMorphologyImportWriter, EfBulkMorphologyWriter>();
        services.AddSingleton<IMorphologyReportWriter, MarkdownJsonMorphologyReportWriter>();

        return services;
    }
}
