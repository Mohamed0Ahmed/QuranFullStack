using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemLemmas;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class StemsRelatedReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int MultiCandidateStemId = 602;
    private const int UnknownStemId = 999_999;

    [Fact]
    public async Task GetStemLemmas_returns_related_lemmas_in_deterministic_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemLemmasHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemLemmasQuery(MultiCandidateStemId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetStemLemmasOutcome.Success>().Subject.Lemmas;
        response.Id.Should().Be(MultiCandidateStemId);
        response.StemText.Should().Be("عَلِمَ");
        response.LemmasCount.Should().Be(2);
        response.Lemmas.Select(l => l.LemmaId).Should().Equal(502, 504);
        response.Lemmas[0].LemmaText.Should().Be("عِلْم");
        response.Lemmas[0].LemmaBuckwalter.Should().Be("Ailm");
        response.Lemmas[0].OccurrencesCount.Should().Be(3);
        response.Lemmas[1].LemmaText.Should().Be("مَعْرِفَة");
        response.Lemmas[1].LemmaBuckwalter.Should().Be("maArifap");
        response.Lemmas[1].OccurrencesCount.Should().Be(1);
    }

    [Fact]
    public void OrderStemLemmas_places_higher_counts_before_earlier_first_occurrences()
    {
        var items = MorphologyRelatedItemsOrdering.OrderStemLemmas(
            [
                (504, "مَعْرِفَة", "maArifap", 100),
                (502, "عِلْم", "Ailm", 200),
                (502, "عِلْم", "Ailm", 300),
            ]);

        items.Select(item => item.LemmaId).Should().Equal(502, 504);
        items.Select(item => item.OccurrencesCount).Should().Equal(2, 1);
    }

    [Fact]
    public async Task GetStemLemmas_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemLemmasHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemLemmasQuery(UnknownStemId),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemLemmasOutcome.NotFound>();
    }

    [Fact]
    public async Task GetStemLemmas_repeated_read_issues_no_new_db_commands_after_cache()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfStemsReader(dbContext);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedStemsReader(inner, cache);

        await reader.GetStemLemmasAsync(MultiCandidateStemId, CancellationToken.None);

        interceptor.Reset();
        var second = await reader.GetStemLemmasAsync(MultiCandidateStemId, CancellationToken.None);

        second.Should().NotBeNull();
        interceptor.CommandCount.Should().Be(0);
    }
}
