using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.AddDoorInclusions;

public abstract record AddDoorInclusionsOutcome
{
    private AddDoorInclusionsOutcome() { }

    public sealed record Success(AbwabDoorInclusionAddResultDto Result) : AddDoorInclusionsOutcome;

    public sealed record InvalidRequest : AddDoorInclusionsOutcome;

    public sealed record NotFound : AddDoorInclusionsOutcome;

    public sealed record ArchivedDoor : AddDoorInclusionsOutcome;

    public sealed record Duplicate : AddDoorInclusionsOutcome;

    public sealed record Cycle : AddDoorInclusionsOutcome;

    public sealed record StaleTargetVersion : AddDoorInclusionsOutcome;

    public sealed record SynchronizationUnavailable : AddDoorInclusionsOutcome;
}
