using FluentAssertions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetSimilarAyahs;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class SimilarAyahsReadTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetSimilarAyahs_returns_empty_list_for_ayah_without_links()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSimilarAyahsHandler>();

        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery("1:1"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetSimilarAyahsOutcome.Success>().Subject.Response;
        response.VerseKey.Should().Be("1:1");
        response.Count.Should().Be(0);
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSimilarAyahs_includes_outgoing_incoming_and_bidirectional_links_once_each()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSimilarAyahsHandler>();

        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetSimilarAyahsOutcome.Success>().Subject.Response;
        response.Count.Should().Be(2);
        response.Items.Should().HaveCount(2);
        response.Items.Select(item => item.TargetVerseKey).Should().BeEquivalentTo(["2:26", "1:2"]);
    }

    [Fact]
    public async Task GetSimilarAyahs_deduplicates_bidirectional_links_into_one_item()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSimilarAyahsHandler>();

        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetSimilarAyahsOutcome.Success>().Subject.Response;
        var bidirectionalItem = response.Items.Single(item => item.TargetVerseKey == "2:26");

        bidirectionalItem.RelationshipDirection.Should().Be(SimilarAyahRelationshipDirections.Bidirectional);
        bidirectionalItem.HasReverseLink.Should().BeTrue();
        bidirectionalItem.Score.Should().Be(80);
    }

    [Fact]
    public async Task GetSimilarAyahs_sorts_by_score_then_mushaf_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSimilarAyahsHandler>();

        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetSimilarAyahsOutcome.Success>().Subject.Response;
        response.Items.Select(item => item.TargetVerseKey).Should().ContainInOrder("2:26", "1:2");
    }

    [Fact]
    public async Task GetSimilarAyahs_uses_canonical_ayah_text_not_link_payload()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSimilarAyahsHandler>();

        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetSimilarAyahsOutcome.Success>().Subject.Response;
        var item226 = response.Items.Single(item => item.TargetVerseKey == "2:26");
        var item12 = response.Items.Single(item => item.TargetVerseKey == "1:2");

        item226.TextUthmani.Should().Be("ٱللَّهُ لَا إِلَٰهَ إِلَّا هُوَ ٱلْحَىُّ ٱلْقَيُّومُ");
        item12.TextUthmani.Should().Be("ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَٰلَمِينَ");
        item226.SurahNameArabic.Should().Be("البقرة");
        item12.SurahNameArabic.Should().Be("الفاتحة");
        item226.PageNumber.Should().Be(5);
        item12.PageNumber.Should().Be(1);
    }
}
