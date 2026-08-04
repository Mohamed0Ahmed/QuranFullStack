namespace QuranDashboard.Application.Abwab.Commands.Templates.ApplyTemplate;

public sealed record ApplyTemplateCommand(int TemplateId, IReadOnlyList<int>? TargetDoorIds);

public sealed record ApplyTemplateBody(IReadOnlyList<int>? TargetDoorIds);
