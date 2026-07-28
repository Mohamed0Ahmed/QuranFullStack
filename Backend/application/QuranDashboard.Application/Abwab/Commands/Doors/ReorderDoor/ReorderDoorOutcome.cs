using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Doors.ReorderDoor;

public abstract record ReorderDoorOutcome
{
    private ReorderDoorOutcome() { }

    public sealed record Success(AbwabDoorDto Door) : ReorderDoorOutcome;
    public sealed record NotFound : ReorderDoorOutcome;
    public sealed record InvalidPosition : ReorderDoorOutcome;
    public sealed record StaleVersion : ReorderDoorOutcome;
}
