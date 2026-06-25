using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMissingSurahs;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class LemmasSurahsReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int HighFrequencyLemmaId = 500;
    private const int UnknownLemmaId = 999_999;

    [Fact]
    public async Task GetLemmaMentionedSurahs_returns_correct_per_surah_counts_with_arabic_names()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaMentionedSurahsQuery(HighFrequencyLemmaId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetLemmaMentionedSurahsOutcome.Success>().Subject.Surahs;
        response.Id.Should().Be(HighFrequencyLemmaId);
        response.LemmaText.Should().Be("كَلِمَة");
        response.SurahsCount.Should().Be(3);
        response.Surahs.Should().HaveCount(3);

        var byNumber = response.Surahs.ToDictionary(s => s.SurahNumber);
        byNumber.Keys.Should().BeEquivalentTo([1, 2, 3]);
        byNumber[1].OccurrencesInSurah.Should().Be(6);
        byNumber[2].OccurrencesInSurah.Should().Be(2);
        byNumber[3].OccurrencesInSurah.Should().Be(3);
        byNumber[1].NameArabic.Should().Be("الفاتحة");
        byNumber[2].NameArabic.Should().Be("البقرة");
        byNumber[3].NameArabic.Should().Be("آل عمران");
    }

    [Fact]
    public async Task GetLemmaMentionedSurahs_returns_mushaf_ordered_surahs()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaMentionedSurahsQuery(HighFrequencyLemmaId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetLemmaMentionedSurahsOutcome.Success>().Subject.Surahs;
        response.Surahs.Select(s => s.SurahNumber).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetLemma_mentioned_and_missing_sets_are_disjoint_and_union_to_114()
    {
        await using var scope = fixture.CreateScope();
        var mentionedHandler = scope.ServiceProvider.GetRequiredService<GetLemmaMentionedSurahsHandler>();
        var missingHandler = scope.ServiceProvider.GetRequiredService<GetLemmaMissingSurahsHandler>();

        var mentioned = await mentionedHandler.HandleAsync(
            new GetLemmaMentionedSurahsQuery(HighFrequencyLemmaId),
            CancellationToken.None);
        var missing = await missingHandler.HandleAsync(
            new GetLemmaMissingSurahsQuery(HighFrequencyLemmaId),
            CancellationToken.None);

        var mentionedResponse = mentioned.Should().BeOfType<GetLemmaMentionedSurahsOutcome.Success>().Subject.Surahs;
        var missingResponse = missing.Should().BeOfType<GetLemmaMissingSurahsOutcome.Success>().Subject.MissingSurahs;

        var mentionedNumbers = mentionedResponse.Surahs.Select(s => s.SurahNumber).ToList();
        var missingNumbers = missingResponse.Surahs.Select(s => s.SurahNumber).ToList();

        mentionedNumbers.Should().NotIntersectWith(missingNumbers);
        (mentionedResponse.SurahsCount + missingResponse.MissingSurahsCount).Should().Be(114);
        missingResponse.Id.Should().Be(HighFrequencyLemmaId);
        missingResponse.LemmaText.Should().Be("كَلِمَة");
    }

    [Fact]
    public async Task GetLemmaMissingSurahs_returns_all_complement_surahs_in_mushaf_order()
    {
        await using var scope = fixture.CreateScope();
        var missingHandler = scope.ServiceProvider.GetRequiredService<GetLemmaMissingSurahsHandler>();

        var outcome = await missingHandler.HandleAsync(
            new GetLemmaMissingSurahsQuery(HighFrequencyLemmaId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetLemmaMissingSurahsOutcome.Success>().Subject.MissingSurahs;
        response.MissingSurahsCount.Should().Be(111);
        response.Surahs.Should().HaveCount(111);
        response.Surahs.Select(s => s.SurahNumber).Should().BeInAscendingOrder();
        response.Surahs.Select(s => s.SurahNumber).Should().NotContain([1, 2, 3]);
        response.Surahs.First().NameArabic.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLemmaMentionedSurahs_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaMentionedSurahsQuery(UnknownLemmaId),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaMentionedSurahsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetLemmaMissingSurahs_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaMissingSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaMissingSurahsQuery(UnknownLemmaId),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaMissingSurahsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetLemmaMentionedSurahs_invalid_id_returns_validation_outcome()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaMentionedSurahsQuery(0),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaMentionedSurahsOutcome.InvalidId>();
    }

    [Fact]
    public async Task GetLemmaMissingSurahs_invalid_id_returns_validation_outcome()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaMissingSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaMissingSurahsQuery(0),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaMissingSurahsOutcome.InvalidId>();
    }

    [Theory]
    [InlineData("mentioned")]
    [InlineData("missing")]
    public async Task GetLemma_surah_read_repeated_call_hits_cache(string view)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfLemmasReader(dbContext);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedLemmasReader(inner, cache);

        if (view == "mentioned")
        {
            await reader.GetLemmaMentionedSurahsAsync(HighFrequencyLemmaId, CancellationToken.None);
            interceptor.Reset();
            var second = await reader.GetLemmaMentionedSurahsAsync(HighFrequencyLemmaId, CancellationToken.None);
            second.Should().NotBeNull();
        }
        else
        {
            await reader.GetLemmaMissingSurahsAsync(HighFrequencyLemmaId, CancellationToken.None);
            interceptor.Reset();
            var second = await reader.GetLemmaMissingSurahsAsync(HighFrequencyLemmaId, CancellationToken.None);
            second.Should().NotBeNull();
        }

        interceptor.CommandCount.Should().Be(0);
    }
}
