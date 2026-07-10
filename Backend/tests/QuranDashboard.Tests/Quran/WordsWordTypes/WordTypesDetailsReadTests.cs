using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesDetailsReadTests(WordTypesTestFixture fixture)
{
    private const int KalimaTashkeelId = 191001;
    private const int AlimaPastTashkeelId = 191003;
    private const int AlimaPassiveTashkeelId = 191004;
    private const int MuthalTashkeelId = 191008;

    [Fact]
    public async Task GetSummary_returns_row_context_summary_for_known_identity()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var summary = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "N", null, null, null),
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.TashkeelWordId.Should().Be(KalimaTashkeelId);
        summary.ContextCode.Should().Be("N");
        summary.OccurrencesCount.Should().Be(1);
        summary.AyahsCount.Should().Be(1);
        summary.SurahsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSummary_returns_null_for_unknown_identity()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var summary = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(999_999, "N", null, null, null),
            CancellationToken.None);

        summary.Should().BeNull();
    }

    [Fact]
    public async Task GetAyahMatches_scopes_to_row_context_not_all_spellings()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var nounAyahs = await reader.GetAyahMatchesAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "N", null, null, null),
            1,
            25,
            CancellationToken.None);
        var properNounAyahs = await reader.GetAyahMatchesAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "PN", null, null, null),
            1,
            25,
            CancellationToken.None);

        nounAyahs.Should().NotBeNull();
        properNounAyahs.Should().NotBeNull();
        nounAyahs!.TotalCount.Should().Be(1);
        properNounAyahs!.TotalCount.Should().Be(1);
        nounAyahs.Items.Single().VerseKey.Should().Be("1:1");
        properNounAyahs.Items.Single().VerseKey.Should().Be("1:1");
        nounAyahs.Items.Single().MatchedWordPositions.Should().Equal([1]);
        properNounAyahs.Items.Single().MatchedWordPositions.Should().Equal([2]);
    }

    [Fact]
    public async Task GetSurahs_returns_mentioned_and_missing_for_row_context()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var surahs = await reader.GetSurahsAsync(
            new WordTypeRowIdentity(AlimaPastTashkeelId, "past", null, null, null),
            CancellationToken.None);

        surahs.Should().NotBeNull();
        surahs!.Surahs.Should().ContainSingle(item => item.SurahNumber == 2);
        surahs.MissingSurahs.Select(item => item.SurahNumber).Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public async Task VerbContexts_do_not_widen_across_tense_or_voice()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var pastSummary = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(AlimaPastTashkeelId, "past", null, null, null),
            CancellationToken.None);
        var passiveSummary = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(AlimaPassiveTashkeelId, "present", null, null, null),
            CancellationToken.None);

        pastSummary.Should().NotBeNull();
        passiveSummary.Should().NotBeNull();
        pastSummary!.OccurrencesCount.Should().Be(1);
        passiveSummary!.OccurrencesCount.Should().Be(1);
    }

    [Fact]
    public async Task Handlers_map_invalid_identity_to_bad_request_and_unknown_to_not_found()
    {
        await using var scope = fixture.CreateScope();
        var summaryHandler = scope.ServiceProvider.GetRequiredService<GetWordTypeSummaryHandler>();
        var ayahsHandler = scope.ServiceProvider.GetRequiredService<GetWordTypeAyahsHandler>();
        var surahsHandler = scope.ServiceProvider.GetRequiredService<GetWordTypeSurahsHandler>();

        (await summaryHandler.HandleAsync(
            new GetWordTypeSummaryQuery(0, "N", null, null, null),
            CancellationToken.None)).Should().BeOfType<GetWordTypeSummaryOutcome.InvalidIdentity>();

        (await ayahsHandler.HandleAsync(
            new GetWordTypeAyahsQuery(KalimaTashkeelId, "", null, null, null, 1, 25),
            CancellationToken.None)).Should().BeOfType<GetWordTypeAyahsOutcome.InvalidIdentity>();

        (await summaryHandler.HandleAsync(
            new GetWordTypeSummaryQuery(999_999, "N", null, null, null),
            CancellationToken.None)).Should().BeOfType<GetWordTypeSummaryOutcome.NotFound>();

        (await ayahsHandler.HandleAsync(
            new GetWordTypeAyahsQuery(999_999, "N", null, null, null, 1, 25),
            CancellationToken.None)).Should().BeOfType<GetWordTypeAyahsOutcome.NotFound>();

        (await surahsHandler.HandleAsync(
            new GetWordTypeSurahsQuery(999_999, "N", null, null, null),
            CancellationToken.None)).Should().BeOfType<GetWordTypeSurahsOutcome.NotFound>();
    }

    [Fact]
    public async Task UnspecifiedVerbContext_does_not_widen_to_non_verb_occurrences()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var verbSummary = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(MuthalTashkeelId, WordTypeRowContext.Unspecified, null, null, null),
            CancellationToken.None);
        var nounSummary = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(MuthalTashkeelId, "N", null, null, null),
            CancellationToken.None);

        verbSummary.Should().NotBeNull();
        nounSummary.Should().NotBeNull();
        verbSummary!.OccurrencesCount.Should().Be(1);
        nounSummary!.OccurrencesCount.Should().Be(1);

        var verbAyahs = await reader.GetAyahMatchesAsync(
            new WordTypeRowIdentity(MuthalTashkeelId, WordTypeRowContext.Unspecified, null, null, null),
            1,
            25,
            CancellationToken.None);
        var nounAyahs = await reader.GetAyahMatchesAsync(
            new WordTypeRowIdentity(MuthalTashkeelId, "N", null, null, null),
            1,
            25,
            CancellationToken.None);

        verbAyahs.Should().NotBeNull();
        nounAyahs.Should().NotBeNull();
        verbAyahs!.Items.Single().VerseKey.Should().Be("1:2");
        nounAyahs!.Items.Single().VerseKey.Should().Be("1:1");
        verbAyahs.Items.Single().MatchedWordPositions.Should().Equal([3]);
        nounAyahs.Items.Single().MatchedWordPositions.Should().Equal([3]);
    }

    [Fact]
    public async Task Direct_reader_ayah_load_uses_bounded_queries()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboard.Infrastructure.Persistence.QuranDashboardDbContext>();
        var reader = new EfWordTypesReader(dbContext);

        var page = await reader.GetAyahMatchesAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "N", null, null, null),
            1,
            25,
            CancellationToken.None);

        page.Should().NotBeNull();
        page!.Items.Should().NotBeEmpty();
        page.Items.Single().Words.Should().NotBeEmpty();
        page.Items.Single().MatchedWordIds.Should().NotBeEmpty();
    }
}
