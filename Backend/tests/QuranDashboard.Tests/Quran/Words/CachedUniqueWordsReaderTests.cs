using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.Words;

namespace QuranDashboard.Tests.Quran.Words;

public sealed class CachedUniqueWordsReaderTests
{
    [Fact]
    public async Task GetUniqueWordsPageAsync_caches_repeated_no_search_read()
    {
        using var cache = CreateCache();
        var inner = new FakeUniqueWordsReader();
        var reader = new CachedUniqueWordsReader(inner, cache);

        var first = await reader.GetUniqueWordsPageAsync(
            UniqueWordKind.Tashkeel,
            null,
            UniqueWordSortSpec.Natural(UniqueWordSortColumn.MushafOrder),
            UniqueWordsCountFilter.None,
            UniqueWordsAssociationFilter.None,
            page: 1,
            pageSize: 50,
            CancellationToken.None);
        var second = await reader.GetUniqueWordsPageAsync(
            UniqueWordKind.Tashkeel,
            null,
            UniqueWordSortSpec.Natural(UniqueWordSortColumn.MushafOrder),
            UniqueWordsCountFilter.None,
            UniqueWordsAssociationFilter.None,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        inner.PageCalls.Should().Be(1);
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task GetUniqueWordsPageAsync_bypasses_cache_when_search_is_present()
    {
        using var cache = CreateCache();
        var inner = new FakeUniqueWordsReader();
        var reader = new CachedUniqueWordsReader(inner, cache);

        await reader.GetUniqueWordsPageAsync(
            UniqueWordKind.Simple,
            "synthetic-search",
            UniqueWordSortSpec.Natural(UniqueWordSortColumn.Alpha),
            UniqueWordsCountFilter.None,
            UniqueWordsAssociationFilter.None,
            page: 1,
            pageSize: 50,
            CancellationToken.None);
        await reader.GetUniqueWordsPageAsync(
            UniqueWordKind.Simple,
            "synthetic-search",
            UniqueWordSortSpec.Natural(UniqueWordSortColumn.Alpha),
            UniqueWordsCountFilter.None,
            UniqueWordsAssociationFilter.None,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        inner.PageCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetUniqueWordsPageAsync_alias_and_canonical_sort_token_share_one_cache_entry()
    {
        using var cache = CreateCache();
        var inner = new FakeUniqueWordsReader();
        var reader = new CachedUniqueWordsReader(inner, cache);

        UniqueWordSortParser.TryParse("occurrences", out var canonical).Should().BeTrue();
        UniqueWordSortParser.TryParse("occurrences-desc", out var alias).Should().BeTrue();

        var first = await ReadPageAsync(reader, canonical);
        var second = await ReadPageAsync(reader, alias);

        inner.PageCalls.Should().Be(1, "the alias names the same ordering as the canonical token");
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task GetUniqueWordsPageAsync_opposite_directions_of_one_column_never_cross_serve()
    {
        using var cache = CreateCache();
        var inner = new FakeUniqueWordsReader();
        var reader = new CachedUniqueWordsReader(inner, cache);

        UniqueWordSortParser.TryParse("occurrences", out var descending).Should().BeTrue();
        UniqueWordSortParser.TryParse("occurrences-asc", out var ascending).Should().BeTrue();

        var first = await ReadPageAsync(reader, descending);
        var second = await ReadPageAsync(reader, ascending);

        inner.PageCalls.Should().Be(2, "opposite directions are two distinct orderings");
        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void ListCacheKeys_are_distinct_per_canonical_token_and_carry_it_verbatim()
    {
        var specs = Enum.GetValues<UniqueWordSortColumn>()
            .SelectMany(column => Enum.GetValues<WordSortDirection>()
                .Select(direction => new UniqueWordSortSpec(column, direction)))
            .Where(spec => spec.Column != UniqueWordSortColumn.MushafOrder
                || spec.Direction == WordSortDirection.Ascending)
            .ToArray();

        specs.Select(spec => spec.CanonicalToken()).Should().OnlyHaveUniqueItems();

        var keysByToken = specs.ToDictionary(
            spec => spec.CanonicalToken(),
            spec => UniqueWordsCacheKeys.List(UniqueWordKind.Tashkeel, spec, 1, 50));

        keysByToken.Values.Should().OnlyHaveUniqueItems();
        foreach (var (token, key) in keysByToken)
        {
            key.Should().Be($"words:tashkeel:list:{token}:p1:s50");
        }
    }

    private static Task<PagedResult<UniqueWordListItemDto>> ReadPageAsync(
        CachedUniqueWordsReader reader,
        UniqueWordSortSpec sort) =>
        reader.GetUniqueWordsPageAsync(
            UniqueWordKind.Tashkeel,
            null,
            sort,
            UniqueWordsCountFilter.None,
            UniqueWordsAssociationFilter.None,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

    [Fact]
    public async Task GetUniqueWordSummaryAsync_does_not_cache_null_result()
    {
        using var cache = CreateCache();
        var inner = new FakeUniqueWordsReader { SummaryResult = null };
        var reader = new CachedUniqueWordsReader(inner, cache);

        var first = await reader.GetUniqueWordSummaryAsync(UniqueWordKind.Tashkeel, id: 404, CancellationToken.None);
        var second = await reader.GetUniqueWordSummaryAsync(UniqueWordKind.Tashkeel, id: 404, CancellationToken.None);

        first.Should().BeNull();
        second.Should().BeNull();
        inner.SummaryCalls.Should().Be(2);
    }

    private static MemoryCache CreateCache() => new(new MemoryCacheOptions());

    private sealed class FakeUniqueWordsReader : IUniqueWordsReader
    {
        public int PageCalls { get; private set; }
        public int SummaryCalls { get; private set; }

    public UniqueWordSummaryDto? SummaryResult { get; init; } = new(
        1,
        UniqueWordKindKeys.Tashkeel,
        "synthetic-display",
        1,
        1,
        1,
        113);

        public Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
            UniqueWordKind kind,
            string? search,
            UniqueWordSortSpec sort,
            UniqueWordsCountFilter filter,
            UniqueWordsAssociationFilter association,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            PageCalls++;
            return Task.FromResult(new PagedResult<UniqueWordListItemDto>(page, pageSize, 0, []));
        }

        public Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
            UniqueWordKind kind,
            int id,
            CancellationToken cancellationToken)
        {
            SummaryCalls++;
            return Task.FromResult(SummaryResult);
        }

        public Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
            UniqueWordKind kind,
            int id,
            CancellationToken cancellationToken) =>
            Task.FromResult<UniqueWordSurahsResponse?>(null);

        public Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
            UniqueWordKind kind,
            int id,
            CancellationToken cancellationToken) =>
            Task.FromResult<UniqueWordMissingSurahsResponse?>(null);

        public Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
            UniqueWordKind kind,
            int id,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult<PagedResult<UniqueWordAyahMatchDto>?>(null);
    }
}
