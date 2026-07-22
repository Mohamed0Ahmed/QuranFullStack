using QuranDashboard.Api.Controllers.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesGroupedSurahsReadTests(WordTypesTestFixture fixture)
{
    private static readonly WordTypeFilter NounScope = new("noun", null, null, null, null);
    private static readonly WordTypeFilter VerbScope = new("verb", null, null, null, null);

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

    [Fact]
    public async Task GroupedSurahs_ActiveFiltersNarrowOccurrenceCounts()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var verbAll = await SurahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190701, VerbScope);
        verbAll!.Surahs.Select(surah => surah.SurahNumber).Should().Equal(2, 3);
        verbAll.Surahs.Select(surah => surah.OccurrencesCount).Should().Equal(2, 1);
        verbAll.MissingSurahs.Select(surah => surah.SurahNumber).Should().Equal(1);

        var past = await SurahsAsync(
            reader, WordTypeGroupedDimensionKind.Root, 190701, new WordTypeFilter("verb", null, null, "past", null));
        past!.Surahs.Select(surah => surah.SurahNumber).Should().Equal(2);
        past.Surahs.Select(surah => surah.OccurrencesCount).Should().Equal(1);
        past.MissingSurahs.Select(surah => surah.SurahNumber).Should().Equal(1, 3);
    }

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
