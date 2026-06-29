using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaStems;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemLemmas;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class MorphologyRelationshipsReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int HighFrequencyLemmaId = 500;
    private const int SameLemmaFanoutLemmaId = 508;
    private const int MultiCandidateStemId = 602;
    private const int UnknownLemmaId = 999_999;
    private const int UnknownStemId = 999_999;

    [Fact]
    public async Task GetLemmaStems_returns_related_stems_in_deterministic_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaStemsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaStemsQuery(HighFrequencyLemmaId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetLemmaStemsOutcome.Success>().Subject.Stems;
        response.Stems.Should().ContainSingle();
        response.Stems[0].StemId.Should().Be(600);
        response.Stems[0].StemText.Should().Be("كَلَّمَ");
        response.Stems[0].OccurrencesCount.Should().Be(11);
    }

    [Fact]
    public async Task GetLemmaStems_segment_only_lemma_uses_matched_words_word_level_stem()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaStemsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaStemsQuery(SameLemmaFanoutLemmaId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetLemmaStemsOutcome.Success>().Subject.Stems;
        response.Stems.Should().ContainSingle();
        response.Stems[0].StemId.Should().Be(606);
        response.Stems[0].StemText.Should().Be("سَاق-تَجْرِيبِيّ");
        response.Stems[0].OccurrencesCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLemmaStems_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaStemsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaStemsQuery(UnknownLemmaId),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaStemsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetLemmaStems_repeated_read_issues_no_new_db_commands_after_cache()
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

        await reader.GetLemmaStemsAsync(HighFrequencyLemmaId, CancellationToken.None);

        interceptor.Reset();
        var second = await reader.GetLemmaStemsAsync(HighFrequencyLemmaId, CancellationToken.None);

        second.Should().NotBeNull();
        interceptor.CommandCount.Should().Be(0);
    }

    [Fact]
    public async Task GetStemLemmas_returns_related_lemmas_in_deterministic_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemLemmasHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemLemmasQuery(MultiCandidateStemId),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetStemLemmasOutcome.Success>().Subject.Lemmas;
        response.Lemmas.Should().HaveCount(2);
        response.Lemmas.Select(l => l.LemmaId).Should().Equal(502, 504);
        response.Lemmas[0].LemmaText.Should().Be("عِلْم");
        response.Lemmas[0].OccurrencesCount.Should().Be(3);
        response.Lemmas[1].LemmaText.Should().Be("مَعْرِفَة");
        response.Lemmas[1].OccurrencesCount.Should().Be(1);
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
