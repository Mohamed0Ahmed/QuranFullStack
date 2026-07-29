using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Queries.GetDoorRelations;

public abstract record GetDoorRelationsOutcome
{
    private GetDoorRelationsOutcome() { }

    public sealed record Success(IReadOnlyList<AbwabDoorRelationDto> Relations) : GetDoorRelationsOutcome;

    public sealed record NotFound : GetDoorRelationsOutcome;
}
