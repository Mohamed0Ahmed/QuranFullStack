using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirImportTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Import_persists_sources_text_blocks_and_ayah_links_with_exact_text()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(sources: TafsirSyntheticSeed.IntegrationSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);
        result.Totals.Should().NotBeNull();
        result.Totals!.SourceRows.Should().Be(1);
        result.Totals.TafsirTextBlockRows.Should().Be(2);
        result.Totals.AyahMappingRows.Should().Be(3);
        result.Totals.DistinctAyahs.Should().Be(3);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var sources = await dbContext.TafsirSources.AsNoTracking().ToListAsync();
        sources.Should().ContainSingle();
        sources[0].SourceKey.Should().Be("ar-test-tafsir");
        sources[0].ContentCoverageCount.Should().Be(TafsirInvariants.ExpectedAyahsPerSource);

        var entries = await dbContext.TafsirEntries.AsNoTracking().ToListAsync();
        entries.Should().HaveCount(2);

        var leaderEntry = entries.Single(entry => entry.SourceEntryKey == "900:1");
        leaderEntry.TafsirText.Should().Be(TafsirSyntheticSeed.SyntheticText("900:1"));
        leaderEntry.TextHash.Should().Be(TafsirAssembler.ComputeTextHash(leaderEntry.TafsirText));

        var ayahLinks = await dbContext.TafsirAyahEntries.AsNoTracking().ToListAsync();
        ayahLinks.Should().HaveCount(3);
        ayahLinks.Select(link => link.AyahId).Distinct().Should().HaveCount(3);

        File.Exists(Path.Combine(reportDir, TafsirImportConstants.JsonReportFileName)).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, TafsirImportConstants.MarkdownReportFileName)).Should().BeTrue();

        var reportJson = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        var reportRoot = reportDocument.RootElement;

        reportRoot.GetProperty("verdict").GetString().Should().Be(TafsirImportConstants.PassVerdict);
        reportRoot.GetProperty("persisted").GetBoolean().Should().BeTrue();

        var checks = reportRoot.GetProperty("checks").EnumerateArray().ToList();
        var reportWrittenCheck = checks.Single(check =>
            check.GetProperty("id").GetString() == TafsirInvariants.CheckReportWritten);
        reportWrittenCheck.GetProperty("passed").GetBoolean().Should().BeTrue();
        reportWrittenCheck.GetProperty("observed").GetString().Should().Be("written");

        var markdown = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.MarkdownReportFileName));
        markdown.Should().Contain("Persisted: true");
        markdown.Should().Contain($"{TafsirInvariants.CheckReportWritten} | required Markdown and JSON reports written | written | yes");
        reportRoot.GetProperty("sourcePath").GetString().Should().Be(Path.GetFullPath(packageDir));
        reportRoot.GetProperty("sourceSummaries").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Import_persists_multi_source_shared_verse_keys_with_TAFSIR_TEXT_UNCHANGED()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.TwoSourceIntegrationSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.TwoSourceTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);
        result.Totals!.SourceRows.Should().Be(2);
        result.Totals.AyahMappingRows.Should().Be(6);
        result.Totals.TafsirTextBlockRows.Should().Be(4);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var entries = await dbContext.TafsirEntries
            .AsNoTracking()
            .Join(
                dbContext.TafsirSources.AsNoTracking(),
                entry => entry.SourceId,
                source => source.Id,
                (entry, source) => new { source.SourceKey, entry.SourceEntryKey, entry.TafsirText })
            .Where(row => row.SourceEntryKey == "900:1")
            .ToListAsync();

        entries.Should().HaveCount(2);
        entries.Select(row => row.TafsirText).Distinct().Should().HaveCount(2);
        entries.Should().Contain(row =>
            row.SourceKey == "ar-test-tafsir-a"
            && row.TafsirText == TafsirSyntheticSeed.SyntheticTextForSource("ar-test-tafsir-a", "900:1"));
        entries.Should().Contain(row =>
            row.SourceKey == "en-test-tafsir-b"
            && row.TafsirText == TafsirSyntheticSeed.SyntheticTextForSource("en-test-tafsir-b", "900:1"));

        var reportJson = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        var textUnchangedCheck = reportDocument.RootElement
            .GetProperty("checks")
            .EnumerateArray()
            .Single(check => check.GetProperty("id").GetString() == TafsirInvariants.CheckTextUnchanged);

        textUnchangedCheck.GetProperty("passed").GetBoolean().Should().BeTrue();
    }
}
