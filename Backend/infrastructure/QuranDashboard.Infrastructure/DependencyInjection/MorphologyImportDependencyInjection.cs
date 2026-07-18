using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

// Public keyed-service keys so the CLI host (a separate assembly) can resolve either import source
// without depending on the internal DI registration class.
public static class MorphologyImportSourceKeys
{
    public const string Legacy = "morphology-source:legacy";
    public const string Enriched = "morphology-source:enriched";
}

internal static class MorphologyImportDependencyInjection
{
    // Keyed-service keys for the two import sources. The CLI runner resolves one of these based on the
    // --enriched flag (or the equivalent default) and constructs ImportMorphologyHandler against it.
    public const string LegacySourceKey = MorphologyImportSourceKeys.Legacy;
    public const string EnrichedSourceKey = MorphologyImportSourceKeys.Enriched;

    public static IServiceCollection AddMorphologyImport(this IServiceCollection services)
    {
        services.AddSingleton<BuckwalterArabicMap>();
        services.AddSingleton<SegmentArabicRenderer>();

        // Legacy pathway (kept runnable for parity/diff until Phase 2 cleanup).
        services.AddSingleton<MorphologyManifestReader>();
        services.AddSingleton<JsonAlignedCorpusReader>();
        services.AddSingleton<JsonQulRootReader>();
        services.AddSingleton<JsonQulLemmaReader>();
        services.AddSingleton<JsonQulStemReader>();
        services.AddSingleton<IWordLemmaNormalizationReader, WordLemmaNormalizationReader>();
        services.AddSingleton<ISegmentStemCorrectionReader, SegmentStemCorrectionReader>();
        services.AddSingleton<MorphologyAssembler>();

        // Enriched pathway (Feature 020). Streaming reader + manifest reader + value-based dimension
        // builder; reuses the unchanged writer/handler/report via the shared MorphologySourceData DTO.
        services.AddSingleton<EnrichedMorphologyManifestReader>();
        services.AddSingleton<EnrichedMorphologyReader>();
        services.AddSingleton<EnrichedDimensionBuilder>();

        // Both sources are registered as keyed services so the handler can be built against either one
        // at run time. The legacy source remains the DEFAULT unkeyed binding to preserve current behavior
        // (the selector is only flipped when --enriched is passed). This keeps old callers/paths working
        // unchanged until the cut-over.
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
