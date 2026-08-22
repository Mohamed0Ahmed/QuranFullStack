using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public interface IMushafDoorHighlightsReader
{
    Task<MushafDoorHighlightsResponse> GetHighlightsAsync(
        int pageNumber,
        IReadOnlyList<int> doorIds,
        CancellationToken ct);
}
