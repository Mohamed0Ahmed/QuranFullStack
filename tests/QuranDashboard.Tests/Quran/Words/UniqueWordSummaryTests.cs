using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;

namespace QuranDashboard.Tests.Quran.Words;

/// <summary>
/// US4 selected unique-word summary read: used to restore modal state from a
/// shared URL. Asserts the summary shape, the missing-surah invariant, both
/// modes, invalid kind, and unknown ID.
/// </summary>
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
        summary.DisplayTextUthmani.Should().Be("ٱللَّهِ");
        summary.OccurrencesCount.Should().Be(5);
        summary.AyahsCount.Should().Be(5);
        summary.SurahsCount.Should().Be(5);
        summary.MissingSurahsCount.Should().Be(114 - summary.SurahsCount);
        summary.FirstVerseKey.Should().Be("1:1");
        summary.FirstLocation.Should().Be("1:1:2");
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
        summary.DisplayTextUthmani.Should().Be("بِسْمِ");
        summary.MissingSurahsCount.Should().Be(114 - summary.SurahsCount);
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
