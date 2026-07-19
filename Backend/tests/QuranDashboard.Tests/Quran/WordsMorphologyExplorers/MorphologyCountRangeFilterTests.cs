using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

// Feature 026, US5 — count-range filters on the Lemmas list (in-memory predicates over the cached
// whole-summary rows). Predicate assertions are data-driven from an unfiltered read.
[Collection(nameof(MorphologyExplorersCollection))]
public sealed class LemmasCountRangeFilterTests(MorphologyExplorersTestFixture fixture)
{
    [Theory]
    [InlineData(9, 2)]     // min > max
    [InlineData(-3, null)] // negative min
    public async Task Invalid_range_returns_invalid_filter(int? occMin, int? occMax)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 1000, FromOccurrences(occMin, occMax)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmasPageOutcome.InvalidFilter>();
    }

    [Fact]
    public async Task Occurrences_range_narrows_rows_and_totalcount_to_the_matching_subset()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var all = (await ReadAsync(handler, LemmasCountFilter.None)).Items;
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
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var all = (await ReadAsync(handler, LemmasCountFilter.None)).Items;
        var minStems = all.Min(i => i.StemsCount);
        var expected = all.Where(i => i.StemsCount == minStems).Select(i => i.Id).OrderBy(id => id).ToList();

        var filter = new LemmasCountFilter(default, default, default, default, default, new(minStems, minStems));
        var filtered = await ReadAsync(handler, filter);

        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count);
    }

    [Fact]
    public async Task Range_composes_with_search_as_intersection()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var searched = (await ReadSearchAsync(handler, "كلمة", LemmasCountFilter.None)).Items;
        searched.Should().NotBeEmpty();
        var maxOcc = searched.Max(i => i.OccurrencesCount);

        var kept = await ReadSearchAsync(handler, "كلمة", FromOccurrences(null, maxOcc));
        kept.Items.Select(i => i.Id).Should().BeEquivalentTo(searched.Select(i => i.Id));

        var dropped = await ReadSearchAsync(handler, "كلمة", FromOccurrences(maxOcc + 1, null));
        dropped.Items.Should().BeEmpty();
        dropped.TotalCount.Should().Be(0);
    }

    private static LemmasCountFilter FromOccurrences(int? min, int? max) =>
        LemmasCountFilter.FromRaw(min, max, null, null, null, null, null, null, null, null, null, null);

    private static async Task<PagedResult<LemmaListItemDto>> ReadAsync(
        GetLemmasPageHandler handler, LemmasCountFilter filter) =>
        (await handler.HandleAsync(new GetLemmasPageQuery(null, null, 1, 1000, filter), CancellationToken.None))
        .Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

    private static async Task<PagedResult<LemmaListItemDto>> ReadSearchAsync(
        GetLemmasPageHandler handler, string search, LemmasCountFilter filter) =>
        (await handler.HandleAsync(new GetLemmasPageQuery(search, null, 1, 1000, filter), CancellationToken.None))
        .Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;
}

// Feature 026, US5 — count-range filters on the Stems list (five metrics, in-memory predicates).
[Collection(nameof(MorphologyExplorersCollection))]
public sealed class StemsCountRangeFilterTests(MorphologyExplorersTestFixture fixture)
{
    [Theory]
    [InlineData(9, 2)]     // min > max
    [InlineData(null, -1)] // negative max
    public async Task Invalid_range_returns_invalid_filter(int? occMin, int? occMax)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 1000, FromOccurrences(occMin, occMax)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemsPageOutcome.InvalidFilter>();
    }

    [Fact]
    public async Task Occurrences_range_narrows_rows_and_totalcount_to_the_matching_subset()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var all = (await ReadAsync(handler, StemsCountFilter.None)).Items;
        var threshold = all.Select(i => i.OccurrencesCount).OrderBy(x => x).ElementAt(all.Count / 2);
        var expected = all.Where(i => i.OccurrencesCount >= threshold).Select(i => i.Id).OrderBy(id => id).ToList();

        var filtered = await ReadAsync(handler, FromOccurrences(threshold, null));

        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count, "filtered TotalCount equals the filtered row count (stat contract)");
    }

    [Fact]
    public async Task Tashkeel_words_subcount_range_filters_by_the_displayed_count()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var all = (await ReadAsync(handler, StemsCountFilter.None)).Items;
        var minTashkeel = all.Min(i => i.TashkeelWordsCount);
        var expected = all.Where(i => i.TashkeelWordsCount == minTashkeel).Select(i => i.Id).OrderBy(id => id).ToList();

        var filter = new StemsCountFilter(default, default, default, default, new(minTashkeel, minTashkeel));
        var filtered = await ReadAsync(handler, filter);

        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count);
    }

    [Fact]
    public async Task Range_composes_with_search_as_intersection()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var searched = (await ReadSearchAsync(handler, "حكم", StemsCountFilter.None)).Items;
        searched.Should().NotBeEmpty();
        var maxOcc = searched.Max(i => i.OccurrencesCount);

        var kept = await ReadSearchAsync(handler, "حكم", FromOccurrences(null, maxOcc));
        kept.Items.Select(i => i.Id).Should().BeEquivalentTo(searched.Select(i => i.Id));

        var dropped = await ReadSearchAsync(handler, "حكم", FromOccurrences(maxOcc + 1, null));
        dropped.Items.Should().BeEmpty();
        dropped.TotalCount.Should().Be(0);
    }

    private static StemsCountFilter FromOccurrences(int? min, int? max) =>
        StemsCountFilter.FromRaw(min, max, null, null, null, null, null, null, null, null);

    private static async Task<PagedResult<StemListItemDto>> ReadAsync(
        GetStemsPageHandler handler, StemsCountFilter filter) =>
        (await handler.HandleAsync(new GetStemsPageQuery(null, null, 1, 1000, filter), CancellationToken.None))
        .Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

    private static async Task<PagedResult<StemListItemDto>> ReadSearchAsync(
        GetStemsPageHandler handler, string search, StemsCountFilter filter) =>
        (await handler.HandleAsync(new GetStemsPageQuery(search, null, 1, 1000, filter), CancellationToken.None))
        .Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;
}
