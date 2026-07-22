using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

namespace QuranDashboard.Tests.Quran.WordsRoots;

[Collection(nameof(RootsExplorerCollection))]
public sealed class RootsCountRangeFilterTests(RootsExplorerTestFixture fixture)
{
    [Theory]
    [InlineData(5, 2)]
    [InlineData(-1, null)]
    public async Task Invalid_range_returns_invalid_filter(int? occMin, int? occMax)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, null, 1, 1000, FromOccurrences(occMin, occMax)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetRootsPageOutcome.InvalidFilter>();
    }

    [Fact]
    public async Task Occurrences_range_narrows_rows_and_totalcount_to_the_matching_subset()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var all = (await ReadAsync(handler, RootsCountFilter.None)).Items;
        var threshold = all.Select(i => i.OccurrencesCount).OrderBy(x => x).ElementAt(all.Count / 2);
        var expected = all.Where(i => i.OccurrencesCount >= threshold).Select(i => i.Id).OrderBy(id => id).ToList();

        var filtered = await ReadAsync(handler, FromOccurrences(threshold, null));

        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count, "filtered TotalCount equals the filtered row count (stat contract)");
    }

    [Fact]
    public async Task Stems_subcount_range_filters_by_the_displayed_stem_count()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var all = (await ReadAsync(handler, RootsCountFilter.None)).Items;
        var minStems = all.Min(i => i.StemsCount);
        var expected = all.Where(i => i.StemsCount == minStems).Select(i => i.Id).OrderBy(id => id).ToList();

        var filter = new RootsCountFilter(
            default, default, default, default, default, default, new(minStems, minStems));
        var filtered = await ReadAsync(handler, filter);

        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count);
    }

    [Fact]
    public async Task Range_composes_with_search_as_intersection()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var searched = (await ReadSearchAsync(handler, "رحم", RootsCountFilter.None)).Items;
        searched.Should().NotBeEmpty();
        var maxOcc = searched.Max(i => i.OccurrencesCount);

        var kept = await ReadSearchAsync(handler, "رحم", FromOccurrences(null, maxOcc));
        kept.Items.Select(i => i.Id).Should().BeEquivalentTo(searched.Select(i => i.Id));

        var dropped = await ReadSearchAsync(handler, "رحم", FromOccurrences(maxOcc + 1, null));
        dropped.Items.Should().BeEmpty();
        dropped.TotalCount.Should().Be(0);
    }

    private static RootsCountFilter FromOccurrences(int? min, int? max) =>
        RootsCountFilter.FromRaw(min, max, null, null, null, null, null, null, null, null, null, null, null, null);

    private static async Task<PagedResult<RootListItemDto>> ReadAsync(
        GetRootsPageHandler handler, RootsCountFilter filter) =>
        (await handler.HandleAsync(new GetRootsPageQuery(null, null, 1, 1000, filter), CancellationToken.None))
        .Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

    private static async Task<PagedResult<RootListItemDto>> ReadSearchAsync(
        GetRootsPageHandler handler, string search, RootsCountFilter filter) =>
        (await handler.HandleAsync(new GetRootsPageQuery(search, null, 1, 1000, filter), CancellationToken.None))
        .Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;
}
