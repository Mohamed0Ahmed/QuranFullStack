using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkSnapshot;

public abstract record GetDoorLinkSnapshotOutcome
{
    private GetDoorLinkSnapshotOutcome() { }

    public sealed record Success(DoorLinkSnapshotDto Snapshot) : GetDoorLinkSnapshotOutcome;
    public sealed record InvalidRequest : GetDoorLinkSnapshotOutcome;
    public sealed record DoorNotFound : GetDoorLinkSnapshotOutcome;
    public sealed record DoorArchived : GetDoorLinkSnapshotOutcome;
    public sealed record TransientFailure : GetDoorLinkSnapshotOutcome;
}
