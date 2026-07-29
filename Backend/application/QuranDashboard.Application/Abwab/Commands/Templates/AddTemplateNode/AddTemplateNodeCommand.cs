namespace QuranDashboard.Application.Abwab.Commands.Templates.AddTemplateNode;

public sealed record AddTemplateNodeCommand(
    int TemplateId,
    int? ParentNodeId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);

// ParentNodeId is nullable on the WIRE only so an omitted one is refused with 400 rather than bound
// to 0 — a template never gains a second root.
public sealed record AddTemplateNodeBody(
    int? ParentNodeId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    IReadOnlyList<string>? Aliases);
