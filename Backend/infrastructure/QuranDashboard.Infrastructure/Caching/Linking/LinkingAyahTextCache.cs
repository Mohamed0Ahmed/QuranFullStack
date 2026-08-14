using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed class LinkingAyahTextCache : IDisposable
{
    private const int MergeLockCount = 64;

    private readonly LinkingScalabilityOptions _options;
    private readonly MemoryCache _cache;
    private readonly Lock[] _mergeLocks = CreateMergeLocks();

    public LinkingAyahTextCache(LinkingScalabilityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.AyahTextCacheBudgetReferences,
        });
    }

    public CachedAyahText? Get(int ayahId, long linkingDataRevision) =>
        _cache.TryGetValue(
            LinkingSourceCacheKeys.AyahText(ayahId, linkingDataRevision),
            out CachedAyahText? cached)
            ? cached
            : null;

    public void Store(LinkingResolvedAyahDto ayah, long linkingDataRevision)
    {
        ArgumentNullException.ThrowIfNull(ayah);

        if (Covers(Get(ayah.AyahId, linkingDataRevision), ayah.Words))
        {
            return;
        }

        lock (MergeLockFor(ayah.AyahId))
        {
            var existing = Get(ayah.AyahId, linkingDataRevision);
            if (Covers(existing, ayah.Words))
            {
                return;
            }

            var absoluteExpiration = existing?.AbsoluteExpiration
                ?? DateTimeOffset.UtcNow.Add(_options.CacheAbsoluteExpiration);

            var merged = Merge(existing, ayah, absoluteExpiration);
            var weight = 1L + merged.WordsById.Count;
            if (weight > _options.AyahTextCacheBudgetReferences)
            {
                return;
            }

            _cache.Set(
                LinkingSourceCacheKeys.AyahText(ayah.AyahId, linkingDataRevision),
                merged,
                _options.CacheEntry(weight, absoluteExpiration));
        }
    }

    public IReadOnlyList<LinkingResolvedAyahDto>? TryHydrate(
        IReadOnlyList<LinkingResolvedSourceCompact.CompactAyah> ayahs,
        long linkingDataRevision)
    {
        ArgumentNullException.ThrowIfNull(ayahs);

        var hydrated = new LinkingResolvedAyahDto[ayahs.Count];
        for (var index = 0; index < ayahs.Count; index++)
        {
            var compact = ayahs[index];
            var text = Get(compact.AyahId, linkingDataRevision);
            if (text is null)
            {
                return null;
            }

            var words = new LinkingResolvedWordDto[compact.QuranWordIds.Count];
            for (var wordIndex = 0; wordIndex < compact.QuranWordIds.Count; wordIndex++)
            {
                if (!text.WordsById.TryGetValue(compact.QuranWordIds[wordIndex], out var word))
                {
                    return null;
                }

                words[wordIndex] = word;
            }

            hydrated[index] = new LinkingResolvedAyahDto(
                text.AyahId,
                text.VerseKey,
                text.SurahNumber,
                text.AyahNumber,
                text.SurahNameArabic,
                text.PageFrom,
                text.PageTo,
                compact.MatchedQuranWordIds,
                words);
        }

        return hydrated;
    }

    public void Dispose() => _cache.Dispose();

    private static Lock[] CreateMergeLocks()
    {
        var locks = new Lock[MergeLockCount];

        for (var index = 0; index < locks.Length; index++)
        {
            locks[index] = new Lock();
        }

        return locks;
    }

    private static CachedAyahText Merge(
        CachedAyahText? existing,
        LinkingResolvedAyahDto ayah,
        DateTimeOffset absoluteExpiration)
    {
        var wordsById = new Dictionary<int, LinkingResolvedWordDto>(
            (existing?.WordsById.Count ?? 0) + ayah.Words.Count);

        if (existing is not null)
        {
            foreach (var word in existing.WordsById)
            {
                wordsById[word.Key] = word.Value;
            }
        }

        foreach (var word in ayah.Words)
        {
            wordsById[word.QuranWordId] = word;
        }

        return new CachedAyahText(
            ayah.AyahId,
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.AyahNumber,
            ayah.SurahNameArabic,
            ayah.PageFrom,
            ayah.PageTo,
            absoluteExpiration,
            wordsById);
    }

    private static bool Covers(CachedAyahText? cached, IReadOnlyList<LinkingResolvedWordDto> words)
    {
        if (cached is null)
        {
            return false;
        }

        foreach (var word in words)
        {
            if (!cached.WordsById.ContainsKey(word.QuranWordId))
            {
                return false;
            }
        }

        return true;
    }

    private Lock MergeLockFor(int ayahId) => _mergeLocks[(uint)ayahId % MergeLockCount];

    public sealed record CachedAyahText(
        int AyahId,
        string VerseKey,
        int SurahNumber,
        int AyahNumber,
        string SurahNameArabic,
        short PageFrom,
        short PageTo,
        DateTimeOffset AbsoluteExpiration,
        IReadOnlyDictionary<int, LinkingResolvedWordDto> WordsById);
}
