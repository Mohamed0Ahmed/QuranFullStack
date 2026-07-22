using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeScopeCounts;
using QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesScopeCountsReadTests(WordTypesTestFixture fixture)
{
    public static IEnumerable<object?[]> EqualityScopes()
    {
        yield return ["noun", null, null, null, null, null, null, null, null];
        yield return ["noun", "PN", null, null, null, null, null, null, null];
        yield return ["noun", null, "nominative", null, null, null, null, null, null];
        yield return ["noun", null, "genitive", null, null, null, null, null, null];
        yield return ["verb", null, null, null, null, null, null, null, null];
        yield return ["verb", null, null, "past", null, null, null, null, null];
        yield return ["verb", null, null, "present", null, null, null, null, null];
        yield return ["verb", null, null, null, "active", null, null, null, null];
        yield return ["particle", null, null, null, null, null, null, null, null];
        yield return ["inl", null, null, null, null, null, null, null, null];
        yield return ["noun", null, null, null, null, "كلم", null, null, null];
        yield return ["noun", null, null, null, null, "مثل", null, null, null];
        yield return ["noun", null, null, null, null, null, true, null, null];
        yield return ["noun", null, null, null, null, null, false, null, null];
        yield return ["noun", null, null, null, null, null, null, true, null];
        yield return ["noun", null, null, null, null, null, null, null, false];
        yield return ["noun", null, null, null, null, "كلم", true, null, null];
        yield return ["verb", null, null, "present", null, null, false, null, null];
    }

    [Theory]
    [MemberData(nameof(EqualityScopes))]
    public async Task ScopeCounts_EqualEveryTableViewTotal_ForIdenticalScope(
        string type, string? childCode, string? @case, string? tense, string? voice,
        string? search, bool? hasRoot, bool? hasStem, bool? hasLemma)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();
        var filter = new WordTypeFilter(type, childCode, @case, tense, voice, search, hasRoot, hasStem, hasLemma);

        var counts = await reader.GetScopeCountsAsync(filter, CancellationToken.None);

        var wordsTotal = await TableTotalAsync(reader, filter, WordTypeTableView.Words);
        var rootsTotal = await TableTotalAsync(reader, filter, WordTypeTableView.Roots);
        var stemsTotal = await TableTotalAsync(reader, filter, WordTypeTableView.Stems);
        var lemmasTotal = await TableTotalAsync(reader, filter, WordTypeTableView.Lemmas);

        counts.WordsCount.Should().Be(wordsTotal, "the words count is the words-view TotalCount for the identical scope");
        counts.RootsCount.Should().Be(rootsTotal, "the roots count is the roots-view TotalCount for the identical scope");
        counts.StemsCount.Should().Be(stemsTotal, "the stems count is the stems-view TotalCount for the identical scope");
        counts.LemmasCount.Should().Be(lemmasTotal, "the lemmas count is the lemmas-view TotalCount for the identical scope");
    }

    [Fact]
    public async Task ScopeCounts_UnscopedNoun_HasNonZeroCountsAcrossEveryDimension()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var counts = await reader.GetScopeCountsAsync(
            new WordTypeFilter("noun", null, null, null, null), CancellationToken.None);

        counts.WordsCount.Should().BeGreaterThan(0);
        counts.RootsCount.Should().BeGreaterThan(0);
        counts.StemsCount.Should().BeGreaterThan(0);
        counts.LemmasCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ScopeCounts_ZeroRowValidScope_ReturnsAllZeros()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var counts = await reader.GetScopeCountsAsync(
            new WordTypeFilter("noun", null, null, null, null, "لاتوجدهذهالكلمة"), CancellationToken.None);

        counts.Should().Be(new Application.Abstractions.Quran.Words.WordTypes.Responses.WordTypeScopeCountsDto(0, 0, 0, 0));
    }

    [Fact]
    public async Task ScopeCounts_UsesSingleSqlCommand()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfWordTypesReader(dbContext);

        interceptor.Reset();
        _ = await reader.GetScopeCountsAsync(
            new WordTypeFilter("noun", null, null, null, null, "كلم", HasRoot: true), CancellationToken.None);

        interceptor.CommandCount.Should().Be(1);
    }

    [Fact]
    public async Task ScopeCountsSql_UsesScopedCountFamily_NeverWordsCount()
    {
        var capture = new CommandTextCapture();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(capture)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfWordTypesReader(dbContext);

        _ = await reader.GetScopeCountsAsync(new WordTypeFilter("noun", null, null, null, null), CancellationToken.None);

        var sql = capture.CommandTexts.Should().ContainSingle().Subject;
        sql.Should().NotContainEquivalentOf("words_count");
        sql.ToLowerInvariant().Should().Contain("count(distinct");
    }

    [Fact]
    public void ScopeCountsCacheKey_IsolatesEveryScopeInput_AndKeepsAbsentStable()
    {
        var baseFilter = new WordTypeFilter("noun", null, null, null, null);
        var explicitAbsent = new WordTypeFilter("noun", null, null, null, null, null, null, null, null);

        var baseKey = WordTypesCacheKeys.ScopeCounts(baseFilter);
        baseKey.Should().StartWith("wordtypes:scope-counts:");
        WordTypesCacheKeys.ScopeCounts(explicitAbsent).Should().Be(baseKey);

        WordTypeFilter[] scopes =
        [
            baseFilter,
            baseFilter with { Type = "verb" },
            baseFilter with { ChildCode = "PN" },
            baseFilter with { Case = "genitive" },
            baseFilter with { Tense = "past" },
            baseFilter with { Voice = "active" },
            baseFilter with { Search = "كلم" },
            baseFilter with { HasRoot = true },
            baseFilter with { HasStem = false },
            baseFilter with { HasLemma = true },
        ];

        scopes.Select(WordTypesCacheKeys.ScopeCounts).Should().OnlyHaveUniqueItems();

        WordTypesCacheKeys.ScopeCounts(baseFilter with { Search = "كَلِم" })
            .Should().Be(WordTypesCacheKeys.ScopeCounts(baseFilter with { Search = "كلم" }));
        WordTypesCacheKeys.ScopeCounts(baseFilter with { Search = "كلم" }).Should().NotContain("كلم");
    }

    [Fact]
    public async Task CachedScopeCounts_RepeatedRead_HitsCache()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedWordTypesReader(new EfWordTypesReader(dbContext), cache);
        var filter = new WordTypeFilter("noun", null, null, null, null);

        interceptor.Reset();
        var first = await reader.GetScopeCountsAsync(filter, CancellationToken.None);
        var firstCommandCount = interceptor.CommandCount;
        var second = await reader.GetScopeCountsAsync(filter, CancellationToken.None);

        second.Should().BeSameAs(first);
        interceptor.CommandCount.Should().Be(firstCommandCount);
        firstCommandCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ScopeCountsHandler_InvalidFilter_ReturnsControlledOutcome()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeScopeCountsHandler>();

        var unknownType = await handler.HandleAsync(
            new GetWordTypeScopeCountsQuery("bogus", null, null, null, null, null), CancellationToken.None);
        var overLongSearch = await handler.HandleAsync(
            new GetWordTypeScopeCountsQuery("noun", null, null, null, null, new string('ا', 65)), CancellationToken.None);

        unknownType.Should().BeOfType<GetWordTypeScopeCountsOutcome.InvalidFilter>();
        overLongSearch.Should().BeOfType<GetWordTypeScopeCountsOutcome.InvalidFilter>();
    }

    [Fact]
    public async Task ScopeCountsHandler_ValidScope_ReturnsSuccessMatchingReader()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeScopeCountsHandler>();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeScopeCountsQuery("noun", null, null, null, null, null), CancellationToken.None);

        var success = outcome.Should().BeOfType<GetWordTypeScopeCountsOutcome.Success>().Subject;
        var expected = await reader.GetScopeCountsAsync(new WordTypeFilter("noun", null, null, null, null), CancellationToken.None);
        success.Counts.Should().Be(expected);
    }

    private static async Task<int> TableTotalAsync(EfWordTypesReader reader, WordTypeFilter filter, WordTypeTableView view)
    {
        var page = await reader.GetTableRowsAsync(filter, view, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        return page.TotalCount;
    }

    private sealed class CommandTextCapture : DbCommandInterceptor
    {
        private readonly List<string> _commandTexts = [];

        public IReadOnlyList<string> CommandTexts => _commandTexts;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            _commandTexts.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            _commandTexts.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
