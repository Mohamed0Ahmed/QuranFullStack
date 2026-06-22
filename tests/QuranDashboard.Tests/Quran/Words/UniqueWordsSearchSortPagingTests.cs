using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

/// <summary>
/// US2 search, sort, paging bounds, and empty-result behavior. Search asserts
/// the symmetric Arabic fold (a plain-alef query matches a stored madda-alef
/// word), diacritic-insensitive matching, occurrences/alpha ordering, paging
/// slicing, and that no matches is a Success page with totalCount 0.
/// </summary>
[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsSearchSortPagingTests(UniqueWordsTestFixture fixture)
{
    [Fact]
    public async Task Search_with_plain_alef_matches_stored_madda_alef_word()
    {
        // SC-006: the stored tashkeel word `ءَامَنُوا۟` has the imlaei form
        // `آمنوا` (madda alef). A plain-alef query `امنوا` must still match it
        // because the symmetric fold maps آ→ا on both sides.
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", "امنوا", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 2003);
    }

    [Fact]
    public async Task Search_tolerates_tashkeel_in_the_query()
    {
        // A fully-voweled query should match the unvoweled stored column after
        // tashkeel stripping on the query side.
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", "رَحْمَٰن", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 1003);
    }

    [Fact]
    public async Task Search_works_in_simple_mode()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("simple", "الله", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 1002);
    }

    [Fact]
    public async Task Sort_by_occurrences_orders_highest_count_first()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, "occurrences", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.OccurrencesCount).Should().BeInDescendingOrder();
        page.Items[0].Id.Should().Be(1002); // seeded highest occurrence count (5)
    }

    [Fact]
    public async Task Sort_by_alpha_orders_alphabetically_then_by_mushaf_order()
    {
        // Alpha sort operates on the searchable imlaei text, not the Uthmani
        // display text (the two are genuinely different orderings because the
        // Uthmani form carries tashkeel/wasla glyphs). Assert against the known
        // imlaei-ascending id sequence from the seed slice, which is stable and
        // independent of Uthmani glyph code points.
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, "alpha", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        // Seed imlaei keys in ascending order: امنوا, الرحمن, الرحيم, الله, بسم, قل
        // → ids 2003, 1003, 1004, 1002, 1001, 60041.
        page.Items.Select(i => i.Id).Should().Equal([2003, 1003, 1004, 1002, 1001, 60041]);
    }

    [Fact]
    public async Task Paging_returns_second_slice_for_smaller_page_size()
    {
        // With pageSize 2, page 1 holds ids 1001/1002 (mushaf order) and page 2
        // holds ids 1003/1004, while totalCount stays the full seeded count.
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var first = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 2),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(6);
        firstPage.Items.Should().HaveCount(2);
        firstPage.Items.Select(i => i.Id).Should().Equal([1001, 1002]);

        var second = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 2, 2),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        secondPage.TotalCount.Should().Be(6);
        secondPage.Items.Should().HaveCount(2);
        secondPage.Items.Select(i => i.Id).Should().Equal([1003, 1004]);
    }

    [Fact]
    public async Task Paging_beyond_last_page_returns_empty_items_with_nonzero_total()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 99, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        page.TotalCount.Should().Be(6);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_with_no_matches_returns_success_empty_page()
    {
        // FR/edge: a search with no matches is a 200 Success page with
        // totalCount 0 and empty items — never an error.
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", "كلمةغيرموجودة", null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }
}
