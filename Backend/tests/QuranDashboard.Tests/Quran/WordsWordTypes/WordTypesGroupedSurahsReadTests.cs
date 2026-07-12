using QuranDashboard.Api.Controllers.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

// Grouped scoped surahs: single-shot mentioned + missing surah lists for the selected numeric head
// root/stem/lemma. Occurrence counts are aggregated inside PostgreSQL over the same dimension-filtered
// BaseRowsSql base as the summary/words/ayahs at head-word grain; the mentioned/missing split is derived
// against the surah catalogue in numeric order. No paging contract.
[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesGroupedSurahsReadTests(WordTypesTestFixture fixture)
{
    private static readonly WordTypeFilter NounScope = new("noun", null, null, null, null);
    private static readonly WordTypeFilter VerbScope = new("verb", null, null, null, null);

    // Root 190700 / stem 190600 / lemma 190500 sit on the same three noun-scope head words, all in surah 1
    // (الفاتحة). So each resolves to a single mentioned surah of 3 occurrences and misses surahs 2 and 3.
    [Theory]
    [InlineData(WordTypeGroupedDimensionKind.Root, 190700)]
    [InlineData(WordTypeGroupedDimensionKind.Stem, 190600)]
    [InlineData(WordTypeGroupedDimensionKind.Lemma, 190500)]
    public async Task GroupedSurahs_RootStemLemma_ReturnScopedMentionedAndMissingSurahs(
        WordTypeGroupedDimensionKind kind,
        int dimensionId)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var response = await SurahsAsync(reader, kind, dimensionId, NounScope);

        response.Should().NotBeNull();
        var mentioned = response!.Surahs.Should().ContainSingle().Subject;
        mentioned.SurahNumber.Should().Be(1);
        mentioned.NameArabic.Should().Be("الفاتحة");
        mentioned.OccurrencesCount.Should().Be(3);

        response.MissingSurahs.Select(surah => surah.SurahNumber).Should().Equal(2, 3);
        response.MissingSurahs.Select(surah => surah.NameArabic).Should().Equal("البقرة", "آل عمران");
    }

    // Active secondary filters re-scope the base before aggregating, so occurrence counts and the
    // mentioned/missing split follow the narrowed rows, not the dimension's global totals.
    [Fact]
    public async Task GroupedSurahs_ActiveFiltersNarrowOccurrenceCounts()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        // Verb root 190701 spans surah 2 (two occurrences: 2:25:1, 2:25:2) and surah 3 (one: 3:8:1).
        var verbAll = await SurahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190701, VerbScope);
        verbAll!.Surahs.Select(surah => surah.SurahNumber).Should().Equal(2, 3);
        verbAll.Surahs.Select(surah => surah.OccurrencesCount).Should().Equal(2, 1);
        verbAll.MissingSurahs.Select(surah => surah.SurahNumber).Should().Equal(1);

        // tense=past narrows to the single 2:25:1 occurrence, so surah 2 drops to one and surah 3 is missing.
        var past = await SurahsAsync(
            reader, WordTypeGroupedDimensionKind.Root, 190701, new WordTypeFilter("verb", null, null, "past", null));
        past!.Surahs.Select(surah => surah.SurahNumber).Should().Equal(2);
        past.Surahs.Select(surah => surah.OccurrencesCount).Should().Equal(1);
        past.MissingSurahs.Select(surah => surah.SurahNumber).Should().Equal(1, 3);
    }

    // Surahs are single-shot: the controller action exposes no page/pageSize, and the response always
    // carries both the mentioned and missing arrays.
    [Fact]
    public async Task GroupedSurahs_IsSingleShotAndHasNoPagingContract()
    {
        var action = typeof(WordTypeGroupedDetailsController).GetMethod(nameof(WordTypeGroupedDetailsController.GetSurahs));
        action.Should().NotBeNull();
        action!.GetParameters().Select(parameter => parameter.Name!.ToLowerInvariant())
            .Should().NotContain("page").And.NotContain("pagesize");

        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var response = await SurahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope);
        response.Should().NotBeNull();
        response!.Surahs.Should().NotBeNull();
        response.MissingSurahs.Should().NotBeNull();
    }

    // The counts come from one server-side aggregate plus one bounded catalogue read — never a per-
    // occurrence hydration. An absent dimension short-circuits on the aggregate before the catalogue read.
    [Fact]
    public async Task GroupedSurahs_UsesServerAggregateInsteadOfLoadingEveryOccurrence()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfWordTypesReader(dbContext);

        interceptor.Reset();
        var present = await reader.GetGroupedSurahsAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190700, NounScope), CancellationToken.None);
        var presentCommandCount = interceptor.CommandCount;

        interceptor.Reset();
        var absent = await reader.GetGroupedSurahsAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190701, NounScope), CancellationToken.None);
        var absentCommandCount = interceptor.CommandCount;

        present.Should().NotBeNull();
        presentCommandCount.Should().Be(2);

        absent.Should().BeNull();
        absentCommandCount.Should().Be(1);
    }

    private static Task<WordTypeSurahsResponse?> SurahsAsync(
        IWordTypesReader reader,
        WordTypeGroupedDimensionKind kind,
        int dimensionId,
        WordTypeFilter filter) =>
        reader.GetGroupedSurahsAsync(new WordTypeGroupedSelection(kind, dimensionId, filter), CancellationToken.None);
}
