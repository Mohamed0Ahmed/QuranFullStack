using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsValidationFailureTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsValidationFailureTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Rebuild_WithWrongExpectedReadableWords_RollsBackAndWritesFailureReport()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var handler = fixture.CreateHandler();
        var reportDir = Path.Combine(Path.GetTempPath(), $"words-display-fail-{Guid.NewGuid():N}");
        var wrongExpected = DisplayWordsSyntheticSeed.ReadableWordCount + 1;

        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: false,
                ReportOutDir: reportDir,
                ExpectedReadableWords: wrongExpected),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(RebuildDisplayWordsResult.FailureExitCode);
        result.ReportOutDir.Should().Be(reportDir);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranWordsOrderedTashkeel.CountAsync()).Should().Be(0);
        (await dbContext.QuranWordsOrderedSimple.CountAsync()).Should().Be(0);
        (await dbContext.QuranWordsUniqueTashkeel.CountAsync()).Should().Be(0);
        (await dbContext.QuranWordsUniqueSimple.CountAsync()).Should().Be(0);

        var jsonPath = Path.Combine(reportDir, "words-display-report.json");
        var markdownPath = Path.Combine(reportDir, "words-display-report.md");
        File.Exists(jsonPath).Should().BeTrue();
        File.Exists(markdownPath).Should().BeTrue();

        var report = await ReadReportAsync(jsonPath);
        report.Verdict.Should().Be("fail");
        report.Persisted.Should().BeFalse();
        report.Errors.Should().NotBeEmpty();

        var failedReadableCheck = report.Checks.Single(check => check.Id == "ORD-READABLE");
        failedReadableCheck.Passed.Should().BeFalse();
        failedReadableCheck.Severity.Should().Be("hard");
        failedReadableCheck.Expected.Should().Be(wrongExpected.ToString());
        failedReadableCheck.Observed.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount.ToString());

        var markdown = await File.ReadAllTextAsync(markdownPath);
        markdown.Should().Contain("ORD-READABLE");
        markdown.Should().Contain("Verdict: FAIL");
        markdown.Should().Contain("Persisted: False");
    }

    private static readonly JsonSerializerOptions ReportJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<WordsDisplayReportDocument> ReadReportAsync(string jsonPath)
    {
        await using var stream = File.OpenRead(jsonPath);
        var report = await JsonSerializer.DeserializeAsync<WordsDisplayReportDocument>(stream, ReportJsonOptions);
        return report ?? throw new InvalidOperationException("Report JSON was empty.");
    }

    private sealed record WordsDisplayReportDocument(
        string Verdict,
        bool Persisted,
        IReadOnlyList<WordsDisplayReportCheck> Checks,
        IReadOnlyList<string> Errors);

    private sealed record WordsDisplayReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);
}
