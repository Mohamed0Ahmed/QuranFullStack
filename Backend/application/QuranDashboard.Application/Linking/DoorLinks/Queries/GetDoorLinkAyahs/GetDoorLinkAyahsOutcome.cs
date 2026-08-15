using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkAyahs;

public abstract record GetDoorLinkAyahsOutcome
{
    private GetDoorLinkAyahsOutcome() { }

    public sealed record Success(DoorLinkAyahsPageDto Page) : GetDoorLinkAyahsOutcome;
    public sealed record InvalidRequest : GetDoorLinkAyahsOutcome;
    public sealed record DoorNotFound : GetDoorLinkAyahsOutcome;
    public sealed record DoorArchived : GetDoorLinkAyahsOutcome;
    public sealed record DoorVersionStale : GetDoorLinkAyahsOutcome;
    public sealed record UnitNotFound : GetDoorLinkAyahsOutcome;
    public sealed record LinkingDataStale : GetDoorLinkAyahsOutcome;
    public sealed record TransientFailure : GetDoorLinkAyahsOutcome;
}
