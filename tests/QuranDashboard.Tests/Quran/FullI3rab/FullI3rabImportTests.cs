using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

[Collection(nameof(FullI3rabImportTestCollection))]
public sealed class FullI3rabImportTests(FullI3rabImportTestFixture fixture) : IDisposable
{
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task Import_persists_sources_entries_and_ayah_links_with_exact_html()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var packageDir = await packages.WriteAsync();
        var result = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3));

        result.Succeeded.Should().BeTrue(result.Message);
        result.Totals.Should().NotBeNull();
        result.Totals!.SourceRows.Should().Be(1);
        result.Totals.EntryRows.Should().Be(2);
        result.Totals.AyahMappingRows.Should().Be(3);
        result.Totals.DistinctAyahs.Should().Be(3);
        result.WarningCount.Should().Be(0);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var sources = await dbContext.FullI3rabSources.AsNoTracking().ToListAsync();
        sources.Should().ContainSingle();
        sources[0].SourceKey.Should().Be("test-muyassar");
        sources[0].ResourceKind.Should().Be(FullI3rabImportConstants.ResourceKind);
        sources[0].MarkupFormat.Should().Be(FullI3rabImportConstants.MarkupFormatHtml);
        sources[0].LicenseStatus.Should().Be(FullI3rabImportConstants.LicenseStatus);
        sources[0].ProvenanceStatus.Should().Be(FullI3rabImportConstants.ProvenanceStatus);
        sources[0].UsageScope.Should().Be(FullI3rabImportConstants.UsageScope);
        sources[0].ContentCoverageCount.Should().Be(FullI3rabInvariants.ExpectedAyahsPerSource);

        var entries = await dbContext.FullI3rabEntries.AsNoTracking().ToListAsync();
        entries.Should().HaveCount(2);

        var groupedEntry = entries.Single(entry => entry.SourceEntryKey == "900:1");
        groupedEntry.SourceShape.Should().Be(FullI3rabImportConstants.SourceShapeGroupedLeader);
        groupedEntry.CoveredAyahCount.Should().Be(2);
        groupedEntry.I3rabHtml.Should().Be(FullI3rabSyntheticSeed.SyntheticHtml("900:1"));
        groupedEntry.TextHash.Should().Be(FullI3rabAssembler.ComputeTextHash(groupedEntry.I3rabHtml));

        var flatEntry = entries.Single(entry => entry.SourceEntryKey == "900:3");
        flatEntry.SourceShape.Should().Be(FullI3rabImportConstants.SourceShapeFlat);
        flatEntry.CoveredAyahCount.Should().Be(1);

        var ayahLinks = await dbContext.FullI3rabAyahEntries.AsNoTracking().ToListAsync();
        ayahLinks.Should().HaveCount(3);
        ayahLinks.Select(link => link.AyahId).Distinct().Should().HaveCount(3);

        ayahLinks.Should().Contain(link =>
            link.VerseKey == "900:1"
            && link.SourceValueKind == FullI3rabImportConstants.ValueKindLeader
            && link.IsGroupLeader);

        ayahLinks.Should().Contain(link =>
            link.VerseKey == "900:2"
            && link.SourceValueKind == FullI3rabImportConstants.ValueKindMemberPointer
            && link.SourceLeaderVerseKey == "900:1"
            && !link.IsGroupLeader);

        ayahLinks.Should().Contain(link =>
            link.VerseKey == "900:3"
            && link.SourceValueKind == FullI3rabImportConstants.ValueKindFlat
            && !link.IsGroupLeader);

        foreach (var link in ayahLinks)
        {
            link.EntryId.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Import_preserves_html_allowlist_warnings_in_result_without_blocking()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var spec = new SyntheticFullI3rabSourceSpec(
            SourceKey: "test-warn",
            HasQuranQuotationMarkup: false,
            CanonicalAyahCoverage: FullI3rabInvariants.ExpectedAyahsPerSource,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new { text = "<table class=\"ar\"><tr><td>إعراب مع وسم غير آمن</td></tr></table>" },
                ["900:2"] = new { text = "<div class=\"ar\"><p>إعراب اختباري 900:2</p></div>" },
                ["900:3"] = new { text = "<div class=\"ar\"><p>إعراب اختباري 900:3</p></div>" }
            },
            ObservedHtmlTags: new Dictionary<string, int>(),
            ObservedHtmlClasses: new Dictionary<string, int>());

        var packageDir = await packages.WriteAsync(sources: [spec]);
        var reportDir = fixture.CreateTempDir();
        var result = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);
        result.WarningCount.Should().Be(1);

        var reportJson = await File.ReadAllTextAsync(
            Path.Combine(reportDir, FullI3rabImportConstants.JsonReportFileName));
        using var document = JsonDocument.Parse(reportJson);
        var warnings = document.RootElement.GetProperty("warnings").EnumerateArray()
            .Select(warning => warning.GetString())
            .ToList();

        warnings.Should().Contain(warning =>
            warning!.Contains(FullI3rabInvariants.CheckHtmlAllowlist, StringComparison.Ordinal));
        warnings.Should().Contain(warning =>
            warning!.Contains("table", StringComparison.Ordinal));
    }

    public void Dispose() => packages.Dispose();
}
