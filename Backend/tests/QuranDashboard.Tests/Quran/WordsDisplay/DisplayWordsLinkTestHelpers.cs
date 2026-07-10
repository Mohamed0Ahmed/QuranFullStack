namespace QuranDashboard.Tests.Quran.WordsDisplay;

internal static class DisplayWordsLinkTestHelpers
{
    public static async Task AssertIdentityLinksAreCompleteAndValidAsync(QuranDashboardDbContext dbContext)
    {
        (await dbContext.QuranWords.CountAsync(word =>
                !word.IsAyahMarker &&
                (word.UniqueTashkeelWordId == null || word.UniqueSimpleWordId == null)))
            .Should()
            .Be(0);

        (await dbContext.QuranWords.CountAsync(word =>
                word.IsAyahMarker &&
                (word.UniqueTashkeelWordId != null || word.UniqueSimpleWordId != null)))
            .Should()
            .Be(0);

        var unresolvedTashkeelLinks = await dbContext.QuranWords
            .Where(word => word.UniqueTashkeelWordId != null)
            .GroupJoin(
                dbContext.QuranWordsUniqueTashkeel,
                word => word.UniqueTashkeelWordId,
                unique => unique.Id,
                (word, matches) => new { word, matches })
            .CountAsync(row => !row.matches.Any());

        unresolvedTashkeelLinks.Should().Be(0);

        var unresolvedSimpleLinks = await dbContext.QuranWords
            .Where(word => word.UniqueSimpleWordId != null)
            .GroupJoin(
                dbContext.QuranWordsUniqueSimple,
                word => word.UniqueSimpleWordId,
                unique => unique.Id,
                (word, matches) => new { word, matches })
            .CountAsync(row => !row.matches.Any());

        unresolvedSimpleLinks.Should().Be(0);

        var inconsistentTashkeelLinks = await (
            from word in dbContext.QuranWords
            join unique in dbContext.QuranWordsUniqueTashkeel on word.UniqueTashkeelWordId equals unique.Id
            where !word.IsAyahMarker && unique.TextUthmani != word.TextUthmani
            select word.Id).CountAsync();

        inconsistentTashkeelLinks.Should().Be(0);

        var inconsistentSimpleLinks = await (
            from word in dbContext.QuranWords
            join unique in dbContext.QuranWordsUniqueSimple on word.UniqueSimpleWordId equals unique.Id
            where !word.IsAyahMarker && unique.WordKeyImlaeiSimple != word.WordKeyImlaeiSimple
            select word.Id).CountAsync();

        inconsistentSimpleLinks.Should().Be(0);
    }
}
