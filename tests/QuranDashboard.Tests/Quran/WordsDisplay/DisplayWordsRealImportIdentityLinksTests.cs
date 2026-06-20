using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(DisplayWordsRealImportCollection))]
public sealed class DisplayWordsRealImportIdentityLinksTests
{
    private readonly DisplayWordsRealImportFixture fixture;

    public DisplayWordsRealImportIdentityLinksTests(DisplayWordsRealImportFixture fixture)
    {
        this.fixture = fixture;
    }

    [CanonicalImportSourceFact]
    public async Task CanonicalImportRebuild_PopulatesExpectedCountsAndIdentityLinks()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranWords.CountAsync())
            .Should()
            .Be(DisplayWordsInvariants.CanonicalTotalQuranWordRows);
        (await dbContext.QuranWords.CountAsync(word => !word.IsAyahMarker))
            .Should()
            .Be(DisplayWordsInvariants.CanonicalReadableWords);
        (await dbContext.QuranWords.CountAsync(word => word.IsAyahMarker))
            .Should()
            .Be(DisplayWordsInvariants.CanonicalAyahMarkers);
        (await dbContext.QuranWordsOrderedTashkeel.CountAsync())
            .Should()
            .Be(DisplayWordsInvariants.CanonicalOrderedTashkeel);
        (await dbContext.QuranWordsOrderedSimple.CountAsync())
            .Should()
            .Be(DisplayWordsInvariants.CanonicalOrderedSimple);
        (await dbContext.QuranWordsUniqueTashkeel.CountAsync())
            .Should()
            .Be(DisplayWordsInvariants.CanonicalUniqueTashkeel);
        (await dbContext.QuranWordsUniqueSimple.CountAsync())
            .Should()
            .Be(DisplayWordsInvariants.CanonicalUniqueSimple);

        await DisplayWordsLinkTestHelpers.AssertIdentityLinksAreCompleteAndValidAsync(dbContext);
    }

    [CanonicalImportSourceTheory]
    [InlineData("الله", 2_155)]
    [InlineData("العظيم", 36)]
    [InlineData("الرحمان", 45)]
    public async Task CanonicalImportRebuild_UniqueSimpleAnchorOccurrencesMatchExpected(
        string wordKeyImlaeiSimple,
        int expectedOccurrences)
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var uniqueRow = await dbContext.QuranWordsUniqueSimple
            .AsNoTracking()
            .SingleAsync(row => row.WordKeyImlaeiSimple == wordKeyImlaeiSimple);

        uniqueRow.OccurrencesCount.Should().Be(expectedOccurrences);
    }

    [CanonicalImportSourceFact]
    public async Task CanonicalImportRebuild_RahmanIdentityKeepsRepresentativeUthmaniDisplay()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var rahmanIdentity = await dbContext.QuranWordsUniqueSimple
            .AsNoTracking()
            .SingleAsync(row => row.WordKeyImlaeiSimple == "الرحمان");

        rahmanIdentity.WordKeyImlaeiSimple.Should().Be("الرحمان");
        rahmanIdentity.TextUthmani.Should().NotBe(rahmanIdentity.WordKeyImlaeiSimple);

        var firstOccurrence = await dbContext.QuranWords
            .AsNoTracking()
            .Where(word => !word.IsAyahMarker && word.WordKeyImlaeiSimple == "الرحمان")
            .OrderBy(word => word.Id)
            .FirstAsync();

        rahmanIdentity.TextUthmani.Should().Be(firstOccurrence.TextUthmani);
        rahmanIdentity.FirstQuranWordId.Should().Be(firstOccurrence.Id);
    }

    [CanonicalImportSourceFact]
    public async Task CanonicalImportRebuild_AlYasinRemainsSingleMultiTokenIdentity()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var alYasin = await dbContext.QuranWordsUniqueSimple
            .AsNoTracking()
            .SingleAsync(row => row.WordKeyImlaeiSimple == "ال ياسين");

        alYasin.OccurrencesCount.Should().Be(1);
    }

    [CanonicalImportSourceFact]
    public async Task CanonicalImportRebuild_Location55212KeepsDairaImlaeiKey()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var daira = await dbContext.QuranWords
            .AsNoTracking()
            .SingleAsync(word => word.Location == "5:52:12");

        daira.WordKeyImlaeiSimple.Should().Be("دايرة");
    }

    [CanonicalImportSourceFact]
    public async Task CanonicalImportRebuild_DoesNotMutateSourceWordTextColumns()
    {
        var sourceWordsAfterRebuild = await fixture.ReadSourceWordColumnsAsync();
        sourceWordsAfterRebuild.Should().BeEquivalentTo(fixture.SourceWordsAfterImport);
    }
}
