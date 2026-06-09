using System.Text.Json;
using QuranDashboard.Application.Quran.Import.ImportQuranFoundation;
using QuranDashboard.Application.Quran.Import.Validation;

namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ValidationReportTests
{
    private readonly ImportTestFixture fixture;

    public ValidationReportTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ValidSource_WritesPassWithWarningsReportForAyah37130()
    {
        var reportDir = Path.Combine(Path.GetTempPath(), $"quran-import-report-{Guid.NewGuid():N}");

        try
        {
            var handler = await fixture.CreateHandlerAsync();
            var result = await handler.HandleAsync(
                new ImportQuranFoundationCommand(fixture.SourceRoot, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeTrue(result.Message);

            var jsonReportPath = Path.Combine(reportDir, "quran-foundation-import-report.json");
            var markdownReportPath = Path.Combine(reportDir, "quran-foundation-import-report.md");

            File.Exists(jsonReportPath).Should().BeTrue();
            File.Exists(markdownReportPath).Should().BeTrue();

            await using var jsonStream = File.OpenRead(jsonReportPath);
            using var document = await JsonDocument.ParseAsync(jsonStream);

            document.RootElement.GetProperty("verdict").GetString().Should().Be(ImportValidationVerdicts.PassWithWarnings);
            document.RootElement.GetProperty("persisted").GetBoolean().Should().BeTrue();

            var warnings = document.RootElement
                .GetProperty("warnings")
                .EnumerateArray()
                .Select(element => element.GetString())
                .Where(text => text is not null)
                .Cast<string>()
                .ToList();

            warnings.Should().ContainSingle();
            warnings[0].Should().Contain("37:130");

            var warningChecks = document.RootElement
                .GetProperty("checks")
                .EnumerateArray()
                .Where(check => check.GetProperty("severity").GetString() == ImportValidationSeverities.Warning)
                .ToList();

            warningChecks.Should().ContainSingle();
            warningChecks[0].GetProperty("id").GetString().Should().Be(ImportValidationCheckIds.Ayah37130Count);

            var markdown = await File.ReadAllTextAsync(markdownReportPath);
            markdown.Should().Contain(ImportValidationVerdicts.PassWithWarnings);
            markdown.Should().Contain("37:130");
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, recursive: true);
            }
        }
    }
}
