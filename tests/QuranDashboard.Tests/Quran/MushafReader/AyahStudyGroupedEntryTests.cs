using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class AyahStudyGroupedEntryTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetAyahStudy_exposes_grouped_tafsir_metadata_for_fixture_ayah()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery("2:25", "ar-muyassar", null, null),
            CancellationToken.None);

        var tafsir = outcome.Should().BeOfType<GetAyahStudyOutcome.Success>().Subject.Response.Tafsir;
        tafsir.Should().NotBeNull();
        tafsir!.IsGroupLeader.Should().BeTrue();
        tafsir.SourceValueKind.Should().Be("leader");
        tafsir.CoveredAyahCount.Should().Be(2);
        tafsir.CoveredAyahKeys.Should().BeEquivalentTo("2:25", "2:26");
        tafsir.SourceLeaderVerseKey.Should().Be("2:25");
        tafsir.Text.Should().NotBeNullOrWhiteSpace();
    }
}
