using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMissingSurahs;

namespace QuranDashboard.Tests.Quran.WordsRoots;

/// <summary>
/// US4 (T059): Root surahs — mentioned + missing sum to 114; per-surah counts;
/// near-all-surahs root yields a near-empty missing list.
/// </summary>
[Collection(nameof(RootsExplorerCollection))]
public sealed class RootsSurahsReadTests(RootsExplorerTestFixture fixture)
{
    private const int HighFrequencyRootId = 10;
    private const int NearAllSurahsRootId = 30;

    [Fact]
    public async Task GetRootMentionedSurahs_returns_correct_per_surah_counts()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootMentionedSurahsQuery(HighFrequencyRootId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetRootMentionedSurahsOutcome.Success>().Subject.Surahs;
        response.SurahsCount.Should().Be(2);
        response.Surahs.Should().HaveCount(2);

        var byNumber = response.Surahs.ToDictionary(s => s.SurahNumber);
        byNumber[1].OccurrencesInSurah.Should().Be(4);
        byNumber[2].OccurrencesInSurah.Should().Be(1);
        byNumber[1].NameArabic.Should().Be("الفاتحة");
    }

    [Fact]
    public async Task GetRootSurahs_mentioned_and_missing_counts_sum_to_114()
    {
        await using var scope = fixture.CreateScope();
        var mentionedHandler = scope.ServiceProvider.GetRequiredService<GetRootMentionedSurahsHandler>();
        var missingHandler = scope.ServiceProvider.GetRequiredService<GetRootMissingSurahsHandler>();

        var mentioned = await mentionedHandler.HandleAsync(
            new GetRootMentionedSurahsQuery(HighFrequencyRootId),
            CancellationToken.None);
        var missing = await missingHandler.HandleAsync(
            new GetRootMissingSurahsQuery(HighFrequencyRootId),
            CancellationToken.None);

        var mentionedResponse = mentioned.Should().BeOfType<GetRootMentionedSurahsOutcome.Success>().Subject.Surahs;
        var missingResponse = missing.Should().BeOfType<GetRootMissingSurahsOutcome.Success>().Subject.MissingSurahs;

        (mentionedResponse.SurahsCount + missingResponse.MissingSurahsCount).Should().Be(114);
    }

    [Fact]
    public async Task GetRootMissingSurahs_near_all_surahs_root_yields_near_empty_missing_list()
    {
        await using var scope = fixture.CreateScope();
        var mentionedHandler = scope.ServiceProvider.GetRequiredService<GetRootMentionedSurahsHandler>();
        var missingHandler = scope.ServiceProvider.GetRequiredService<GetRootMissingSurahsHandler>();

        var mentioned = await mentionedHandler.HandleAsync(
            new GetRootMentionedSurahsQuery(NearAllSurahsRootId),
            CancellationToken.None);
        var missing = await missingHandler.HandleAsync(
            new GetRootMissingSurahsQuery(NearAllSurahsRootId),
            CancellationToken.None);

        var mentionedResponse = mentioned.Should().BeOfType<GetRootMentionedSurahsOutcome.Success>().Subject.Surahs;
        var missingResponse = missing.Should().BeOfType<GetRootMissingSurahsOutcome.Success>().Subject.MissingSurahs;

        mentionedResponse.SurahsCount.Should().Be(9);
        missingResponse.MissingSurahsCount.Should().Be(105);
        missingResponse.Surahs.Should().HaveCount(105);
    }
}
