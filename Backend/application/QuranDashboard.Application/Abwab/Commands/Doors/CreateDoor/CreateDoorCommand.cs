namespace QuranDashboard.Application.Abwab.Commands.Doors.CreateDoor;

public sealed record CreateDoorCommand(
    int? SectionId,
    int? ParentId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);
