using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesRowIdentityTests(WordTypesTestFixture fixture)
{
    private const int KalimaTashkeelId = 191001;
    private const int AlimaPassiveTashkeelId = 191004;

    public static TheoryData<int, string, string?, string?, string?, string, int> ExactIdentityRows => new()
    {
        { KalimaTashkeelId, "N", "nominative", null, null, "1:1", 1 },
        { KalimaTashkeelId, "PN", "genitive", null, null, "1:1", 2 },
        { AlimaPassiveTashkeelId, "present", null, "present", "passive", "2:25", 2 },
    };

    [Theory]
    [MemberData(nameof(ExactIdentityRows))]
    public async Task Exact_row_identity_keeps_selected_context_scoped_to_one_row(
        int tashkeelWordId,
        string contextCode,
        string? caseValue,
        string? tense,
        string? voice,
        string verseKey,
        int matchedWordPosition)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var identity = new WordTypeRowIdentity(tashkeelWordId, contextCode, caseValue, tense, voice);

        var summary = await reader.GetSummaryAsync(identity, CancellationToken.None);
        var ayahs = await reader.GetAyahMatchesAsync(identity, 1, 25, CancellationToken.None);
        var surahs = await reader.GetSurahsAsync(identity, CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.TashkeelWordId.Should().Be(tashkeelWordId);
        summary.ContextCode.Should().Be(contextCode);
        summary.OccurrencesCount.Should().Be(1);

        ayahs.Should().NotBeNull();
        ayahs!.TotalCount.Should().Be(1);
        ayahs.Items.Single().VerseKey.Should().Be(verseKey);
        ayahs.Items.Single().MatchedWordPositions.Should().Equal([matchedWordPosition]);

        surahs.Should().NotBeNull();
        surahs!.Surahs.Should().ContainSingle();
    }

    [Fact]
    public async Task Same_spelling_different_context_rows_do_not_widen_to_all_usages()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var nounIdentity = new WordTypeRowIdentity(KalimaTashkeelId, "N", "nominative", null, null);
        var properNounIdentity = new WordTypeRowIdentity(KalimaTashkeelId, "PN", "genitive", null, null);

        var nounSummary = await reader.GetSummaryAsync(nounIdentity, CancellationToken.None);
        var properNounSummary = await reader.GetSummaryAsync(properNounIdentity, CancellationToken.None);
        var nounAyahs = await reader.GetAyahMatchesAsync(nounIdentity, 1, 25, CancellationToken.None);
        var properNounAyahs = await reader.GetAyahMatchesAsync(properNounIdentity, 1, 25, CancellationToken.None);

        nounSummary.Should().NotBeNull();
        properNounSummary.Should().NotBeNull();
        nounSummary!.ContextCode.Should().Be("N");
        properNounSummary!.ContextCode.Should().Be("PN");
        nounSummary.OccurrencesCount.Should().Be(1);
        properNounSummary.OccurrencesCount.Should().Be(1);

        nounAyahs.Should().NotBeNull();
        properNounAyahs.Should().NotBeNull();
        nounAyahs!.Items.Single().MatchedWordPositions.Should().Equal([1]);
        properNounAyahs!.Items.Single().MatchedWordPositions.Should().Equal([2]);
    }
}
