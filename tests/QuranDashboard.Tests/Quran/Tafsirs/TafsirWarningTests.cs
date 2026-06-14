using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirWarningTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Successful_import_report_includes_required_warning_and_info_note_ids()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.TwoSourceIntegrationSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-warnings-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.TwoSourceTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);
        result.WarningCount.Should().BeGreaterThan(0);

        var reportJson = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.JsonReportFileName));
        using var document = JsonDocument.Parse(reportJson);
        var root = document.RootElement;

        var warnings = root.GetProperty("warnings").EnumerateArray()
            .Select(element => element.GetString()!)
            .ToList();
        warnings.Should().Contain(warning => warning.StartsWith(TafsirInvariants.WarningProvenance, StringComparison.Ordinal));
        warnings.Should().Contain(warning => warning.StartsWith(TafsirInvariants.WarningModernWorks, StringComparison.Ordinal));

        var infoNotes = root.GetProperty("infoNotes").EnumerateArray()
            .Select(element => element.GetString()!)
            .ToList();
        infoNotes.Should().Contain(note => note.StartsWith(TafsirInvariants.InfoInlineMarkup, StringComparison.Ordinal));
        infoNotes.Should().Contain(note => note.StartsWith(TafsirInvariants.InfoLanguageCoverage, StringComparison.Ordinal));
        infoNotes.Should().Contain(note => note.StartsWith(TafsirInvariants.InfoTextBlockCount, StringComparison.Ordinal));
    }
}
