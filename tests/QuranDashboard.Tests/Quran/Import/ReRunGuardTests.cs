using QuranDashboard.Application.Abstractions.Quran.Import;

namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ReRunGuardTests
{
    private readonly ImportTestFixture fixture;

    public ReRunGuardTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ReRunWithoutForce_OnPopulatedTables_RefusesAndChangesNothing()
    {
        var handler = await fixture.CreateHandlerAsync();

        var firstResult = await handler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: null),
            CancellationToken.None);

        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var countsBefore = await ReadTableCountsAsync();

        var secondHandler = fixture.CreateHandlerWithoutTruncate();
        var refusedResult = await secondHandler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: null, Force: false),
            CancellationToken.None);

        refusedResult.Succeeded.Should().BeFalse();
        refusedResult.ExitCode.Should().Be(ImportQuranFoundationResult.RefusedExitCode);
        refusedResult.Message.Should().Be(ImportRefusalMessages.TablesNotEmpty);

        var countsAfter = await ReadTableCountsAsync();
        countsAfter.Should().BeEquivalentTo(countsBefore);
    }

    private async Task<ImportTableCounts> ReadTableCountsAsync()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new ImportTableCounts(
            await dbContext.QuranSurahs.CountAsync(),
            await dbContext.QuranAyahs.CountAsync(),
            await dbContext.QuranMushafPages.CountAsync(),
            await dbContext.QuranMushafLines.CountAsync(),
            await dbContext.QuranWords.CountAsync());
    }

    private sealed record ImportTableCounts(
        int Surahs,
        int Ayahs,
        int Pages,
        int Lines,
        int Words);
}
