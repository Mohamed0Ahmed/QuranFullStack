namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

public sealed record AbwabDoorDto(
    int Id,
    int SectionId,
    int? ParentId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    int OrderValue,
    int? GlobalOrderValue,
    uint Version,
    IReadOnlyList<string> Aliases);
