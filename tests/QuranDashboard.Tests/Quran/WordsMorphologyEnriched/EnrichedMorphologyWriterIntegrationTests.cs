using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;
using QuranDashboard.Infrastructure.ServiceRegistration;
using QuranDashboard.Tests.Quran.WordsMorphology;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class EnrichedMorphologyWriterIntegrationTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Enriched_source_imports_through_existing_writer_even_when_legacy_correction_readers_throw()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await WriteEnrichedSourceFolderAsync();
        var reportOutDir = Path.Combine(Path.GetTempPath(), "enriched-morph-writer-report-" + Guid.NewGuid().ToString("N"));

        await using var scope = fixture.CreateServiceProvider(services =>
        {
            services.AddSingleton<IWordLemmaNormalizationReader, ThrowingWordLemmaNormalizationReader>();
            services.AddSingleton<ISegmentStemCorrectionReader, ThrowingSegmentStemCorrectionReader>();
        }).CreateAsyncScope();
        var importSource = scope.ServiceProvider.GetRequiredKeyedService<IMorphologyImportSource>(
            MorphologyImportSourceKeys.Enriched);
        var importWriter = scope.ServiceProvider.GetRequiredService<IMorphologyImportWriter>();
        var reportWriter = scope.ServiceProvider.GetRequiredService<IMorphologyReportWriter>();
        var handler = new ImportMorphologyHandler(importSource, importWriter, reportWriter);

        var result = await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, Force: false, ExpectedReadableWords: 5, ReportOutDir: reportOutDir),
            CancellationToken.None);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);
        result.Totals!.SegmentRows.Should().Be(5);
        result.ReportOutDir.Should().Be(reportOutDir);
    }

    private static async Task<string> WriteEnrichedSourceFolderAsync()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "enriched-morph-writer-source-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var artifactPath = Path.Combine(tempDir, "corpus-based-enriched-morphology.dashboard-ready.json");
        await File.WriteAllTextAsync(artifactPath, """
            [
              {
                "location": "1:1:1", "quranWordId": 1, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "synA", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة أ", "featuresRaw": "STEM|POS:N|NOM", "rootBuckwalter": "rootA", "rootArabic": "جذر تجريبي أ", "lemmaBuckwalter": "lemmaA", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة أ", "stemBuckwalter": "synA" }
                ]
              },
              {
                "location": "1:1:2", "quranWordId": 2, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "synB", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة ب", "featuresRaw": "STEM|POS:N|GEN", "rootBuckwalter": "rootB", "rootArabic": "جذر تجريبي ب", "lemmaBuckwalter": "lemmaB", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة ب", "stemBuckwalter": "synB" }
                ]
              },
              {
                "location": "1:1:3", "quranWordId": 3, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "PN", "formBuckwalter": "synC", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة ج", "featuresRaw": "STEM|POS:PN|GEN", "rootBuckwalter": "rootC", "rootArabic": "جذر تجريبي ج", "lemmaBuckwalter": "lemmaC", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة ج", "stemBuckwalter": "synC" }
                ]
              },
              {
                "location": "1:2:1", "quranWordId": 4, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "V", "formBuckwalter": "synD", "formArabic": "فِعْلٌ تَجْرِيبِيّ", "featuresRaw": "STEM|POS:V|PERF|ACT", "rootBuckwalter": "rootD", "rootArabic": "جذر تجريبي د", "lemmaBuckwalter": "lemmaD", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة د", "stemBuckwalter": "synD" }
                ]
              },
              {
                "location": "1:2:2", "quranWordId": 5, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "synE", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة هـ", "featuresRaw": "STEM|POS:N|ACC", "rootBuckwalter": "rootE", "rootArabic": "جذر تجريبي هـ", "lemmaBuckwalter": "lemmaE", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة هـ", "stemBuckwalter": "synE" }
                ]
              }
            ]
            """);

        var bytes = await File.ReadAllBytesAsync(artifactPath);
        var sha = Convert.ToHexString(SHA256.HashData(bytes));
        var manifestJson = $$"""
            {
              "schema": "quran-enriched-morphology-v1",
              "provenance": "corpus-bridge-enriched",
              "quranWordIdVerifiedAgainstDashboard": true,
              "files": [
                {
                  "role": "enriched-morphology",
                  "relativePath": "corpus-based-enriched-morphology.dashboard-ready.json",
                  "sha256": "{{sha}}",
                  "sizeBytes": {{bytes.Length}},
                  "recordCount": 5,
                  "segmentCount": 5
                }
              ]
            }
            """;
        await File.WriteAllTextAsync(Path.Combine(tempDir, "manifest.json"), manifestJson);

        return tempDir;
    }

    private sealed class ThrowingWordLemmaNormalizationReader : IWordLemmaNormalizationReader
    {
        public WordLemmaNormalizationLoaded Load() =>
            throw new InvalidOperationException("Legacy word-lemma normalization must not load for enriched imports.");

        public WordLemmaNormalizationResult Apply(
            IReadOnlyDictionary<string, string> rawLemmas,
            WordLemmaNormalizationLoaded loaded,
            IReadOnlySet<string>? readableWordLocations = null,
            string? rawLemmasSha256 = null) =>
            throw new InvalidOperationException("Legacy word-lemma normalization must not apply for enriched imports.");
    }

    private sealed class ThrowingSegmentStemCorrectionReader : ISegmentStemCorrectionReader
    {
        public SegmentStemCorrectionLoaded Load() =>
            throw new InvalidOperationException("Legacy segment-stem correction must not load for enriched imports.");
    }
}
