using FluentAssertions;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class AyahStudyMissingSourceTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetAyahStudy_unknown_source_yields_null_block_and_null_selected_key_without_substitution()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery("2:25", null, null, "does-not-exist"),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahStudyOutcome.Success>().Subject.Response;
        response.FullI3rab.Should().BeNull();
        response.SelectedSources.FullI3rabSource.Should().BeNull();
        response.Tafsir.Should().NotBeNull();
        response.Translation.Should().NotBeNull();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("2")]
    [InlineData("surah:ayah")]
    public async Task GetAyahStudy_malformed_verse_key_returns_invalid_outcome(string verseKey)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery(verseKey, null, null, null),
            CancellationToken.None);

        outcome.Should().BeOfType<GetAyahStudyOutcome.InvalidVerseKey>();
    }

    [Fact]
    public async Task GetAyahStudy_unknown_verse_key_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery("999:999", null, null, null),
            CancellationToken.None);

        outcome.Should().BeOfType<GetAyahStudyOutcome.NotFound>();
    }
}
