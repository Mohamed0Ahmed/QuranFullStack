using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootWords;

namespace QuranDashboard.Tests.Quran.WordsRoots;

/// <summary>
/// US3 (T051): Root words — correct unique word ids and in-context occurrence
/// counts for both kinds; pagination.
/// </summary>
[Collection(nameof(RootsExplorerCollection))]
public sealed class RootsWordsReadTests(RootsExplorerTestFixture fixture)
{
    private const int HighFrequencyRootId = 10;

    [Fact]
    public async Task GetRootWords_simple_returns_correct_unique_ids_and_in_context_counts()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootWordsQuery(HighFrequencyRootId, RootWordKindKeys.Simple, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetRootWordsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);

        var byId = page.Items.ToDictionary(i => i.UniqueWordId);
        byId[1003].OccurrencesCount.Should().Be(2);
        byId[1003].Kind.Should().Be(RootWordKindKeys.Simple);
        byId[1004].OccurrencesCount.Should().Be(3);
        byId[1004].DisplayTextUthmani.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetRootWords_tashkeel_returns_correct_unique_ids_and_in_context_counts()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootWordsQuery(HighFrequencyRootId, RootWordKindKeys.Tashkeel, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetRootWordsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(i => i.Kind == RootWordKindKeys.Tashkeel);
    }

    [Fact]
    public async Task GetRootWords_pages_the_results()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootWordsHandler>();

        var first = await handler.HandleAsync(
            new GetRootWordsQuery(HighFrequencyRootId, RootWordKindKeys.Simple, 1, 1),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetRootWordsOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(2);
        firstPage.Items.Should().HaveCount(1);

        var second = await handler.HandleAsync(
            new GetRootWordsQuery(HighFrequencyRootId, RootWordKindKeys.Simple, 2, 1),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetRootWordsOutcome.Success>().Subject.Page;
        secondPage.Items.Should().HaveCount(1);
        secondPage.Items[0].UniqueWordId.Should().NotBe(firstPage.Items[0].UniqueWordId);
    }

    [Fact]
    public async Task GetRootWords_unknown_root_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootWordsQuery(999_999, RootWordKindKeys.Simple, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetRootWordsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetRootWords_invalid_kind_returns_invalid_kind()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootWordsQuery(HighFrequencyRootId, "invalid", 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetRootWordsOutcome.InvalidKind>();
    }
}
