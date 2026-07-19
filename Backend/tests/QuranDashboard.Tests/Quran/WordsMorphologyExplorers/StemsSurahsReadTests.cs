using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMissingSurahs;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class StemsSurahsReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int HighFrequencyStemId = 600;
    private const int SegmentFanoutStemId = 606;
    private const int UnknownStemId = 999_999;

    [Fact]
    public async Task GetStemMentionedSurahs_returns_correct_per_surah_counts_with_arabic_names()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemMentionedSurahsQuery(HighFrequencyStemId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetStemMentionedSurahsOutcome.Success>().Subject.Surahs;
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
    public async Task GetStemMentionedSurahs_counts_segment_membership_for_secondary_and_fanout_rows()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemMentionedSurahsQuery(SegmentFanoutStemId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetStemMentionedSurahsOutcome.Success>().Subject.Surahs;
        response.Surahs.Should().HaveCount(2);

        var byNumber = response.Surahs.ToDictionary(s => s.SurahNumber);
        byNumber[2].OccurrencesInSurah.Should().Be(3);
        byNumber[3].OccurrencesInSurah.Should().Be(1);
        byNumber[2].NameArabic.Should().Be("البقرة");
        byNumber[3].NameArabic.Should().Be("آل عمران");
    }

    [Fact]
    public async Task GetStemMentionedSurahs_returns_mushaf_ordered_surahs()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemMentionedSurahsQuery(HighFrequencyStemId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetStemMentionedSurahsOutcome.Success>().Subject.Surahs;
        response.Surahs.Select(s => s.SurahNumber).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetStem_mentioned_and_missing_sets_are_disjoint_and_union_to_114()
    {
        await using var scope = fixture.CreateScope();
        var mentionedHandler = scope.ServiceProvider.GetRequiredService<GetStemMentionedSurahsHandler>();
        var missingHandler = scope.ServiceProvider.GetRequiredService<GetStemMissingSurahsHandler>();

        var mentioned = await mentionedHandler.HandleAsync(
            new GetStemMentionedSurahsQuery(HighFrequencyStemId),
            CancellationToken.None);
        var missing = await missingHandler.HandleAsync(
            new GetStemMissingSurahsQuery(HighFrequencyStemId),
            CancellationToken.None);

        var mentionedResponse = mentioned.Should().BeOfType<GetStemMentionedSurahsOutcome.Success>().Subject.Surahs;
        var missingResponse = missing.Should().BeOfType<GetStemMissingSurahsOutcome.Success>().Subject.MissingSurahs;

        var mentionedNumbers = mentionedResponse.Surahs.Select(s => s.SurahNumber).ToList();
        var missingNumbers = missingResponse.Surahs.Select(s => s.SurahNumber).ToList();

        mentionedNumbers.Should().NotIntersectWith(missingNumbers);
        (mentionedResponse.Surahs.Count + missingResponse.Surahs.Count).Should().Be(114);
    }

    [Fact]
    public async Task GetStemMissingSurahs_returns_all_complement_surahs_in_mushaf_order()
    {
        await using var scope = fixture.CreateScope();
        var missingHandler = scope.ServiceProvider.GetRequiredService<GetStemMissingSurahsHandler>();

        var outcome = await missingHandler.HandleAsync(
            new GetStemMissingSurahsQuery(HighFrequencyStemId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetStemMissingSurahsOutcome.Success>().Subject.MissingSurahs;
        response.Surahs.Should().HaveCount(111);
        response.Surahs.Select(s => s.SurahNumber).Should().BeInAscendingOrder();
        response.Surahs.Select(s => s.SurahNumber).Should().NotContain([1, 2, 3]);
        response.Surahs.First().NameArabic.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetStemMentionedSurahs_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemMentionedSurahsQuery(UnknownStemId),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemMentionedSurahsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetStemMissingSurahs_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMissingSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemMissingSurahsQuery(UnknownStemId),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemMissingSurahsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetStemMentionedSurahs_invalid_id_returns_validation_outcome()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMentionedSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemMentionedSurahsQuery(0),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemMentionedSurahsOutcome.InvalidId>();
    }

    [Fact]
    public async Task GetStemMissingSurahs_invalid_id_returns_validation_outcome()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMissingSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemMissingSurahsQuery(0),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemMissingSurahsOutcome.InvalidId>();
    }

    [Theory]
    [InlineData("mentioned")]
    [InlineData("missing")]
    public async Task GetStem_surah_read_repeated_call_hits_cache(string view)
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

        if (view == "mentioned")
        {
            await reader.GetStemMentionedSurahsAsync(HighFrequencyStemId, CancellationToken.None);
            interceptor.Reset();
            var second = await reader.GetStemMentionedSurahsAsync(HighFrequencyStemId, CancellationToken.None);
            second.Should().NotBeNull();
        }
        else
        {
            await reader.GetStemMissingSurahsAsync(HighFrequencyStemId, CancellationToken.None);
            interceptor.Reset();
            var second = await reader.GetStemMissingSurahsAsync(HighFrequencyStemId, CancellationToken.None);
            second.Should().NotBeNull();
        }

        interceptor.CommandCount.Should().Be(0);
    }
}
