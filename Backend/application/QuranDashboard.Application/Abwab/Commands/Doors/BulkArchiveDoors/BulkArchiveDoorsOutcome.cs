namespace QuranDashboard.Application.Abwab.Commands.Doors.BulkArchiveDoors;

public abstract record BulkArchiveDoorsOutcome
{
    private BulkArchiveDoorsOutcome() { }

    public sealed record Success(IReadOnlyList<int> ArchivedDoorIds) : BulkArchiveDoorsOutcome;
    public sealed record InvalidRequest : BulkArchiveDoorsOutcome;
    public sealed record NotFound : BulkArchiveDoorsOutcome;
    public sealed record StaleVersion : BulkArchiveDoorsOutcome;
}
