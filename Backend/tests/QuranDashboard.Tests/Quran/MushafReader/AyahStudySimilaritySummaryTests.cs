using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class AyahStudySimilaritySummaryTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetAyahStudy_includes_similarity_summary_counts_for_ayah_with_data()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery("2:25", null, null, null),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahStudyOutcome.Success>().Subject.Response;
        response.SimilaritySummary.SimilarAyahCount.Should().Be(2);
        response.SimilaritySummary.MutashabihatGroupCount.Should().Be(2);
        response.SimilaritySummary.MutashabihatOccurrenceCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAyahStudy_returns_zero_similarity_counts_for_ayah_without_data()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery("1:1", null, null, null),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahStudyOutcome.Success>().Subject.Response;
        response.SimilaritySummary.SimilarAyahCount.Should().Be(0);
        response.SimilaritySummary.MutashabihatGroupCount.Should().Be(0);
        response.SimilaritySummary.MutashabihatOccurrenceCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAyahStudy_deduplicates_bidirectional_similar_links_in_count()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery("2:25", null, null, null),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahStudyOutcome.Success>().Subject.Response;
        response.SimilaritySummary.SimilarAyahCount.Should().BeLessThan(3,
            "outgoing 2:25->2:26 and incoming 2:26->2:25 must dedupe to one related ayah");
    }

    [Fact]
    public async Task GetAyahStudy_includes_similarity_summary_even_when_study_sources_are_null()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery("2:25", "missing-tafsir", "missing-translation", "missing-i3rab"),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahStudyOutcome.Success>().Subject.Response;
        response.Tafsir.Should().BeNull();
        response.Translation.Should().BeNull();
        response.FullI3rab.Should().BeNull();
        response.SimilaritySummary.SimilarAyahCount.Should().Be(2);
        response.SimilaritySummary.MutashabihatGroupCount.Should().Be(2);
        response.SimilaritySummary.MutashabihatOccurrenceCount.Should().Be(3);
    }
}
