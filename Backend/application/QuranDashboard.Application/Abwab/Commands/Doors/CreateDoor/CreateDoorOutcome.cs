using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Doors.CreateDoor;

public abstract record CreateDoorOutcome
{
    private CreateDoorOutcome() { }

    public sealed record Success(AbwabDoorDto Door) : CreateDoorOutcome;
    public sealed record InvalidName : CreateDoorOutcome;
    public sealed record ParentNotFound : CreateDoorOutcome;
    public sealed record SectionNotFound : CreateDoorOutcome;
    public sealed record SectionRequired : CreateDoorOutcome;
    public sealed record SectionParentMismatch : CreateDoorOutcome;
    public sealed record StaleVersion : CreateDoorOutcome;
    public sealed record DuplicateName : CreateDoorOutcome;
}
