using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class MorphologyImportDependencyInjection
{
    public static IServiceCollection AddMorphologyImport(this IServiceCollection services)
    {
        services.AddSingleton<BuckwalterArabicMap>();
        services.AddSingleton<SegmentArabicRenderer>();
        services.AddSingleton<MorphologyManifestReader>();
        services.AddSingleton<JsonAlignedCorpusReader>();
        services.AddSingleton<JsonQulRootReader>();
        services.AddSingleton<JsonQulLemmaReader>();
        services.AddSingleton<JsonQulStemReader>();
        services.AddSingleton<MorphologyAssembler>();
        services.AddScoped<IMorphologyImportSource, MorphologyImportSource>();
        services.AddScoped<IMorphologyImportWriter, EfBulkMorphologyWriter>();
        services.AddSingleton<IMorphologyReportWriter, MarkdownJsonMorphologyReportWriter>();

        return services;
    }
}
