using QuranDashboard.Domain.Quran.MushafPages;

namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ImportReconstructionTests
{
    private readonly ImportTestFixture fixture;

    public ImportReconstructionTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ImportIntoEmptyDatabase_ResolvesSampleWordAndPageOneLayout()
    {
        var handler = await fixture.CreateHandlerAsync();

        var result = await handler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: null),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var sampleWord = await dbContext.QuranWords
            .AsNoTracking()
            .SingleAsync(word => word.Location == "2:25:3");

        sampleWord.PageNumber.Should().BeGreaterThan(0);
        sampleWord.LineNumber.Should().BeGreaterThan(0);
        sampleWord.LineWordOrder.Should().BeGreaterThan(0);
        sampleWord.IsAyahMarker.Should().BeFalse();
        sampleWord.QpcGlyph.Should().NotBeNullOrWhiteSpace();
        sampleWord.TextUthmani.Should().NotBeNullOrWhiteSpace();
        sampleWord.TextUthmaniSimple.Should().NotBeNullOrWhiteSpace();
        sampleWord.TextImlaeiSimple.Should().NotBeNullOrWhiteSpace();
        sampleWord.AyahId.Should().BeGreaterThan(0);

        var pageOneLines = await dbContext.QuranMushafLines
            .AsNoTracking()
            .Where(line => line.PageNumber == 1)
            .OrderBy(line => line.LineNumber)
            .ToListAsync();

        pageOneLines.Should().HaveCount(8);
        pageOneLines[0].LineType.Should().Be(MushafLineType.SurahName);
        pageOneLines.Skip(1).Should().OnlyContain(line => line.LineType == MushafLineType.Ayah);
    }
}
