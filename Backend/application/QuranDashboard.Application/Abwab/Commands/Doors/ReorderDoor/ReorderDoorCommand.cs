using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.ReorderDoor;

public sealed record ReorderDoorCommand(int Id, int Position, AbwabReorderScope Scope, uint Version);
