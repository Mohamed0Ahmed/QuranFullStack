using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Commands.ReplaceDoorLinkWords;

public abstract record ReplaceDoorLinkWordsOutcome
{
    private ReplaceDoorLinkWordsOutcome() { }

    public sealed record Success(DoorLinkMutationDto Result) : ReplaceDoorLinkWordsOutcome;
    public sealed record InvalidRequest : ReplaceDoorLinkWordsOutcome;
    public sealed record DoorNotFound : ReplaceDoorLinkWordsOutcome;
    public sealed record UnitNotFound : ReplaceDoorLinkWordsOutcome;
    public sealed record DoorArchived : ReplaceDoorLinkWordsOutcome;
    public sealed record DoorVersionStale : ReplaceDoorLinkWordsOutcome;
}
