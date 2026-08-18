using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Commands.DeleteDoorLinks;

public abstract record DeleteDoorLinksOutcome
{
    private DeleteDoorLinksOutcome() { }

    public sealed record Success(DoorLinkMutationDto Result) : DeleteDoorLinksOutcome;
    public sealed record InvalidRequest : DeleteDoorLinksOutcome;
    public sealed record DoorNotFound : DeleteDoorLinksOutcome;
    public sealed record UnitNotFound : DeleteDoorLinksOutcome;
    public sealed record DoorArchived : DeleteDoorLinksOutcome;
    public sealed record DoorVersionStale : DeleteDoorLinksOutcome;
    public sealed record SynchronizationUnavailable : DeleteDoorLinksOutcome;
}
