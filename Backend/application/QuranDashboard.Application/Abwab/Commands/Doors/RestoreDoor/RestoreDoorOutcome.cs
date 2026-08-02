using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Doors.RestoreDoor;

public abstract record RestoreDoorOutcome
{
    private RestoreDoorOutcome() { }

    public sealed record Success(AbwabDoorDto Door) : RestoreDoorOutcome;
    public sealed record NotFound : RestoreDoorOutcome;
    public sealed record StaleVersion : RestoreDoorOutcome;
    public sealed record ParentStillArchived : RestoreDoorOutcome;
    public sealed record DuplicateName : RestoreDoorOutcome;
    public sealed record SectionRequired : RestoreDoorOutcome;
    public sealed record SectionNotFound : RestoreDoorOutcome;
    public sealed record SectionParentMismatch : RestoreDoorOutcome;
}
