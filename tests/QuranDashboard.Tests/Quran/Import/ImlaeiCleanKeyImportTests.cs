using QuranDashboard.Application.Quran.Import.ImportQuranFoundation;

namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ImlaeiCleanKeyImportTests
{
    private const string Sajdah = "۩";          // PLACE OF SAJDAH ۩ (U+06E9)
    private const string RubElHizb = "۞";       // START OF RUB EL HIZB ۞ (U+06DE)
    private const string RightToLeftMark = "‏"; // RLM

    private readonly ImportTestFixture fixture;

    public ImlaeiCleanKeyImportTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Import_BindsCleanImlaeiKeyAndPreservesRawImlaeiText()
    {
        var handler = await fixture.CreateHandlerAsync();

        var result = await handler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: null),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        // Every imported word received a clean identity key — no silent gaps.
        (await dbContext.QuranWords.CountAsync(word => word.WordKeyImlaeiSimple == ""))
            .Should().Be(0);

        // No readable word's clean identity key retains a stripped annotation/control mark.
        (await dbContext.QuranWords.CountAsync(word =>
                !word.IsAyahMarker &&
                (word.WordKeyImlaeiSimple.Contains(Sajdah) ||
                 word.WordKeyImlaeiSimple.Contains(RubElHizb) ||
                 word.WordKeyImlaeiSimple.Contains(RightToLeftMark))))
            .Should().Be(0);

        // Anchor 1:1:2 (الله) — no marks to strip, so the clean key equals the raw imlaei text.
        var allah = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Location == "1:1:2");
        allah.Id.Should().Be(2);
        allah.TextImlaeiSimple.Should().Be("الله");
        allah.WordKeyImlaeiSimple.Should().Be("الله");

        // Anchor 27:26:8 (العظيم) — raw imlaei carries sajdah ۩ + RLM; the clean key strips them.
        var azim = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Location == "27:26:8");
        azim.Id.Should().Be(51944);
        azim.TextImlaeiSimple.Should().Contain(Sajdah);
        azim.WordKeyImlaeiSimple.Should().Be("العظيم");
        azim.WordKeyImlaeiSimple.Should().NotContain(Sajdah);
        azim.WordKeyImlaeiSimple.Should().NotContain(RightToLeftMark);
    }
}
