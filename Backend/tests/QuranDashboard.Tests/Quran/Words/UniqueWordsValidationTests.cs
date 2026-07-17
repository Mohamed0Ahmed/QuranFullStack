using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsValidationTests(UniqueWordsTestFixture fixture)
{
    [Theory]
    [InlineData("not-a-kind")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Invalid_kind_returns_validation_outcome(string? kind)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery(kind, null, null, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.InvalidKind>();
    }

    [Theory]
    [InlineData("bogus")]
    [InlineData("relevance")]
    // mushaf-order is the release order, not a column: ascending-only, bare token only.
    [InlineData("mushaf-order-asc")]
    [InlineData("mushaf-order-desc")]
    // Columns other explorers offer are still unlisted here.
    [InlineData("simple")]
    [InlineData("tashkeel")]
    [InlineData("lemmas")]
    [InlineData("stems")]
    public async Task Invalid_sort_returns_validation_outcome(string sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, sort, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.InvalidSort>();
    }

    [Fact]
    public async Task Empty_or_null_sort_defaults_to_mushaf_order()
    {

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var nullOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);
        nullOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>();

        var emptyOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, "", 1, 50),
            CancellationToken.None);
        emptyOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Invalid_page_returns_invalid_paging(int page)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, page, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.InvalidPaging>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1001)]
    public async Task Invalid_page_size_returns_invalid_paging(int pageSize)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, pageSize),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.InvalidPaging>();
    }

    [Fact]
    public async Task Max_page_size_boundary_is_accepted()
    {

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 1000),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>();
    }

    [Fact]
    public async Task Ayah_max_page_size_boundary_is_accepted()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 100),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>();
    }

    [Fact]
    public async Task Ayah_page_size_above_max_returns_invalid_paging()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 101),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordAyahsOutcome.InvalidPaging>();
    }
}
