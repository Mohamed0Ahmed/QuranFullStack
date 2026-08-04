namespace QuranDashboard.Application.Abwab.Commands.Templates.CreateTemplate;

public sealed record CreateTemplateCommand(
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);

public sealed record CreateTemplateBody(
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);
