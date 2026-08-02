using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Doors.BulkMoveDoors;

public abstract record BulkMoveDoorsOutcome
{
    private BulkMoveDoorsOutcome() { }

    public sealed record Success(IReadOnlyList<AbwabDoorDto> Doors) : BulkMoveDoorsOutcome;
    public sealed record InvalidRequest : BulkMoveDoorsOutcome;
    public sealed record NotFound : BulkMoveDoorsOutcome;
    public sealed record ParentNotFound : BulkMoveDoorsOutcome;
    public sealed record SectionNotFound : BulkMoveDoorsOutcome;
    public sealed record SectionRequired : BulkMoveDoorsOutcome;
    public sealed record WouldCycle : BulkMoveDoorsOutcome;
    public sealed record StaleVersion : BulkMoveDoorsOutcome;
    public sealed record DuplicateName : BulkMoveDoorsOutcome;
}
