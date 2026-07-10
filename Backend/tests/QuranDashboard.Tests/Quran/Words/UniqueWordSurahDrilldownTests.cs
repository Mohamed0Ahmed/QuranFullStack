using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordMissingSurahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSurahs;

namespace QuranDashboard.Tests.Quran.Words;

[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordSurahDrilldownTests(UniqueWordsTestFixture fixture)
{
    [Fact]
    public async Task GetMentionedSurahs_returns_surahs_ordered_with_counts()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSurahsQuery("tashkeel", 1002),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetUniqueWordSurahsOutcome.Success>().Subject.Response;
        response.Surahs.Should().HaveCount(5);
        response.Surahs.Select(s => s.SurahNumber).Should().BeInAscendingOrder();
        response.Surahs.Should().OnlyContain(s => s.OccurrencesInSurah > 0 && !string.IsNullOrWhiteSpace(s.NameArabic));
    }

    [Fact]
    public async Task GetMissingSurahs_partitions_114_surahs_disjoint_from_mentioned()
    {
        await using var scope = fixture.CreateScope();
        var surahsHandler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSurahsHandler>();
        var missingHandler = scope.ServiceProvider.GetRequiredService<GetUniqueWordMissingSurahsHandler>();

        var mentionedOutcome = await surahsHandler.HandleAsync(
            new GetUniqueWordSurahsQuery("tashkeel", 1002),
            CancellationToken.None);
        var mentioned = mentionedOutcome.Should().BeOfType<GetUniqueWordSurahsOutcome.Success>().Subject.Response;

        var missingOutcome = await missingHandler.HandleAsync(
            new GetUniqueWordMissingSurahsQuery("tashkeel", 1002),
            CancellationToken.None);
        var missing = missingOutcome.Should().BeOfType<GetUniqueWordMissingSurahsOutcome.Success>().Subject.Response;

        missing.Surahs.Should().HaveCount(114 - mentioned.Surahs.Count);

        var mentionedNumbers = mentioned.Surahs.Select(s => s.SurahNumber).ToHashSet();
        var missingNumbers = missing.Surahs.Select(s => s.SurahNumber).ToHashSet();

        mentionedNumbers.Intersect(missingNumbers).Should().BeEmpty();
        mentionedNumbers.Union(missingNumbers).Should().BeEquivalentTo(Enumerable.Range(1, 114));
        missing.Surahs.Select(s => s.SurahNumber).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetMentionedSurahs_simple_mode_uses_simple_link()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSurahsQuery("simple", 1001),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetUniqueWordSurahsOutcome.Success>().Subject.Response;
        response.Surahs.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMentionedSurahs_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSurahsQuery("tashkeel", 999999),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordSurahsOutcome.NotFound>();
    }
}
