namespace QuranDashboard.Application.Abwab.Commands.Templates.EditTemplateNode;

public sealed record EditTemplateNodeCommand(
    int NodeId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);

public sealed record EditTemplateNodeBody(
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);
