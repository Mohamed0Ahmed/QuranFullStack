using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.BulkMoveDoors;

public sealed record BulkMoveDoorsCommand(
    IReadOnlyList<AbwabBulkDoorRef?> Doors,
    int? TargetSectionId,
    int? TargetParentId);
