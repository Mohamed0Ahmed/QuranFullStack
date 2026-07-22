using QuranDashboard.Application.Quran.DataPipelines.Foundation.Validation;

using QuranDashboard.Application.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ValidationFailureTests
{
    private readonly ImportTestFixture fixture;

    public ValidationFailureTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [CanonicalImportSourceFact]
    public async Task CorruptedSourceWithDuplicateWordId_AbortsImportAndPersistsNothing()
    {
        var corruptSourceRoot = ImportSourceTestHelpers.CopySourceToTemp(fixture.SourceRoot);
        var reportDir = Path.Combine(Path.GetTempPath(), $"quran-import-report-{Guid.NewGuid():N}");

        try
        {
            ImportSourceTestHelpers.IntroduceDuplicateWordId(corruptSourceRoot);

            var handler = await fixture.CreateHandlerAsync();
            var result = await handler.HandleAsync(
                new ImportQuranFoundationCommand(corruptSourceRoot, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("id-contiguous");

            await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

            (await dbContext.QuranSurahs.CountAsync()).Should().Be(0);
            (await dbContext.QuranAyahs.CountAsync()).Should().Be(0);
            (await dbContext.QuranMushafPages.CountAsync()).Should().Be(0);
            (await dbContext.QuranMushafLines.CountAsync()).Should().Be(0);
            (await dbContext.QuranWords.CountAsync()).Should().Be(0);

            var jsonReportPath = Path.Combine(reportDir, "quran-foundation-import-report.json");
            File.Exists(jsonReportPath).Should().BeTrue();

            await using var jsonStream = File.OpenRead(jsonReportPath);
            using var document = await JsonDocument.ParseAsync(jsonStream);
            document.RootElement.GetProperty("verdict").GetString().Should().Be(ImportValidationVerdicts.Fail);
            document.RootElement.GetProperty("persisted").GetBoolean().Should().BeFalse();

            var failedChecks = document.RootElement
                .GetProperty("checks")
                .EnumerateArray()
                .Where(check => check.GetProperty("id").GetString() == "id-contiguous")
                .ToList();

            failedChecks.Should().ContainSingle();
            failedChecks[0].GetProperty("passed").GetBoolean().Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(corruptSourceRoot))
            {
                Directory.Delete(corruptSourceRoot, recursive: true);
            }

            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, recursive: true);
            }
        }
    }
}
