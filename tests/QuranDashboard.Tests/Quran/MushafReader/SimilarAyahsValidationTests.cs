using FluentAssertions;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetSimilarAyahs;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class SimilarAyahsValidationTests(MushafReaderTestFixture fixture)
{
    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("2")]
    [InlineData("2:abc")]
    [InlineData("surah:1")]
    public async Task GetSimilarAyahs_rejects_malformed_verse_key(string verseKey)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSimilarAyahsHandler>();

        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery(verseKey), CancellationToken.None);

        outcome.Should().BeOfType<GetSimilarAyahsOutcome.InvalidVerseKey>();
    }

    [Fact]
    public async Task GetSimilarAyahs_returns_not_found_for_unknown_verse_key()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSimilarAyahsHandler>();

        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery("9:9"), CancellationToken.None);

        outcome.Should().BeOfType<GetSimilarAyahsOutcome.NotFound>();
    }
}
