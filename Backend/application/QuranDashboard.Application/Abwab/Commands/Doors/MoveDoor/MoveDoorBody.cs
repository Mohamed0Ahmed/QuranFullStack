namespace QuranDashboard.Application.Abwab.Commands.Doors.MoveDoor;

public sealed record MoveDoorBody(int? TargetSectionId, int? TargetParentId, uint Version);
