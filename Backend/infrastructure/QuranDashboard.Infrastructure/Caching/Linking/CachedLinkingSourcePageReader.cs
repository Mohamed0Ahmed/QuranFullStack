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
        var compact = await ResolveCompactAsync(
            descriptor,
            resolutionIdentity,
            linkingDataRevision,
            cancellationToken);
        var displayDescriptor = LinkingSourceTypeFilter.Supports(descriptor)
            ? LinkingSourceTypeFilter.Apply(descriptor, view.TypeCodes)
            : descriptor;
        var displayIdentity = LinkingSourceIdentity.For(displayDescriptor);
        var displayCompact = string.Equals(displayIdentity, resolutionIdentity, StringComparison.Ordinal)
            ? compact
            : await ResolveCompactAsync(
                displayDescriptor,
                displayIdentity,
                linkingDataRevision,
                cancellationToken);
        var compactPage = await efReader.ResolveCompactPageAsync(
            displayCompact,
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
                displayCompact,
                [.. pageAyahs.Select(ayah => ayah.AyahId)],
                cancellationToken);
            foreach (var ayah in items)
            {
                ayahTextCache.Store(ayah, linkingDataRevision);
            }
        }

        var linkingMatchesByAyahId = pageAyahs
            .Where(ayah => compact.AyahsById.ContainsKey(ayah.AyahId))
            .ToDictionary(
                ayah => ayah.AyahId,
                ayah => compact.AyahsById[ayah.AyahId].MatchedQuranWordIds);

        return new LinkingResolvedSourcePageDto(
            resolutionIdentity,
            sourceViewIdentity,
            linkingDataRevision,
            total,
            compact.AyahCount,
            page,
            pageSize,
            totalPages,
            compact.AvailableTypes,
            [.. linkingMatchesByAyahId.Keys],
            linkingMatchesByAyahId,
            items);
    }

    private Task<LinkingResolvedSourceCompact> ResolveCompactAsync(
        LinkingSourceDescriptor descriptor,
        string resolutionIdentity,
        long linkingDataRevision,
        CancellationToken cancellationToken) => sourceCache.GetOrLoadAsync(
            LinkingSourceCacheKeys.For(descriptor.Kind, resolutionIdentity, linkingDataRevision),
            resolutionIdentity,
            token => efReader.ResolveCompactAsync(descriptor, token),
            cancellationToken);
}
