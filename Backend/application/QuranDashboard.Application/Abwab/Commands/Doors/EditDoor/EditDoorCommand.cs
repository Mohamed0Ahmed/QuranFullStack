namespace QuranDashboard.Application.Abwab.Commands.Doors.EditDoor;

public sealed record EditDoorCommand(
    int Id,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases,
    uint Version);
