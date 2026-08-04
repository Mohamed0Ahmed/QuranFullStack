namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

public sealed record AbwabTemplateDto(
    int Id,
    string Name,
    IReadOnlyList<AbwabTemplateNodeDto> Nodes);
