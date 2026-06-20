using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsValidationSuccessReportTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsValidationSuccessReportTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Rebuild_WithValidData_WritesPassReportWithAllHardChecksPassed()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var handler = fixture.CreateHandler();
        var reportDir = Path.Combine(Path.GetTempPath(), $"words-display-pass-{Guid.NewGuid():N}");

        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: false,
                ReportOutDir: reportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);
        result.ReportOutDir.Should().Be(reportDir);
        result.Totals.Should().NotBeNull();
        result.Totals!.OrderedTashkeelRows.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        result.Totals.OrderedSimpleRows.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        result.Totals.UniqueTashkeelRows.Should().Be(DisplayWordsSyntheticSeed.UniqueTashkeelCount);
        result.Totals.UniqueSimpleRows.Should().Be(DisplayWordsSyntheticSeed.UniqueSimpleCount);
        result.Totals.ReadableWords.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);

        var jsonPath = Path.Combine(reportDir, "words-display-report.json");
        var markdownPath = Path.Combine(reportDir, "words-display-report.md");
        File.Exists(jsonPath).Should().BeTrue();
        File.Exists(markdownPath).Should().BeTrue();

        var report = await ReadReportAsync(jsonPath);
        report.Verdict.Should().Be("pass");
        report.Persisted.Should().BeTrue();
        report.Errors.Should().BeEmpty();

        report.Totals.OrderedTashkeelRows.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        report.Totals.OrderedSimpleRows.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        report.Totals.UniqueTashkeelRows.Should().Be(DisplayWordsSyntheticSeed.UniqueTashkeelCount);
        report.Totals.UniqueSimpleRows.Should().Be(DisplayWordsSyntheticSeed.UniqueSimpleCount);
        report.Totals.ReadableWords.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);

        var hardChecks = report.Checks.Where(check => check.Severity == "hard").ToList();
        hardChecks.Should().NotBeEmpty();
        hardChecks.Should().OnlyContain(check => check.Passed);

        report.Checks.Should().Contain(check =>
            check.Id == "UNQ-EXPECT-TASHKEEL"
            && check.Severity == "warning"
            && check.Observed == DisplayWordsSyntheticSeed.UniqueTashkeelCount.ToString());
        report.Checks.Should().Contain(check =>
            check.Id == "UNQ-EXPECT-SIMPLE"
            && check.Severity == "warning"
            && check.Observed == DisplayWordsSyntheticSeed.UniqueSimpleCount.ToString());

        report.Warnings.Should().NotBeEmpty();
        report.Warnings.Should().Contain(warning => warning.Contains("UNQ-EXPECT-TASHKEEL", StringComparison.Ordinal));
        report.Warnings.Should().Contain(warning => warning.Contains("UNQ-EXPECT-SIMPLE", StringComparison.Ordinal));

        var markdown = await File.ReadAllTextAsync(markdownPath);
        markdown.Should().Contain("Verdict: PASS");
        markdown.Should().Contain("Persisted: True");
        markdown.Should().Contain("| ORD-READABLE | hard |");
        markdown.Should().Contain(DisplayWordsSyntheticSeed.UniqueTashkeelCount.ToString());
        markdown.Should().Contain(DisplayWordsSyntheticSeed.UniqueSimpleCount.ToString());
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
        DisplayWordsTotals Totals,
        IReadOnlyList<WordsDisplayReportCheck> Checks,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);

    private sealed record WordsDisplayReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);
}
