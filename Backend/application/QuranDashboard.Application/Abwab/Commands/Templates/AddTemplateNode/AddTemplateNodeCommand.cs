namespace QuranDashboard.Application.Abwab.Commands.Templates.AddTemplateNode;

public sealed record AddTemplateNodeCommand(
    int TemplateId,
    int? ParentNodeId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);

public sealed record AddTemplateNodeBody(
    int? ParentNodeId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);
