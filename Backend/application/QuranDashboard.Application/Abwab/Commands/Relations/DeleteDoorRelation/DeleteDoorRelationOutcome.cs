namespace QuranDashboard.Application.Abwab.Commands.Relations.DeleteDoorRelation;

public abstract record DeleteDoorRelationOutcome
{
    private DeleteDoorRelationOutcome() { }

    public sealed record Success : DeleteDoorRelationOutcome;

    public sealed record NotFound : DeleteDoorRelationOutcome;
}
