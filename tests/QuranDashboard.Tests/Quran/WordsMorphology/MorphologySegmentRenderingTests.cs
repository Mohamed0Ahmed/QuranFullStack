using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;
using QuranDashboard.Application.Quran.Words.ImportMorphology;

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
    public async Task Not_uthmani_guard_no_render_equals_uthmani_or_qpc_glyph()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var joins = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology_segments s
            JOIN quran_words w ON w.id = s.quran_word_id
            WHERE s.form_arabic_normalized IS NOT NULL
              AND (s.form_arabic_normalized = w.text_uthmani
                   OR s.form_arabic_normalized = w.qpc_glyph)
            """).FirstAsync();

        joins.Should().Be(0, "no rendered segment should equal text_uthmani or qpc_glyph");
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
}
