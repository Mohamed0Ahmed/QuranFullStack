using Microsoft.Extensions.Logging.Abstractions;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

public sealed class WordTypesChildCatalogueDriftTests
{
    [Fact]
    public void Validation_Accepts_EveryNounCategoryCode_InThePosCatalogue()
    {
        var nounCatalogueCodes = PosTagSeed.GetAll()
            .Where(pos => pos.Category == "noun")
            .Select(pos => pos.Code)
            .ToList();

        nounCatalogueCodes.Should().NotBeEmpty();
        nounCatalogueCodes.Should().OnlyContain(
            code => WordTypeScope.Create("noun", code, null, null, null) != null,
            "every noun-category POS code is rendered as a selectable tree child, so noun child-code validation must accept it");
    }

    [Fact]
    public void Validation_Accepts_EveryParticleCategoryCode_ExceptInl()
    {
        var particleCatalogueCodes = PosTagSeed.GetAll()
            .Where(pos => pos.Category == "particle" && pos.Code != "INL")
            .Select(pos => pos.Code)
            .ToList();

        particleCatalogueCodes.Should().NotBeEmpty();
        particleCatalogueCodes.Should().OnlyContain(
            code => WordTypeScope.Create("particle", code, null, null, null) != null,
            "every non-INL particle-category POS code is rendered as a selectable tree child, so particle child-code validation must accept it");
        WordTypeScope.Create("particle", "INL", null, null, null).Should().BeNull();
    }

    [Theory]
    [InlineData("canonical", typeof(object))]
    [InlineData("rows-filter", typeof(WordTypesCatalogueResult.Rows.InvalidFilter))]
    [InlineData("rows-sort", typeof(WordTypesCatalogueResult.Rows.InvalidSort))]
    [InlineData("rows-paging", typeof(WordTypesCatalogueResult.Rows.InvalidPaging))]
    [InlineData("table-filter", typeof(WordTypesCatalogueResult.Table.InvalidFilter))]
    [InlineData("table-view", typeof(WordTypesCatalogueResult.Table.InvalidTableView))]
    [InlineData("table-sort", typeof(WordTypesCatalogueResult.Table.InvalidSort))]
    [InlineData("table-paging", typeof(WordTypesCatalogueResult.Table.InvalidPaging))]
    [InlineData("counts-filter", typeof(WordTypesCatalogueResult.ScopeCounts.InvalidFilter))]
    [InlineData("selected-identity", typeof(WordTypeWordResult.Ayahs.InvalidIdentity))]
    [InlineData("selected-paging", typeof(WordTypeWordResult.Ayahs.InvalidPaging))]
    [InlineData("grouped-kind", typeof(WordTypeGroupedResult.Words.InvalidKind))]
    [InlineData("grouped-id", typeof(WordTypeGroupedResult.Words.InvalidId))]
    [InlineData("grouped-scope", typeof(WordTypeGroupedResult.Words.InvalidFilter))]
    [InlineData("grouped-paging", typeof(WordTypeGroupedResult.Words.InvalidPaging))]
    public async Task CanonicalFactories_AndExplorerValidationPrecedence_AreStable(string scenario, Type expectedType)
    {
        var reader = new UnreachableWordTypesReader();
        var catalogue = new WordTypesCatalogueExplorer(NullLogger<WordTypesCatalogueExplorer>.Instance, reader);
        var word = new WordTypeWordExplorer(NullLogger<WordTypeWordExplorer>.Instance, reader);
        var grouped = new WordTypeGroupedExplorer(NullLogger<WordTypeGroupedExplorer>.Instance, reader);

        object result = scenario switch
        {
            "canonical" => AssertCanonicalFactories(),
            "rows-filter" => await catalogue.GetRowsAsync("bad", null, null, null, null, null, "bad", 0, 1001, null, null, null, default),
            "rows-sort" => await catalogue.GetRowsAsync(null, null, null, null, null, null, "bad", 0, 1001, null, null, null, default),
            "rows-paging" => await catalogue.GetRowsAsync(null, null, null, null, null, null, null, 0, 1001, null, null, null, default),
            "table-filter" => await catalogue.GetTableAsync("bad", "bad", null, null, null, null, null, "bad", 0, 1001, null, null, null, default),
            "table-view" => await catalogue.GetTableAsync("bad", null, null, null, null, null, null, "bad", 0, 1001, null, null, null, default),
            "table-sort" => await catalogue.GetTableAsync(null, null, null, null, null, null, null, "bad", 0, 1001, null, null, null, default),
            "table-paging" => await catalogue.GetTableAsync(null, null, null, null, null, null, null, null, 0, 1001, null, null, null, default),
            "counts-filter" => await catalogue.GetScopeCountsAsync("bad", null, null, null, null, new string('x', 65), null, null, null, default),
            "selected-identity" => await word.GetAyahsAsync(0, " ", "bad", null, null, 0, 101, default),
            "selected-paging" => await word.GetAyahsAsync(1, " N ", "all", "all", "all", 0, 101, default),
            "grouped-kind" => await grouped.GetWordsAsync("root", 0, "bad", null, null, null, null, 0, 101, default),
            "grouped-id" => await grouped.GetWordsAsync("roots", 0, "bad", null, null, null, null, 0, 101, default),
            "grouped-scope" => await grouped.GetWordsAsync("roots", 1, "bad", null, null, null, null, 0, 101, default),
            "grouped-paging" => await grouped.GetWordsAsync("roots", 1, null, null, null, null, null, 0, 101, default),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        };

        result.Should().BeOfType(expectedType);
    }

    private static object AssertCanonicalFactories()
    {
        var scope = WordTypeScope.Create(" VERB ", " present ", " all ", " present ", " passive ");
        scope.Should().NotBeNull();
        scope!.Type.Should().Be("verb");
        scope.ChildCode.Should().Be("present");
        scope.Case.Should().Be("all");
        scope.Tense.Should().Be("present");
        scope.Voice.Should().Be("passive");
        WordTypeScope.Create(null, null, null, null, null)!.Type.Should().Be("noun");
        WordTypeScope.Create("particle", null, "all", "all", "all").Should().NotBeNull();
        WordTypeScope.Create("particle", null, "nominative", null, null).Should().BeNull();

        var filter = WordTypeFilter.Create(null, null, null, null, null, "  كلمة  ", true, false, true);
        filter.Should().NotBeNull();
        filter!.Search.Should().Be("كلمة");
        filter.HasRoot.Should().BeTrue();
        filter.HasStem.Should().BeFalse();
        filter.HasLemma.Should().BeTrue();
        WordTypeFilter.Create(null, null, null, null, null, " ", null, null, null)!.Search.Should().BeNull();
        WordTypeFilter.Create(null, null, null, null, null, new string('x', 65), null, null, null).Should().BeNull();

        WordTypeSortSpec.Create(null)!.CanonicalToken().Should().Be("occurrences");
        WordTypeSortSpec.Create(" AYAHs-ASC ")!.CanonicalToken().Should().Be("ayahs-asc");
        WordTypeSortSpec.Create("mushaf-order-desc").Should().BeNull();
        WordTypeTableView.Create(null)!.Key.Should().Be("words");
        WordTypeTableView.Create(" ROOTS ")!.Key.Should().Be("roots");
        WordTypeTableView.Create("bad").Should().BeNull();

        WordTypeListPaging.Create(1, 1000).Should().NotBeNull();
        WordTypeListPaging.Create(1, 1001).Should().BeNull();
        WordTypeDetailPaging.Create(1, 100).Should().NotBeNull();
        WordTypeDetailPaging.Create(1, 101).Should().BeNull();

        var identity = WordTypeRowIdentity.Create(7, " N ", "all", "present", "passive");
        identity.Should().NotBeNull();
        identity!.ContextCode.Should().Be("N");
        WordTypeRowIdentity.Create(7, " ", null, null, null).Should().BeNull();
        WordTypeRowIdentity.Create(7, "N", "bad", null, null).Should().BeNull();
        WordTypeRowIdentity.Create(7, "N", " all ", null, null).Should().BeNull();

        var kind = WordTypeGroupedDimensionKind.Create(" ROOTS ");
        kind.Should().NotBeNull();
        kind!.RouteKey.Should().Be("roots");
        kind.DtoKind.Should().Be("root");
        WordTypeGroupedDimensionKind.Create("root").Should().BeNull();
        var selection = WordTypeGroupedSelection.Create(kind, 1, scope);
        selection.Should().NotBeNull();
        selection!.DimensionId.Should().Be(1);
        selection.Scope.Should().BeSameAs(scope);
        WordTypeGroupedSelection.Create(kind, 0, scope).Should().BeNull();

        return new object();
    }

    private sealed class UnreachableWordTypesReader : IWordTypesReader
    {
        private static InvalidOperationException Unreachable() => new("Validation should reject before reader dispatch.");

        public Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken) => throw Unreachable();
        public Task<PagedResult<WordTypeRowDto>> GetRowsAsync(WordTypeFilter filter, WordTypeSortSpec sort, WordTypeListPaging paging, CancellationToken cancellationToken) => throw Unreachable();
        public Task<PagedResult<WordTypeTableRowDto>> GetTableRowsAsync(WordTypeFilter filter, WordTypeTableView tableView, WordTypeSortSpec sort, WordTypeListPaging paging, CancellationToken cancellationToken) => throw Unreachable();
        public Task<WordTypeScopeCountsDto> GetScopeCountsAsync(WordTypeFilter filter, CancellationToken cancellationToken) => throw Unreachable();
        public Task<WordTypeSummaryDto?> GetSummaryAsync(WordTypeRowIdentity identity, CancellationToken cancellationToken) => throw Unreachable();
        public Task<PagedResult<WordTypeAyahMatchDto>?> GetAyahMatchesAsync(WordTypeRowIdentity identity, WordTypeDetailPaging paging, CancellationToken cancellationToken) => throw Unreachable();
        public Task<WordTypeSurahsResponse?> GetSurahsAsync(WordTypeRowIdentity identity, CancellationToken cancellationToken) => throw Unreachable();
        public Task<WordTypeGroupedSummaryDto?> GetGroupedSummaryAsync(WordTypeGroupedSelection selection, CancellationToken cancellationToken) => throw Unreachable();
        public Task<PagedResult<WordTypeGroupedMemberWordDto>?> GetGroupedMemberWordsAsync(WordTypeGroupedSelection selection, WordTypeDetailPaging paging, CancellationToken cancellationToken) => throw Unreachable();
        public Task<PagedResult<WordTypeAyahMatchDto>?> GetGroupedAyahMatchesAsync(WordTypeGroupedSelection selection, WordTypeDetailPaging paging, CancellationToken cancellationToken) => throw Unreachable();
        public Task<WordTypeSurahsResponse?> GetGroupedSurahsAsync(WordTypeGroupedSelection selection, CancellationToken cancellationToken) => throw Unreachable();
    }
}
