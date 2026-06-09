using QuranDashboard.Application.Abstractions.Quran.Words.Display;
using QuranDashboard.Application.Quran.Words.RebuildDisplayWords;
using QuranDashboard.Domain.Quran.Words.Display;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsRefusalForceTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsRefusalForceTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Rebuild_RefusesWhenTargetsNonEmptyUnlessForce_RebuildsOnForce()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var initialReportDir = Path.Combine(Path.GetTempPath(), $"words-display-initial-{Guid.NewGuid():N}");
        var rebuildCommand = new RebuildDisplayWordsCommand(
            Force: false,
            ReportOutDir: initialReportDir,
            ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount);

        var firstResult = await fixture.CreateHandler().HandleAsync(rebuildCommand, CancellationToken.None);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);
        firstResult.ExitCode.Should().Be(RebuildDisplayWordsResult.SuccessExitCode);

        var snapshotBeforeRefusal = await CaptureDerivedSnapshotAsync();

        var refusedReportDir = Path.Combine(Path.GetTempPath(), $"words-display-refused-{Guid.NewGuid():N}");
        var refusedResult = await fixture.CreateHandler().HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: false,
                ReportOutDir: refusedReportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        refusedResult.Succeeded.Should().BeFalse();
        refusedResult.ExitCode.Should().Be(RebuildDisplayWordsResult.RefusedExitCode);
        refusedResult.Message.Should().Be(DisplayWordsInvariants.TargetsNotEmpty);
        refusedResult.ReportOutDir.Should().BeNull();
        Directory.Exists(refusedReportDir).Should().BeFalse();

        var snapshotAfterRefusal = await CaptureDerivedSnapshotAsync();
        snapshotAfterRefusal.Should().BeEquivalentTo(snapshotBeforeRefusal);

        var forceReportDir = Path.Combine(Path.GetTempPath(), $"words-display-force-{Guid.NewGuid():N}");
        var forceResult = await fixture.CreateHandler().HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: true,
                ReportOutDir: forceReportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        forceResult.Succeeded.Should().BeTrue(forceResult.Message);
        forceResult.ExitCode.Should().Be(RebuildDisplayWordsResult.SuccessExitCode);
        forceResult.Totals.Should().NotBeNull();
        forceResult.Totals!.OrderedTashkeelRows.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        forceResult.Totals.OrderedSimpleRows.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        forceResult.Totals.UniqueTashkeelRows.Should().Be(DisplayWordsSyntheticSeed.UniqueTashkeelCount);
        forceResult.Totals.UniqueSimpleRows.Should().Be(DisplayWordsSyntheticSeed.UniqueSimpleCount);

        File.Exists(Path.Combine(forceReportDir, "words-display-report.json")).Should().BeTrue();
        File.Exists(Path.Combine(forceReportDir, "words-display-report.md")).Should().BeTrue();
    }

    private async Task<DerivedSnapshot> CaptureDerivedSnapshotAsync()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var orderedTashkeelCount = await dbContext.QuranWordsOrderedTashkeel.CountAsync();
        var orderedSimpleCount = await dbContext.QuranWordsOrderedSimple.CountAsync();
        var uniqueTashkeelCount = await dbContext.QuranWordsUniqueTashkeel.CountAsync();
        var uniqueSimpleCount = await dbContext.QuranWordsUniqueSimple.CountAsync();

        var firstOrderedRow = await dbContext.QuranWordsOrderedTashkeel
            .AsNoTracking()
            .OrderBy(row => row.WordOrderInMushaf)
            .FirstAsync();

        var firstUniqueRow = await dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .OrderBy(row => row.Id)
            .FirstAsync();

        return new DerivedSnapshot(
            orderedTashkeelCount,
            orderedSimpleCount,
            uniqueTashkeelCount,
            uniqueSimpleCount,
            firstOrderedRow,
            firstUniqueRow);
    }

    private sealed record DerivedSnapshot(
        int OrderedTashkeelCount,
        int OrderedSimpleCount,
        int UniqueTashkeelCount,
        int UniqueSimpleCount,
        OrderedTashkeelWord FirstOrderedRow,
        UniqueTashkeelWord FirstUniqueRow);
}
