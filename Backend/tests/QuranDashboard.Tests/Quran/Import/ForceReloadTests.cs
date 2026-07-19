using QuranDashboard.Domain.Quran.MushafPages;

using QuranDashboard.Application.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ForceReloadTests
{
    private readonly ImportTestFixture fixture;

    public ForceReloadTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ForceReRun_ProducesTableStateIdenticalToFirstImport()
    {
        var handler = await fixture.CreateHandlerAsync();

        var firstResult = await handler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: ImportSourceTestHelpers.TempReportDir()),
            CancellationToken.None);

        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var snapshotBeforeForce = await CaptureSnapshotAsync();

        var secondHandler = fixture.CreateHandlerWithoutTruncate();
        var forceResult = await secondHandler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: ImportSourceTestHelpers.TempReportDir(), Force: true),
            CancellationToken.None);

        forceResult.Succeeded.Should().BeTrue(forceResult.Message);
        forceResult.Totals.Should().BeEquivalentTo(firstResult.Totals);

        var snapshotAfterForce = await CaptureSnapshotAsync();
        snapshotAfterForce.Should().BeEquivalentTo(snapshotBeforeForce);
    }

    private async Task<ImportSnapshot> CaptureSnapshotAsync()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var sampleWord = await dbContext.QuranWords
            .AsNoTracking()
            .SingleAsync(word => word.Location == "2:25:3");

        var pageOneLines = await dbContext.QuranMushafLines
            .AsNoTracking()
            .Where(line => line.PageNumber == 1)
            .OrderBy(line => line.LineNumber)
            .Select(line => new PageOneLineSnapshot(
                line.LineNumber,
                line.LineType,
                line.FirstWordId,
                line.LastWordId,
                line.WordsCount))
            .ToListAsync();

        return new ImportSnapshot(
            await dbContext.QuranSurahs.CountAsync(),
            await dbContext.QuranAyahs.CountAsync(),
            await dbContext.QuranMushafPages.CountAsync(),
            await dbContext.QuranMushafLines.CountAsync(),
            await dbContext.QuranWords.CountAsync(),
            await dbContext.QuranWords.CountAsync(word => word.IsAyahMarker),
            await dbContext.QuranWords.CountAsync(word => !word.IsAyahMarker),
            new SampleWordSnapshot(
                sampleWord.Location,
                sampleWord.AyahId,
                sampleWord.PageNumber,
                sampleWord.LineNumber,
                sampleWord.LineWordOrder,
                sampleWord.IsAyahMarker,
                sampleWord.QpcGlyph,
                sampleWord.TextUthmani,
                sampleWord.TextUthmaniSimple,
                sampleWord.TextImlaeiSimple),
            pageOneLines);
    }

    private sealed record ImportSnapshot(
        int Surahs,
        int Ayahs,
        int Pages,
        int Lines,
        int Words,
        int AyahMarkers,
        int ReadableWords,
        SampleWordSnapshot SampleWord,
        IReadOnlyList<PageOneLineSnapshot> PageOneLines);

    private sealed record SampleWordSnapshot(
        string Location,
        int AyahId,
        int PageNumber,
        int LineNumber,
        int LineWordOrder,
        bool IsAyahMarker,
        string QpcGlyph,
        string TextUthmani,
        string TextUthmaniSimple,
        string TextImlaeiSimple);

    private sealed record PageOneLineSnapshot(
        int LineNumber,
        MushafLineType LineType,
        int? FirstWordId,
        int? LastWordId,
        int WordsCount);
}
