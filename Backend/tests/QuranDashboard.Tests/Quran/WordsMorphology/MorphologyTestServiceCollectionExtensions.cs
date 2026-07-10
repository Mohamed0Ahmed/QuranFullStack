using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;
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
        services.AddSingleton<IWordLemmaNormalizationReader, NoOpWordLemmaNormalizationReader>();
        services.AddSingleton<MorphologyAssembler>();
        services.AddScoped<MorphologyImportSource>();
        services.AddScoped<IMorphologyImportSource>(sp => sp.GetRequiredService<MorphologyImportSource>());
        services.AddScoped<IMorphologyImportWriter, EfBulkMorphologyWriter>();
        services.AddSingleton<IMorphologyReportWriter, MarkdownJsonMorphologyReportWriter>();

        return services;
    }

    private sealed class NoOpWordLemmaNormalizationReader : IWordLemmaNormalizationReader
    {
        public WordLemmaNormalizationLoaded Load() => new(
            new WordLemmaNormalizationArtifact
            {
                SchemaVersion = WordLemmaNormalizationArtifact.SupportedSchemaVersion,
                ArtifactId = "test-no-op-normalization",
                Entries = []
            },
            [],
            "0".PadRight(64, '0'),
            new WordLemmaNormalizationCounts(0, 0, 0, 0, 0, 0, 0, 0, 0));

        public WordLemmaNormalizationResult Apply(
            IReadOnlyDictionary<string, string> rawLemmas,
            WordLemmaNormalizationLoaded loaded,
            IReadOnlySet<string>? readableWordLocations = null,
            string? rawLemmasSha256 = null)
        {
            var corrected = new Dictionary<string, string>(rawLemmas, StringComparer.Ordinal);
            var summary = new WordLemmaCorrectionSummary(
                ArtifactSha256: loaded.ArtifactSha256,
                RawLemmasSha256: rawLemmasSha256,
                TotalEntries: 0,
                AppliedAdd: 0,
                AppliedRemove: 0,
                AppliedReplace: 0,
                ReviewedKeep: 0,
                ReviewedException: 0,
                FailedOrSkipped: 0,
                SpotChecks: []);

            return new WordLemmaNormalizationResult(corrected, summary);
        }
    }
}
