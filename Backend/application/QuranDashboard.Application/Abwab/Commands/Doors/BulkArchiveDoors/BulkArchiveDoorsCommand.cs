using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.BulkArchiveDoors;

public sealed record BulkArchiveDoorsCommand(IReadOnlyList<AbwabBulkDoorRef?> Doors);
