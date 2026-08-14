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
        long linkingDataRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var sourceIdentity = LinkingSourceIdentity.For(descriptor);
        var key = LinkingSourceCacheKeys.For(descriptor.Kind, sourceIdentity, linkingDataRevision);

        var compact = await _sourceCache.GetOrLoadAsync(
            key,
            sourceIdentity,
            token => _ef.ResolveCompactAsync(descriptor, token),
            cancellationToken);

        return Hydrate(compact, linkingDataRevision)
            ?? await HydrateAndWarmAsync(compact, linkingDataRevision, cancellationToken);
    }

    private async Task<LinkingResolvedSourceDto> HydrateAndWarmAsync(
        LinkingResolvedSourceCompact compact,
        long linkingDataRevision,
        CancellationToken cancellationToken)
    {
        var ayahs = await _ef.HydrateAsync(compact, compact.AyahIds, cancellationToken);
        var resolved = new LinkingResolvedSourceDto(
            compact.SourceIdentity,
            linkingDataRevision,
            DateTimeOffset.UtcNow,
            compact.AyahCount,
            ayahs);

        foreach (var ayah in resolved.Ayahs)
        {
            _ayahTextCache.Store(ayah, linkingDataRevision);
        }

        return resolved;
    }

    private LinkingResolvedSourceDto? Hydrate(
        LinkingResolvedSourceCompact compact,
        long linkingDataRevision)
    {
        var ayahs = _ayahTextCache.TryHydrate(compact.Ayahs, linkingDataRevision);
        if (ayahs is null)
        {
            return null;
        }

        return new LinkingResolvedSourceDto(
            compact.SourceIdentity,
            linkingDataRevision,
            DateTimeOffset.UtcNow,
            ayahs.Count,
            ayahs);
    }
}
