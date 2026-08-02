using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.ReorderDoor;

public sealed record ReorderDoorBody(int Position, AbwabReorderScope Scope, uint Version);
