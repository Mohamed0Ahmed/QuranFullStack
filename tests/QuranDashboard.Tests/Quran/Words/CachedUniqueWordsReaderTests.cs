using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Common.Paging;
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
            UniqueWordSort.MushafOrder,
            page: 1,
            pageSize: 50,
            CancellationToken.None);
        var second = await reader.GetUniqueWordsPageAsync(
            UniqueWordKind.Tashkeel,
            null,
            UniqueWordSort.MushafOrder,
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
            UniqueWordSort.Alpha,
            page: 1,
            pageSize: 50,
            CancellationToken.None);
        await reader.GetUniqueWordsPageAsync(
            UniqueWordKind.Simple,
            "synthetic-search",
            UniqueWordSort.Alpha,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        inner.PageCalls.Should().Be(2);
    }

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
            UniqueWordSort sort,
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
