using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;
using QuranDashboard.Application.Quran.Words.ImportMorphology;
using QuranDashboard.Infrastructure.Files.Quran.Morphology;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologySegmentRenderingTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Segments_receive_correct_render_tiers()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);
        result.Totals!.RenderTierCounts["clean"].Should().Be(7);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var allSegments = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync();

        allSegments.Should().OnlyContain(s => s.FormArabicNormalized != null || s.FormBuckwalter == "");
        allSegments.Should().OnlyContain(s => s.ArabicRenderTier != null || s.FormBuckwalter == "");
        allSegments.Should().OnlyContain(s => s.ArabicRenderSource == MorphologyInvariants.RenderSource);
    }

    [Fact]
    public async Task Raw_form_buckwalter_always_present()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var segments = await dbContext.WordMorphologySegments.AsNoTracking().ToListAsync();
        segments.Should().OnlyContain(s => s.FormBuckwalter != null);
    }

    [Fact]
    public async Task Render_equal_to_uthmani_passes_when_it_is_deterministic_from_buckwalter()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var rendered = new BuckwalterArabicMap().Transliterate("l~Ahi").Arabic;

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var word = await dbContext.QuranWords.SingleAsync(w => w.Location == "1:1:2");
        word.TextUthmani = rendered;
        word.QpcGlyph = rendered;
        await dbContext.SaveChangesAsync();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        var check = await ReadReportCheckAsync(result.ReportOutDir!, "MORPH-SEG-RENDER-PROVENANCE");
        check.Should().NotBeNull();
        check!.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task Copied_or_mismatching_render_fails_render_provenance()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var result = await RunWithMutatedSourceAsync(sourcePath, RenderMutation.WrongArabic);

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);

        var check = await ReadReportCheckAsync(result.ReportOutDir!, "MORPH-SEG-RENDER-PROVENANCE");
        check.Should().NotBeNull();
        check!.Passed.Should().BeFalse();
        check.Observed.Should().Contain("render_mismatches=1");
    }

    [Fact]
    public async Task Missing_buckwalter_form_fails_render_provenance()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var result = await RunWithMutatedSourceAsync(sourcePath, RenderMutation.MissingBuckwalter);

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);

        var check = await ReadReportCheckAsync(result.ReportOutDir!, "MORPH-SEG-RENDER-PROVENANCE");
        check.Should().NotBeNull();
        check!.Passed.Should().BeFalse();
        check.Observed.Should().Contain("missing_buckwalter=1");
    }

    [Fact]
    public async Task Wrong_render_source_fails_render_provenance()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var result = await RunWithMutatedSourceAsync(sourcePath, RenderMutation.WrongSource);

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);

        var check = await ReadReportCheckAsync(result.ReportOutDir!, "MORPH-SEG-RENDER-PROVENANCE");
        check.Should().NotBeNull();
        check!.Passed.Should().BeFalse();
        check.Observed.Should().Contain("source_violations=1");
    }

    [Fact]
    public async Task Multiword_tier_space_does_not_fail_charset()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:3",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "PN",
                    form = "<ilo yaAsiyna",
                    features = "STEM|POS:PN|LEM:<iloyaAs|GEN",
                    root = (string?)null,
                    lemma = "<iloyaAs"
                }
            ]);

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        var charset = await ReadReportCheckAsync(result.ReportOutDir!, "MORPH-SEG-CHARSET");
        charset.Should().NotBeNull();
        charset!.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task Non_space_invalid_character_fails_charset()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:3",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "PN",
                    form = "ba€b",
                    features = "STEM|POS:PN|GEN",
                    root = (string?)null,
                    lemma = (string?)null
                }
            ]);

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);

        var charset = await ReadReportCheckAsync(result.ReportOutDir!, "MORPH-SEG-CHARSET");
        charset.Should().NotBeNull();
        charset!.Passed.Should().BeFalse();
    }

    [Fact]
    public async Task Report_includes_tier_distribution_and_empty_form_count()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"morph-render-report-{Guid.NewGuid():N}");
        var readableCount = fixture.GetReadableWordCount();

        try
        {
            await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
            var result = await handler.HandleAsync(
                new ImportMorphologyCommand(sourcePath, false, readableCount, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Totals!.RenderTierCounts.Should().ContainKey("clean");
            result.Totals.EmptyFormRenders.Should().BeGreaterThanOrEqualTo(0);

            var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
            File.Exists(jsonPath).Should().BeTrue();

            await using var jsonStream = File.OpenRead(jsonPath);
            var report = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Text.Json.JsonElement>(jsonStream);

            report.TryGetProperty("totals", out var totalsEl).Should().BeTrue();
            totalsEl.TryGetProperty("cleanCount", out _).Should().BeTrue();
            totalsEl.TryGetProperty("emptyFormRenders", out _).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    [Fact]
    public async Task Report_warnings_include_word_agreement_and_review_multiword_lists()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"morph-warn-report-{Guid.NewGuid():N}");
        var readableCount = fixture.GetReadableWordCount();

        try
        {
            await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
            var result = await handler.HandleAsync(
                new ImportMorphologyCommand(sourcePath, false, readableCount, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeTrue();

            var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
            await using var jsonStream = File.OpenRead(jsonPath);
            var report = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Text.Json.JsonElement>(jsonStream);

            var warnings = report.GetProperty("warnings").EnumerateArray()
                .Select(warning => warning.GetString())
                .ToList();

            warnings.Should().Contain(w => w!.StartsWith("MORPH-SEG-WORD-AGREEMENT", StringComparison.Ordinal));
            warnings.Should().Contain(w => w!.StartsWith("MORPH-SEG-REVIEW-LIST", StringComparison.Ordinal));
            warnings.Should().Contain(w => w!.StartsWith("MORPH-SEG-MULTIWORD-LIST", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    [Fact]
    public async Task Charset_check_passes_with_valid_buckwalter_forms()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var nonEmptyNullRender = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology_segments
            WHERE form_buckwalter <> '' AND form_arabic_normalized IS NULL
            """).FirstAsync();
        nonEmptyNullRender.Should().Be(0, "all non-empty forms should have a rendered Arabic value");
    }

    private async Task<ImportMorphologyResult> RunWithMutatedSourceAsync(
        string sourcePath,
        RenderMutation mutation)
    {
        await using var scope = fixture.CreateServiceProvider(services =>
        {
            services.AddScoped<IMorphologyImportSource>(sp =>
                new MutatingMorphologyImportSource(
                    sp.GetRequiredService<MorphologyImportSource>(),
                    mutation));
        }).CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
        return await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, false, fixture.GetReadableWordCount()),
            CancellationToken.None);
    }

    private static async Task<ReportCheck?> ReadReportCheckAsync(string reportDir, string checkId)
    {
        var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
        File.Exists(jsonPath).Should().BeTrue();

        await using var stream = File.OpenRead(jsonPath);
        var report = await System.Text.Json.JsonSerializer.DeserializeAsync<ReportDocument>(
            stream,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        return report?.Checks.FirstOrDefault(check => check.Id == checkId);
    }

    private enum RenderMutation
    {
        WrongArabic,
        MissingBuckwalter,
        WrongSource
    }

    private sealed class MutatingMorphologyImportSource(
        MorphologyImportSource inner,
        RenderMutation mutation) : IMorphologyImportSource
    {
        public async Task<MorphologySourceData> LoadAsync(string sourcePath, CancellationToken ct)
        {
            var source = await inner.LoadAsync(sourcePath, ct);
            var firstWord = source.Words[0];
            var firstSegment = firstWord.Segments[0];
            var mutatedSegment = mutation switch
            {
                RenderMutation.WrongArabic => firstSegment with { FormArabicNormalized = "نسخ" },
                RenderMutation.MissingBuckwalter => firstSegment with { FormBuckwalter = "" },
                RenderMutation.WrongSource => firstSegment with { RenderSource = "qpc-glyph" },
                _ => firstSegment
            };

            var segments = firstWord.Segments
                .Select((segment, index) => index == 0 ? mutatedSegment : segment)
                .ToList();
            var words = source.Words
                .Select((word, index) => index == 0 ? word with { Segments = segments } : word)
                .ToList();

            return source with { Words = words };
        }

        public Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct) =>
            inner.SourceUnchangedAsync(sourcePath, ct);
    }

    private sealed record ReportDocument(IReadOnlyList<ReportCheck> Checks);

    private sealed record ReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);
}
