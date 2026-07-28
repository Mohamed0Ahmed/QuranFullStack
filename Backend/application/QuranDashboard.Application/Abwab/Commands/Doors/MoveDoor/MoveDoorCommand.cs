namespace QuranDashboard.Application.Abwab.Commands.Doors.MoveDoor;

public sealed record MoveDoorCommand(int Id, int? TargetSectionId, int? TargetParentId, uint Version);
