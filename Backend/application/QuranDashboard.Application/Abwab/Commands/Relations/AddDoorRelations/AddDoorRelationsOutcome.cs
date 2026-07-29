using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Relations.AddDoorRelations;

public abstract record AddDoorRelationsOutcome
{
    private AddDoorRelationsOutcome() { }

    public sealed record Success(IReadOnlyList<AbwabDoorRelationDto> Relations) : AddDoorRelationsOutcome;

    public sealed record InvalidRequest : AddDoorRelationsOutcome;

    public sealed record InvalidType : AddDoorRelationsOutcome;

    public sealed record InvalidDirection : AddDoorRelationsOutcome;

    public sealed record SelfRelation : AddDoorRelationsOutcome;

    public sealed record ArchivedDoor : AddDoorRelationsOutcome;

    public sealed record NotFound : AddDoorRelationsOutcome;

    public sealed record Duplicate(IReadOnlyList<string> DoorNames) : AddDoorRelationsOutcome;
}
