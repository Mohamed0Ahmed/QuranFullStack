namespace QuranDashboard.Application.Abwab.Commands.Doors.RestoreDoor;

// SectionId is the restore destination and stays nullable: omitting it means "wherever this door came
// from", which is the ordinary case. A root whose section was retired meanwhile has no such place, and
// is refused rather than restored somewhere nobody chose.
public sealed record RestoreDoorBody(int? SectionId, uint Version);
