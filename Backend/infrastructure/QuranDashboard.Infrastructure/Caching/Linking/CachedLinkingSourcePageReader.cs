using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Linking;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed class CachedLinkingSourcePageReader(
    EfLinkingSourceResolutionReader efReader,
    LinkingSourceResolutionCache sourceCache,
    LinkingAyahTextCache ayahTextCache) : ILinkingSourcePageReader
{
    public async Task<LinkingResolvedSourcePageDto> ResolvePageAsync(
        LinkingSourceDescriptor descriptor,
        long linkingDataRevision,
        string sourceViewIdentity,
        LinkingSourcePageView view,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var resolutionIdentity = LinkingSourceIdentity.For(descriptor);
        var key = LinkingSourceCacheKeys.For(descriptor.Kind, resolutionIdentity, linkingDataRevision);
        var compact = await sourceCache.GetOrLoadAsync(
            key,
            resolutionIdentity,
            token => efReader.ResolveCompactAsync(descriptor, token),
            cancellationToken);
        var compactPage = await efReader.ResolveCompactPageAsync(
            compact,
            view,
            page,
            pageSize,
            cancellationToken);
        var total = compactPage.TotalAyahs;
        var totalPages = total == 0 ? 0 : (total + pageSize - 1) / pageSize;
        if (page > Math.Max(1, totalPages))
        {
            throw new LinkingPageOutOfRangeException(page);
        }

        var pageAyahs = compactPage.Ayahs;
        var items = ayahTextCache.TryHydrate(pageAyahs, linkingDataRevision);
        if (items is null)
        {
            items = await efReader.HydrateAsync(
                compact,
                [.. pageAyahs.Select(ayah => ayah.AyahId)],
                cancellationToken);
            foreach (var ayah in items)
            {
                ayahTextCache.Store(ayah, linkingDataRevision);
            }
        }

        return new LinkingResolvedSourcePageDto(
            resolutionIdentity,
            sourceViewIdentity,
            linkingDataRevision,
            total,
            page,
            pageSize,
            totalPages,
            compact.AvailableTypes,
            items);
    }
}
