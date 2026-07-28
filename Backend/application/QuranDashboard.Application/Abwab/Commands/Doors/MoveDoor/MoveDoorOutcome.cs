using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Doors.MoveDoor;

public abstract record MoveDoorOutcome
{
    private MoveDoorOutcome() { }

    public sealed record Success(AbwabDoorDto Door) : MoveDoorOutcome;
    public sealed record NotFound : MoveDoorOutcome;
    public sealed record ParentNotFound : MoveDoorOutcome;
    public sealed record SectionNotFound : MoveDoorOutcome;
    public sealed record WouldCycle : MoveDoorOutcome;
    public sealed record StaleVersion : MoveDoorOutcome;
    public sealed record DuplicateName : MoveDoorOutcome;
}
