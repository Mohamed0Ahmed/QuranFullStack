namespace QuranDashboard.Tests.Quran.WordsDisplay;

internal static class DisplayWordsLinkTestHelpers
{
    private const string IgnoredTashkeelIdentityMarks =
        "\u0640\u0653\u06D6\u06D7\u06D8\u06D9\u06DA\u06DB\u06DC\u06DE\u06E9\u200F";

    public static string TashkeelIdentityOf(string text) =>
        string.Concat(text.Where(character => !IgnoredTashkeelIdentityMarks.Contains(character))).Trim();

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

        var ignoredMarks = IgnoredTashkeelIdentityMarks;
        var inconsistentTashkeelLinks = await dbContext.Database.SqlQuery<int>(
                $"""
                SELECT COUNT(*)::int AS "Value"
                FROM quran_words word
                JOIN quran_words_unique_tashkeel unique_word
                  ON unique_word.id = word.unique_tashkeel_word_id
                WHERE word.is_ayah_marker = false
                  AND btrim(translate(unique_word.text_uthmani, {ignoredMarks}, ''))
                      <> btrim(translate(word.text_uthmani, {ignoredMarks}, ''))
                """)
            .SingleAsync();

        inconsistentTashkeelLinks.Should().Be(0);

        var inconsistentSimpleLinks = await (
            from word in dbContext.QuranWords
            join unique in dbContext.QuranWordsUniqueSimple on word.UniqueSimpleWordId equals unique.Id
            where !word.IsAyahMarker && unique.WordKeyImlaeiSimple != word.WordKeyImlaeiSimple
            select word.Id).CountAsync();

        inconsistentSimpleLinks.Should().Be(0);
    }
}
