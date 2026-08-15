using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkRecords;

public abstract record GetDoorLinkRecordsOutcome
{
    private GetDoorLinkRecordsOutcome() { }

    public sealed record Success(DoorLinkRecordsPageDto Page) : GetDoorLinkRecordsOutcome;
    public sealed record InvalidRequest : GetDoorLinkRecordsOutcome;
    public sealed record DoorNotFound : GetDoorLinkRecordsOutcome;
    public sealed record DoorArchived : GetDoorLinkRecordsOutcome;
    public sealed record DoorVersionStale : GetDoorLinkRecordsOutcome;
}
