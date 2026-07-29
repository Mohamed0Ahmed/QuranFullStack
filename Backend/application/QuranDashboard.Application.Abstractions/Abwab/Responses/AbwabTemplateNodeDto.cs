namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

public sealed record AbwabTemplateNodeDto(
    int Id,
    int? ParentNodeId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string> Aliases,
    int OrderValue);
