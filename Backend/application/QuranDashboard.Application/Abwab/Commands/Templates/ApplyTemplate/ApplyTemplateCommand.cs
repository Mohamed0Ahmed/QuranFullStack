namespace QuranDashboard.Application.Abwab.Commands.Templates.ApplyTemplate;

public sealed record ApplyTemplateCommand(int TemplateId, IReadOnlyList<int>? TargetDoorIds);

// There is no "as a root door" option, by design: an empty target list is the only way to express
// it, and that is exactly what the handler refuses.
public sealed record ApplyTemplateBody(IReadOnlyList<int>? TargetDoorIds);
