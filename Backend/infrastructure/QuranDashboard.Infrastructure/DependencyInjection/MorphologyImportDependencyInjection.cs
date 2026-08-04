using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

public static class MorphologyImportSourceKeys
{
    public const string Legacy = "morphology-source:legacy";
    public const string Enriched = "morphology-source:enriched";
}

internal static class MorphologyImportDependencyInjection
{
    public const string LegacySourceKey = MorphologyImportSourceKeys.Legacy;
    public const string EnrichedSourceKey = MorphologyImportSourceKeys.Enriched;

    public static IServiceCollection AddMorphologyImport(this IServiceCollection services)
    {
        services.AddSingleton<BuckwalterArabicMap>();
        services.AddSingleton<SegmentArabicRenderer>();

        services.AddSingleton<MorphologyManifestReader>();
        services.AddSingleton<JsonAlignedCorpusReader>();
        services.AddSingleton<JsonQulRootReader>();
        services.AddSingleton<JsonQulLemmaReader>();
        services.AddSingleton<JsonQulStemReader>();
        services.AddSingleton<IWordLemmaNormalizationReader, WordLemmaNormalizationReader>();
        services.AddSingleton<ISegmentStemCorrectionReader, SegmentStemCorrectionReader>();
        services.AddSingleton<MorphologyAssembler>();

        services.AddSingleton<EnrichedMorphologyManifestReader>();
        services.AddSingleton<EnrichedMorphologyReader>();
        services.AddSingleton<EnrichedDimensionBuilder>();

        services.AddScoped<MorphologyImportSource>();
        services.AddScoped<EnrichedMorphologyImportSource>();
        services.AddKeyedScoped<IMorphologyImportSource, MorphologyImportSource>(LegacySourceKey);
        services.AddKeyedScoped<IMorphologyImportSource, EnrichedMorphologyImportSource>(EnrichedSourceKey);
        services.AddScoped<IMorphologyImportSource>(sp =>
            sp.GetRequiredKeyedService<IMorphologyImportSource>(LegacySourceKey));

        services.AddScoped<IMorphologyImportWriter, EfBulkMorphologyWriter>();
        services.AddSingleton<IMorphologyReportWriter, MarkdownJsonMorphologyReportWriter>();

        return services;
    }
}
