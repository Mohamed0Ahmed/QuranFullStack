using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed class LinkingAyahTextCache : IDisposable
{
    private const long AyahEntrySize = 1;
    private const int MergeLockCount = 64;

    private readonly LinkingSourceCacheEntryOptions _options;
    private readonly MemoryCache _cache;
    private readonly Lock[] _mergeLocks = CreateMergeLocks();

    public LinkingAyahTextCache(LinkingSourceCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.AyahTextSizeLimitAyahs,
        });
    }

    public CachedAyahText? Get(int ayahId) =>
        _cache.TryGetValue(LinkingSourceCacheKeys.AyahText(ayahId), out CachedAyahText? cached)
            ? cached
            : null;

    public void Store(LinkingResolvedAyahDto ayah)
    {
        ArgumentNullException.ThrowIfNull(ayah);

        if (Covers(Get(ayah.AyahId), ayah.Words))
        {
            return;
        }

        lock (MergeLockFor(ayah.AyahId))
        {
            var existing = Get(ayah.AyahId);
            if (Covers(existing, ayah.Words))
            {
                return;
            }

            var absoluteExpiration = existing?.AbsoluteExpiration
                ?? DateTimeOffset.UtcNow.Add(_options.AbsoluteExpirationRelativeToNow);

            _cache.Set(
                LinkingSourceCacheKeys.AyahText(ayah.AyahId),
                Merge(existing, ayah, absoluteExpiration),
                _options.Entry(AyahEntrySize, absoluteExpiration));
        }
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
