using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsCountRangeFilterTests(UniqueWordsTestFixture fixture)
{
    [Theory]
    [InlineData(5, 2, 0, 0, 0, 0)]
    [InlineData(-1, null, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 3, 1)]
    public async Task Invalid_range_returns_invalid_filter(
        int? occMin, int? occMax, int? ayahsMin, int? ayahsMax, int? surahsMin, int? surahsMax)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery(
                "tashkeel", null, null, 1, 1000,
                UniqueWordsCountFilter.FromRaw(occMin, occMax, ayahsMin, ayahsMax, surahsMin, surahsMax)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.InvalidFilter>();
    }

    [Fact]
    public async Task Open_ended_range_is_accepted()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery(
                "tashkeel", null, null, 1, 1000,
                UniqueWordsCountFilter.FromRaw(1, null, null, null, null, null)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>();
    }

    [Fact]
    public async Task Occurrences_range_narrows_rows_and_totalcount_to_the_matching_subset()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var all = (await ReadAsync(handler, UniqueWordsCountFilter.None)).Items;
        var threshold = MedianOccurrences(all);
        var expected = all.Where(i => i.OccurrencesCount >= threshold).Select(i => i.Id).OrderBy(id => id).ToList();

        var filtered = await ReadAsync(
            handler,
            UniqueWordsCountFilter.FromRaw(threshold, null, null, null, null, null));

        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count, "filtered TotalCount equals the filtered row count (stat contract)");
    }

    [Fact]
    public async Task Closed_surahs_range_matches_exact_bounds_inclusive()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var all = (await ReadAsync(handler, UniqueWordsCountFilter.None)).Items;
        var minSurahs = all.Min(i => i.SurahsCount);
        var expected = all.Where(i => i.SurahsCount == minSurahs).Select(i => i.Id).OrderBy(id => id).ToList();

        var filtered = await ReadAsync(
            handler,
            UniqueWordsCountFilter.FromRaw(null, null, null, null, minSurahs, minSurahs));

        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count);
    }

    [Fact]
    public async Task Range_composes_with_search_as_intersection()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var searched = (await ReadSearchAsync(handler, "امنوا", UniqueWordsCountFilter.None)).Items;
        searched.Should().NotBeEmpty();
        var maxOcc = searched.Max(i => i.OccurrencesCount);

        var kept = await ReadSearchAsync(
            handler, "امنوا", UniqueWordsCountFilter.FromRaw(null, maxOcc, null, null, null, null));
        kept.Items.Select(i => i.Id).Should().BeEquivalentTo(searched.Select(i => i.Id));
        kept.TotalCount.Should().Be(searched.Count);

        var dropped = await ReadSearchAsync(
            handler, "امنوا", UniqueWordsCountFilter.FromRaw(maxOcc + 1, null, null, null, null, null));
        dropped.Items.Should().BeEmpty();
        dropped.TotalCount.Should().Be(0);
    }

    private static int MedianOccurrences(IReadOnlyList<UniqueWordListItemDto> items)
    {
        var sorted = items.Select(i => i.OccurrencesCount).OrderBy(x => x).ToList();
        return sorted[sorted.Count / 2];
    }

    private static async Task<PagedResult<UniqueWordListItemDto>> ReadAsync(
        GetUniqueWordsPageHandler handler, UniqueWordsCountFilter filter) =>
        (await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 1000, filter),
            CancellationToken.None))
        .Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

    private static async Task<PagedResult<UniqueWordListItemDto>> ReadSearchAsync(
        GetUniqueWordsPageHandler handler, string search, UniqueWordsCountFilter filter) =>
        (await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", search, null, 1, 1000, filter),
            CancellationToken.None))
        .Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
}
