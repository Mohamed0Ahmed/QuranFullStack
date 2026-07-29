namespace QuranDashboard.Application.Abwab.Commands.Templates.CreateTemplate;

public sealed record CreateTemplateCommand(
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);

// The name is the ROOT NODE's name — AbwabTemplate has no name column.
public sealed record CreateTemplateBody(
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);
