using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.BulkArchiveDoors;

// Doors is nullable-element on purpose: a JSON body like {"doors":[null]} deserializes cleanly (STJ
// does not reject null array elements for a reference-type item), so the handler must check for a
// null element itself rather than trusting the NRT annotation to have stopped it at the boundary.
public sealed record BulkArchiveDoorsCommand(IReadOnlyList<AbwabBulkDoorRef?> Doors);
