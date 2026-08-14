using System.Runtime.CompilerServices;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Linking;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed class CachedLinkingSourcePreparationReader(
    EfLinkingSourceResolutionReader efReader,
    LinkingSourceResolutionCache sourceCache,
    LinkingAyahTextCache ayahTextCache) : ILinkingSourcePreparationReader
{
    public async IAsyncEnumerable<LinkingSourcePreparationBatch> ReadBatchesAsync(
        LinkingSourceDescriptor descriptor,
        long linkingDataRevision,
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var identity = LinkingSourceIdentity.For(descriptor);
        var key = LinkingSourceCacheKeys.For(descriptor.Kind, identity, linkingDataRevision);
        var compact = await sourceCache.GetOrLoadAsync(
            key,
            identity,
            token => efReader.ResolveCompactAsync(descriptor, token),
            cancellationToken);

        for (var offset = 0; offset < compact.AyahCount; offset += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var compactBatch = compact.Ayahs.Skip(offset).Take(batchSize).ToList();
            var ayahs = ayahTextCache.TryHydrate(compactBatch, linkingDataRevision);
            if (ayahs is null)
            {
                ayahs = await efReader.HydrateAsync(
                    compact,
                    [.. compactBatch.Select(ayah => ayah.AyahId)],
                    cancellationToken);
                foreach (var ayah in ayahs)
                {
                    ayahTextCache.Store(ayah, linkingDataRevision);
                }
            }

            yield return new LinkingSourcePreparationBatch(compact.AyahCount, ayahs);
        }

        if (compact.AyahCount == 0)
        {
            yield return new LinkingSourcePreparationBatch(0, []);
        }
    }
}
