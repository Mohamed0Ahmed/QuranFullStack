using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootLemmas;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootStems;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

namespace QuranDashboard.Tests.Quran.WordsRoots;

[Collection(nameof(RootsExplorerCollection))]
public sealed class RootsLemmasStemsReadTests(RootsExplorerTestFixture fixture)
{
    private const int DivergentRootId = 10;
    private const int MultiStemRootId = 20;

    [Fact]
    public async Task GetRootLemmas_count_equals_list_lemmasCount_for_divergent_root()
    {
        await using var scope = fixture.CreateScope();
        var listHandler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();
        var lemmasHandler = scope.ServiceProvider.GetRequiredService<GetRootLemmasHandler>();

        var listOutcome = await listHandler.HandleAsync(
            new GetRootsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var listPage = listOutcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;
        var listItem = listPage.Items.Single(i => i.Id == DivergentRootId);

        var lemmasOutcome = await lemmasHandler.HandleAsync(
            new GetRootLemmasQuery(DivergentRootId),
            CancellationToken.None);
        var lemmas = lemmasOutcome.Should().BeOfType<GetRootLemmasOutcome.Success>().Subject.Lemmas;

        lemmas.LemmasCount.Should().Be(listItem.LemmasCount);
        lemmas.Lemmas.Should().HaveCount(listItem.LemmasCount);
        lemmas.LemmasCount.Should().Be(2);

        var byId = lemmas.Lemmas.ToDictionary(l => l.LemmaId);
        byId[100].OccurrencesCount.Should().Be(2);
        byId[101].OccurrencesCount.Should().Be(3);
    }

    [Fact]
    public async Task GetRootStems_returns_correct_distinct_stems_and_counts()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootStemsHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootStemsQuery(MultiStemRootId),
            CancellationToken.None);

        var stems = outcome.Should().BeOfType<GetRootStemsOutcome.Success>().Subject.Stems;
        stems.StemsCount.Should().Be(2);
        stems.Stems.Should().HaveCount(2);
        stems.Stems.Select(s => s.StemId).Should().BeEquivalentTo([200, 201]);
        stems.Stems.Should().OnlyContain(s => s.OccurrencesCount == 1);
    }

    [Fact]
    public async Task GetRootLemmas_and_stems_are_whole_lists_without_paging()
    {
        await using var scope = fixture.CreateScope();
        var listHandler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();
        var lemmasHandler = scope.ServiceProvider.GetRequiredService<GetRootLemmasHandler>();
        var stemsHandler = scope.ServiceProvider.GetRequiredService<GetRootStemsHandler>();

        var listOutcome = await listHandler.HandleAsync(
            new GetRootsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var listItem = listOutcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page.Items
            .Single(i => i.Id == DivergentRootId);

        var lemmasOutcome = await lemmasHandler.HandleAsync(
            new GetRootLemmasQuery(DivergentRootId),
            CancellationToken.None);
        var stemsOutcome = await stemsHandler.HandleAsync(
            new GetRootStemsQuery(DivergentRootId),
            CancellationToken.None);

        var lemmas = lemmasOutcome.Should().BeOfType<GetRootLemmasOutcome.Success>().Subject.Lemmas;
        var stems = stemsOutcome.Should().BeOfType<GetRootStemsOutcome.Success>().Subject.Stems;

        lemmas.LemmasCount.Should().Be(listItem.LemmasCount);
        lemmas.Lemmas.Should().HaveCount(listItem.LemmasCount);
        stems.StemsCount.Should().Be(listItem.StemsCount);
        stems.Stems.Should().HaveCount(listItem.StemsCount);
    }
}
