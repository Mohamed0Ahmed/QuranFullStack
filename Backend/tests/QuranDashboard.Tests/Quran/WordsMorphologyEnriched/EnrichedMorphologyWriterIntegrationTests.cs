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

    [Fact]
    public async Task Multi_stem_word_with_distinct_lemmas_imports_without_violating_the_first_word_order_unique_index()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await WriteMultiStemEnrichedSourceFolderAsync();
        var reportOutDir = Path.Combine(Path.GetTempPath(), "enriched-morph-multistem-report-" + Guid.NewGuid().ToString("N"));

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var importSource = scope.ServiceProvider.GetRequiredKeyedService<IMorphologyImportSource>(
            MorphologyImportSourceKeys.Enriched);
        var importWriter = scope.ServiceProvider.GetRequiredService<IMorphologyImportWriter>();
        var reportWriter = scope.ServiceProvider.GetRequiredService<IMorphologyReportWriter>();
        var handler = new ImportMorphologyHandler(importSource, importWriter, reportWriter);

        var result = await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, Force: false, ExpectedReadableWords: 5, ReportOutDir: reportOutDir),
            CancellationToken.None);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode,
            "the multi-STEM word must import without hitting the FirstWordOrder unique-index violation");
        result.Totals!.SegmentRows.Should().Be(6, "one two-STEM word plus four single-STEM words");

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(5);
        snapshot.SegmentRows.Should().Be(6);
        snapshot.LemmaRows.Should().Be(5,
            "head-only minting yields 5 distinct lemmas (min, maA and the three other heads); the secondary "
            + "'maA' reuses the existing lemma instead of minting a 6th row that collides on FirstWordOrder");
    }

    [Fact]
    public async Task Colliding_lemma_text_variants_collapse_to_one_lemma_and_preserve_distinct_analyses()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await WriteCollidingLemmaEnrichedSourceFolderAsync();
        var reportOutDir = Path.Combine(Path.GetTempPath(), "enriched-morph-collision-report-" + Guid.NewGuid().ToString("N"));

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var importSource = scope.ServiceProvider.GetRequiredKeyedService<IMorphologyImportSource>(
            MorphologyImportSourceKeys.Enriched);
        var importWriter = scope.ServiceProvider.GetRequiredService<IMorphologyImportWriter>();
        var reportWriter = scope.ServiceProvider.GetRequiredService<IMorphologyReportWriter>();
        var handler = new ImportMorphologyHandler(importSource, importWriter, reportWriter);

        var result = await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, Force: false, ExpectedReadableWords: 5, ReportOutDir: reportOutDir),
            CancellationToken.None);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode,
            "two head buckwalters sharing lemma_text عَصَا must import without violating UNIQUE(lemma_text)");

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.LemmaRows.Should().Be(4, "عَصَا collapses to one display lemma; the other three heads are distinct");
        snapshot.LemmaAnalysisRows.Should().Be(5, "each of the five distinct buckwalters keeps its own analysis");

        var asaAnalyses = await fixture.QueryLemmaAnalysesByTextAsync("عَصَا");
        asaAnalyses.Should().HaveCount(2, "both عَصَا buckwalter variants persist under the one display lemma");
        asaAnalyses.Select(analysis => analysis.Buckwalter).Should().BeEquivalentTo(["EaSaA2", "EaSaA"]);
        asaAnalyses.Should().OnlyContain(analysis => analysis.RootId != null);
        asaAnalyses.Select(analysis => analysis.RootId).Should().OnlyHaveUniqueItems(
            "different-root variants (ع ص و vs ع ص ي) are not analytically merged");
        asaAnalyses.Select(analysis => analysis.HeadPos).Should().BeEquivalentTo(["N", "V"],
            "different-POS variants keep their head POS");
    }

    [Fact]
    public async Task Enriched_import_normalizes_small_yeh_for_stem_identity_only()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await WriteSmallYehStemIdentitySourceFolderAsync();
        var reportOutDir = Path.Combine(Path.GetTempPath(), "enriched-morph-small-yeh-report-" + Guid.NewGuid().ToString("N"));

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var importSource = scope.ServiceProvider.GetRequiredKeyedService<IMorphologyImportSource>(
            MorphologyImportSourceKeys.Enriched);
        var importWriter = scope.ServiceProvider.GetRequiredService<IMorphologyImportWriter>();
        var reportWriter = scope.ServiceProvider.GetRequiredService<IMorphologyReportWriter>();
        var handler = new ImportMorphologyHandler(importSource, importWriter, reportWriter);

        var result = await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, Force: false, ExpectedReadableWords: 5, ReportOutDir: reportOutDir),
            CancellationToken.None);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);
        result.Totals!.StemRows.Should().Be(3,
            "هِۦ collapses into هِ, the suffix ۦ does not mint a stem, and two filler stems remain distinct");
        result.Totals.LemmaRows.Should().Be(2, "PRON head stems carry no lemma, while waliY~ and katab persist");
        result.Totals.LemmaAnalysisRows.Should().Be(2);
        result.Totals.RootRows.Should().Be(2);
        result.Totals.PosTagRows.Should().Be(49);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(5);
        snapshot.SegmentRows.Should().Be(6);
        snapshot.StemRows.Should().Be(3);

        var persisted = await fixture.QuerySmallYehStemIdentityRowsAsync();
        persisted.Stems.Should().Contain("هِ");
        persisted.Stems.Should().NotContain(stem => stem.Contains('ۦ'));
        persisted.SuffixRender.Should().Be((".", "ۦ", "PRON", "SUFFIX", null));
        persisted.HeadStemRenderAndIdentity.Should().Contain(row =>
            row.FormArabicNormalized == "هِۦ" && row.StemText == "هِ");
    }

    private static async Task<string> WriteSmallYehStemIdentitySourceFolderAsync()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "enriched-morph-small-yeh-source-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var artifactPath = Path.Combine(tempDir, "corpus-based-enriched-morphology.dashboard-ready.json");
        await File.WriteAllTextAsync(artifactPath, """
            [
              {
                "location": "1:1:1", "quranWordId": 1, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "PRON", "formBuckwalter": "hi", "formArabic": "هِ", "featuresRaw": "STEM|POS:PRON|3MS" }
                ]
              },
              {
                "location": "1:1:2", "quranWordId": 2, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "PRON", "formBuckwalter": "hi.", "formArabic": "هِۦ", "featuresRaw": "STEM|POS:PRON|3MS" }
                ]
              },
              {
                "location": "1:1:3", "quranWordId": 3, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "waliY~i", "formArabic": "وَلِىِّ", "featuresRaw": "STEM|POS:N|LEM:waliY~|ROOT:wly|M|NOM", "rootBuckwalter": "wly", "rootArabic": "و ل ي", "lemmaBuckwalter": "waliY~", "lemmaArabic": "وَلِىّ" },
                  { "segmentNumber": 2, "kind": "SUFFIX", "pos": "PRON", "formBuckwalter": ".", "formArabic": "ۦ", "featuresRaw": "SUFFIX|PRON:1S" }
                ]
              },
              {
                "location": "1:2:1", "quranWordId": 4, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "V", "formBuckwalter": "kataba", "formArabic": "كَتَبَ", "featuresRaw": "STEM|POS:V|PERF|ACT", "rootBuckwalter": "ktb", "rootArabic": "ك ت ب", "lemmaBuckwalter": "katab", "lemmaArabic": "كَتَبَ" }
                ]
              },
              {
                "location": "1:2:2", "quranWordId": 5, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "V", "formBuckwalter": "kataba2", "formArabic": "كَتَبَ", "featuresRaw": "STEM|POS:V|PERF|ACT", "rootBuckwalter": "ktb", "rootArabic": "ك ت ب", "lemmaBuckwalter": "katab", "lemmaArabic": "كَتَبَ" }
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
                  "segmentCount": 6
                }
              ]
            }
            """;
        await File.WriteAllTextAsync(Path.Combine(tempDir, "manifest.json"), manifestJson);

        return tempDir;
    }

    private static async Task<string> WriteCollidingLemmaEnrichedSourceFolderAsync()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "enriched-morph-collision-source-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var artifactPath = Path.Combine(tempDir, "corpus-based-enriched-morphology.dashboard-ready.json");
        await File.WriteAllTextAsync(artifactPath, """
            [
              {
                "location": "1:1:1", "quranWordId": 1, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "EaSaAsyn", "formArabic": "عَصَا", "featuresRaw": "STEM|POS:N|NOM", "rootBuckwalter": "ESw", "rootArabic": "ع ص و", "lemmaBuckwalter": "EaSaA2", "lemmaArabic": "عَصَا" }
                ]
              },
              {
                "location": "1:1:2", "quranWordId": 2, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "synB", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة ب", "featuresRaw": "STEM|POS:N|GEN", "rootBuckwalter": "rootB", "rootArabic": "جذر تجريبي ب", "lemmaBuckwalter": "lemmaB", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة ب" }
                ]
              },
              {
                "location": "1:1:3", "quranWordId": 3, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "PN", "formBuckwalter": "synC", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة ج", "featuresRaw": "STEM|POS:PN|GEN", "rootBuckwalter": "rootC", "rootArabic": "جذر تجريبي ج", "lemmaBuckwalter": "lemmaC", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة ج" }
                ]
              },
              {
                "location": "1:2:1", "quranWordId": 4, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "synD", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة د", "featuresRaw": "STEM|POS:N|ACC", "rootBuckwalter": "rootD", "rootArabic": "جذر تجريبي د", "lemmaBuckwalter": "lemmaD", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة د" }
                ]
              },
              {
                "location": "1:2:2", "quranWordId": 5, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "V", "formBuckwalter": "EaSaAsyn2", "formArabic": "عَصَاۦ", "featuresRaw": "STEM|POS:V|PERF|ACT", "rootBuckwalter": "ESy", "rootArabic": "ع ص ي", "lemmaBuckwalter": "EaSaA", "lemmaArabic": "عَصَا" }
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

    private static async Task<string> WriteMultiStemEnrichedSourceFolderAsync()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "enriched-morph-multistem-source-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var artifactPath = Path.Combine(tempDir, "corpus-based-enriched-morphology.dashboard-ready.json");
        await File.WriteAllTextAsync(artifactPath, """
            [
              {
                "location": "1:1:1", "quranWordId": 1, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "minSyn", "formArabic": "مِنْ", "featuresRaw": "STEM|POS:N|GEN", "lemmaBuckwalter": "min", "lemmaArabic": "مِنْ" },
                  { "segmentNumber": 2, "kind": "STEM", "pos": "N", "formBuckwalter": "maASyn", "formArabic": "مَا", "featuresRaw": "STEM|POS:N|GEN", "lemmaBuckwalter": "maA", "lemmaArabic": "مَا" }
                ]
              },
              {
                "location": "1:1:2", "quranWordId": 2, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "synB", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة ب", "featuresRaw": "STEM|POS:N|GEN", "rootBuckwalter": "rootB", "rootArabic": "جذر تجريبي ب", "lemmaBuckwalter": "lemmaB", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة ب" }
                ]
              },
              {
                "location": "1:1:3", "quranWordId": 3, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "PN", "formBuckwalter": "synC", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة ج", "featuresRaw": "STEM|POS:PN|GEN", "rootBuckwalter": "rootC", "rootArabic": "جذر تجريبي ج", "lemmaBuckwalter": "lemmaC", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة ج" }
                ]
              },
              {
                "location": "1:2:1", "quranWordId": 4, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "V", "formBuckwalter": "synD", "formArabic": "فِعْلٌ تَجْرِيبِيّ", "featuresRaw": "STEM|POS:V|PERF|ACT", "rootBuckwalter": "rootD", "rootArabic": "جذر تجريبي د", "lemmaBuckwalter": "lemmaD", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة د" }
                ]
              },
              {
                "location": "1:2:2", "quranWordId": 5, "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  { "segmentNumber": 1, "kind": "STEM", "pos": "N", "formBuckwalter": "maASyn2", "formArabic": "مَا", "featuresRaw": "STEM|POS:N|ACC", "lemmaBuckwalter": "maA", "lemmaArabic": "مَا" }
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
                  "segmentCount": 6
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
