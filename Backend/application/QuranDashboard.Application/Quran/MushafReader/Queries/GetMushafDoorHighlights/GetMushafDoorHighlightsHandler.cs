using QuranDashboard.Application.Abstractions.Quran.MushafReader;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafDoorHighlights;

public sealed class GetMushafDoorHighlightsHandler(IMushafDoorHighlightsReader highlightsReader)
{
    private const int MinPageNumber = 1;
    private const int MaxPageNumber = 604;

    public async Task<GetMushafDoorHighlightsOutcome> HandleAsync(
        GetMushafDoorHighlightsQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.DoorIds);

        if (query.PageNumber < MinPageNumber || query.PageNumber > MaxPageNumber)
        {
            return new GetMushafDoorHighlightsOutcome.InvalidPageNumber();
        }

        if (query.DoorIds.Any(doorId => doorId <= 0))
        {
            return new GetMushafDoorHighlightsOutcome.InvalidDoorIds();
        }

        var normalizedDoorIds = query.DoorIds.Distinct().ToList();
        var response = await highlightsReader.GetHighlightsAsync(
            query.PageNumber,
            normalizedDoorIds,
            ct);

        return new GetMushafDoorHighlightsOutcome.Success(response);
    }
}
