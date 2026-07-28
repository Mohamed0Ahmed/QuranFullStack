using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Doors.EditDoor;

public abstract record EditDoorOutcome
{
    private EditDoorOutcome() { }

    public sealed record Success(AbwabDoorDto Door) : EditDoorOutcome;
    public sealed record InvalidName : EditDoorOutcome;
    public sealed record NotFound : EditDoorOutcome;
    public sealed record StaleVersion : EditDoorOutcome;
    public sealed record DuplicateName : EditDoorOutcome;
}
