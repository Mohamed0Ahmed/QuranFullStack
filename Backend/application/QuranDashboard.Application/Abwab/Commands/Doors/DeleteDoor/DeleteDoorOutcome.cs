namespace QuranDashboard.Application.Abwab.Commands.Doors.DeleteDoor;

public abstract record DeleteDoorOutcome
{
    private DeleteDoorOutcome() { }

    public sealed record Success : DeleteDoorOutcome;
    public sealed record NotFound : DeleteDoorOutcome;
    public sealed record StaleVersion : DeleteDoorOutcome;
}
