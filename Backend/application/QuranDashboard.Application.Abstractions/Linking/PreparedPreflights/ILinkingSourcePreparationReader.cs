using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

public interface ILinkingSourcePreparationReader
{
    IAsyncEnumerable<LinkingSourcePreparationBatch> ReadBatchesAsync(
        LinkingSourceDescriptor descriptor,
        long linkingDataRevision,
        int batchSize,
        CancellationToken cancellationToken);
}

public sealed record LinkingSourcePreparationBatch(
    int TotalAyahCount,
    IReadOnlyList<LinkingResolvedAyahDto> Ayahs);
