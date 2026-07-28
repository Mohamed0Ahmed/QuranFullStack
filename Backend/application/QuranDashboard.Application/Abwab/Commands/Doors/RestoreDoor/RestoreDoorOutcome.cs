using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Doors.RestoreDoor;

public abstract record RestoreDoorOutcome
{
    private RestoreDoorOutcome() { }

    public sealed record Success(AbwabDoorDto Door, bool DetachedFromArchivedSection) : RestoreDoorOutcome;
    public sealed record NotFound : RestoreDoorOutcome;
    public sealed record StaleVersion : RestoreDoorOutcome;
    public sealed record ParentStillArchived : RestoreDoorOutcome;
    public sealed record DuplicateName : RestoreDoorOutcome;
}
