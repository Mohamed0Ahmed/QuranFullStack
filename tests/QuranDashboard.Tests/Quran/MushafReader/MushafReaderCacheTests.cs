using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

namespace QuranDashboard.Tests.Quran.MushafReader;

public sealed class MushafReaderCacheTests
{
    [Fact]
    public async Task CachedMushafPageReader_second_identical_read_is_served_from_cache()
    {
        var inner = new CountingMushafPageReader();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedMushafPageReader(inner, cache);

        var first = await reader.GetPageAsync(5, CancellationToken.None);
        var second = await reader.GetPageAsync(5, CancellationToken.None);

        first.Should().NotBeNull();
        second.Should().BeSameAs(first);
        inner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task CachedMushafPageReader_does_not_cache_missing_page()
    {
        var inner = new CountingMushafPageReader { ReturnNull = true };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedMushafPageReader(inner, cache);

        await reader.GetPageAsync(999, CancellationToken.None);
        await reader.GetPageAsync(999, CancellationToken.None);

        inner.CallCount.Should().Be(2);
        cache.TryGetValue(MushafReaderCacheKeys.Page(999), out _).Should().BeFalse();
    }

    [Fact]
    public async Task CachedAyahStudyReader_second_identical_read_is_served_from_cache()
    {
        var inner = new CountingAyahStudyReader();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedAyahStudyReader(inner, cache);

        var first = await reader.GetAyahStudyAsync("2:25", "ar-muyassar", "en-sahih-international", "muyassar", CancellationToken.None);
        var second = await reader.GetAyahStudyAsync("2:25", "ar-muyassar", "en-sahih-international", "muyassar", CancellationToken.None);

        first.Should().NotBeNull();
        second.Should().BeSameAs(first);
        inner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task CachedWordAnalysisReader_caches_found_only()
    {
        var inner = new CountingWordAnalysisReader();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedWordAnalysisReader(inner, cache);

        var foundFirst = await reader.GetWordAnalysisAsync("2:25:3", CancellationToken.None);
        var foundSecond = await reader.GetWordAnalysisAsync("2:25:3", CancellationToken.None);
        var notFoundFirst = await reader.GetWordAnalysisAsync("9:9:9", CancellationToken.None);
        var notFoundSecond = await reader.GetWordAnalysisAsync("9:9:9", CancellationToken.None);
        var notAnalyzableFirst = await reader.GetWordAnalysisAsync("2:25:1", CancellationToken.None);
        var notAnalyzableSecond = await reader.GetWordAnalysisAsync("2:25:1", CancellationToken.None);

        foundFirst.Should().BeOfType<WordAnalysisOutcome.Found>();
        foundSecond.Should().BeSameAs(foundFirst);
        notFoundFirst.Should().BeOfType<WordAnalysisOutcome.NotFound>();
        notFoundSecond.Should().BeOfType<WordAnalysisOutcome.NotFound>();
        notAnalyzableFirst.Should().BeOfType<WordAnalysisOutcome.NotAnalyzable>();
        notAnalyzableSecond.Should().BeOfType<WordAnalysisOutcome.NotAnalyzable>();

        inner.FoundCallCount.Should().Be(1);
        inner.NotFoundCallCount.Should().Be(2);
        inner.NotAnalyzableCallCount.Should().Be(2);
    }

    private sealed class CountingMushafPageReader : IMushafPageReader
    {
        public int CallCount { get; private set; }

        public bool ReturnNull { get; init; }

        public Task<MushafPageResponse?> GetPageAsync(int pageNumber, CancellationToken ct)
        {
            CallCount++;
            if (ReturnNull)
            {
                return Task.FromResult<MushafPageResponse?>(null);
            }

            return Task.FromResult<MushafPageResponse?>(new MushafPageResponse(
                pageNumber,
                pageNumber > 1 ? pageNumber - 1 : null,
                pageNumber < 604 ? pageNumber + 1 : null,
                [],
                new AyahRange("1:1", "1:1"),
                new PageNavigationSummary([], [], []),
                [],
                []));
        }
    }

    private sealed class CountingAyahStudyReader : IAyahStudyReader
    {
        public int CallCount { get; private set; }

        public Task<AyahStudyResponse?> GetAyahStudyAsync(
            string verseKey,
            string? tafsirSourceKey,
            string? translationSourceKey,
            string? fullI3rabSourceKey,
            CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult<AyahStudyResponse?>(new AyahStudyResponse(
                new AyahCoreDto(verseKey, 2, "البقرة", 25, "نص اختباري", 10, 5, 5, 1, 1, 1, null),
                new SelectedSourcesDto(tafsirSourceKey, translationSourceKey, fullI3rabSourceKey),
                null,
                null,
                null));
        }
    }

    private sealed class CountingWordAnalysisReader : IWordAnalysisReader
    {
        public int FoundCallCount { get; private set; }

        public int NotFoundCallCount { get; private set; }

        public int NotAnalyzableCallCount { get; private set; }

        public Task<WordAnalysisOutcome> GetWordAnalysisAsync(string wordLocation, CancellationToken ct)
        {
            if (wordLocation == "2:25:3")
            {
                FoundCallCount++;
                return Task.FromResult<WordAnalysisOutcome>(new WordAnalysisOutcome.Found(
                    new WordAnalysisResponse(
                        new WordOccurrenceDto(1, wordLocation, "2:25", 2, 25, 3, 5, 1, 1, "كلمة", null, null, null),
                        new WordIdentityDto(
                            new WordCountSummary(1, 1, 1),
                            new WordCountSummary(1, 1, 1),
                            new UniqueWordCountSummary(1, 1, 1, 1),
                            new UniqueSimpleWordCountSummary(1, 1, 1, 1, "key")),
                        new WordMorphologyDto(
                            "noun",
                            new LocalizedLabel("اسم", "noun"),
                            null,
                            null,
                            null,
                            false,
                            null,
                            null,
                            null),
                        [])));
            }

            if (wordLocation == "2:25:1")
            {
                NotAnalyzableCallCount++;
                return Task.FromResult<WordAnalysisOutcome>(new WordAnalysisOutcome.NotAnalyzable());
            }

            NotFoundCallCount++;
            return Task.FromResult<WordAnalysisOutcome>(new WordAnalysisOutcome.NotFound());
        }
    }
}
