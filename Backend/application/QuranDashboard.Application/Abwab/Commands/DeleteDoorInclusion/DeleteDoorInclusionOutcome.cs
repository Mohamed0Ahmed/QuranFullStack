using QuranDashboard.Application.Abstractions.Abwab.Inclusions;

namespace QuranDashboard.Application.Abwab.Commands.DeleteDoorInclusion;

public abstract record DeleteDoorInclusionOutcome
{
    private DeleteDoorInclusionOutcome() { }

    public sealed record Success(AbwabDoorInclusionDetachResultDto Result)
        : DeleteDoorInclusionOutcome;

    public sealed record InvalidRequest : DeleteDoorInclusionOutcome;

    public sealed record NotFound : DeleteDoorInclusionOutcome;

    public sealed record ArchivedTarget : DeleteDoorInclusionOutcome;

    public sealed record StaleTargetVersion : DeleteDoorInclusionOutcome;

    public sealed record SynchronizationUnavailable : DeleteDoorInclusionOutcome;
}
