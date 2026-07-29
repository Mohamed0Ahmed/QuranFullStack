namespace QuranDashboard.Application.Abwab.Commands.Templates.EditTemplateNode;

public sealed record EditTemplateNodeCommand(
    int NodeId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);

// Editing the ROOT through this body is how a template is renamed — there is no separate rename
// route, since the template's name is its root node's name.
public sealed record EditTemplateNodeBody(
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);
