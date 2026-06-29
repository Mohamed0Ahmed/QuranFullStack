using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsSearchSortPagingTests(UniqueWordsTestFixture fixture)
{
    [Fact]
    public async Task Search_with_plain_alef_matches_stored_madda_alef_word()
    {

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
    public async Task Search_tashkeel_matches_text_uthmani_simple()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", "ءامنوا", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 2003);
    }

    [Fact]
    public async Task Search_simple_matches_text_uthmani_simple()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("simple", "ءامنوا", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 2003);
    }

    [Fact]
    public async Task Search_simple_matches_text_imlaei_simple()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("simple", "آمنوا", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 2003);
    }

    [Fact]
    public async Task Search_simple_matches_word_key_imlaei_simple()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("simple", "امنوا", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 2003);
    }

    [Theory]
    [InlineData("tashkeel")]
    [InlineData("simple")]
    public async Task Search_normalizes_pasted_visible_uthmani_with_alef_wasla(string kind)
    {

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery(kind, "ٱللَّهِ", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 1002);
    }

    [Theory]
    [InlineData("tashkeel")]
    [InlineData("simple")]
    public async Task Search_normalizes_pasted_visible_uthmani_with_final_quranic_mark(string kind)
    {

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery(kind, "ءَامَنُوا۟", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Id == 2003);
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
        page.Items[0].Id.Should().Be(1002);
    }

    [Fact]
    public async Task Sort_by_alpha_orders_alphabetically_then_by_mushaf_order()
    {

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, "alpha", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal([2003, 1003, 1004, 1002, 31001, 1001, 60041, 1202]);
    }

    [Fact]
    public async Task Paging_returns_second_slice_for_smaller_page_size()
    {

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var first = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 2),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(8);
        firstPage.Items.Should().HaveCount(2);
        firstPage.Items.Select(i => i.Id).Should().Equal([1001, 1002]);

        var second = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 2, 2),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        secondPage.TotalCount.Should().Be(8);
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

        page.TotalCount.Should().Be(8);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_with_no_matches_returns_success_empty_page()
    {

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
