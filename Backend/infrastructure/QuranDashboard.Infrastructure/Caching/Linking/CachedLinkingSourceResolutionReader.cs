using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Linking;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed class CachedLinkingSourceResolutionReader(
    EfLinkingSourceResolutionReader efReader,
    LinkingSourceResolutionCache sourceCache,
    LinkingAyahTextCache ayahTextCache) : ILinkingSourceResolutionReader
{
    private readonly EfLinkingSourceResolutionReader _ef = efReader;
    private readonly LinkingSourceResolutionCache _sourceCache = sourceCache;
    private readonly LinkingAyahTextCache _ayahTextCache = ayahTextCache;

    public async Task<LinkingResolvedSourceDto> ResolveAsync(
        LinkingSourceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var sourceIdentity = LinkingSourceIdentity.For(descriptor);
        var key = LinkingSourceCacheKeys.For(descriptor.Kind, sourceIdentity);

        LinkingResolvedSourceDto? loaded = null;

        var compact = await _sourceCache.GetOrLoadAsync(
            key,
            sourceIdentity,
            async token =>
            {
                loaded = await ResolveAndWarmAsync(descriptor, token);
                return LinkingResolvedSourceCompact.From(loaded);
            },
            cancellationToken);

        if (loaded is not null)
        {
            return loaded;
        }

        return Hydrate(compact) ?? await ResolveAndWarmAsync(descriptor, cancellationToken);
    }

    private async Task<LinkingResolvedSourceDto> ResolveAndWarmAsync(
        LinkingSourceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        var resolved = await _ef.ResolveAsync(descriptor, cancellationToken);

        foreach (var ayah in resolved.Ayahs)
        {
            _ayahTextCache.Store(ayah);
        }

        return resolved;
    }

    private LinkingResolvedSourceDto? Hydrate(LinkingResolvedSourceCompact compact)
    {
        var texts = new LinkingAyahTextCache.CachedAyahText[compact.AyahIds.Count];

        for (var index = 0; index < compact.AyahIds.Count; index++)
        {
            var text = _ayahTextCache.Get(compact.AyahIds[index]);
            if (text is null)
            {
                return null;
            }

            texts[index] = text;
        }

        var ayahs = new LinkingResolvedAyahDto[compact.Ayahs.Count];

        for (var index = 0; index < compact.Ayahs.Count; index++)
        {
            var compactAyah = compact.Ayahs[index];
            var text = texts[index];
            var words = new LinkingResolvedWordDto[compactAyah.QuranWordIds.Count];

            for (var wordIndex = 0; wordIndex < compactAyah.QuranWordIds.Count; wordIndex++)
            {
                if (!text.WordsById.TryGetValue(compactAyah.QuranWordIds[wordIndex], out var word))
                {
                    return null;
                }

                words[wordIndex] = word;
            }

            ayahs[index] = new LinkingResolvedAyahDto(
                text.AyahId,
                text.VerseKey,
                text.SurahNumber,
                text.AyahNumber,
                text.SurahNameArabic,
                text.PageFrom,
                text.PageTo,
                compactAyah.MatchedQuranWordIds,
                words);
        }

        return new LinkingResolvedSourceDto(
            compact.SourceIdentity,
            DateTimeOffset.UtcNow,
            ayahs.Length,
            ayahs);
    }
}
