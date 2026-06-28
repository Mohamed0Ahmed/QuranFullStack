using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;

namespace QuranDashboard.Tests.Quran.Words;

[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordSummaryTests(UniqueWordsTestFixture fixture)
{
    [Fact]
    public async Task GetSummary_tashkeel_returns_summary_with_counts_and_missing_invariant()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSummaryQuery("tashkeel", 1002),
            CancellationToken.None);

        var summary = outcome.Should().BeOfType<GetUniqueWordSummaryOutcome.Success>().Subject.Summary;
        summary.Id.Should().Be(1002);
        summary.Kind.Should().Be(UniqueWordKindKeys.Tashkeel);
        summary.DisplayText.Should().Be("ٱللَّهِ");
        summary.OccurrencesCount.Should().Be(5);
        summary.AyahsCount.Should().Be(5);
        summary.SurahsCount.Should().Be(5);
        summary.MissingSurahsCount.Should().Be(114 - summary.SurahsCount);
    }

    [Fact]
    public async Task GetSummary_simple_mode_uses_simple_link()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSummaryQuery("simple", 1001),
            CancellationToken.None);

        var summary = outcome.Should().BeOfType<GetUniqueWordSummaryOutcome.Success>().Subject.Summary;
        summary.Kind.Should().Be(UniqueWordKindKeys.Simple);
        summary.DisplayText.Should().Be("بسم");
        summary.MissingSurahsCount.Should().Be(114 - summary.SurahsCount);
    }

    [Fact]
    public async Task GetSummary_returns_display_text_for_each_kind_mode()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSummaryHandler>();

        var tashkeelOutcome = await handler.HandleAsync(
            new GetUniqueWordSummaryQuery("tashkeel", 1002),
            CancellationToken.None);
        var tashkeel = tashkeelOutcome.Should().BeOfType<GetUniqueWordSummaryOutcome.Success>().Subject.Summary;
        tashkeel.DisplayText.Should().Be("ٱللَّهِ");

        var simpleOutcome = await handler.HandleAsync(
            new GetUniqueWordSummaryQuery("simple", 2003),
            CancellationToken.None);
        var simple = simpleOutcome.Should().BeOfType<GetUniqueWordSummaryOutcome.Success>().Subject.Summary;
        simple.DisplayText.Should().Be("ءامنوا");
    }

    [Theory]
    [InlineData("not-a-kind")]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetSummary_invalid_kind_returns_validation_outcome(string? kind)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSummaryQuery(kind, 1002),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordSummaryOutcome.InvalidKind>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetSummary_invalid_id_returns_validation_outcome(int id)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSummaryQuery("tashkeel", id),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordSummaryOutcome.InvalidId>();
    }

    [Fact]
    public async Task GetSummary_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSummaryQuery("tashkeel", 999999),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordSummaryOutcome.NotFound>();
    }
}
