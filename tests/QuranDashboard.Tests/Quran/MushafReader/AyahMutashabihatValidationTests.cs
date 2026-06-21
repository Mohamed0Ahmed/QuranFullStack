using FluentAssertions;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahMutashabihat;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class AyahMutashabihatValidationTests(MushafReaderTestFixture fixture)
{
    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("2")]
    [InlineData("2:abc")]
    [InlineData("surah:1")]
    public async Task GetAyahMutashabihat_rejects_malformed_verse_key(string verseKey)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahMutashabihatHandler>();

        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery(verseKey), CancellationToken.None);

        outcome.Should().BeOfType<GetAyahMutashabihatOutcome.InvalidVerseKey>();
    }

    [Fact]
    public async Task GetAyahMutashabihat_returns_not_found_for_unknown_verse_key()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahMutashabihatHandler>();

        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery("9:9"), CancellationToken.None);

        outcome.Should().BeOfType<GetAyahMutashabihatOutcome.NotFound>();
    }
}
