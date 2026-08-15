using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkRecords;

public sealed class GetDoorLinkRecordsHandler(
    IDoorLinkRecordsReader reader,
    ILinkingScalabilityPolicy policy)
{
    public async Task<GetDoorLinkRecordsOutcome> HandleAsync(
        GetDoorLinkRecordsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.DoorId <= 0
            || query.Page <= 0
            || query.PageSize <= 0
            || query.PageSize > policy.PageSizeMaximum
            || query.ExpectedDoorVersion is 0
            || (query.Page > 1 && query.ExpectedDoorVersion is null))
        {
            return new GetDoorLinkRecordsOutcome.InvalidRequest();
        }

        var result = await reader.ReadRecordsAsync(
            query.DoorId,
            query.ExpectedDoorVersion,
            query.Page,
            query.PageSize,
            cancellationToken);

        return result switch
        {
            DoorLinkRecordsReadResult.Success success =>
                new GetDoorLinkRecordsOutcome.Success(success.Page),
            DoorLinkRecordsReadResult.DoorNotFound =>
                new GetDoorLinkRecordsOutcome.DoorNotFound(),
            DoorLinkRecordsReadResult.DoorArchived =>
                new GetDoorLinkRecordsOutcome.DoorArchived(),
            DoorLinkRecordsReadResult.DoorVersionStale =>
                new GetDoorLinkRecordsOutcome.DoorVersionStale(),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(DoorLinkRecordsReadResult)} variant."),
        };
    }
}
